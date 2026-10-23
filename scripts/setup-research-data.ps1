
$ErrorActionPreference = 'Stop'

# Resolve the repository root from this script's location.
$repoRoot = Split-Path -Parent $PSScriptRoot

# Expected research-data paths.
$researchDatabase = Join-Path $repoRoot 'data\gdelt-lab.db'
$databaseZip = Join-Path $repoRoot 'gdelt-lab-database.zip'

$prototypeDatabase = Join-Path $repoRoot 'research-data\news-feed\news-feed.db'
$themeDictionary = Join-Path $repoRoot 'research-data\reference-data\gkg-theme-dictionary.json'
$themeLabels = Join-Path $repoRoot 'frontend\NewsFeed.Client\public\data\gkg-theme-labels.json'

$hasErrors = $false

function Test-RequiredFile {
    param(
        [string]$Path,
        [string]$Description
    )

    if (Test-Path -LiteralPath $Path -PathType Leaf) {
        Write-Host "[OK] $Description" -ForegroundColor Green
        return $true
    }

    Write-Host "[MISSING] $Description" -ForegroundColor Red
    Write-Host "         Expected: $Path" -ForegroundColor DarkGray
    return $false
}

Write-Host ''
Write-Host '========================================' -ForegroundColor Cyan
Write-Host ' GDELT-Lab Research Data Setup' -ForegroundColor Cyan
Write-Host '========================================' -ForegroundColor Cyan
Write-Host ''

# Check the files committed to GitHub.
if (-not (Test-RequiredFile -Path $prototypeDatabase -Description 'Prototype news-feed database')) {
    $hasErrors = $true
}

if (-not (Test-RequiredFile -Path $themeDictionary -Description 'Full GDELT theme dictionary')) {
    $hasErrors = $true
}

if (-not (Test-RequiredFile -Path $themeLabels -Description 'Frontend theme labels')) {
    $hasErrors = $true
}

Write-Host ''
Write-Host 'Checking the large GDELT research database...' -ForegroundColor Cyan

# Only extract the ZIP if the database does not already exist.
if (Test-Path -LiteralPath $researchDatabase -PathType Leaf) {
    Write-Host '[OK] GDELT research database already exists. No extraction needed.' -ForegroundColor Green
}
elseif (Test-Path -LiteralPath $databaseZip -PathType Leaf) {
    Write-Host 'Found gdelt-lab-database.zip. Extracting database...' -ForegroundColor Yellow

    $dataDirectory = Split-Path -Parent $researchDatabase
    $tempDirectory = Join-Path $env:TEMP "GDELT-Lab-setup-$PID"

    try {
        New-Item -ItemType Directory -Path $dataDirectory -Force | Out-Null
        New-Item -ItemType Directory -Path $tempDirectory -Force | Out-Null

        Expand-Archive -LiteralPath $databaseZip -DestinationPath $tempDirectory

        # Locate the database regardless of its folder inside the ZIP.
        $extractedDatabase = Get-ChildItem `
            -LiteralPath $tempDirectory `
            -Filter 'gdelt-lab.db' `
            -File `
            -Recurse |
            Select-Object -First 1

        if ($null -eq $extractedDatabase) {
            throw 'The ZIP archive does not contain a file named gdelt-lab.db.'
        }

        # Do not overwrite an existing database.
        if (Test-Path -LiteralPath $researchDatabase) {
            throw "The destination already exists: $researchDatabase"
        }

        Copy-Item -LiteralPath $extractedDatabase.FullName -Destination $researchDatabase

        Write-Host '[OK] GDELT research database extracted successfully.' -ForegroundColor Green
    }
    catch {
        Write-Host "[ERROR] Could not prepare the research database: $($_.Exception.Message)" -ForegroundColor Red
        $hasErrors = $true
    }
    finally {
        if (Test-Path -LiteralPath $tempDirectory) {
            Remove-Item -LiteralPath $tempDirectory -Recurse -Force
        }
    }
}
else {
    Write-Host '[MISSING] Large GDELT research database.' -ForegroundColor Yellow
    Write-Host 'Download gdelt-lab-database.zip from the shared OneDrive folder.'
    Write-Host "Place the ZIP here: $databaseZip"
    Write-Host 'Then run this script again.'
    $hasErrors = $true
}

Write-Host ''
Write-Host '========================================' -ForegroundColor Cyan

if ($hasErrors) {
    Write-Host 'Setup is incomplete. Resolve the missing files or errors above.' -ForegroundColor Yellow
    exit 1
}

Write-Host 'All expected research files are available.' -ForegroundColor Green
Write-Host 'You can now proceed with the project setup instructions in the README.'
Write-Host '========================================' -ForegroundColor Cyan
