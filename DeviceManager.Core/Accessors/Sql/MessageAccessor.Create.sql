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