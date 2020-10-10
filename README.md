# Simple WebSocket server implementation in C#

![made-with-csharp](https://forthebadge.com/images/badges/made-with-c-sharp.svg)

[![Maintenance](https://img.shields.io/badge/Maintained%3F-no-red.svg)](https://bitbucket.org/lbesson/ansi-colors)

# Start work

__Publish your app before creating Docker image.__
 ```
 dotnet publish -c Release
 ```

__Create Docker Image from Dockerfile__
 ```
docker build -t websockerserver .
 ```
 
 __Create and run docker container__
 ```
docker run --name=websocketserver -d -p 5000:5000 websockerserver
 ```

> See more info about Dockerize .NET Core app [here](https://docs.microsoft.com/ru-ru/dotnet/core/docker/build-container?wt.mc_id=personal-blog-chnoring&tabs=windows)

__Application entrypoint.
Set the port on which the server will listen for connections.__
 ```csharp
public static void Main(string[] args)
{
      WebSocketsServer server = new WebSocketsServer(5000);
      server.Start();
}
```

__Client side. Open browser and press F12 -> Console__

* Connect to server
 ```js
 let socket = new WebSocket("ws://localhost:5000");
 ```

# Commands

 * Send simple message to all clients
```js
socket.send("your message");
```

* Change nickname
```js
socket.send("/change_name=your nickname");
```

