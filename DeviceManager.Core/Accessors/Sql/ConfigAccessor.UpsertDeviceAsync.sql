INSERT INTO DeviceConfig (DeviceId, Key, Value, Description, UpdatedAt)
VALUES (/*@ deviceId */'', /*@ key */'', /*@ value */'', /*@ description */NULL, /*@ updatedAt */'')
ON CONFLICT (DeviceId, Key) DO UPDATE SET
    Value = excluded.Value,
    Description = excluded.Description,
    UpdatedAt = excluded.UpdatedAt