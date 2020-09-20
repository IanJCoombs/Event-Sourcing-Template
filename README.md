Still very early days here, mostly working on architecture so there is almost nothing to play with.

Interesting parts are aggregate.fs and node.fs in the dotnetapp project under aggregates.

# Bit of a mouthfull these... 

# build
dotnet fake run build.fsx --target Build

# bring up docker containers for data
dotnet fake run build.fsx --target DockerUp

# run core backend server
dotnet fake -v run build.fsx --target RunServer

# run angular frontend server
dotnet fake -v run build.fsx --target RunNg

# Browse Front End (bare bones)
navigate to http://localhost:5000/ or https://localhost:5001/

# Manually use the API

https://localhost:5001/swagger/

# eventstore
http://localhost:2113/web/index.html#/dashboard
