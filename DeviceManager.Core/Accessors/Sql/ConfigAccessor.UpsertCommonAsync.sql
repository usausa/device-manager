INSERT INTO CommonConfig (Key, Value, Description, UpdatedAt)
VALUES (/*@ key */'', /*@ value */'', /*@ description */NULL, /*@ updatedAt */'')
ON CONFLICT (Key) DO UPDATE SET
    Value = excluded.Value,
    Description = excluded.Description,
    UpdatedAt = excluded.UpdatedAt