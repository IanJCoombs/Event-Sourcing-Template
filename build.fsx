// Don't forget to delete the build.fsx.lock file if you add new modules
#r "paket:
nuget Fake.Core.Target
nuget Fake.DotNet.Cli
nuget Fake.JavaScript.Npm //"
#load "./.fake/build.fsx/intellisense.fsx"


open Fake.Core
open Fake.DotNet
open Fake.JavaScript

Target.create "DockerDown" (fun _ ->
  [ "down"; ]
  |> CreateProcess.fromRawCommand "docker-compose"
  |> CreateProcess.withWorkingDirectory "./docker/"
  |> Proc.run
  |> ignore
)

Target.create "DockerUp" (fun _ ->
  let processResult =
    [ "up"; "-d" ]
    |> CreateProcess.fromRawCommand "docker-compose"
    |> CreateProcess.withWorkingDirectory "./docker/"
    |> Proc.run
  
  if processResult.ExitCode <> 0 then
    failwith "Failed to start docker containers"
)

let inline withWorkDir wd =
    DotNet.Options.withWorkingDirectory wd

Target.create "Build" (fun _ ->
  let processResult = DotNet.exec (withWorkDir "./dotnet-app") "build" ""
  
  if processResult.ExitCode <> 0 then
    failwith "Failed to Build"
)

Target.create "BuildTests" (fun _ ->
  let processResult = DotNet.exec (withWorkDir "./dotnet-app.tests") "build" ""
  
  if processResult.ExitCode <> 0 then
    failwith "Failed to Build tests"
)

Target.create "RunTests" (fun _ -> 
  DotNet.test (withWorkDir "./dotnet-app.tests") ""
)

Target.create "PaketInstall" (fun _ ->
  let processResult = DotNet.exec (withWorkDir ".") "paket" "install"
  
  if processResult.ExitCode <> 0 then
    failwith "Failed to install Paket dependencies"
)

Target.create "RunServer" (fun _ ->
  let processResult = DotNet.exec (withWorkDir "./dotnet-app") "run" ""
  
  if processResult.ExitCode <> 0 then
    failwith "Process exited unexpectedly"
)

Target.create "RunNg" (fun _ ->
  Npm.run "ng serve" (fun o -> { o with WorkingDirectory = "./dotnet-app/ClientApp/" }) |> ignore
)

open Fake.Core.TargetOperators

"PaketInstall" ==> "Build"

// *** Start Build ***
Target.runOrDefault "Build"