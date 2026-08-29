<# :
@echo off
setlocal
set "BAT_DIR=%~dp0"
cd /d "%BAT_DIR%"

echo ======================================================================
echo             YOGURTING MODERN SERVER - ACCOUNT CREATOR                 
echo ======================================================================
echo.

set /p "ACC_USER=Enter Username (Account ID): "
if "%ACC_USER%"=="" (
    echo [Error] Username cannot be empty!
    pause
    exit /b 1
)

set /p "ACC_PASS=Enter Password: "
if "%ACC_PASS%"=="" (
    echo [Error] Password cannot be empty!
    pause
    exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -Command "$env:ACC_USER='%ACC_USER%'; $env:ACC_PASS='%ACC_PASS%'; $env:BAT_DIR='%BAT_DIR%'; iex ((Get-Content -LiteralPath '%~f0') -join [Environment]::NewLine)"
echo.
pause
exit /b
#>

$user = $env:ACC_USER
$pass = $env:ACC_PASS

$md5 = [System.Security.Cryptography.MD5]::Create()
$hashBytes = $md5.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($pass))
$hashStr = [BitConverter]::ToString($hashBytes).Replace('-', '').ToLower()

$account = @{
    AccountId = $user
    CharacterName = ""
    PasswordHash = $hashStr
    HasCharacter = $false
    AuthType = "normal"
}

$json = $account | ConvertTo-Json -Depth 5

$baseDir = $env:BAT_DIR
if (-not $baseDir) { $baseDir = (Get-Location).Path }

$paths = @(
    (Join-Path $baseDir 'data\save'),
    (Join-Path $baseDir 'src\Yogurting.Server\bin\Debug\net8.0\data\save'),
    (Join-Path $baseDir 'src\Yogurting.Server\data\save'),
    (Join-Path (Split-Path $baseDir -Parent) 'server_modern\data\save')
)

Write-Host ""
Write-Host ("-" * 60)
Write-Host " SUCCESS! Account '$user' has been created."
Write-Host " Password MD5: $hashStr"
Write-Host " Initial State: HasCharacter = False (Triggers 3D Character Creator)"
Write-Host " Starter Gear: Loaded dynamically from 'starter_items.json' on creation."
Write-Host ("-" * 60)

$seen = @{}
foreach ($dir in $paths) {
    $parent = Split-Path -Parent $dir
    if (Test-Path $parent) {
        [System.IO.Directory]::CreateDirectory($dir) | Out-Null
        $targetFile = Join-Path $dir "$user.json"
        if (-not $seen.ContainsKey($targetFile)) {
            [System.IO.File]::WriteAllText($targetFile, $json, [System.Text.Encoding]::UTF8)
            Write-Host "  Saved to: $targetFile"
            $seen[$targetFile] = $true
        }
    }
}
Write-Host ("=" * 60)
