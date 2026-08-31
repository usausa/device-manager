INSERT INTO Message (DeviceId, Direction, MessageType, Content, Status, CreatedAt)
VALUES (/*@ deviceId */NULL, /*@ direction */0, /*@ messageType */'', /*@ content */'', /*@ status */0, /*@ createdAt */'');
SELECT last_insert_rowid();