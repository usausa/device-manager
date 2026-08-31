INSERT INTO ErrorReport (DeviceId, ExceptionType, Message, StackTrace, InnerException, AppVersion, OsVersion, OccurredAt, ReceivedAt)
VALUES (/*@ deviceId */'', /*@ exceptionType */'', /*@ message */'', /*@ stackTrace */NULL, /*@ innerException */NULL, /*@ appVersion */NULL, /*@ osVersion */NULL, /*@ occurredAt */'', /*@ receivedAt */'');
SELECT last_insert_rowid();