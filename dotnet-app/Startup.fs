namespace DotnetApp

open System
open System.Collections.Generic
open System.Linq
open System.Threading.Tasks
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.HttpsPolicy
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.AspNetCore.SpaServices
open Microsoft.AspNetCore.SpaServices.AngularCli
open Microsoft.AspNetCore.Routing
open Microsoft.AspNetCore.SpaServices.StaticFiles
open System.Text.Json.Serialization
open Microsoft.Extensions.Logging
open Microsoft.OpenApi.Models
open DotnetApp.Aggregate.ES
open DotnetApp.Configuration
open DotnetApp.Aggregate.CommandHandler

type Startup private () =
    new (configuration: IConfiguration) as this =
        Startup() then
        this.Configuration <- configuration

    // This method gets called by the runtime. Use this method to add services to the container.
    member this.ConfigureServices(services: IServiceCollection) =
        services.AddControllersWithViews().AddJsonOptions(fun options -> options.JsonSerializerOptions.Converters.Add(JsonFSharpConverter())) |> ignore
        services.AddSpaStaticFiles(fun configuration -> configuration.RootPath <- "ClientApp/dist")
        services.AddScoped<IEventStoreService, EventStoreService>() |> ignore
        services.AddScoped<IEventStoreProjectorService, EventStoreProjectorService>() |> ignore
        services.AddSingleton<ESConfiguration>() |> ignore
        services.AddSingleton<CommandHandler>() |> ignore

        services.AddSwaggerGen(fun c -> c.SwaggerDoc("v1", OpenApiInfo(Title = "API", Version = "v1"))) |> ignore

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    member this.Configure(app: IApplicationBuilder, env: IWebHostEnvironment) =
        if (env.IsDevelopment()) then
            app.UseDeveloperExceptionPage() |> ignore
        else
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts() |> ignore

        app.UseHttpsRedirection() |> ignore
        app.UseRouting() |> ignore

        app.UseEndpoints(fun endpoints -> endpoints.MapControllerRoute("default", "{controller}/{action}/{id?}") |> ignore) |> ignore
        app.UseSpaStaticFiles() |> ignore
        
        app.UseSwagger() |> ignore
        app.UseSwaggerUI(fun c -> c.SwaggerEndpoint("/swagger/v1/swagger.json", "API V1")) |> ignore
        
        app.UseSpa(fun spa ->
            spa.Options.SourcePath <- "ClientApp"

            // if (env.IsDevelopment()) then
                // spa.UseAngularCliServer("start")

            spa.UseProxyToSpaDevelopmentServer("http://localhost:4200")) 
        |> ignore
        
    member val Configuration : IConfiguration = null with get, set
