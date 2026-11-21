$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$root='D:\Projects\BuildingManagement-MVC'
$out=Join-Path $root 'Tests\BuildingManagement.Tests\TestData\Receipts'
New-Item -ItemType Directory -Force -Path $out | Out-Null
$specJson=@'
[
 {"file":"instapay-success-en.png","title":"INSTANT TRANSFER","accent":"#5630A5","lines":[
  {"text":"Transfer successful","size":38,"bold":true},
  {"text":"EGP 1,250.75","size":58,"bold":true},
  {"text":"From: 01000000001","size":30},
  {"text":"To: Wallet 01000000002","size":30},
  {"text":"Reference: IP-20261121-0001","size":30},
  {"text":"Date: 21/11/2026 14:35","size":30},
  {"text":"Status: Successful","size":34,"bold":true}
 ]},
 {"file":"wallet-failed-ar.png","title":"WALLET TRANSFER","accent":"#B42318","lines":[
  {"text":"\u0641\u0634\u0644 \u0627\u0644\u062a\u062d\u0648\u064a\u0644","size":42,"bold":true,"rtl":true},
  {"text":"\u0627\u0644\u0645\u0628\u0644\u063a: \u0664\u0665\u0660\u066b\u0660\u0660 \u062c\u0646\u064a\u0647","size":42,"bold":true,"rtl":true},
  {"text":"\u0631\u0642\u0645 \u0627\u0644\u0639\u0645\u0644\u064a\u0629: WLT-20261121-0003","size":30,"rtl":true},
  {"text":"\u0627\u0644\u0633\u0628\u0628: \u0627\u0644\u0631\u0635\u064a\u062f \u063a\u064a\u0631 \u0643\u0627\u0641\u064d","size":30,"rtl":true},
  {"text":"\u0627\u0644\u062d\u0627\u0644\u0629: \u0645\u0631\u0641\u0648\u0636","size":36,"bold":true,"rtl":true}
 ]},
 {"file":"wallet-pending-en.png","title":"MOBILE WALLET","accent":"#B86A00","lines":[
  {"text":"Transfer pending","size":40,"bold":true},
  {"text":"Amount: EGP 875.00","size":50,"bold":true},
  {"text":"Reference: WLT-20261121-0004","size":30},
  {"text":"Recipient: Example Wallet","size":30},
  {"text":"Date: 21 Nov 2026 16:20","size":30},
  {"text":"Status: Pending","size":36,"bold":true}
 ]},
 {"file":"bank-transfer-success-en.png","title":"BANK TRANSFER CONFIRMATION","accent":"#135A8A","lines":[
  {"text":"Transfer completed successfully","size":36,"bold":true},
  {"text":"Amount: EGP 12,500.75","size":52,"bold":true},
  {"text":"Source account: 000123456789","size":30},
  {"text":"Beneficiary account: 000987654321","size":30},
  {"text":"Reference number: BNK-20261121-0007","size":30},
  {"text":"Date: 21 Nov 2026 15:42","size":30},
  {"text":"Status: Completed","size":36,"bold":true}
 ]},
 {"file":"bank-transfer-failed-ar.png","title":"BANK TRANSFER","accent":"#9B1C1C","lines":[
  {"text":"\u062a\u0645 \u0631\u0641\u0636 \u0627\u0644\u062a\u062d\u0648\u064a\u0644","size":42,"bold":true,"rtl":true},
  {"text":"\u0627\u0644\u0645\u0628\u0644\u063a: \u0662\u0665\u0660\u0660\u066b\u0660\u0660 \u062c\u0646\u064a\u0647","size":42,"bold":true,"rtl":true},
  {"text":"\u0631\u0642\u0645 \u0627\u0644\u0645\u0631\u062c\u0639: BNK-20261121-0008","size":30,"rtl":true},
  {"text":"\u0627\u0644\u0633\u0628\u0628: \u062a\u0639\u0630\u0631 \u0625\u062a\u0645\u0627\u0645 \u0627\u0644\u0639\u0645\u0644\u064a\u0629","size":30,"rtl":true},
  {"text":"\u0627\u0644\u062d\u0627\u0644\u0629: \u0645\u0631\u0641\u0648\u0636","size":36,"bold":true,"rtl":true}
 ]}
]
'@
$specs=ConvertFrom-Json -InputObject $specJson
foreach($spec in $specs){
 $bitmap=New-Object System.Drawing.Bitmap(1080,1500)
 $graphics=[System.Drawing.Graphics]::FromImage($bitmap)
 try{
  $graphics.SmoothingMode=[System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
  $graphics.TextRenderingHint=[System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
  $graphics.Clear([System.Drawing.Color]::FromArgb(246,248,251))
  $accent=[System.Drawing.ColorTranslator]::FromHtml($spec.accent)
  $headerBrush=New-Object System.Drawing.SolidBrush($accent)
  $graphics.FillRectangle($headerBrush,0,0,1080,190)
  $headerFont=New-Object System.Drawing.Font('Segoe UI',34,[System.Drawing.FontStyle]::Bold)
  $graphics.DrawString([string]$spec.title,$headerFont,[System.Drawing.Brushes]::White,55,65)
  $cardBrush=New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
  $graphics.FillRectangle($cardBrush,45,220,990,1200)
  $y=275
  foreach($line in $spec.lines){
   $style=if($line.bold){[System.Drawing.FontStyle]::Bold}else{[System.Drawing.FontStyle]::Regular}
   $fontName=if($line.rtl){'Tahoma'}else{'Segoe UI'}
   $font=New-Object System.Drawing.Font($fontName,[single]$line.size,$style)
   $brush=if($line.bold -and $y -lt 500){New-Object System.Drawing.SolidBrush($accent)}else{[System.Drawing.Brushes]::Black}
   $format=New-Object System.Drawing.StringFormat
   if($line.rtl){$format.FormatFlags=[System.Drawing.StringFormatFlags]::DirectionRightToLeft;$format.Alignment=[System.Drawing.StringAlignment]::Far}else{$format.Alignment=[System.Drawing.StringAlignment]::Near}
   $rect=New-Object System.Drawing.RectangleF(85,[single]$y,910,90)
   $graphics.DrawString([string]$line.text,$font,$brush,$rect,$format)
   $y += 145
   $format.Dispose();$font.Dispose()
   if($brush -is [System.IDisposable] -and $brush -ne [System.Drawing.Brushes]::Black){$brush.Dispose()}
  }
  $headerBrush.Dispose();$headerFont.Dispose();$cardBrush.Dispose()
  $bitmap.Save((Join-Path $out $spec.file),[System.Drawing.Imaging.ImageFormat]::Png)
 } finally {$graphics.Dispose();$bitmap.Dispose()}
}
Write-Output 'Synthetic receipt fixtures generated:'
Get-ChildItem $out -File | Select-Object Name,Length
