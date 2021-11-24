# Apollo BLE Controller Sample

This repository controls LED strips controlled via the "Apollo Lighting" app, and creates an API as well as a Windows consumable service that will control it via a SignalR service.

Explore the files to figure out what it does and how it works.

Proper publish technique for `Apollo.Service`:

`dotnet publish -c Release -p:PublishSingleFile=true -r win-x64 --no-self-contained`

Command to add and/or update the service to Windows:

*The service will take in the first command line paramter or the environment variable `APOLLO_SERVICE_URL` to configure it's SignalR connection.*

`sc create "Apollo Light Service" binpath="C:\Services\Apollo.Service.exe <url>/realtime/hub"`

`sc config "Apollo Light Service" binpath="C:\Services\Apollo.Service.exe <url>/realtime/hub"`

*You should probably have it automatically turn on on startup, and have it automatically restart at least once. A "Logs/" folder will be needed as well*

Todo:
- Implement fade states.