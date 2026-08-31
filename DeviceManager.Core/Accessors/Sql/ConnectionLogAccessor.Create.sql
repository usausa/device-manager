CREATE TABLE IF NOT EXISTS ConnectionLog (
    Id         INTEGER  NOT NULL,
    DeviceId   TEXT     NOT NULL,
    EventType  TEXT     NOT NULL,
    CreatedAt  TEXT     NOT NULL,
    PRIMARY KEY (Id AUTOINCREMENT)
);
CREATE INDEX IF NOT EXISTS IX_ConnectionLog_DeviceId_CreatedAt ON ConnectionLog (DeviceId, CreatedAt);