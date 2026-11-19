[CmdletBinding()]
param([string]$ConnectionString="Server=(localdb)\MSSQLLocalDB;Database=BuildingManagement_RestoreTest;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30")
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$dir=Join-Path $root 'Database/Redesign/Review-Reports'
New-Item -ItemType Directory -Force -Path $dir|Out-Null
$report=Join-Path $dir ("Phase25-CredentialFormatProfile-"+(Get-Date -Format 'yyyyMMdd-HHmmss')+".md")
$log=[System.Collections.Generic.List[string]]::new()
function Log([string]$s){$log.Add($s);Write-Host $s}
function Query($c,[string]$sql){$cmd=$c.CreateCommand();$cmd.CommandTimeout=180;$cmd.CommandText=$sql;$dt=[System.Data.DataTable]::new();$da=[System.Data.SqlClient.SqlDataAdapter]::new($cmd);[void]$da.Fill($dt);return ,$dt}
$c=[System.Data.SqlClient.SqlConnection]::new($ConnectionString)
try{
 $c.Open()
 $db=[string](Query $c "SELECT DB_NAME() N").Rows[0].N
 if($db -ne 'BuildingManagement_RestoreTest'){throw "Target database mismatch: $db"}
 foreach($t in @('Residents','Users','__EFMigrationsHistory')){$v=(Query $c "SELECT OBJECT_ID(N'dbo.$t',N'U') N").Rows[0].N;if($null -eq $v -or $v -is [DBNull]){throw "Missing dbo.$t"}}
 $sql=@"
SELECT N'Residents.PinHash' SourceColumn,
 COUNT(*) PopulatedRows,
 SUM(CASE WHEN LEN(CONVERT(nvarchar(max),PinHash))=60 AND LEFT(CONVERT(nvarchar(max),PinHash),4) IN (N'\$2a\$',N'\$2b\$',N'\$2y\$') THEN 1 ELSE 0 END) BCryptShapeRows,
 SUM(CASE WHEN PinHash IS NOT NULL AND NOT (LEN(CONVERT(nvarchar(max),PinHash))=60 AND LEFT(CONVERT(nvarchar(max),PinHash),4) IN (N'\$2a\$',N'\$2b\$',N'\$2y\$')) THEN 1 ELSE 0 END) OtherShapeRows
FROM dbo.Residents
UNION ALL
SELECT N'Residents.PasswordHash',COUNT(*),0,
 SUM(CASE WHEN PasswordHash IS NOT NULL THEN 1 ELSE 0 END)
FROM dbo.Residents
"@
 $dt=Query $c $sql
 foreach($row in $dt.Rows){Log "$($row.SourceColumn): populated=$($row.PopulatedRows); BCrypt-shaped=$($row.BCryptShapeRows); other-shape=$($row.OtherShapeRows)"}
 $users=(Query $c "SELECT COUNT(*) N FROM dbo.Users").Rows[0].N
 $hasPin=(Query $c "SELECT CASE WHEN COL_LENGTH(N'dbo.Users',N'Pin') IS NULL THEN 0 ELSE 1 END N").Rows[0].N
 $hasMfa=(Query $c "SELECT CASE WHEN COL_LENGTH(N'dbo.Users',N'TwoFactorSecretEncrypted') IS NULL THEN 0 ELSE 1 END N").Rows[0].N
 Log "Users rows: $users; legacy Users.Pin column present: $hasPin; encrypted MFA column present: $hasMfa"
 Log 'No credential content, prefixes, hashes, ciphertext, or plaintext values were returned.'
 Log 'FORMAT PROFILE ONLY: BCrypt-shaped values are not proof of successful verification or policy compatibility.'
 Log 'No credential conversion or target credential rows were created.'
}catch{Log "ERROR: $($_.Exception.Message)";throw}
finally{
 if($c.State -ne 'Closed'){$c.Close()}
 $lines=@('# Phase 25 - Credential format profile','',"- Run: $(Get-Date -Format o)",'- Mode: READ ONLY','','## Results')
 $lines+=$log|%{"- $_"}
 $lines+=@('','## Security decision','- This report contains aggregate format counts only, never credential values.','- Hash-shape recognition is not cryptographic verification. Any future migration requires known algorithm/version, successful test vectors, and compatible verification code.','- Legacy PIN/password/MFA values are not modified or copied.','- No source rows are updated or deleted.')
 Set-Content -LiteralPath $report -Value ($lines -join [Environment]::NewLine) -Encoding UTF8
 Write-Host "Report: $report"
}
Push-Location $root
try{
 $script=Join-Path $PSScriptRoot 'Invoke-Phase25-CredentialFormatProfile.ps1'
 git add -- $script $report
 if((git status --short -- $script $report).Count -gt 0){git commit --only -m "docs: add credential format profile" -- $script $report;git push origin development/database-redesign-enterprise}
}finally{Pop-Location}
