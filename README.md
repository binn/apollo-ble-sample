# Apollo BLE Controller Sample

This repository controls LED strips controlled via the "Apollo Lighting" app, and creates an API as well as a Windows consumable service that will control it via a SignalR service.

Explore the files to figure out what it does and how it works.

Proper publish technique for `Apollo.Service`:

`dotnet publish -c Release -p:PublishSingleFile=true -r win-x64 --no-self-contained`