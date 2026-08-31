SELECT * FROM Message
WHERE (/*@ deviceId */'' IS NULL) OR (DeviceId = /*@ deviceId */'')
ORDER BY MessageId DESC
LIMIT /*@ size */20 OFFSET /*@ offset */0