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