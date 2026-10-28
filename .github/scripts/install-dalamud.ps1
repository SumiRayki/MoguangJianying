$ErrorActionPreference = "Stop"
$pin = Get-Content -Raw -Path (Join-Path $PSScriptRoot "..\dalamud-pin.json") | ConvertFrom-Json
$root = Join-Path $env:RUNNER_TEMP "dalamud"
New-Item -ItemType Directory -Force -Path $root | Out-Null

$versionFile = Join-Path $root "version"
Invoke-WebRequest -Uri $pin.distributionVersion -OutFile $versionFile
$raw = (Get-Content -Raw -Path $versionFile).Trim().TrimStart([char]0xFEFF)
if ($raw.StartsWith("{")) {
    $version = $raw | ConvertFrom-Json
} else {
    $encoded = [regex]::Replace($raw, '[^A-Za-z0-9+/=]', '')
    $version = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($encoded)) | ConvertFrom-Json
}

if ($version.AssemblyVersion -ne $pin.assemblyVersion) {
    throw "Dalamud $($version.AssemblyVersion) does not match pinned $($pin.assemblyVersion)."
}
if ($version.SupportedGameVer -ne $pin.supportedGameVer) {
    throw "Game $($version.SupportedGameVer) does not match pinned $($pin.supportedGameVer)."
}
if ($version.GitSha -ne $pin.gitSha) {
    throw "Dalamud git $($version.GitSha) does not match pinned $($pin.gitSha)."
}

$archive = Join-Path $root "dalamud.zip"
Invoke-WebRequest -Uri $pin.distributionZip -OutFile $archive
$header = [System.IO.File]::ReadAllBytes($archive)
if ($header.Length -lt 2 -or $header[0] -ne 0x50 -or $header[1] -ne 0x4B) {
    throw "Dalamud download is not a zip archive."
}

$sdk = Join-Path $root "sdk"
Expand-Archive -Path $archive -DestinationPath $sdk -Force
if (-not (Test-Path (Join-Path $sdk "Dalamud.dll"))) {
    throw "Dalamud.dll is missing from the pinned distribution."
}

"DALAMUD_HOME=$sdk" | Out-File -FilePath $env:GITHUB_ENV -Encoding utf8 -Append
Write-Output "Pinned Dalamud $($version.AssemblyVersion) for game $($version.SupportedGameVer)."
