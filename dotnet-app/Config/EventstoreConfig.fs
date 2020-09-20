module DotnetApp.Configuration

open System
open Microsoft.Extensions.Configuration
open DotnetApp.Validator
open System.Text.Json
open System.Text.Json.Serialization
open Microsoft.Extensions.Logging

type EsEndPoint = {
    Url: string
    Port: int
    Ssl: bool
}

type ESConfig = {
    Username: string
    Password: string
    EndPoint: EsEndPoint
    ProjectionsEndPoint : EsEndPoint
}

let tryGetUsername(config : IConfiguration) =
    let section = config.GetSection("username")
    if section.Exists() then
        Ok section.Value
    else
        Error "Missing eventstore username in config"

let tryGetPassword(config : IConfiguration) =
        let section = config.GetSection("password")
        if section.Exists() then
            Ok section.Value
        else
            Error "Missing password in config"

let tryGetEsEndPoint(section : IConfigurationSection) =
        Validator {
            do! validate (section.Exists(), "missing configuration")

            let url = section.GetSection("url")
            let port = section.GetSection("port")
            let ssl = section.GetSection("ssl")
            do! validate (port.Exists(), "config missing port")
            do! validate (ssl.Exists(), "config missing SSL flag")
            do! validate (url.Exists(), "config missing URL endpoint")
            
            let! portVal = tryValidate (port.Value, Int32.TryParse, "port config not an integer")
            let! sslVal = tryValidate (ssl.Value, Boolean.TryParse, "SSL config flag not a boolean value")

            return 
                {
                    Url = url.Value
                    Port = portVal
                    Ssl = sslVal
                }
        }

type ESConfiguration(configuration: IConfiguration, log : ILogger<ESConfiguration>) =
    member this.ValidatedConfig =
        let validated = Validator {
            let! username = tryGetUsername(configuration)
            let! password = tryGetPassword(configuration)
            let! endPoint = tryGetEsEndPoint(configuration.GetSection("endPoint"))
            let! projectionsEndPoint = tryGetEsEndPoint(configuration.GetSection("projectionsEndPoint"))

            return {
                Username = username
                Password = password
                EndPoint = endPoint
                ProjectionsEndPoint = projectionsEndPoint
            }
        }

        match validated with
        | Ok conf ->
            log.LogInformation "Eventstore configuration validated OK"
            conf
        | Error e ->
            failwithf "Eventstore configuration validated Fail with error: %s" e


    