param(
    [Parameter(Mandatory = $true)][string]$ZipPath,
    [Parameter(Mandatory = $true)][string]$DownloadUrl,
    [Parameter(Mandatory = $true)][string]$OutputPath
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.IO.Compression.FileSystem

$header = [System.IO.File]::ReadAllBytes($ZipPath)
if ($header.Length -lt 2 -or $header[0] -ne 0x50 -or $header[1] -ne 0x4B) {
    throw "Refusing to publish a subscription entry. $ZipPath is not a zip archive."
}

$zip = [System.IO.Compression.ZipFile]::OpenRead($ZipPath)
try {
    $manifestEntry = $zip.Entries | Where-Object { $_.FullName -eq "MoguangJianying.json" } | Select-Object -First 1
    if ($null -eq $manifestEntry) {
        throw "MoguangJianying.json is missing from the release archive."
    }

    $reader = New-Object System.IO.StreamReader($manifestEntry.Open())
    try { $manifest = $reader.ReadToEnd() | ConvertFrom-Json } finally { $reader.Dispose() }
}
finally {
    $zip.Dispose()
}

if ($manifest.InternalName -ne "MoguangJianying") {
    throw "Release manifest InternalName is '$($manifest.InternalName)'."
}
if ([string]::IsNullOrWhiteSpace($manifest.AssemblyVersion) -or [string]::IsNullOrWhiteSpace([string]$manifest.DalamudApiLevel)) {
    throw "Release manifest is missing AssemblyVersion or DalamudApiLevel."
}

$entry = [ordered]@{
    Author = $manifest.Author
    Name = $manifest.Name
    Punchline = $manifest.Punchline
    Description = $manifest.Description
    Changelog = $manifest.Changelog
    Tags = @($manifest.Tags)
    CategoryTags = @($manifest.CategoryTags)
    InternalName = $manifest.InternalName
    AssemblyVersion = $manifest.AssemblyVersion
    TestingAssemblyVersion = $manifest.AssemblyVersion
    RepoUrl = $manifest.RepoUrl
    ApplicableVersion = $manifest.ApplicableVersion
    DalamudApiLevel = $manifest.DalamudApiLevel
    TestingDalamudApiLevel = $manifest.DalamudApiLevel
    DownloadLinkInstall = $DownloadUrl
    DownloadLinkUpdate = $DownloadUrl
    DownloadLinkTesting = $DownloadUrl
    LoadRequiredState = $manifest.LoadRequiredState
    LoadSync = $manifest.LoadSync
    LoadPriority = $manifest.LoadPriority
    CanUnloadAsync = $manifest.CanUnloadAsync
    AcceptsFeedback = $false
    IsTestingExclusive = $true
    DownloadCount = 0
}

$json = ConvertTo-Json -InputObject @($entry) -Depth 6
[System.IO.File]::WriteAllText($OutputPath, $json + "`n", [System.Text.UTF8Encoding]::new($false))
Write-Output "Wrote $OutputPath for $($manifest.InternalName) $($manifest.AssemblyVersion)"
