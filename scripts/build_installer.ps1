# Automated Build Script for BabyKeyBoardSmash Setup Installer
# Builds the game, zips the payload, and produces BabyKeyBoardSmash_Setup.exe in dist/

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $PSScriptRoot

Write-Host "==> 1. Publishing BabyKeyBoardSmash app..." -ForegroundColor Cyan
dotnet publish "$RepoRoot\src\BabySmashBN\BabySmashBN.csproj" `
    -c Release `
    -r win-x64 `
    --self-contained `
    -p:PublishSingleFile=true `
    -o "$RepoRoot\dist\app"

Write-Host "==> 2. Compressing payload for installer..." -ForegroundColor Cyan
$PayloadZip = "$RepoRoot\src\BabyKeyBoardSmash.Installer\app_payload.zip"
if (Test-Path $PayloadZip) {
    Remove-Item -Force $PayloadZip
}

Compress-Archive -Path "$RepoRoot\dist\app\*" -DestinationPath $PayloadZip -CompressionLevel Optimal
Copy-Item $PayloadZip "$RepoRoot\dist\app_payload.zip" -Force

Write-Host "==> 3. Publishing BabyKeyBoardSmash_Setup installer..." -ForegroundColor Cyan
dotnet publish "$RepoRoot\src\BabyKeyBoardSmash.Installer\BabyKeyBoardSmash.Installer.csproj" `
    -c Release `
    -r win-x64 `
    --self-contained `
    -p:PublishSingleFile=true `
    -o "$RepoRoot\dist\installer_bin"

Copy-Item "$RepoRoot\dist\installer_bin\BabyKeyBoardSmash_Setup.exe" "$RepoRoot\dist\BabyKeyBoardSmash_Setup.exe" -Force
Remove-Item -Recurse -Force "$RepoRoot\dist\installer_bin" -ErrorAction SilentlyContinue

Write-Host "`n✅ Build Successful!" -ForegroundColor Green
Write-Host "Installer created at: $RepoRoot\dist\BabyKeyBoardSmash_Setup.exe" -ForegroundColor Yellow
