<#
.SYNOPSIS
    Builds the HT ISO Creator release packages from one clean Release build.

.DESCRIPTION
    1. Checks that the version is the same in AssemblyInfo.cs, the documentation and this script.
    2. Cleans and builds the solution in Release (any compiler warning stops the build).
    3. Runs the automated tests (skip with -SkipTests).
    4. Stages the files a user needs: program, configuration, DiscUtils DLLs, notices, documentation.
    5. Creates, in artifacts\v<version>\:
         HTISOCreator_Setup_v<version>.exe          Windows installer (Inno Setup 6)
         HTISOCreator_Portable_v<version>.zip       no-install version
         HTISOCreator_Documentation_v<version>.zip  all documentation
         HTISOCreator_Stage4_Complete.zip           complete source code (no build output)
         SHA256SUMS.txt                              SHA-256 of each file above

    Requirements: .NET SDK (for dotnet build/test) and Inno Setup 6 (ISCC.exe).

    -RefreshDocs rebuilds only the documentation and source packages and SHA256SUMS.txt, and keeps the
    installer and portable package that were already built and tested. Use it after changing TEST_REPORT.md
    or developer-only files. It stops if any document that the installer and portable package contain has
    changed since they were built, or if any program source file is newer than the built program.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File Tools\Build-Release.ps1
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File Tools\Build-Release.ps1 -RefreshDocs
#>
param(
    [string]$Version = "1.0.1",
    [switch]$SkipTests,
    [switch]$RefreshDocs,
    [string]$Iscc
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$out = Join-Path $root "artifacts\v$Version"
$stage = Join-Path $out "stage\HTISOCreator"
$binary = Join-Path $root "HTISOCreator\bin\Release\net462"

function Step([string]$text) { Write-Host ""; Write-Host "== $text" -ForegroundColor Cyan }

function Zip([string]$folder, [string]$zipPath) {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    if (Test-Path $zipPath) { Remove-Item $zipPath }
    [IO.Compression.ZipFile]::CreateFromDirectory($folder, $zipPath, [IO.Compression.CompressionLevel]::Optimal, $true)
}

Step "Checking version $Version"
$assemblyInfo = Get-Content (Join-Path $root "HTISOCreator\Properties\AssemblyInfo.cs") -Raw
foreach ($pattern in @("AssemblyVersion(""$Version.0"")", "AssemblyFileVersion(""$Version.0"")", "AssemblyInformationalVersion(""$Version"")")) {
    if (-not $assemblyInfo.Contains($pattern)) { throw "AssemblyInfo.cs does not contain $pattern" }
}
foreach ($doc in "README.md", "docs\RELEASE_NOTES.md") {
    if (-not (Get-Content (Join-Path $root $doc) -Raw).Contains($Version)) { throw "$doc does not mention version $Version" }
}

$userDocs = "README.md", "CHANGELOG.md", "docs\INSTALLATION.md", "docs\USER_GUIDE.md", "docs\FILESYSTEM_GUIDE.md", "docs\TROUBLESHOOTING.md", "docs\RELEASE_NOTES.md", "docs\THIRD_PARTY_NOTICES.md"

if ($RefreshDocs) {
    Step "Checking the existing installer and portable package"
    foreach ($name in "HTISOCreator_Setup_v$Version.exe", "HTISOCreator_Portable_v$Version.zip", "stage\HTISOCreator\HTISOCreator.exe") {
        if (-not (Test-Path (Join-Path $out $name))) { throw "$name was not found in $out. Run a full build first." }
    }
    foreach ($doc in $userDocs) {
        $packed = Join-Path $stage ("Documentation\" + (Split-Path $doc -Leaf))
        if ((Get-FileHash (Join-Path $root $doc)).Hash -ne (Get-FileHash $packed).Hash) {
            throw "$doc has changed since the installer and portable package were built. Run a full build."
        }
    }
    $built = (Get-Item (Join-Path $stage "HTISOCreator.exe")).LastWriteTimeUtc
    $newer = Get-ChildItem (Join-Path $root "HTISOCreator") -Recurse -File -Include *.cs, *.csproj, *.resx, *.config, *.ico |
        Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' -and $_.LastWriteTimeUtc -gt $built }
    if ($newer) { throw "Program source changed after the build ($($newer[0].Name)). Run a full build." }
    if ((Get-FileHash (Join-Path $stage "HTISOCreator.exe")).Hash -ne (Get-FileHash (Join-Path $binary "HTISOCreator.exe")).Hash) {
        throw "The built program differs from the one in the packages. Run a full build."
    }
    "  Installer, portable package and their documents are unchanged; keeping them."
}
else {

if (-not $Iscc) {
    $Iscc = @("$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe", "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe", "$env:ProgramFiles\Inno Setup 6\ISCC.exe") |
        Where-Object { Test-Path $_ } | Select-Object -First 1
}
if (-not $Iscc -or -not (Test-Path $Iscc)) { throw "Inno Setup 6 (ISCC.exe) was not found. Install it from https://jrsoftware.org/isdl.php or pass -Iscc." }

Step "Clean Release build"
Push-Location $root
try {
    & dotnet clean HTISOCreator.sln -c Release -v quiet | Out-Null
    $buildLog = & dotnet build HTISOCreator.sln -c Release -warnaserror 2>&1
    $buildLog | Select-String -Pattern "Warning\(s\)|Error\(s\)|Build succeeded" | ForEach-Object { $_.Line.Trim() }
    if ($LASTEXITCODE -ne 0) { $buildLog | Select-Object -Last 30; throw "The Release build failed." }

    if (-not $SkipTests) {
        Step "Automated tests"
        $testLog = & dotnet test HTISOCreator.sln -c Release --no-build 2>&1
        $testLog | Select-String -Pattern "Passed!|Failed!|^\s+Failed " | ForEach-Object { $_.Line.Trim() }
        if ($LASTEXITCODE -ne 0) { throw "Tests failed; no packages were made." }
    }
}
finally { Pop-Location }

Step "Staging release files"
if (Test-Path $out) { Remove-Item $out -Recurse -Force }
New-Item -ItemType Directory -Force (Join-Path $stage "Documentation") | Out-Null
foreach ($file in "HTISOCreator.exe", "HTISOCreator.exe.config", "DiscUtils.Core.dll", "DiscUtils.Iso9660.dll", "DiscUtils.Streams.dll", "THIRD-PARTY-NOTICES.txt") {
    Copy-Item (Join-Path $binary $file) $stage
}
foreach ($doc in $userDocs) { Copy-Item (Join-Path $root $doc) (Join-Path $stage "Documentation") }
$exeInfo = (Get-Item (Join-Path $stage "HTISOCreator.exe")).VersionInfo
if ($exeInfo.ProductVersion -ne $Version -or $exeInfo.CompanyName -ne "HTREASURE LTD") {
    throw "HTISOCreator.exe reports product version '$($exeInfo.ProductVersion)' by '$($exeInfo.CompanyName)'."
}
Get-ChildItem $stage -Recurse -File | ForEach-Object { "  " + $_.FullName.Substring($stage.Length + 1) }

Step "Portable package"
$portable = Join-Path $out "portable\HTISOCreator_Portable_v$Version"
New-Item -ItemType Directory -Force $portable | Out-Null
Copy-Item "$stage\*" $portable -Recurse
Copy-Item (Join-Path $root "README.md") $portable
Zip $portable (Join-Path $out "HTISOCreator_Portable_v$Version.zip")

Step "Installer"
& $Iscc /Q "/DAppVersion=$Version" "/DSourceDir=$stage" "/DOutputDir=$out" (Join-Path $root "installer\HTISOCreator.iss")
if ($LASTEXITCODE -ne 0) { throw "Inno Setup failed (exit code $LASTEXITCODE)." }
}

Step "Documentation package"
$docs = Join-Path $out "docs\HTISOCreator_Documentation_v$Version"
foreach ($old in (Join-Path $out "docs"), (Join-Path $out "source")) { if (Test-Path $old) { Remove-Item $old -Recurse -Force } }
New-Item -ItemType Directory -Force $docs | Out-Null
foreach ($doc in $userDocs + "docs\TEST_REPORT.md") { Copy-Item (Join-Path $root $doc) $docs }
Zip $docs (Join-Path $out "HTISOCreator_Documentation_v$Version.zip")

Step "Source package"
$source = Join-Path $out "source\HTISOCreator"
& robocopy $root $source /E /XD .git .vs bin obj TestResults artifacts /XF *.user /NFL /NDL /NJH /NJS | Out-Null
if ($LASTEXITCODE -ge 8) { throw "Copying the source failed (robocopy $LASTEXITCODE)." }
Zip $source (Join-Path $out "HTISOCreator_Stage4_Complete.zip")

Step "Checksums"
$packages = "HTISOCreator_Setup_v$Version.exe", "HTISOCreator_Portable_v$Version.zip", "HTISOCreator_Documentation_v$Version.zip", "HTISOCreator_Stage4_Complete.zip"
$lines = foreach ($name in $packages) { (Get-FileHash (Join-Path $out $name) -Algorithm SHA256).Hash.ToLowerInvariant() + " *" + $name }
[IO.File]::WriteAllLines((Join-Path $out "SHA256SUMS.txt"), $lines)
$lines
foreach ($name in $packages) { "{0,-45} {1,12:N0} bytes" -f $name, (Get-Item (Join-Path $out $name)).Length }
Write-Host ""
Write-Host "Release packages are in $out" -ForegroundColor Green
