[CmdletBinding()]
param([string]$ServerInstance="(localdb)\MSSQLLocalDB",[string]$Database="BuildingManagement_RestoreTest")
$ErrorActionPreference="Stop"
$ExpectedBranch="development/database-redesign-enterprise"
$RepoRoot="D:\Projects\BuildingManagement-MVC"
Push-Location $RepoRoot
try {
 if (!(Test-Path ".git")) { throw "Repository path is invalid." }
 $b=(git branch --show-current).Trim()
 if ($LASTEXITCODE -ne 0 -or $b -ne $ExpectedBranch) { throw "Run only on $ExpectedBranch. Current: $b" }
 $staged=git diff --cached --name-only
 if ($LASTEXITCODE -ne 0) { throw "Cannot inspect Git index." }
 if ($staged) { throw "Git index has staged changes. Stop and resolve them first." }
 $stamp=Get-Date -Format "yyyyMMdd-HHmmss"
 $relative="Database/Diagnostics/View-Inventory-Results/$stamp"
 $out=Join-Path $RepoRoot $relative
 New-Item -ItemType Directory -Path $out -Force | Out-Null
 $sql=@'
SET NOCOUNT ON;
IF DB_NAME() <> N'BuildingManagement_RestoreTest'
 THROW 51000, 'Only BuildingManagement_RestoreTest is allowed.', 1;

SELECT s.name SchemaName,v.name ViewName,v.object_id ObjectId,v.create_date CreateDate,v.modify_date ModifyDate,
 OBJECTPROPERTYEX(v.object_id,'IsSchemaBound') IsSchemaBound,
 OBJECTPROPERTYEX(v.object_id,'IsIndexed') IsIndexed,
 OBJECTPROPERTYEX(v.object_id,'IsEncrypted') IsEncrypted
FROM sys.views v JOIN sys.schemas s ON s.schema_id=v.schema_id ORDER BY s.name,v.name;

SELECT s.name SchemaName,v.name ViewName,
 CASE WHEN OBJECTPROPERTYEX(v.object_id,'IsEncrypted')=1 THEN N'ENCRYPTED'
 WHEN m.definition IS NULL THEN N'DEFINITION_UNAVAILABLE' ELSE N'AVAILABLE' END DefinitionStatus,
 m.definition ViewDefinition
FROM sys.views v JOIN sys.schemas s ON s.schema_id=v.schema_id
LEFT JOIN sys.sql_modules m ON m.object_id=v.object_id ORDER BY s.name,v.name;

SELECT OBJECT_SCHEMA_NAME(d.referencing_id) ReferencingSchema,OBJECT_NAME(d.referencing_id) ReferencingObject,
 o.type_desc ReferencingType,d.referenced_server_name ReferencedServer,d.referenced_database_name ReferencedDatabase,
 d.referenced_schema_name ReferencedSchema,d.referenced_entity_name ReferencedEntity,d.referenced_id ReferencedObjectId,
 d.is_schema_bound_reference IsSchemaBound,d.is_ambiguous IsAmbiguous
FROM sys.sql_expression_dependencies d LEFT JOIN sys.objects o ON o.object_id=d.referencing_id
WHERE o.type IN ('V','P','FN','IF','TF','TR')
ORDER BY ReferencingSchema,ReferencingObject,ReferencedSchema,ReferencedEntity;

SELECT vs.name ViewSchema,v.name ViewName,OBJECT_SCHEMA_NAME(d.referencing_id) ConsumerSchema,
 OBJECT_NAME(d.referencing_id) ConsumerObject,o.type_desc ConsumerType
FROM sys.views v JOIN sys.schemas vs ON vs.schema_id=v.schema_id
LEFT JOIN sys.sql_expression_dependencies d ON d.referenced_id=v.object_id
LEFT JOIN sys.objects o ON o.object_id=d.referencing_id
ORDER BY vs.name,v.name,ConsumerSchema,ConsumerObject;

SELECT s.name SchemaName,v.name ViewName,i.name IndexName,i.type_desc IndexType,
 i.is_unique IsUnique,i.is_primary_key IsPrimaryKey,i.has_filter HasFilter,i.filter_definition FilterDefinition,
 c.name ColumnName,ic.key_ordinal KeyOrdinal,ic.is_included_column IsIncluded
FROM sys.views v JOIN sys.schemas s ON s.schema_id=v.schema_id
JOIN sys.indexes i ON i.object_id=v.object_id AND i.index_id>0
LEFT JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id
LEFT JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
ORDER BY s.name,v.name,i.name,ic.key_ordinal,ic.index_column_id;

SELECT s.name SchemaName,sy.name SynonymName,sy.base_object_name BaseObjectName,sy.create_date CreateDate,sy.modify_date ModifyDate
FROM sys.synonyms sy JOIN sys.schemas s ON s.schema_id=sy.schema_id ORDER BY s.name,sy.name;

SELECT s.name SchemaName,t.name TableName,
 SUM(CASE WHEN p.index_id IN (0,1) THEN p.rows ELSE 0 END) ApproxRows,
 SUM(a.total_pages)*8.0/1024 ReservedMB,SUM(a.used_pages)*8.0/1024 UsedMB
FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id
JOIN sys.indexes i ON i.object_id=t.object_id
JOIN sys.partitions p ON p.object_id=i.object_id AND p.index_id=i.index_id
JOIN sys.allocation_units a ON a.container_id=p.partition_id
GROUP BY s.name,t.name ORDER BY s.name,t.name;

SELECT actual_state_desc,desired_state_desc,readonly_reason,current_storage_size_mb,max_storage_size_mb,query_capture_mode_desc
FROM sys.database_query_store_options;

IF EXISTS(SELECT 1 FROM sys.database_query_store_options WHERE actual_state_desc<>'OFF')
 SELECT TOP (500) q.query_id,p.plan_id,rs.count_executions,rs.avg_duration,rs.max_duration,
 rs.avg_logical_io_reads,qt.query_sql_text
 FROM sys.query_store_query q JOIN sys.query_store_query_text qt ON qt.query_text_id=q.query_text_id
 LEFT JOIN sys.query_store_plan p ON p.query_id=q.query_id
 LEFT JOIN sys.query_store_runtime_stats rs ON rs.plan_id=p.plan_id
 WHERE qt.query_sql_text LIKE N'%vw[_]%' OR qt.query_sql_text LIKE N'%ApartmentWallet%'
 ORDER BY rs.avg_duration DESC;
ELSE SELECT CAST(NULL AS bigint) query_id,N'QUERY_STORE_OFF' Status;
'@
 $cs="Data Source=$ServerInstance;Initial Catalog=$Database;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=15;Application Name=BuildingManagement-ViewInventory"
 $cn=New-Object System.Data.SqlClient.SqlConnection($cs)
 $cmd=$cn.CreateCommand();$cmd.CommandText=$sql;$cmd.CommandTimeout=180
 $da=New-Object System.Data.SqlClient.SqlDataAdapter($cmd);$ds=New-Object System.Data.DataSet
 try {$cn.Open();[void]$da.Fill($ds)} finally {$da.Dispose();$cmd.Dispose();$cn.Dispose()}
 if($ds.Tables.Count -lt 9){throw "Expected 9 result sets; received $($ds.Tables.Count)."}
 $names=@("01-Views.csv","02-View-Definitions.csv","03-Dependency-Edges.csv","04-View-Consumers.csv","05-Indexed-View-Indexes.csv","06-Synonyms.csv","07-Table-RowCounts-Storage.csv","08-QueryStore-State.csv","09-QueryStore-View-Usage.csv")
 for($i=0;$i -lt $ds.Tables.Count;$i++){
  $ds.Tables[$i] | Export-Csv -LiteralPath (Join-Path $out $names[$i]) -NoTypeInformation -Encoding UTF8
  Write-Host ("{0}: {1} rows" -f $names[$i],$ds.Tables[$i].Rows.Count)
 }
 @"
Read-only SQL view inventory
Generated: $(Get-Date -Format o)
Server: $ServerInstance
Database: $Database
Branch: $ExpectedBranch
Result sets: $($ds.Tables.Count)
SQL Agent jobs, SSRS catalogs, dynamic SQL dependencies and external application consumers require separate inspection.
No schema or data changes are issued by this script.
"@ | Set-Content (Join-Path $out "README.txt") -Encoding UTF8
 git add -- $relative
 if($LASTEXITCODE -ne 0){throw "git add failed"}
 git commit -m "Diagnostics: SQL view inventory $stamp"
 if($LASTEXITCODE -ne 0){throw "git commit failed"}
 git push origin $ExpectedBranch
 if($LASTEXITCODE -ne 0){throw "git push failed; inspect local commit before retry"}
 Write-Host "Results committed and pushed successfully."
 Write-Host "Output: $relative"
} finally {Pop-Location}
