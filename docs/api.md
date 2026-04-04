# API Specification

## Overview

DeviceManager exposes three API surfaces:

| Surface | Transport | Base URL / Path | Consumers |
|---|---|---|---|
| **REST API** | HTTP/1.1 + HTTP/2 | `/api/` | Dashboard UI, external systems |
| **SignalR Hub** | WebSocket (HTTP/1.1) | `/hubs/device` | Device clients, Dashboard UI |
| **gRPC Service** | HTTP/2 | `DeviceManagerService` | Device clients |

Ports are configurable in `appsettings.json`; defaults:
- `http://localhost:5178` – HTTP/1.1 & HTTP/2 (Blazor, REST, SignalR)
- `https://localhost:7400` – HTTPS HTTP/2 (gRPC)

---

## REST API

### Devices — `api/devices`

#### `GET /api/devices`
Returns all registered devices with their latest status.

**Response `200 OK`:** `DeviceSummary[]`

---

#### `GET /api/devices/summary`
Returns aggregated device counts by status.

**Response `200 OK`:** `StatusSummary`

---

#### `GET /api/devices/{deviceId}`
Returns full details for a single device.

**Response `200 OK`:** `DeviceDetail`  
**Response `404 Not Found`**

---

#### `POST /api/devices`
Registers a new device (or updates an existing registration).

**Request body:** `DeviceRegistration`  
**Response `201 Created`:** `DeviceRegistration` (with `Location` header)

---

#### `PUT /api/devices/{deviceId}`
Updates editable fields of an existing device.

**Request body:** `DeviceUpdateRequest`  
**Response `204 No Content`**  
**Response `404 Not Found`**

---

#### `DELETE /api/devices/{deviceId}`
Deletes a device and its per-device database.

**Response `204 No Content`**  
**Response `404 Not Found`**

---

### Messages — `api/messages`

#### `GET /api/messages`
Retrieves message history.

**Query parameters:**

| Name | Type | Default | Description |
|---|---|---|---|
| `deviceId` | string | (all) | Filter by device ID |
| `skip` | int | 0 | Pagination offset |
| `take` | int | 50 | Page size |

**Response `200 OK`:** `ServerMessage[]`

---

#### `POST /api/messages/send`
Sends a message from the server to a device (or broadcasts to all devices).

**Request body:** `SendMessageRequest`  
**Response `200 OK`:** `ServerMessage` (persisted record)

---

#### `POST /api/messages/command`
Sends a command to a specific device.

**Request body:** `SendCommandRequest`  
**Response `200 OK`:** `{ "commandId": "string" }`

---

### Storage — `api/storage`

Files are served from the directory configured in `Storage:RootPath` (defaults to `{AppDir}/storage`).
All paths are validated to stay within the root (path-traversal protection).

#### `GET /api/storage/{**path}`
Returns a file download, or a directory listing when the path ends with `/` or resolves to a directory.

**File response:** binary stream with `Content-Disposition: attachment`  
**Directory response `200 OK`:** `StorageEntry[]`  
**Response `404 Not Found`**

---

#### `POST /api/storage/{**path}`
Uploads a file (multipart/form-data).

**Form field:** `file` — the file to upload  
**Response `201 Created`:** `{ "path": "string", "size": long }`

---

#### `PUT /api/storage/mkdir/{**path}`
Creates a directory.

**Response `200 OK`**

---

#### `DELETE /api/storage/{**path}`
Deletes a file or directory (recursive for directories).

**Response `204 No Content`**  
**Response `404 Not Found`**

---

## SignalR Hub (`/hubs/device`)

### Connection

Clients connect with `HubConnectionBuilder` using the URL `{serverUrl}/hubs/device`.  
Dashboard clients should call `JoinGroup("dashboard")` after connecting to receive device notifications.

---

### Client → Server Methods (device clients)

#### `Register(DeviceRegistration registration)`
Registers the device on the hub. The server assigns the connection to per-device and per-group SignalR groups.  
On success the server sends `ConfigReload` back to the caller.

---

#### `ReportStatus(DeviceStatusReport report)`
Sends a status update. The server persists it and notifies the dashboard.

---

#### `SendMessage(string messageType, string content)`
Sends an application-level message to the server.

---

#### `SendLog(LogEntry entry)`
Sends a log entry to be stored server-side.

---

#### `CommandResult(string commandId, bool success, string? result)`
Reports the result of a command previously issued by the server.

---

### Client → Server Methods (dashboard clients)

#### `JoinGroup(string groupName)`
Subscribes the connection to a notification group.

Common group names:

| Group name | Description |
|---|---|
| `"dashboard"` | All device events (connect, status, messages, logs) |
| `"device:{deviceId}"` | Events for a specific device |
| `"group:{groupName}"` | Events for all devices in a group |

---

### Server → Device Methods

#### `ConfigReload(List<ConfigEntry> entries)`
Sent immediately after `Register` with the merged (common + device) configuration.

#### `ReceiveMessage(string messageType, string content)`
Delivers a server-to-device message.

#### `ReceiveCommand(string commandId, string command, string payload)`
Delivers a command for the device to execute.

---

### Server → Dashboard Methods

