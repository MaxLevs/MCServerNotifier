# MCServerNotifier
C# .Net Core library which lets you get notifications about online/offline status of server and login/logout of players. 
It uses minecraft server *Query protocol* for getting server status and *terminal-notifier* console app for spawning notifications

There are several libraries here:
- MCQuery - helps you to getting data from server via Query protocol
- UdpExtension - now it just send package and receive an answer. It will be upgraded later for pairing responses with their requests
- TerminalNotifierLib - a wrapper under terminal-notifier application. Helps you to spawn notifications in MacOS. You can upgrade it for your system or needs.

# Preparations
For using app you have to create `Resources/servers.json` file with content like that
```json
[
  {"Name": "My cool server", "Host": "mc.my-cool-server.net", "QueryPort": 25565 }
]
```
