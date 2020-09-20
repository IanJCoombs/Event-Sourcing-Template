namespace DotnetApp

open System
open System.Collections.Generic
open System.IO
open System.Linq
open System.Threading.Tasks
open Microsoft.AspNetCore
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging
open DotnetApp.Aggregate.ES
open DotnetApp.Aggregate.NodeAggregate
open Microsoft.Extensions.DependencyInjection
open Configuration
open System.Threading
open DotnetApp.Aggregate.CommandHandler

module Program =
    let exitCode = 0

    let CreateWebHostBuilder args =
        Host.CreateDefaultBuilder(args)
            .ConfigureLogging(fun logging ->
                logging.ClearProviders() |> ignore
                logging.AddConsole() |> ignore
            )
            .ConfigureAppConfiguration(fun (context: HostBuilderContext) (config: IConfigurationBuilder) ->
                config.AddJsonFile("appsettings.json",false,true) |> ignore
                config.AddJsonFile(sprintf "appsettings.%s.json" context.HostingEnvironment.EnvironmentName, true) |> ignore
                config.AddJsonFile(sprintf "config/%s/eventstore.json" context.HostingEnvironment.EnvironmentName, true) |> ignore
            )
            .ConfigureWebHostDefaults(fun webBuilder ->
                webBuilder.UseStartup<Startup>() |> ignore
            )

    let Projections () =
        [
            NodeProjection()
        ]

    [<EntryPoint>]
    let main args =
        let host = CreateWebHostBuilder(args).Build()
        
        use serviceScope = (host.Services.CreateScope())
        let services = serviceScope.ServiceProvider
        try
            let esConfig = services.GetRequiredService<ESConfiguration>()
            let esService = EventStoreService(esConfig)
            let projectorService = EventStoreProjectorService(esService, esConfig) :> IEventStoreProjectorService
            Projections () |> List.iter projectorService.AddProjection
            
            let commandHandler = services.GetRequiredService<CommandHandler>()
            Async.Start (commandHandler.Monitor())
        with
        | exn ->
            failwith exn.Message

        host.Run()

        exitCode
