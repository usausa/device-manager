INSERT INTO FunctionDefinition (Name, Json, CallCount, UpdatedAt)
VALUES (/*@ name */'', /*@ json */'', 0, /*@ updatedAt */'')
ON CONFLICT (Name) DO UPDATE SET
    Json = excluded.Json,
    UpdatedAt = excluded.UpdatedAt