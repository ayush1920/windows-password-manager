# PowerShell build and run script for KeyCraft
$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir
Set-Location $projectRoot

$cscPath = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if (-not (Test-Path $cscPath)) {
    Write-Error "C# compiler not found at $cscPath"
    exit 1
}

# Ensure output directories exist
if (-not (Test-Path "$projectRoot\bin")) { New-Item -ItemType Directory -Path "$projectRoot\bin" | Out-Null }
if (-not (Test-Path "$projectRoot\data")) { New-Item -ItemType Directory -Path "$projectRoot\data" | Out-Null }

$outputPath = "$projectRoot\bin\PasswordManager.exe"
# Stop any running instances to avoid file lock
Get-Process -Name "PasswordManager" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 300

$sourceFiles = Get-ChildItem -Path "$projectRoot" -Filter "*.cs" | ForEach-Object { $_.FullName }

Write-Host "Compiling source files to bin\PasswordManager.exe..." -ForegroundColor Cyan
& $cscPath /nologo /target:winexe /out:$outputPath /reference:System.dll,System.Drawing.dll,System.Windows.Forms.dll $sourceFiles

if ($LASTEXITCODE -eq 0) {
    Write-Host "Build Succeeded! Starting bin\PasswordManager.exe..." -ForegroundColor Green
    Start-Process -FilePath $outputPath
    Write-Host "PasswordManager launched successfully." -ForegroundColor Cyan
} else {
    Write-Host "Compilation failed with exit code $LASTEXITCODE" -ForegroundColor Red
}
