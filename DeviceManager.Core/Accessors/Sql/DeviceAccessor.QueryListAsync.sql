SELECT
    d.DeviceId,
    d.Name,
    d.GroupName,
    d.Note,
    d.IsEnabled,
    d.Status,
    d.LastConnectedAt,
    s.Battery,
    s.WifiRssi,
    s.ApName,
    s.Moving,
    s.ScanCount,
    s.Progress1,
    s.Progress2,
    s.Latitude,
    s.Longitude,
    s.UpdatedAt AS StatusUpdatedAt,
    (SELECT COUNT(*) FROM DeviceLog l WHERE l.DeviceId = d.DeviceId) AS LogCount
FROM Device d
LEFT JOIN DeviceStatus s ON s.DeviceId = d.DeviceId
WHERE (/*@ filter */'' IS NULL)
   OR (d.DeviceId LIKE '%' || /*@ filter */'' || '%')
   OR (d.Name LIKE '%' || /*@ filter */'' || '%')
   OR (d.GroupName LIKE '%' || /*@ filter */'' || '%')
ORDER BY d.DeviceId