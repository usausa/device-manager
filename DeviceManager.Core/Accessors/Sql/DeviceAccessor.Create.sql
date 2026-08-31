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