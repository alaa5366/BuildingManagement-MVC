# مسار الملفات
$files = @(
    "Resources\Shared.resx",
    "Resources\Shared.ar.resx",
    "Resources\Shared.en.resx",
    "Resources\Shared.fr.resx",
    "Resources\Shared.de.resx"
)

foreach ($file in $files) {
    if (-not (Test-Path $file)) { continue }
    
    Write-Host "Processing: $file" -ForegroundColor Cyan
    
    # اقرأ الملف
    [xml]$xml = Get-Content $file -Encoding UTF8
    
    # شوف المفاتيح المكررة
    $dataNodes = $xml.root.data
    $names = @{}
    $toRemove = @()
    
    foreach ($node in $dataNodes) {
        $name = $node.name
        if ($names.ContainsKey($name)) {
            $toRemove += $node
            Write-Host "  Duplicate found: $name" -ForegroundColor Yellow
        } else {
            $names[$name] = $true
        }
    }
    
    # امسح المكرر
    foreach ($node in $toRemove) {
        $node.ParentNode.RemoveChild($node) | Out-Null
    }
    
    # احفظ الملف
    $xml.Save((Resolve-Path $file))
    Write-Host "  Removed $($toRemove.Count) duplicates" -ForegroundColor Green
}

Write-Host "Done!" -ForegroundColor Green