# Original Roadmap Phase09 — TEST Smoke Checklist

Run these checks against the local application or TEST deployment only. No database migration is required for this phase.

## 1. Liveness

```powershell
curl.exe -i https://localhost:<PORT>/health/live
```

Expected:
- HTTP 200 when the web process is running.
- JSON status is `Healthy`.
- `checks` is an empty array.
- The endpoint does not require SQL connectivity.

## 2. Readiness

```powershell
curl.exe -i https://localhost:<PORT>/health/ready
```

Expected when TEST SQL is available:
- HTTP 200.
- JSON overall status is `Healthy`.
- `checks` includes `Database` with status `Healthy`.

If TEST SQL is unavailable, readiness should return HTTP 503 and report an unhealthy database status. The response must not include exception text or connection details.

## 3. Backward compatibility

```powershell
curl.exe -i https://localhost:<PORT>/health
```

Expected: same readiness behavior as `/health/ready`.

## 4. Correlation ID is server-owned

```powershell
curl.exe -i -H "X-Correlation-ID: caller-controlled-value" https://localhost:<PORT>/health/live
```

Expected:
- Response contains `X-Correlation-ID`.
- The returned value is a 32-character lowercase hexadecimal ID.
- It is not `caller-controlled-value`.

## 5. Rolling file log properties

After making the requests above, inspect the latest log:

```powershell
$log = Get-ChildItem .\logs\serilog\serilog-*.txt |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

$log.FullName
Select-String -Path $log.FullName -Pattern 'CorrelationId|RequestMethod|RequestPath|StatusCode'
```

Expected: request completion events include structured properties, especially the same `CorrelationId` returned in the HTTP response.

## Safety

- Do not stop or modify Production services.
- Do not change TEST database availability just to force an unhealthy result; use an isolated local configuration if that negative path must be exercised.
- No EF migration or SQL script is part of this phase.
