# Oxide.Ext.AdminPanel

Oxide.Ext.AdminPanel - This is an extension for Oxide that provides the functionality of the administrative dashboard.

## Opportunities

* Web interface for server management
* Support for websockets for real-time information updates
* The ability to extend functionality with the help of data providers

## Why?

Oxide.Ext.AdminPanel is designed as a lightweight, embedded extension running within the Oxide modding framework, which integrates tightly with Unity-based game servers like Rust. Given the nature of this environment, using ASP.NET introduces several disadvantages:

1. **Self-hosted environment limitations**: Oxide plugins operate within the game server's process space. Integrating ASP.NET would require hosting a full-fledged Kestrel server, which adds unnecessary complexity and overhead for a plugin architecture.

2. **Portability and independence**: The current custom HTTP/WebSocket server built on top of `HttpListener` and `Fleck` allows complete control over the lifecycle, threading model, and request routing without relying on external dependencies or web servers.

3. **Lightweight and fast startup**: The goal is to keep the AdminPanel extension lightweight and optimized for minimal footprint. ASP.NET, while powerful, is relatively heavyweight for simple admin dashboard use cases like static file serving, JWT auth, and WebSocket support.

4. **Custom architecture**: Implementing our own routing, middleware, DI, and WebSocket communication makes the project highly customizable and easier to debug or extend within the Oxide ecosystem, without the abstraction layers and constraints of ASP.NET middleware pipelines.

5. **Better control over concurrency and threading**: Since this extension runs in a Unity context (which is single-threaded by design), precise control over async I/O and thread safety is critical — something that's much more predictable with minimal dependencies and handcrafted logic.

By choosing a handcrafted approach, Oxide.Ext.AdminPanel remains fast, easy to integrate, and specifically tailored for game server environments.

## Installation

1. Download and install Oxide on your server.
2. Download and install Oxide.Ext.AdminPanel.
3. Configure the configuration of the administrative panel in the file `config.json'.

## Using

1. Open the web interface of the administrative panel using the address `http://localhost/adminpanel/`.
2. Log in using the username and password specified in the configuration.
3. Use the web interface to manage the server and view information.

## Development

* Oxide.Ext.AdminPanel is written in C# and uses the Fleck library to support websockets.
* To extend the functionality of the administrative panel, you can create your own data providers.

## Roadmap

### DONE

1. **Routing**: implemented a request routing system
2. **Middleware support**: implemented Middleware support for query processing
3. **Support for JWT tokens**: implemented support for JWT tokens for authentication
4. **Static file output**: implemented the ability to output static files (HTML, CSS, etc.) from the server folder
5. **Its own implementation of DI**: implemented its own Dependency Injection implementation
6. **API**: implemented an API for interacting with the server
7. **Websocket**: implemented a websocket for live communication between the client and the server

### TODO

1. **Authorization**: implement user authorization on the server
2. **The players' live map**: implement a live player map on the server
3. **Installation on the server**: to implement the possibility of installing the administrative panel on the server
4. **Functionality for server management**: implement functionality for server management (e.g. restart, stop, etc.)
5. **Tests** cover the entire code with tests

### PLANNED

1. **Improve security**: improving the security of the administrative panel (for example, adding protection against CSRF, etc.)
2. **Performance optimization**: optimizing the performance of the administrative panel
3. **Adding new features**: adding new functions and features to the administrative dashboard

### FUTURE

1. **Multi-server support**: implementation of multi-server support in the administrative panel
2. **Integration with other services**: integration of the administrative panel with other services (for example, monitoring systems, etc.)

This roadmap describes the current and future tasks for the administrative panel. It will be updated as tasks are completed and new ones are added.


## WebSocket Usage Example

Here is a simple example of how to connect to the admin panel WebSocket endpoint from the browser using JavaScript:

```javascript
const socket = new WebSocket("ws://localhost:8181/ws");

socket.addEventListener("open", () => {
  console.log("Connection established");
  socket.send("Salam!");
});

socket.addEventListener("message", (event) => {
  console.log("Server response:", event.data);
});

socket.addEventListener("error", (event) => {
  console.error("WebSocket error:", event);
});

socket.addEventListener("close", () => {
  console.log("Connection closed");
});
```

## REST API Usage Examples

You can also fetch information from the AdminPanel backend using HTTP requests.

### Get Server Performance Info
```javascript
fetch("http://localhost/adminpanel/api/server/performance")
  .then(response => response.json())
  .then(data => {
    console.log("Server performance:", data);
  })
  .catch(error => {
    console.error("Failed to fetch performance data:", error);
  });
```

### Get Player Count
```javascript
fetch("http://localhost/adminpanel/api/player/count")
  .then(response => response.json())
  .then(data => {
    console.log("Current player count:", data.count);
  })
  .catch(error => {
    console.error("Failed to fetch player count:", error);
  });
```
