[CmdletBinding()]
param([string]$ConnectionString="Server=(localdb)\MSSQLLocalDB;Database=BuildingManagement_RestoreTest;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30")
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$dir=Join-Path $root 'Database/Redesign/Review-Reports'
New-Item -ItemType Directory -Force -Path $dir|Out-Null
$report=Join-Path $dir ("Phase31-IdentitySchemaProfile-"+(Get-Date -Format 'yyyyMMdd-HHmmss')+".md")
$log=[System.Collections.Generic.List[string]]::new()
function Log([string]$s){$log.Add($s);Write-Host $s}
$c=[System.Data.SqlClient.SqlConnection]::new($ConnectionString)
try{
 $c.Open()
 $cmd=$c.CreateCommand();$cmd.CommandText="SELECT DB_NAME()";$db=[string]$cmd.ExecuteScalar()
 if($db -ne 'BuildingManagement_RestoreTest'){throw "Target database mismatch: $db"}
 $cmd=$c.CreateCommand()
 $cmd.CommandText=@"
SELECT t.name TableName,c.column_id ColumnOrder,c.name ColumnName,
 TYPE_NAME(c.user_type_id) DataType,c.max_length MaxLength,c.is_nullable IsNullable
FROM sys.tables t JOIN sys.columns c ON c.object_id=t.object_id
WHERE t.name IN(N'Users',N'Residents',N'BuildingAdmins',N'Persons',N'UserAccounts',N'UserCredentials',N'LegacyIdentityMap')
ORDER BY t.name,c.column_id;
"@
 $dt=[System.Data.DataTable]::new();$da=[System.Data.SqlClient.SqlDataAdapter]::new($cmd);[void]$da.Fill($dt)
 foreach($table in @('Users','Residents','BuildingAdmins','Persons','UserAccounts','UserCredentials','LegacyIdentityMap')){
  Log "## $table"
  foreach($r in $dt.Select("TableName='$table'")){Log ("- {0}: {1}, max_length={2}, nullable={3}" -f $r.ColumnName,$r.DataType,$r.MaxLength,$r.IsNullable)}
 }
 $cmd=$c.CreateCommand()
 $cmd.CommandText=@"
SELECT
 (SELECT COUNT(*) FROM dbo.Users) UsersCount,
 (SELECT COUNT(*) FROM dbo.Residents) ResidentsCount,
 (SELECT COUNT(*) FROM dbo.BuildingAdmins) BuildingAdminLinks,
 (SELECT COUNT(*) FROM dbo.Buildings) BuildingsCount,
 (SELECT COUNT(*) FROM dbo.Persons) PersonsCount,
 (SELECT COUNT(*) FROM dbo.UserAccounts) UserAccountsCount,
 (SELECT COUNT(*) FROM dbo.UserCredentials) UserCredentialsCount,
 (SELECT COUNT(*) FROM dbo.LegacyIdentityMap WHERE SourceSystem=N'legacy-sql' AND SourceTable=N'Users') UserMapRows,
 (SELECT COUNT(*) FROM dbo.LegacyIdentityMap WHERE SourceSystem=N'legacy-sql' AND SourceTable=N'Residents') ResidentMapRows;
"@
 $dt2=[System.Data.DataTable]::new();$da=[System.Data.SqlClient.SqlDataAdapter]::new($cmd);[void]$da.Fill($dt2)
 $r=$dt2.Rows[0]
 Log "Database: $db"
 foreach($col in $dt2.Columns){Log "$($col.ColumnName): $($r[$col.ColumnName])"}
 Log 'No row values, names, contact details, UID values, credential material, or hashes were exported.'
 Log 'READ-ONLY SCHEMA PROFILE COMPLETE.'
}catch{Log "ERROR: $($_.Exception.Message)";throw}
finally{
 if($c.State -ne 'Closed'){$c.Close()}
 $lines=@('# Phase 31 - Identity schema profile','',"- Run: $(Get-Date -Format o)",'- Mode: READ ONLY','','## Results')
 $lines+=$log|%{"$_"}
 $lines+=@('','## Safety','- Schema metadata and aggregate counts only.','- No source or target data changed.','- No names, contact details, raw identifiers, credential material, or hashes exported.')
 Set-Content -LiteralPath $report -Value ($lines -join [Environment]::NewLine) -Encoding UTF8
 Write-Host "Report: $report"
}
Push-Location $root
try{
 $script=Join-Path $PSScriptRoot 'Invoke-Phase31-IdentitySchemaProfile.ps1'
 git add -- $script $report
 if((git status --short -- $script $report).Count -gt 0){git commit --only -m "feat: add identity schema profile audit" -- $script $report;git push origin development/database-redesign-enterprise}
}finally{Pop-Location}
