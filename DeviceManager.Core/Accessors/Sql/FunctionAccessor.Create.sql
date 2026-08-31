CREATE TABLE IF NOT EXISTS FunctionDefinition (
    Name       TEXT     NOT NULL,
    Json       TEXT     NOT NULL,
    CallCount  INTEGER  NOT NULL DEFAULT 0,
    UpdatedAt  TEXT     NOT NULL,
    PRIMARY KEY (Name)
);