# Oxide.Ext.AdminPanel

Oxide.Ext.AdminPanel - This is an extension for Oxide that provides the functionality of the administrative dashboard.

## Opportunities

* Web interface for server management
* Support for websockets for real-time information updates
* The ability to extend functionality with the help of data providers

## Installation

1. Download and install Oxide on your server.
2. Download and install Oxide.Ext.AdminPanel.
3. Configure the configuration of the administrative panel in the file `config.json'.

## Using

1. Open the web interface of the administrative panel using the address `http://localhost/adminpanel /`.
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