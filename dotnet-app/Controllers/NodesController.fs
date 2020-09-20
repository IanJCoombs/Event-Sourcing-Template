namespace DotnetApp.Controllers

open System
open System.Collections.Generic
open System.Linq
open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc
open DotnetApp.Aggregate.NodeAggregate
open DotnetApp.Aggregate.ES
open DotnetApp.Aggregate.CommandHandler

type GetNode = {
    Id : Guid
}





[<Route("[controller]")>]
[<ApiController>]
type NodesController (commandHandler : CommandHandler, esService : IEventStoreService, projectorService : IEventStoreProjectorService) =
    inherit Controller()

    let nodeCommandHandler = NodeCommandHandler(commandHandler, esService, projectorService)

    [<HttpPost>]
    [<Route("[action]")>]
    member this.Create() =
        let result =
            CreateNode(
                {
                    Id = Guid.NewGuid()
                })
            |> nodeCommandHandler.Handle
        
        match result with
        | Ok res ->
            this.Ok(res)
        | Error e ->
            failwith e

    [<HttpPost>]
    [<Route("[action]")>]
    member this.GetNode([<FromBody>] node : GetNode) =
        let state = nodeCommandHandler.GetNode(node.Id)

        match state with
        | Some state ->
            this.Ok(state)
        | None ->
            failwithf "Failed to find node stream for id: %s" (id.ToString())

    
    [<HttpPost>]
    [<Route("[action]")>]
    member this.SetNodeName([<FromBody>] cmd : SetNodeNameCommand) =
        let result = 
            cmd
            |> SetName
            |> nodeCommandHandler.Handle
        
        match result with
        | Ok res ->
            this.Ok(res)
        | Error e ->
            failwithf "Failed to set name: %s, to node: %s, with error: %s" cmd.Name (cmd.Id.ToString()) e

    [<HttpPost>]
    [<Route("[action]")>]
    member this.GetActiveNodeIds() =
        let result = nodeCommandHandler.GetActiveNodeIds()
        
        match result with
        | Ok res ->
            this.Ok(res)
        | Error e ->
            failwithf "Failed to get node IDs with error: %s" e

    [<HttpPost>]
    [<Route("[action]")>]
    member this.GetActiveNodesPaged([<FromBody>] cmd : GetNodesPaged) =
        let result = nodeCommandHandler.GetActiveNodesPaged(cmd)

        match result with
        | Ok res ->
            this.Ok(res)
        | Error e ->
            failwithf "Failed to get paged nodes for page %d, taking %d, with error: %s" cmd.Page cmd.Take e