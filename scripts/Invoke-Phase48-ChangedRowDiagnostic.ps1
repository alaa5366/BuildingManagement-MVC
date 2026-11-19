[CmdletBinding()]
param(
 [string]$SourceConnectionString="Server=(localdb)\MSSQLLocalDB;Database=BuildingManagement;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30",
 [string]$TargetConnectionString="Server=(localdb)\MSSQLLocalDB;Database=BuildingManagement_RestoreTest;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30"
)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$dir=Join-Path $root 'Database/Redesign/Review-Reports'
New-Item -ItemType Directory -Force -Path $dir|Out-Null
$report=Join-Path $dir ("Phase48-ChangedRowDiagnostic-"+(Get-Date -Format 'yyyyMMdd-HHmmss')+".md")
function Open-Db([string]$cs,[string]$expected){
 $c=[System.Data.SqlClient.SqlConnection]::new($cs);$c.Open()
 $cmd=$c.CreateCommand();$cmd.CommandText='SELECT DB_NAME()'
 $actual=[string]$cmd.ExecuteScalar()
 if($actual -ne $expected){$c.Close();throw "Expected $expected but connected to $actual"}
 return $c
}
function Quote([string]$n){'['+$n.Replace(']',']]')+']'}
function Get-TableMeta($c,[string]$table){
 $cmd=$c.CreateCommand()
 $cmd.CommandText=@"
 SELECT c.name ColumnName,ty.name DataType,CONVERT(int,ISNULL(ic.key_ordinal,0)) KeyOrdinal
 FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id
 JOIN sys.columns c ON c.object_id=t.object_id JOIN sys.types ty ON ty.user_type_id=c.user_type_id
 LEFT JOIN sys.indexes ix ON ix.object_id=t.object_id AND ix.is_primary_key=1
 LEFT JOIN sys.index_columns ic ON ic.object_id=t.object_id AND ic.index_id=ix.index_id AND ic.column_id=c.column_id
 WHERE s.name='dbo' AND t.name=@t ORDER BY c.column_id
"@
 [void]$cmd.Parameters.Add('@t',[Data.SqlDbType]::NVarChar,128);$cmd.Parameters['@t'].Value=$table
 $dt=[System.Data.DataTable]::new();$da=[System.Data.SqlClient.SqlDataAdapter]::new($cmd);[void]$da.Fill($dt)
 $cols=@($dt.Rows|ForEach-Object{[pscustomobject]@{Name=[string]$_.ColumnName;Type=[string]$_.DataType;Key=[int]$_.KeyOrdinal}})
 [pscustomobject]@{Columns=$cols;PK=@($cols|Where-Object Key -gt 0|Sort-Object Key)}
}
function Get-Rows($c,[string]$table,$meta){
 $names=@($meta.Columns|ForEach-Object{Quote $_.Name}) -join ','
 $cmd=$c.CreateCommand();$cmd.CommandTimeout=300;$cmd.CommandText="SELECT $names FROM dbo.$(Quote $table)"
 $dt=[System.Data.DataTable]::new();$da=[System.Data.SqlClient.SqlDataAdapter]::new($cmd);[void]$da.Fill($dt)
 $map=@{}
 foreach($r in $dt.Rows){
  $keyObj=[ordered]@{};foreach($k in $meta.PK){$v=$r[$k.Name];$keyObj[$k.Name]=if($v -is [DBNull]){$null}else{[string]$v}}
  $key=ConvertTo-Json -InputObject $keyObj -Compress -Depth 5
  $map[$key]=$r
 }
 return $map
}
$sc=$null;$tc=$null
try{
 $sc=Open-Db $SourceConnectionString 'BuildingManagement';$tc=Open-Db $TargetConnectionString 'BuildingManagement_RestoreTest'
 $lines=@('# Phase 48 — Changed-row diagnostic','','- Read-only comparison of the two Phase 47 exception tables.','- Source and target values, primary keys, credentials, tokens, and personal data are never written to this report.','- Reports only changed column names, SQL types, null-state, and value lengths.','')
 foreach($table in @('Presence','ScheduledBackupSettings')){
  $sm=Get-TableMeta $sc $table;$tm=Get-TableMeta $tc $table
  if($sm.PK.Count -eq 0 -or (($sm.PK.Name -join '|') -ne ($tm.PK.Name -join '|'))){throw "No compatible primary key for dbo.$table"}
  $sr=Get-Rows $sc $table $sm;$tr=Get-Rows $tc $table $tm
  $common=@($sm.Columns|Where-Object{$_.Name -in $tm.Columns.Name -and $_.Name -notin @('rowversion','timestamp')})
  $lines+=("## dbo.$table","","- Source rows: $($sr.Count); target rows: $($tr.Count)")
  $changed=0
  foreach($key in $sr.Keys){
   if(-not $tr.ContainsKey($key)){continue}
   $a=$sr[$key];$b=$tr[$key];$diff=@()
   foreach($col in $common){
    $av=$a[$col.Name];$bv=$b[$col.Name]
    $an=($av -is [DBNull]);$bn=($bv -is [DBNull])
    $same=$false
    if($an -and $bn){$same=$true}
    elseif(-not $an -and -not $bn){
      if($av -is [byte[]] -and $bv -is [byte[]]){$same=([Convert]::ToBase64String($av) -ceq [Convert]::ToBase64String($bv))}
      elseif($av -is [datetime] -and $bv -is [datetime]){$same=($av.Ticks -eq $bv.Ticks)}
      else{$same=([string]$av -ceq [string]$bv)}
    }
    if(-not $same){
      $al=if($an){0}elseif($av -is [byte[]]){$av.Length}else{([string]$av).Length}
      $bl=if($bn){0}elseif($bv -is [byte[]]){$bv.Length}else{([string]$bv).Length}
      $diff+=("$($col.Name) [$($col.Type)]: sourceNull=$an,targetNull=$bn,sourceLength=$al,targetLength=$bl")
    }
   }
   if($diff.Count -gt 0){$changed++;$lines+=("- Matched row changed in $($diff.Count) column(s): "+($diff -join '; '))}
  }
  $extra=@($tr.Keys|Where-Object{-not $sr.ContainsKey($_)}).Count
  $lines+=("","- Matched rows with differences (direct value comparison): $changed","- Target-only rows: $extra","")
 }
 $lines+=@('## Interpretation','','Use the changed column names and metadata to determine whether differences are intentional operational updates or migration defects. Do not overwrite target values blindly. If source values must be preserved, retain them as immutable migration evidence before any approved target correction.','')
 Set-Content -LiteralPath $report -Value ($lines -join [Environment]::NewLine) -Encoding UTF8
 Write-Host "PHASE48 READ-ONLY DIAGNOSTIC COMPLETE. Report: $report"
}
catch{Write-Host "ERROR: $($_.Exception.Message)";throw}
finally{if($sc -and $sc.State -ne 'Closed'){$sc.Close()};if($tc -and $tc.State -ne 'Closed'){$tc.Close()}}
Push-Location $root
try{
 git add -- $PSCommandPath $report
 if((git status --short -- $PSCommandPath $report).Count -gt 0){
  git commit --only -m "feat(migration): diagnose row-level reconciliation differences" -- $PSCommandPath $report
  git push origin development/database-redesign-enterprise
 }
}finally{Pop-Location}
