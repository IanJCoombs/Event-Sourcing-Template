module DotnetApp.Aggregate.CommandHandler

open System.Collections.Concurrent
open System.Threading.Tasks
open System
open DotnetApp.Aggregate.ES


type CommandHandler () =
    let queue : BlockingCollection<Task<Result<Guid, string>>> = new BlockingCollection<Task<Result<Guid, string>>>(new ConcurrentQueue<Task<Result<Guid, string>>>())

    member this.AddTask (task : Task<Result<Guid, string>>) : unit =
        queue.Add task

    member this.Monitor () =
        async {
            while true do
                let task = queue.Take()
                try
                    task.Start()
                    task.Wait()
                with
                | exc ->
                    ()
        }