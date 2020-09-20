module DotnetApp.Aggregate.NodeAggregate

open System
open DotnetApp.Aggregate.ES
open DotnetApp.Aggregate.CommandHandler
open DotnetApp.Validator
open System.Collections.Generic
open System.Threading.Tasks


type NodeState = {
    Id: Guid
    Name: string
}
with static member Init = 
        {
            Id = Guid.Empty
            Name = "New Node"
        }

type CreateNodeCommand = {
    Id: Guid
}

type NodeCreatedEvent = {
    Id: Guid
}

type SetNodeNameCommand = {
    Id: Guid
    Name: string
}

type NodeNameSetEvent = {
    Id: Guid
    Name: string
}

type NodeCommand =
    | CreateNode of CreateNodeCommand
    | SetName of SetNodeNameCommand

type NodeEvent =
    | NodeCreated of NodeCreatedEvent
    | NameSet of NodeNameSetEvent

let execute (state : NodeState) (command : NodeCommand) : Result<NodeEvent list, string> =
    Validator {
        match command with
        | CreateNode cmd ->
            return [ NodeCreated { Id = cmd.Id } ]
        | SetName cmd ->
            do! validate ((cmd.Name <> ""), "Nodes must have a non empty name")
            return [
                NameSet 
                    { 
                        Id = cmd.Id
                        Name = cmd.Name
                    }
            ]
    }

let apply (state : NodeState) (event : NodeEvent) =
    match event with
    | NodeCreated evt ->
        { state with Id = evt.Id }
    | NameSet evt ->
        { state with Name = evt.Name }

let nodeAggregate : Aggregate<NodeState, NodeCommand, NodeEvent> = {
    Init = NodeState.Init
    Execute = execute
    Apply = apply
    StreamPrefix = "Node"
}

let getId (cmd : NodeCommand) : Guid =
    match cmd with
    | CreateNode cmd -> cmd.Id
    | SetName cmd -> cmd.Id

type NodeProjectionNodeState = {
    Active: bool
    Name: string
}

type NodeProjectionState = {
    // Can't use Guid as the key because not supported yet - https://github.com/dotnet/runtime/issues/30524
    Nodes: Dictionary<string, NodeProjectionNodeState>
}

type NodeProjection() =
    interface IEventStoreProjection<NodeProjectionState> with
        member this.Projection = {
            Name = "$AllNodes"
            // Todo: Let's auto generate these from known types so we can avoid manually writing them
            // I am thinking we could use attributes - https://docs.microsoft.com/en-us/dotnet/fsharp/language-reference/attributes
            Code = """
fromStream('$ce-Node')
.when({
    $init:function(){
        return {
            Nodes: {}
        };
    },
    "DotnetApp.Aggregate.NodeAggregate+NodeEvent+NodeCreated": function(state, event){
        state.Nodes[event.body.Fields.Item.Id] = {Active: true, Name:""};
    },
    "DotnetApp.Aggregate.NodeAggregate+NodeEvent+NameSet": function(state, event){
        var node = event.body.Fields.Item;
        var nodeState = state.Nodes[node.Id];
        nodeState.Name = node.Name;        
        state.Nodes[node.Id] = nodeState;
    }
}).outputState();"""
        }

type GetNodesPaged = {
    Take : int
    Page : int
}

let pageNodes (page : int) (take : int) (nodes : Dictionary<string, NodeProjectionNodeState>)=
    nodes
    |> Seq.filter (fun kvp -> kvp.Value.Active)
    |> Seq.skip (page * take)
    |> Seq.take take

type NodeCommandHandler(commandHandler : CommandHandler,
    esService : IEventStoreService, 
    projectionService : IEventStoreProjectorService) = 

    member this.Handle (cmd : NodeCommand) : Result<Guid, string> =
        let handle =
            new Task<Result<Guid, string>> (fun () -> 
                let idStreamName = 
                    {
                        IdStreamName.StreamPrefix = nodeAggregate.StreamPrefix
                        Id = getId(cmd)
                    }  

                let state = 
                    match cmd with
                    | CreateNode _ -> nodeAggregate.Init
                    | _ -> 
                        esService.GetCurrentStreamState(nodeAggregate, idStreamName)
                        
                let executed = nodeAggregate.Execute state cmd

                match executed with
                | Ok events ->
                    let newState =                
                        events
                        |> Seq.fold nodeAggregate.Apply state
                    esService.Write(idStreamName.ToString(), events)
                    Ok newState.Id
                | Error error ->
                    Error error
            )

        commandHandler.AddTask(handle) |> ignore
        handle.Wait()
        handle.Result


        // let result = Async.

    member this.GetNode (id: Guid) : NodeState option =
        let idStreamName = 
            {
                IdStreamName.StreamPrefix = nodeAggregate.StreamPrefix
                Id = id
            }            
        let state = esService.GetCurrentStreamState(nodeAggregate, idStreamName)

        if state = nodeAggregate.Init then
            None
        else
            Some state

    member this.GetActiveNodeIds () =
        projectionService.GetProjectionState(NodeProjection())
        |> Result.map(fun nodeStates -> 
            nodeStates.Nodes
            |> Seq.filter (fun kvp -> kvp.Value.Active)
            |> Seq.map (fun kvp -> Guid(kvp.Key)))
        

    member this.GetActiveNodesPaged(cmd : GetNodesPaged) =
        projectionService.GetProjectionState(NodeProjection())
        |> Result.map(fun nodeStates -> 
            nodeStates.Nodes |> pageNodes cmd.Page cmd.Take)





