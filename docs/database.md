# Database Specification

## Overview

DeviceManager uses **SQLite** as the data store. There are two categories of database files:

| Category | File location | Scope |
|---|---|---|
| Common DB | `Data/device_manager.db` | All devices, shared tables |
| Device DB | `Data/devices/{deviceId}.db` | Per-device private tables |

Initialisation and migration are handled by `DatabaseInitializer` on server startup.

---

## Common Database (`device_manager.db`)

### Table: `Device`

Stores device registration and status information.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `DeviceId` | TEXT | PRIMARY KEY | Unique device identifier |
| `Name` | TEXT | NOT NULL | Human-readable display name |
| `Platform` | TEXT | NULL | OS / platform string (e.g. `"Android"`, `"Windows"`) |
| `Group` | TEXT | NULL | Optional group label for logical grouping |
| `Tags` | TEXT | NULL | JSON array of tag strings |
| `Status` | INTEGER | NOT NULL DEFAULT 0 | Connection status (enum, see below) |
| `IsEnabled` | INTEGER | NOT NULL DEFAULT 1 | `1` = enabled, `0` = disabled |
| `Note` | TEXT | NULL | Free-text administrator note |
| `RegisteredAt` | TEXT | NOT NULL | ISO-8601 UTC timestamp of first registration |
| `LastConnectedAt` | TEXT | NULL | ISO-8601 UTC timestamp of most recent connection |
| `CreatedAt` | TEXT | NOT NULL | ISO-8601 UTC timestamp of row creation |
| `UpdatedAt` | TEXT | NOT NULL | ISO-8601 UTC timestamp of last update |

**`Status` enum values:**

| Value | Name | Description |
|---|---|---|
| 0 | Inactive | Device is not connected |
| 1 | Active | Device is connected and communicating |
| 2 | Warning | Device is connected but reporting a warning condition |
| 3 | Error | Device is connected but reporting an error condition |

---

### Table: `DeviceStatus`

Stores the *current* (most-recent) status snapshot for each device.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `DeviceId` | TEXT | PRIMARY KEY, FK → Device | Device identifier |
| `Level` | INTEGER | NOT NULL DEFAULT 0 | Application-level status code |
| `Progress` | REAL | NOT NULL DEFAULT 0 | Progress value 0–100 |
| `Battery` | INTEGER | NULL | Battery percentage 0–100 |
| `WifiRssi` | INTEGER | NULL | Wi-Fi signal strength in dBm (e.g. -55) |
| `Latitude` | REAL | NULL | GPS latitude |
| `Longitude` | REAL | NULL | GPS longitude |
| `CustomData` | TEXT | NULL | JSON object `{ "key": "value" }` of arbitrary fields |
| `Timestamp` | TEXT | NOT NULL | ISO-8601 UTC timestamp of the report |

> **Migration note:** `WifiRssi` was added as an `ALTER TABLE` migration; existing rows default to `NULL`.

---

### Table: `CommonConfig`

Key-value configuration shared across all devices.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `Key` | TEXT | PRIMARY KEY | Configuration key |
| `Value` | TEXT | NOT NULL | Configuration value (always string) |
| `ValueType` | TEXT | NOT NULL DEFAULT `'string'` | Hint for the consumer: `"string"`, `"int"`, `"bool"`, etc. |
| `Description` | TEXT | NULL | Human-readable description |
| `UpdatedAt` | TEXT | NOT NULL | ISO-8601 UTC timestamp of last change |

---

### Table: `CommonDataStore`

General-purpose key-value store accessible to all devices.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `Key` | TEXT | PRIMARY KEY | Entry key |
| `Value` | TEXT | NOT NULL | Entry value |
| `CreatedAt` | TEXT | NOT NULL | ISO-8601 UTC timestamp of creation |
| `UpdatedAt` | TEXT | NOT NULL | ISO-8601 UTC timestamp of last update |

---

### Table: `Message`

