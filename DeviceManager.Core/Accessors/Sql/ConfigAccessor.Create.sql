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