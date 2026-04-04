# Core Services — Public Method Reference

All services are defined in `DeviceManager.Server.Core.Services` and registered as **scoped** (per-request) services via `AddDeviceManagerCore(dataDirectory)`, except where noted.

---

## DeviceService

Manages device lifecycle, status, and history.

### Device CRUD

```csharp
Task<List<DeviceSummary>> GetAllDevicesAsync()
```
Returns all devices with their latest status snapshot.

---

```csharp
Task<DeviceDetail?> GetDeviceAsync(string deviceId)
```
Returns full details (including latest status) for a single device, or `null` if not found.

---

```csharp
Task RegisterDeviceAsync(DeviceRegistration registration)
```
Inserts or updates the device record and creates its per-device SQLite database if missing.

---

```csharp
Task UpdateDeviceAsync(string deviceId, DeviceUpdateRequest request)
```
Applies partial updates (Name, Group, Tags, Note, IsEnabled) to an existing device.

---

```csharp
Task DeleteDeviceAsync(string deviceId)
```
Removes the device record, its status row, and deletes its per-device database file.

---

### Connection Status

```csharp
Task UpdateConnectionStatusAsync(string deviceId, DeviceConnectionStatus status)
```
Updates `Device.Status` and `Device.LastConnectedAt` (when setting Active).

---

### Status Reporting

```csharp
Task UpdateStatusAsync(string deviceId, DeviceStatusReport report)
```
Upserts `DeviceStatus` with the latest values and appends a row to the device's `StatusHistory` table.

---

### Status History

```csharp
Task<List<DeviceStatusReport>> GetStatusHistoryAsync(
    string deviceId,
    DateTime? from = null,
    DateTime? to = null)
```
Returns historical status reports from the device database, optionally filtered by time range.

---

### Dashboard Summary

```csharp
Task<StatusSummary> GetStatusSummaryAsync()
```
Returns device counts grouped by connection status (Active / Inactive / Warning / Error / Total).

---

## ConfigService

Manages common and per-device configuration with change history.

### Common Config

```csharp
Task<List<ConfigEntry>> GetCommonConfigAsync()
```
Returns all entries from `CommonConfig`, ordered by key.

---

```csharp
Task SetCommonConfigAsync(string key, ConfigEntry entry)
```
Upserts a common configuration entry and records the change in `ConfigHistory`.

---

```csharp
Task DeleteCommonConfigAsync(string key)
```
Deletes a common configuration entry.

---

### Device Config

```csharp
Task<List<ConfigEntry>> GetDeviceConfigAsync(string deviceId)
```
Returns all device-specific configuration entries.

---

```csharp
Task SetDeviceConfigAsync(string deviceId, string key, ConfigEntry entry)
```
Upserts a device-specific configuration entry and records the change in `ConfigHistory` with scope `"device:{deviceId}"`.

---

```csharp
Task DeleteDeviceConfigAsync(string deviceId, string key)
```
Deletes a device-specific configuration entry.

---

### Resolved Config

```csharp
Task<List<ConfigEntry>> GetResolvedConfigAsync(string deviceId)
```
Returns the merged configuration for a device: common entries are the baseline, device-specific entries override by key. Result is ordered alphabetically.

---

## DataStoreService

Key-value data store with separate common and per-device scopes.

### Common Data Store

```csharp
Task<List<DataStoreEntry>> GetCommonEntriesAsync()
```
Returns all entries in `CommonDataStore`, ordered by key.

---

```csharp
Task<DataStoreEntry?> GetCommonEntryAsync(string key)
```
Returns a single entry by key, or `null` if not found.

---

```csharp
Task SetCommonEntryAsync(string key, string value)
```
Upserts a common data store entry.

---

```csharp
Task DeleteCommonEntryAsync(string key)
```
Deletes a common data store entry.

---

### Device Data Store

```csharp
Task<List<DataStoreEntry>> GetDeviceEntriesAsync(string deviceId)
```
Returns all entries in the device's `DeviceDataStore`, ordered by key.

---

```csharp
Task<DataStoreEntry?> GetDeviceEntryAsync(string deviceId, string key)
```
Returns a single device-scoped entry by key, or `null` if not found.

---

```csharp
Task SetDeviceEntryAsync(string deviceId, string key, string value)
```
Upserts a device-scoped data store entry.

---

```csharp
Task DeleteDeviceEntryAsync(string deviceId, string key)
```
Deletes a device-scoped data store entry.

---

## MessageService

Persists and retrieves server ↔ device messages.

```csharp
Task AddMessageAsync(ServerMessage message)
```
Inserts a message record into `Message`.

---

```csharp
Task<List<ServerMessage>> GetMessagesAsync(
    string? deviceId,
    int skip,
    int take)
```
Returns messages in descending creation order, optionally filtered by `deviceId`. Supports pagination via `skip` / `take`.

---

## LogService

Stores and retrieves log entries from devices.

```csharp
Task AddLogEntryAsync(LogEntry entry)
```
Inserts a single log entry into `DeviceLog`.

---

```csharp
Task AddLogEntriesAsync(IEnumerable<LogEntry> entries)
```
Inserts multiple log entries in a single transaction.

---

```csharp
Task<List<LogEntry>> GetLogsAsync(
    string? deviceId,
    int? level,
    int skip,
    int take)
```
Returns log entries in descending timestamp order. Optional filters:
- `deviceId` — restrict to a single device
- `level` — minimum log level (inclusive, e.g. `3` returns Warning, Error, Critical)

---

## Service Registration

Services are registered in `ServiceCollectionExtensions.AddDeviceManagerCore`:

| Service | Lifetime |
|---|---|
| `DbConnectionFactory` | Singleton |
| `DeviceService` | Singleton |
| `ConfigService` | Singleton |
| `DataStoreService` | Singleton |
| `MessageService` | Singleton |
| `LogService` | Singleton |

`DbConnectionFactory` also initialises and migrates the common database, and seeds sample data on first run.