#### `DeviceConnected(DeviceRegistration registration)`
A device has connected and registered.

#### `DeviceDisconnected(string deviceId)`
A device has disconnected.

#### `DeviceStatusUpdated(string deviceId, DeviceStatusReport report)`
A device has sent a status report.

#### `MessageReceived(ServerMessage message)`
A device-to-server message has been received.

#### `LogReceived(LogEntry entry)`
A device log entry has been received.

#### `CommandResult(CommandResult result)`
A device has responded to a command.

---

## gRPC Service (`DeviceManagerService`)

Proto file: `DeviceManager.Shared/Protos/device_service.proto`

### RPC Methods

#### `Register(RegisterRequest) → RegisterResponse`
Registers a device.

#### `ReportStatus(StatusReport) → StatusResponse`
Persists a device status report and notifies the dashboard.

#### `SendMessage(DeviceMessage) → MessageResponse`
Stores a device-to-server message and notifies the dashboard.

#### `SendLog(LogRequest) → LogResponse`
Stores a device log entry and notifies the dashboard.

#### `GetConfig(ConfigRequest) → ConfigResponse`
Returns the merged (common + device-specific) configuration for the device.

#### `GetDataStoreValue(DataStoreRequest) → DataStoreResponse`
Reads a single value from the device's data store.

#### `SetDataStoreValue(DataStoreSetRequest) → DataStoreResponse`
Writes a value to the device's data store.

#### `Subscribe(SubscribeRequest) → stream ServerEvent`
Opens a server-push event stream. The server sends configuration updates, commands, and messages to the device over this stream.

---

## Request / Response Data Structures

### `DeviceRegistration`
```json
{
  "deviceId": "string",
  "name": "string",
  "platform": "string | null",
  "group": "string | null",
  "additionalInfo": { "key": "value" }
}
```

### `DeviceUpdateRequest`
```json
{
  "name": "string | null",
  "group": "string | null",
  "tags": ["string"] | null,
  "note": "string | null",
  "isEnabled": "bool | null"
}
```

### `DeviceSummary`
```json
{
  "deviceId": "string",
  "name": "string",
  "group": "string | null",
  "status": 0,
  "lastConnectedAt": "ISO-8601 | null",
  "level": 0,
  "progress": 0.0,
  "battery": 0 | null,
  "wifiRssi": 0 | null,
  "statusTimestamp": "ISO-8601 | null"
}
```

### `DeviceDetail`
```json
{
  "deviceId": "string",
  "name": "string",
  "platform": "string | null",
  "group": "string | null",
  "tags": ["string"] | null,
  "note": "string | null",
  "status": 0,
  "isEnabled": true,
  "registeredAt": "ISO-8601",
  "lastConnectedAt": "ISO-8601 | null",
  "level": 0,
  "progress": 0.0,
  "battery": 0 | null,
  "wifiRssi": 0 | null,
  "latitude": 0.0 | null,
  "longitude": 0.0 | null,
  "customData": { "key": "value" } | null,
  "statusTimestamp": "ISO-8601 | null"
}
```

### `StatusSummary`
```json
{
  "total": 10,
  "active": 5,
  "inactive": 3,
  "warning": 1,
  "error": 1
}
```

### `DeviceStatusReport`
```json
{
  "level": 0,
  "progress": 0.0,
  "battery": 0 | null,
  "wifiRssi": 0 | null,
  "latitude": 0.0 | null,
  "longitude": 0.0 | null,
  "progressValues": { "key": 0.0 } | null,
  "customData": { "key": "value" } | null,
  "timestamp": "ISO-8601"
}
```

### `ServerMessage`
```json
{
  "messageId": 0,
  "deviceId": "string | null",
  "direction": 0,
  "messageType": "string",
  "content": "string",
  "status": 0,
  "createdAt": "ISO-8601"
}
```
> `direction`: 0 = ServerToDevice, 1 = DeviceToServer  
> `status`: 0 = Sent, 1 = Delivered, 2 = Read

### `SendMessageRequest`
```json
{
  "deviceId": "string | null",
  "messageType": "string",
  "content": "string"
}
```

### `SendCommandRequest`
```json
{
  "deviceId": "string",
  "command": "string",
  "payload": "string"
}
```

### `ConfigEntry`
```json
{
  "key": "string",
  "value": "string",
  "valueType": "string",
  "description": "string | null"
}
```

### `DataStoreEntry`
```json
{
  "key": "string",
  "value": "string",
  "createdAt": "ISO-8601",
  "updatedAt": "ISO-8601"
}
```

### `LogEntry`
```json
{
  "logId": 0,
  "deviceId": "string",
  "level": 0,
  "category": "string",
  "message": "string",
  "exception": "string | null",
  "timestamp": "ISO-8601"
}
```
> `level`: 0=Trace, 1=Debug, 2=Information, 3=Warning, 4=Error, 5=Critical

### `CommandResult`
```json
{
  "commandId": "string",
  "success": true,
  "result": "string | null"
}
```

### `StorageEntry` (directory listing)
```json
[
  { "name": "string", "type": "directory" },
  { "name": "string", "type": "file", "length": 0, "lastModified": "ISO-8601" }
]
```
