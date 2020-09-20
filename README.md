# Event Sourcing Template

This template is a basic F# ASP.NET Core 3.1 application that uses event sourcing as its source of truth.

The purpose of this template is for learning or getting started with event sourcing.

I built this because when I was learning event sourcing, although there were some fantastic resources out there for learning, I found it hard to piece the components together in my own implementation.

This template is a working, ready to go app. It uses Docker with an EventStore database container wired up.

There is a basic Angular 9 front end that I have spent all of no time developing, so you are on your own there. But it is functional.

The idea is that with a few commands after cloning/forking the repo, you can start playing with the event themselves. Pressing the very few buttons in the front end.

You can log into EventStore and watch the event streams and see the actual events.

You can debug the code and see how the data flows and aggregates.

I recommend this for anyone currently reading through a learning resource on event sourcing so you can experiment with it without spending too much time building the boilerplate stuff.

If you are an event sourcing wizard, and you see something that could make this template better, please reach out! I am by far not an expert and would love to keep this template up to date and make it as good as can be.

If you want to use this for work or some project, I don't mind, but be aware that I have no idea what I am doing so, you know, do your own due diligence. For starters, you will need to fix the TLS stuff for EventStore, and probably don't use a Docker container for production. bla bla bla.

# Prerequisites

You will need Docker on your machine. This template uses Docker-compose (even though there is currently only one container to spin up)

You will need dotnet, specifically 3.1; Grab the dotnet 3.1 SDK from M$

Node.js; install the latest Node.js for NPM fun

# build
dotnet fake build

# bring up docker containers for data
dotnet fake build -t DockerUp

# run core backend server
dotnet fake build -t RunServer

# run angular frontend server
dotnet fake build -t RunNg

# Browse Front End (bare bones)
navigate to http://localhost:5000/ or https://localhost:5001/

# Manually use the API

https://localhost:5001/swagger/

# eventstore
http://localhost:2113/web/index.html#/dashboard

