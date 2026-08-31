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