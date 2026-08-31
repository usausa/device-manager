SELECT * FROM ErrorReport
WHERE (/*@ deviceId */'' IS NULL) OR (DeviceId = /*@ deviceId */'')
ORDER BY ReportId DESC
LIMIT /*@ size */20 OFFSET /*@ offset */0