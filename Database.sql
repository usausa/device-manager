-- SQLite
-- 参照用 DDL。実際のテーブル作成は起動時に各 Accessor の Create.sql で行われる
-- (スキーマ変更時は DeviceManager.Core\Accessors\Sql\*.Create.sql と本ファイルの両方を更新すること)

CREATE TABLE IF NOT EXISTS Device (
    DeviceId         TEXT     NOT NULL,
    Name             TEXT     NOT NULL,
    Platform         TEXT,
    GroupName        TEXT,
    Note             TEXT,
    IsEnabled        INTEGER  NOT NULL DEFAULT 1,
    Status           INTEGER  NOT NULL DEFAULT 0,
    RegisteredAt     TEXT     NOT NULL,
    LastConnectedAt  TEXT,
    PRIMARY KEY (DeviceId)
);

CREATE TABLE IF NOT EXISTS DeviceStatus (
    DeviceId   TEXT     NOT NULL,
    Level      INTEGER  NOT NULL,
    Battery    REAL     NOT NULL,
    WifiRssi   INTEGER  NOT NULL,
    ApName     TEXT,
    Moving     INTEGER  NOT NULL,
    ScanCount  INTEGER  NOT NULL,
    Progress1  REAL     NOT NULL,
    Progress2  REAL     NOT NULL,
    Latitude   REAL,
    Longitude  REAL,
    UpdatedAt  TEXT     NOT NULL,
    PRIMARY KEY (DeviceId)
);

CREATE TABLE IF NOT EXISTS StatusHistory (
    Id         INTEGER  NOT NULL,
    DeviceId   TEXT     NOT NULL,
    Level      INTEGER  NOT NULL,
    Battery    REAL     NOT NULL,
    WifiRssi   INTEGER  NOT NULL,
    ApName     TEXT,
    Moving     INTEGER  NOT NULL,
    ScanCount  INTEGER  NOT NULL,
    Progress1  REAL     NOT NULL,
    Progress2  REAL     NOT NULL,
    Latitude   REAL,
    Longitude  REAL,
    CreatedAt  TEXT     NOT NULL,
    PRIMARY KEY (Id AUTOINCREMENT)
);
CREATE INDEX IF NOT EXISTS IX_StatusHistory_DeviceId_CreatedAt ON StatusHistory (DeviceId, CreatedAt);
CREATE INDEX IF NOT EXISTS IX_StatusHistory_CreatedAt ON StatusHistory (CreatedAt);

CREATE TABLE IF NOT EXISTS ConnectionLog (
    Id         INTEGER  NOT NULL,
    DeviceId   TEXT     NOT NULL,
    EventType  TEXT     NOT NULL,
    CreatedAt  TEXT     NOT NULL,
    PRIMARY KEY (Id AUTOINCREMENT)
);
CREATE INDEX IF NOT EXISTS IX_ConnectionLog_DeviceId_CreatedAt ON ConnectionLog (DeviceId, CreatedAt);

CREATE TABLE IF NOT EXISTS Message (
    MessageId    INTEGER  NOT NULL,
    DeviceId     TEXT,
    Direction    INTEGER  NOT NULL,
    MessageType  TEXT     NOT NULL,
    Content      TEXT     NOT NULL,
    Status       INTEGER  NOT NULL,
    CreatedAt    TEXT     NOT NULL,
    PRIMARY KEY (MessageId AUTOINCREMENT)
);
CREATE INDEX IF NOT EXISTS IX_Message_DeviceId ON Message (DeviceId);

CREATE TABLE IF NOT EXISTS DeviceLog (
    LogId      INTEGER  NOT NULL,
    DeviceId   TEXT     NOT NULL,
    Level      INTEGER  NOT NULL,
    Category   TEXT     NOT NULL,
    Message    TEXT     NOT NULL,
    Exception  TEXT,
    CreatedAt  TEXT     NOT NULL,
    PRIMARY KEY (LogId AUTOINCREMENT)
);
CREATE INDEX IF NOT EXISTS IX_DeviceLog_DeviceId_CreatedAt ON DeviceLog (DeviceId, CreatedAt);
CREATE INDEX IF NOT EXISTS IX_DeviceLog_CreatedAt ON DeviceLog (CreatedAt);

CREATE TABLE IF NOT EXISTS ErrorReport (
    ReportId        INTEGER  NOT NULL,
    DeviceId        TEXT     NOT NULL,
    ExceptionType   TEXT     NOT NULL,
    Message         TEXT     NOT NULL,
    StackTrace      TEXT,
    InnerException  TEXT,
    AppVersion      TEXT,
    OsVersion       TEXT,
    OccurredAt      TEXT     NOT NULL,
    ReceivedAt      TEXT     NOT NULL,
    PRIMARY KEY (ReportId AUTOINCREMENT)
);
CREATE INDEX IF NOT EXISTS IX_ErrorReport_DeviceId_ReceivedAt ON ErrorReport (DeviceId, ReceivedAt);

CREATE TABLE IF NOT EXISTS CommonConfig (
    Key          TEXT  NOT NULL,
    Value        TEXT  NOT NULL,
    Description  TEXT,
    UpdatedAt    TEXT  NOT NULL,
    PRIMARY KEY (Key)
);

CREATE TABLE IF NOT EXISTS DeviceConfig (
    DeviceId     TEXT  NOT NULL,
    Key          TEXT  NOT NULL,
    Value        TEXT  NOT NULL,
    Description  TEXT,
    UpdatedAt    TEXT  NOT NULL,
    PRIMARY KEY (DeviceId, Key)
);

CREATE TABLE IF NOT EXISTS ConfigHistory (
    Id         INTEGER  NOT NULL,
    Scope      TEXT     NOT NULL,
    Key        TEXT     NOT NULL,
    OldValue   TEXT,
    NewValue   TEXT,
    ChangedAt  TEXT     NOT NULL,
    PRIMARY KEY (Id AUTOINCREMENT)
);

CREATE TABLE IF NOT EXISTS FunctionDefinition (
    Name       TEXT     NOT NULL,
    Json       TEXT     NOT NULL,
    CallCount  INTEGER  NOT NULL DEFAULT 0,
    UpdatedAt  TEXT     NOT NULL,
    PRIMARY KEY (Name)
);

CREATE TABLE IF NOT EXISTS Account (
    Id         INTEGER  NOT NULL,
    Name       TEXT     NOT NULL,
    Password   BLOB     NOT NULL,
    Role       TEXT     NOT NULL,
    CreatedAt  TEXT     NOT NULL,
    PRIMARY KEY (Id AUTOINCREMENT),
    UNIQUE (Name)
);
