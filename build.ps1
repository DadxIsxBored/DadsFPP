param([switch]$Package)

$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
$manifestPath = Join-Path $projectRoot 'package\manifest.json'
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
dotnet build (Join-Path $projectRoot 'DadsFPP.csproj') -c Release
if ($LASTEXITCODE -ne 0) { throw "DadsFPP build failed: $LASTEXITCODE" }
$dllPath = Join-Path $projectRoot 'bin\Release\net48\DadsFPP.dll'
$version = [Reflection.AssemblyName]::GetAssemblyName($dllPath).Version
if ("$($version.Major).$($version.Minor).$($version.Build)" -ne $manifest.version_number) { throw 'Assembly and package versions differ.' }
if (-not $Package) { return }
$entries = [ordered]@{
    'DadsFPP.dll' = $dllPath
    'manifest.json' = $manifestPath
    'README.md' = (Join-Path $projectRoot 'README.md')
    'icon.png' = (Join-Path $projectRoot 'package\icon.png')
    'CHANGELOG.md' = (Join-Path $projectRoot 'CHANGELOG.md')
    'LICENSE' = (Join-Path $projectRoot 'LICENSE')
}
foreach ($path in $entries.Values) { if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Missing package file: $path" } }
Add-Type -AssemblyName System.Drawing
$image = [Drawing.Image]::FromFile($entries['icon.png'])
try {
    if ($image.Width -ne 256 -or $image.Height -ne 256 -or $image.RawFormat.Guid -ne [Drawing.Imaging.ImageFormat]::Png.Guid) { throw 'icon.png must be a 256x256 PNG.' }
} finally { $image.Dispose() }
$distRoot = [IO.Path]::GetFullPath((Join-Path $projectRoot 'dist'))
$archiveRoot = Join-Path $projectRoot 'Archive\package-builds'
New-Item -ItemType Directory -Path $distRoot,$archiveRoot -Force | Out-Null
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss-fff'
foreach ($artifact in Get-ChildItem -LiteralPath $distRoot -Force) {
    if ([IO.Path]::GetDirectoryName($artifact.FullName) -ne $distRoot) { throw 'Artifact is outside the distribution directory.' }
    Move-Item -LiteralPath $artifact.FullName -Destination (Join-Path $archiveRoot ($artifact.Name + '-' + $stamp))
}
$folder = Join-Path $distRoot "DadsFPP-$($manifest.version_number)"
New-Item -ItemType Directory -Path $folder | Out-Null
foreach ($entry in $entries.GetEnumerator()) { Copy-Item -LiteralPath $entry.Value -Destination (Join-Path $folder $entry.Key) }
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zipPath = Join-Path $distRoot "DadsFPP-$($manifest.version_number).zip"
[IO.Compression.ZipFile]::CreateFromDirectory($folder, $zipPath, [IO.Compression.CompressionLevel]::Optimal, $false)
$zip = [IO.Compression.ZipFile]::OpenRead($zipPath)
try {
    foreach ($name in $entries.Keys) { if ($null -eq $zip.GetEntry($name)) { throw "ZIP missing $name" } }
    if ($zip.Entries.Count -ne $entries.Count) { throw 'ZIP contains unexpected files.' }
} finally { $zip.Dispose() }
Write-Output "Thunderstore package: $zipPath"
Write-Output "SHA256: $((Get-FileHash -LiteralPath $zipPath).Hash)"
