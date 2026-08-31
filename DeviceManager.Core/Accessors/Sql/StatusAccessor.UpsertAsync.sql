INSERT INTO DeviceStatus (DeviceId, Level, Battery, WifiRssi, ApName, Moving, ScanCount, Progress1, Progress2, Latitude, Longitude, UpdatedAt)
VALUES (/*@ deviceId */'', /*@ level */0, /*@ battery */0, /*@ wifiRssi */0, /*@ apName */NULL, /*@ moving */0, /*@ scanCount */0, /*@ progress1 */0, /*@ progress2 */0, /*@ latitude */NULL, /*@ longitude */NULL, /*@ updatedAt */'')
ON CONFLICT (DeviceId) DO UPDATE SET
    Level = excluded.Level,
    Battery = excluded.Battery,
    WifiRssi = excluded.WifiRssi,
    ApName = excluded.ApName,
    Moving = excluded.Moving,
    ScanCount = excluded.ScanCount,
    Progress1 = excluded.Progress1,
    Progress2 = excluded.Progress2,
    Latitude = excluded.Latitude,
    Longitude = excluded.Longitude,
    UpdatedAt = excluded.UpdatedAt