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