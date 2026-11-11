$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$sources = Get-ChildItem -LiteralPath (Join-Path $projectRoot 'src') -Filter '*.cs' | ForEach-Object { $_.FullName }
$exePath = Join-Path $projectRoot 'BannerCutIntegrado.exe'
$macroPath = Join-Path $projectRoot 'src\BannerCut.bas'
$guidePath = Join-Path $projectRoot 'LEIA-ME.txt'
& $compiler '/nologo' '/target:winexe' ('/out:' + $exePath) '/reference:System.Windows.Forms.dll' '/reference:System.Drawing.dll' '/reference:System.Xml.Linq.dll' '/reference:System.Web.Extensions.dll' ('/resource:' + $macroPath + ',Macro') ('/resource:' + $guidePath + ',Guia') $sources
if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar a integracao.' }
Write-Output $exePath
