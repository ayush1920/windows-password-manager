# KeyCraft Test Suite Runner (Tier 1 & Tier 2)
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
Write-Host "             KEYCRAFT AUTOMATED TEST SUITE RUNNER                 " -ForegroundColor Cyan
Write-Host "==================================================================" -ForegroundColor Cyan

# 1. Compile and Run Tier 1
Write-Host "`n[1/2] Compiling & Executing Tier 1 (Engine & Functional Suite)..." -ForegroundColor Yellow

$tier1Sources = @(
    "$projectRoot\SafeFileStorage.cs",
    "$projectRoot\VaultSecurity.cs",
    "$projectRoot\Credential.cs",
    "$projectRoot\CredentialRepository.cs",
    "$projectRoot\CredentialService.cs",
    "$projectRoot\KeyboardShortcutManager.cs",
    "$projectRoot\SingleInstanceController.cs",
    "$projectRoot\NativeMethods.cs",
    "$projectRoot\GlobalHotkeyManager.cs",
    "$projectRoot\AppSettings.cs",
    "$projectRoot\tests\TestRunner_Tier1.cs"
)

& $cscPath /nologo /target:exe /out:"$projectRoot\bin\TestRunner_Tier1.exe" /reference:System.dll,System.Drawing.dll,System.Windows.Forms.dll,System.Core.dll $tier1Sources
if ($LASTEXITCODE -ne 0) {
    Write-Host "[FAIL] Failed to compile Tier 1 test runner." -ForegroundColor Red
    exit 1
}

& "$projectRoot\bin\TestRunner_Tier1.exe"
$tier1Exit = $LASTEXITCODE

# 2. Compile and Run Tier 2
Write-Host "`n[2/2] Compiling & Executing Tier 2 (End-to-End UI Automation Suite)..." -ForegroundColor Yellow

$coreAppSources = Get-ChildItem -Path "$projectRoot" -Filter "*.cs" | Where-Object { $_.Name -ne "Program.cs" } | ForEach-Object { $_.FullName }
$tier2Sources = $coreAppSources + @("$projectRoot\tests\TestRunner_Tier2.cs")

& $cscPath /nologo /target:exe /out:"$projectRoot\bin\TestRunner_Tier2.exe" /reference:System.dll,System.Drawing.dll,System.Windows.Forms.dll,System.Core.dll $tier2Sources
if ($LASTEXITCODE -ne 0) {
    Write-Host "[FAIL] Failed to compile Tier 2 test runner." -ForegroundColor Red
    exit 1
}

& "$projectRoot\bin\TestRunner_Tier2.exe"
$tier2Exit = $LASTEXITCODE

Write-Host "`n==================================================================" -ForegroundColor Cyan
if ($tier1Exit -eq 0 -and $tier2Exit -eq 0) {
    Write-Host "       ALL TEST SUITES (TIER 1 & TIER 2) PASSED SUCCESSFULLY!     " -ForegroundColor Green
    Write-Host "==================================================================" -ForegroundColor Cyan
    exit 0
} else {
    Write-Host "       TEST SUITE RUN COMPLETED WITH FAILURES!                    " -ForegroundColor Red
    Write-Host "==================================================================" -ForegroundColor Cyan
    exit 1
}
