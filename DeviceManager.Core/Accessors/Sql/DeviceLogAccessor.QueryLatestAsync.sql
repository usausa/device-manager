SELECT * FROM DeviceLog
WHERE ((/*@ deviceId */'' IS NULL) OR (DeviceId = /*@ deviceId */''))
  AND (Level >= /*@ minLevel */0)
ORDER BY LogId DESC
LIMIT /*@ take */100