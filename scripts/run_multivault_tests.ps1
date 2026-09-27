# KeyCraft KeePass Multi-Vault Architecture Automated Test Runner
$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir
Set-Location $projectRoot

$cscPath = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $cscPath)) {
    Write-Error "C# compiler not found at $cscPath"
    exit 1
}

if (-not (Test-Path "$projectRoot\bin")) { New-Item -ItemType Directory -Path "$projectRoot\bin" | Out-Null }

Write-Host "==================================================================" -ForegroundColor Cyan
Write-Host "       KEYCRAFT KEEPASS MULTI-VAULT TEST SUITE RUNNER             " -ForegroundColor Cyan
Write-Host "==================================================================" -ForegroundColor Cyan

Write-Host "`nCompiling Multi-Vault Test Runner (TestRunner_MultiVault.cs)..." -ForegroundColor Yellow

$coreAppSources = Get-ChildItem -Path "$projectRoot" -Filter "*.cs" | Where-Object { $_.Name -ne "Program.cs" } | ForEach-Object { $_.FullName }
$sources = $coreAppSources + @("$projectRoot\tests\TestRunner_MultiVault.cs")

& $cscPath /nologo /target:exe /out:"$projectRoot\bin\TestRunner_MultiVault.exe" /reference:System.dll,System.Drawing.dll,System.Windows.Forms.dll,System.Core.dll $sources
if ($LASTEXITCODE -ne 0) {
    Write-Host "[FAIL] Failed to compile Multi-Vault test runner." -ForegroundColor Red
    exit 1
}

Write-Host "[OK] Compilation successful. Running tests...`n" -ForegroundColor Green

& "$projectRoot\bin\TestRunner_MultiVault.exe"
$testExit = $LASTEXITCODE

Write-Host "`n==================================================================" -ForegroundColor Cyan
if ($testExit -eq 0) {
    Write-Host "       ALL MULTI-VAULT TESTS PASSED SUCCESSFULLY!                 " -ForegroundColor Green
    Write-Host "==================================================================" -ForegroundColor Cyan
    exit 0
} else {
    Write-Host "       MULTI-VAULT TEST RUN COMPLETED WITH FAILURES!              " -ForegroundColor Red
    Write-Host "==================================================================" -ForegroundColor Cyan
    exit 1
}
