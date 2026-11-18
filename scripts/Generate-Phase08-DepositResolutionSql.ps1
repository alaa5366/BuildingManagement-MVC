[CmdletBinding()]
param([string]$RepoPath = "D:\Projects\BuildingManagement-MVC")
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$Branch = "development/database-redesign-enterprise"
$Server = "(localdb)\MSSQLLocalDB"
$Database = "BuildingManagement_RestoreTest"
$From = "20261118224524_Phase08FinanceReversalScopeIntegrity"
$To = "20261118234143_Phase08_PaymentDepositResolutionReasonIntegrity"
$Backup = Join-Path $env:LOCALAPPDATA "BuildingManagement\Backups\BuildingManagement_RestoreTest-Phase08-20261119-011703.bak"
$SqlRel = "Database/Redesign/Generated-Sql/Phase08-Deposit-Resolution-20261119.sql"
$ReportRel = "Database/Redesign/Review-Reports/Phase08-Deposit-Resolution-SQL-Review-20261119.md"
$SqlPath = Join-Path $RepoPath $SqlRel.Replace("/", "\")
$ReportPath = Join-Path $RepoPath $ReportRel.Replace("/", "\")
function Git([string[]]$Args) {
  $old=$ErrorActionPreference
  try {$ErrorActionPreference="Continue";$o=@(& git -C $RepoPath @Args 2>&1);$ec=$LASTEXITCODE}
  finally {$ErrorActionPreference=$old}
  if($ec -ne 0){throw "git failed: $($o -join [Environment]::NewLine)"}
  return $o
}
function Query([string]$Db,[string]$Sql) {
  $cs="Server=$Server;Initial Catalog=$Db;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=15;"
  $cn=[System.Data.SqlClient.SqlConnection]::new($cs)
  try {$cn.Open();$cmd=$cn.CreateCommand();$cmd.CommandText=$Sql;$ad=[System.Data.SqlClient.SqlDataAdapter]::new($cmd);$dt=[System.Data.DataTable]::new();[void]$ad.Fill($dt);return ,$dt}
  finally {$cn.Dispose()}
}
if((Git @("branch","--show-current") | Select-Object -First 1).Trim() -ne $Branch){throw "Wrong branch."}
if(-not(Test-Path (Join-Path $RepoPath "BuildingManagementMVC.csproj"))){throw "Project missing."}
if(-not(Test-Path $Backup -PathType Leaf)){throw "Verified Phase08 backup missing."}
$hist=Query $Database "SELECT MigrationId FROM dbo.__EFMigrationsHistory ORDER BY MigrationId;"
$ids=@($hist.Rows|%{[string]$_.MigrationId})
if($ids.Count -ne 8 -or $ids[-1] -ne $From -or $ids -contains $To){throw "RestoreTest history gate failed. Expected exactly eight migrations ending at $From."}
$check=Query $Database "SELECT name,is_disabled,is_not_trusted FROM sys.check_constraints WHERE name=N'CK_PaymentDeposits_Resolution';"
if($check.Rows.Count -ne 1 -or [bool]$check.Rows[0].is_disabled -or [bool]$check.Rows[0].is_not_trusted){throw "Existing resolution constraint is absent, disabled, or untrusted."}
$masterCs="Server=$Server;Database=master;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=15;"
$cn=[System.Data.SqlClient.SqlConnection]::new($masterCs)
try{$cn.Open();$cmd=$cn.CreateCommand();$b=$Backup.Replace("'","''");$cmd.CommandText="RESTORE VERIFYONLY FROM DISK=N'$b' WITH CHECKSUM;";[void]$cmd.ExecuteNonQuery()}
finally{$cn.Dispose()}
$ef=@(& dotnet ef migrations script $From $To --project (Join-Path $RepoPath "BuildingManagementMVC.csproj") --startup-project (Join-Path $RepoPath "BuildingManagementMVC.csproj") --output $SqlPath 2>&1)
if($LASTEXITCODE -ne 0){throw "EF SQL generation failed: $($ef -join [Environment]::NewLine)"}
$sql=[IO.File]::ReadAllText($SqlPath)
if([string]::IsNullOrWhiteSpace($sql)){throw "Generated SQL is empty."}
if($sql -notmatch 'CK_PaymentDeposits_Resolution'){throw "Expected check constraint not found."}
if([regex]::IsMatch($sql,'(?im)^\s*(DROP\s+TABLE|TRUNCATE\s+TABLE|DELETE\s+FROM|UPDATE\s+\[)\b')){throw "Unexpected destructive/data mutation SQL."}
$drop=@([regex]::Matches($sql,'(?im)^\s*ALTER\s+TABLE[^\r\n]*DROP\s+CONSTRAINT\s+\[([^\]]+)\]')|%{$_.Groups[1].Value})
if($drop.Count -ne 1 -or $drop[0] -ne "CK_PaymentDeposits_Resolution"){throw "Unexpected DROP CONSTRAINT set: $($drop -join ',')."}
$add=@([regex]::Matches($sql,'(?im)^\s*ALTER\s+TABLE[^\r\n]*ADD\s+CONSTRAINT\s+\[([^\]]+)\]')|%{$_.Groups[1].Value})
if($add.Count -ne 1 -or $add[0] -ne "CK_PaymentDeposits_Resolution"){throw "Unexpected ADD CONSTRAINT set: $($add -join ',')."}
$hash=(Get-FileHash $SqlPath -Algorithm SHA256).Hash
$report=@(
"# Phase08 Deposit Resolution SQL Review","",
"- Server/database gate: PASS ($Server / $Database)",
"- EF history gate: PASS (8 rows; latest $From; target absent)",
"- Existing CK_PaymentDeposits_Resolution: enabled and trusted",
"- Backup RESTORE VERIFYONLY WITH CHECKSUM: PASS",
"- Migration range: $From -> $To",
"- Generated SQL SHA256: $hash",
"- DROP CONSTRAINT: CK_PaymentDeposits_Resolution only",
"- ADD CONSTRAINT: CK_PaymentDeposits_Resolution only",
"- DROP TABLE/COLUMN, TRUNCATE, DELETE, UPDATE: none",
"- EF migration execution: NOT PERFORMED",
"- Production: NOT TARGETED",
"",
"SQL replaces one check constraint only. Review report and SQL artifact before running the dedicated RestoreTest apply gate."
) -join [Environment]::NewLine
New-Item -ItemType Directory (Split-Path $ReportPath) -Force|Out-Null
[IO.File]::WriteAllText($ReportPath,$report+[Environment]::NewLine,[Text.UTF8Encoding]::new($false))
$paths=@($SqlRel,$ReportRel)
Git (@("add","--")+$paths)|Out-Null
Git (@("commit","--only","-m","review: generate Phase08 deposit resolution SQL","--")+$paths)|Out-Null
Git @("push","origin",$Branch)|Out-Null
Write-Host "Reviewed one-constraint SQL and report pushed." -ForegroundColor Green
Write-Host "SQL: $SqlRel"
Write-Host "SHA256: $hash"
Write-Host "No SQL was executed and no database was modified."
