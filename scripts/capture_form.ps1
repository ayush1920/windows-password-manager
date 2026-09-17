Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptRoot
Set-Location $projectRoot

# Compile test runner or load assembly
$assembly = [System.Reflection.Assembly]::LoadFile("$projectRoot\bin\PasswordManager.exe")
$formType = $assembly.GetType("PasswordGui.MainForm")
$serviceType = $assembly.GetType("PasswordGui.CredentialService")
$repoType = $assembly.GetType("PasswordGui.CredentialRepository")

$repo = [Activator]::CreateInstance($repoType, "$projectRoot\data\credentials.txt")
$service = [Activator]::CreateInstance($serviceType, $repo)
$form = [Activator]::CreateInstance($formType, $service)

$form.Show()
[System.Windows.Forms.Application]::DoEvents()
Start-Sleep -Milliseconds 500

$bmp = New-Object System.Drawing.Bitmap $form.Width, $form.Height
$form.DrawToBitmap($bmp, (New-Object System.Drawing.Rectangle 0, 0, $form.Width, $form.Height))
$bmp.Save("$projectRoot\screenshot.png", [System.Drawing.Imaging.ImageFormat]::Png)

$form.Close()
Write-Output "Screenshot successfully saved to screenshot.png ($($bmp.Width)x$($bmp.Height))"
