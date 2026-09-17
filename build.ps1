$ErrorActionPreference = 'Stop'
$compilerPath = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path -LiteralPath $compilerPath)) { $compilerPath = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
if (!(Test-Path -LiteralPath $compilerPath)) { throw '未找到 .NET Framework C# 编译器。' }
$outputPath = Join-Path $PSScriptRoot 'bin'
New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
$sourceFiles = Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.cs' -File | Sort-Object Name
if ($sourceFiles.Count -eq 0) { throw '未找到 C# 源文件。' }
& $compilerPath /nologo /target:winexe /optimize+ "/out:$outputPath\轻记账.exe" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Data.dll $sourceFiles.FullName
if ($LASTEXITCODE -ne 0) { throw '编译失败' }
Write-Output "编译完成：$outputPath\轻记账.exe"
