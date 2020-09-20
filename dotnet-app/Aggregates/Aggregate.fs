module DotnetApp.Aggregate.ES

open System
open EventStore.ClientAPI
open EventStore.ClientAPI.Projections
open System.Text.Json
open System.Text
open System.Text.Json.Serialization
open System.Net
open System.Net.Http
open System.Net.Security
open DotnetApp.Configuration

type IdStreamName = {
    StreamPrefix : string
    Id : Guid
}
with
    override this.ToString () : string =
        let streamName = sprintf "%s-%s" this.StreamPrefix (this.Id.ToString())
        streamName

type Aggregate<'state, 'command, 'event> = {
    Init: 'state
    Apply: 'state -> 'event -> 'state
    Execute: 'state -> 'command -> Result<'event list, string>
    StreamPrefix: string
}

type EventMetadata = {
    ServerEventEmitTime : DateTime
}

type Command =
| NodeCommand

let options = JsonSerializerOptions()
options.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.AdjacentTag ||| JsonUnionEncoding.NamedFields))

let rec foldEvents (aggregate : Aggregate<'state, 'command, 'event>, stream : string, store : IEventStoreConnection, state : 'state, lastEvent : int64, isEndOfStream : bool) : 'state =
    if isEndOfStream then
        state
    else
        let slice = 
            store.ReadStreamEventsForwardAsync(stream, (lastEvent + 1L), 200, true)
            |> Async.AwaitTask        
            |> Async.RunSynchronously
        let nextState = 
            slice.Events
            |> Seq.map (fun x -> 
                let data = Encoding.UTF8.GetString(x.Event.Data)
                JsonSerializer.Deserialize(data, options))
            |> Seq.fold aggregate.Apply state
        foldEvents (aggregate, stream, store, nextState, slice.LastEventNumber, slice.IsEndOfStream)

let write (streamName : string, conn : IEventStoreConnection, event : 'event) =
    let eventType = event.GetType().ToString()
    let data = JsonSerializer.Serialize(event, options)
    let metadata = JsonSerializer.Serialize(({EventMetadata.ServerEventEmitTime = DateTime.UtcNow}), options)
    let eventPayload = [| EventData(Guid.NewGuid(), eventType, true, Encoding.UTF8.GetBytes(data), Encoding.UTF8.GetBytes(metadata)) |]
    let ev = int64 ExpectedVersion.Any
    
    conn.AppendToStreamAsync(streamName, ev, eventPayload).Wait()

type IEventStoreService =
    interface
        abstract member Connection : IEventStoreConnection
        abstract member GetConnection : unit -> IEventStoreConnection
        abstract member Write : string * 'event seq -> unit
        abstract member GetCurrentStreamState : Aggregate<'state, 'command, 'event> * IdStreamName -> 'state
    end

type EventStoreService(esConfig : ESConfiguration) =
    interface IEventStoreService with
        member this.Connection =
            // TODO look at bug: https://github.com/EventStore/EventStore/issues/2547
            // Need to disable tls for workaround (version 5 only?)
            let settings = ConnectionSettings.Create().KeepReconnecting().UseConsoleLogger().EnableVerboseLogging()
            let vConf = esConfig.ValidatedConfig
            let ep = vConf.EndPoint
            let settings = if ep.Ssl then settings else settings.DisableTls()
            let connectionString = sprintf "ConnectTo=tcp://%s:%s@%s:%d" vConf.Username vConf.Password ep.Url ep.Port
            EventStoreConnection.Create(connectionString, settings, "dotnet-app")

        member this.GetConnection () : IEventStoreConnection = 
            let cThis = this :> IEventStoreService
            let conn = cThis.Connection
            conn.ConnectAsync().Wait()
            conn

        member this.Write (streamName : string, events : 'event seq) =
            let cThis = this :> IEventStoreService
            use conn = cThis.GetConnection()

            events
            |> Seq.iter (fun evt -> write(streamName, conn, evt))


        member this.GetCurrentStreamState (aggregate : Aggregate<'state, 'command, 'event>, streamName : IdStreamName) : 'state =
            let cThis = this :> IEventStoreService
            use conn = cThis.GetConnection()

            foldEvents (aggregate, (streamName.ToString()), conn, aggregate.Init, -1L, false)

type RawEventStoreProjection = {
    Name : string
    Code : string
}

type IEventStoreProjection<'ProjectionState> =
    interface
        abstract member Projection : RawEventStoreProjection
    end

type IEventStoreProjectorService =
    interface
        abstract member GetProjectionManager : unit -> ProjectionsManager
        abstract member AddProjection : IEventStoreProjection<'ProjectionState> -> unit
        abstract member GetProjectionState : IEventStoreProjection<'ProjectionState> -> Result<'ProjectionState, string>
    end

type EventStoreProjectorService(esService : IEventStoreService, esConfig : ESConfiguration) =
    let vConf = esConfig.ValidatedConfig
    let pep = vConf.ProjectionsEndPoint
    let connectionEndPoint =
        match IPAddress.TryParse pep.Url with
        | true, ip ->
            IPEndPoint(ip, pep.Port) :> EndPoint
        | _ ->
            DnsEndPoint(pep.Url, pep.Port) :> EndPoint

    let log = esService.Connection.Settings.Log
    let user = esService.Connection.Settings.DefaultUserCredentials
    
    interface IEventStoreProjectorService with
        
        member this.GetProjectionManager () =
            if pep.Ssl then
                ProjectionsManager(log, connectionEndPoint, TimeSpan.FromSeconds(30.0))
            else
                // More of the same TLS issue with the specific version of ES - https://github.com/EventStore/EventStore/issues/2615
                let messageHandler = new SocketsHttpHandler()
                let ops = SslClientAuthenticationOptions()
                ops.RemoteCertificateValidationCallback <- new RemoteCertificateValidationCallback((fun _ _ _ _ -> true))
                messageHandler.SslOptions <- ops
                ProjectionsManager(log, connectionEndPoint, TimeSpan.FromSeconds(30.0), messageHandler, "http")

        member this.AddProjection (proj : IEventStoreProjection<'ProjectionState>) =
            let cThis = this :> IEventStoreProjectorService
            let projMan = cThis.GetProjectionManager()
            let existingProjections = projMan.ListAllAsync(user).Result
            let exists =
                existingProjections
                |> Seq.exists(fun details -> details.Name = proj.Projection.Name)
            if not exists then projMan.CreateContinuousAsync(proj.Projection.Name, proj.Projection.Code, user).Wait()

        member this.GetProjectionState (proj : IEventStoreProjection<'ProjectionState>) : Result<'ProjectionState, string> =
            let cThis = this :> IEventStoreProjectorService
            let projMan = cThis.GetProjectionManager();
            let state = projMan.GetStateAsync(proj.Projection.Name, user).Result
            try
                Ok (JsonSerializer.Deserialize<'ProjectionState>(state, options))
            with
            | exc -> 
                Error exc.Message