Server ↔ device message history.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `MessageId` | INTEGER | PRIMARY KEY AUTOINCREMENT | Auto-incremented message ID |
| `DeviceId` | TEXT | NULL | Target/source device ID; NULL = broadcast |
| `Direction` | INTEGER | NOT NULL | 0 = ServerToDevice, 1 = DeviceToServer |
| `MessageType` | TEXT | NOT NULL | Application-defined message type string |
| `Content` | TEXT | NOT NULL | Message body |
| `Status` | INTEGER | NOT NULL DEFAULT 0 | 0 = Sent, 1 = Delivered, 2 = Read |
| `CreatedAt` | TEXT | NOT NULL | ISO-8601 UTC timestamp |

---

### Table: `DeviceLog`

Log entries sent by devices.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `LogId` | INTEGER | PRIMARY KEY AUTOINCREMENT | Auto-incremented log ID |
| `DeviceId` | TEXT | NOT NULL | Source device ID |
| `Level` | INTEGER | NOT NULL | Log level (0=Trace … 5=Critical) |
| `Category` | TEXT | NOT NULL | Logger category name |
| `Message` | TEXT | NOT NULL | Log message |
| `Exception` | TEXT | NULL | Exception stack trace (if any) |
| `Timestamp` | TEXT | NOT NULL | ISO-8601 UTC timestamp |

**Indexes:**
- `IX_DeviceLog_DeviceId` on `DeviceId`
- `IX_DeviceLog_Timestamp` on `Timestamp`

---

### Table: `ConfigHistory`

Audit log for configuration changes.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `Id` | INTEGER | PRIMARY KEY AUTOINCREMENT | Auto-incremented ID |
| `Scope` | TEXT | NOT NULL | `"common"` or `"device:{deviceId}"` |
| `Key` | TEXT | NOT NULL | Changed configuration key |
| `OldValue` | TEXT | NULL | Previous value (NULL if newly created) |
| `NewValue` | TEXT | NULL | New value (NULL if deleted) |
| `ChangedAt` | TEXT | NOT NULL | ISO-8601 UTC timestamp |

---

## Per-Device Database (`Data/devices/{deviceId}.db`)

A separate SQLite file is created for each device when it first registers.

### Table: `DeviceConfig`

Device-specific configuration overrides (merged with `CommonConfig` at read time).

| Column | Type | Constraints | Description |
|---|---|---|---|
| `Key` | TEXT | PRIMARY KEY | Configuration key |
| `Value` | TEXT | NOT NULL | Configuration value |
| `ValueType` | TEXT | NOT NULL DEFAULT `'string'` | Value type hint |
| `UpdatedAt` | TEXT | NOT NULL | ISO-8601 UTC timestamp of last change |

---

### Table: `DeviceDataStore`

Device-specific key-value data store.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `Key` | TEXT | PRIMARY KEY | Entry key |
| `Value` | TEXT | NOT NULL | Entry value |
| `CreatedAt` | TEXT | NOT NULL | ISO-8601 UTC timestamp of creation |
| `UpdatedAt` | TEXT | NOT NULL | ISO-8601 UTC timestamp of last update |

---

### Table: `StatusHistory`

Historical record of all status reports for a device.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `Id` | INTEGER | PRIMARY KEY AUTOINCREMENT | Auto-incremented ID |
| `Level` | INTEGER | NOT NULL DEFAULT 0 | Application-level status code |
| `Progress` | REAL | NOT NULL DEFAULT 0 | Progress value 0–100 |
| `Battery` | INTEGER | NULL | Battery percentage 0–100 |
| `Latitude` | REAL | NULL | GPS latitude |
| `Longitude` | REAL | NULL | GPS longitude |
| `CustomData` | TEXT | NULL | JSON object of arbitrary fields |
| `Timestamp` | TEXT | NOT NULL | ISO-8601 UTC timestamp |

---

## Datetime Format

All datetime columns store values as ISO-8601 round-trip format strings in UTC:

```
2024-07-01T12:34:56.7890000Z
```

---

## Connection Management

| Connection type | Method | Notes |
|---|---|---|
| Common DB | `DbConnectionFactory.GetCommonConnection()` | Opens shared `device_manager.db` |
| Device DB | `DbConnectionFactory.GetDeviceConnection(deviceId)` | Opens `Data/devices/{deviceId}.db` |

Device DBs are initialised on first `RegisterDeviceAsync` call and deleted on `DeleteDeviceAsync`.
