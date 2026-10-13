
param(
    [int]$HoursBack = 24
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$dataRoot = Join-Path $root "research-data"

$archiveRoot = Join-Path $dataRoot "archives"
$masterListPath = Join-Path $dataRoot "MASTERFILELIST.TXT"

$datasets = @(
    @{
        Name = "events"
        Folder = "events"
        Pattern = "\.export\.CSV\.zip$"
    },
    @{
        Name = "mentions"
        Folder = "mentions"
        Pattern = "\.mentions\.CSV\.zip$"
    },
    @{
        Name = "gkg"
        Folder = "gkg"
        Pattern = "\.gkg\.csv\.zip$"
    }
)

New-Item -ItemType Directory -Force -Path $archiveRoot |
    Out-Null

$masterUrl = "http://data.gdeltproject.org/gdeltv2/masterfilelist.txt"

Write-Host "Downloading GDELT master file list..."

Invoke-WebRequest `
    -Uri $masterUrl `
    -OutFile $masterListPath

$cutoff = [DateTime]::UtcNow.AddHours(-$HoursBack)

$lines = Get-Content $masterListPath

foreach ($dataset in $datasets) {
    $datasetArchiveFolder = Join-Path `
        $archiveRoot `
        $dataset.Folder

    New-Item `
        -ItemType Directory `
        -Force `
        -Path $datasetArchiveFolder |
        Out-Null

    Write-Host ""
    Write-Host "Processing $($dataset.Name)..."

    $matches = foreach ($line in $lines) {
        $parts = $line -split "\s+"

        if ($parts.Count -lt 3) {
            continue
        }

        $url = $parts[-1]
        $fileName = [System.IO.Path]::GetFileName($url)

        if ($fileName -notmatch $dataset.Pattern) {
            continue
        }

        if ($fileName -notmatch '^(\d{14})') {
            continue
        }

        $timestamp = [DateTime]::ParseExact(
            $Matches[1],
            "yyyyMMddHHmmss",
            [Globalization.CultureInfo]::InvariantCulture,
            [Globalization.DateTimeStyles]::AssumeUniversal
        ).ToUniversalTime()

        if ($timestamp -ge $cutoff) {
            [PSCustomObject]@{
                Timestamp = $timestamp
                Url = $url
                FileName = $fileName
            }
        }
    }

    $matches = @($matches | Sort-Object Timestamp -Descending)

    Write-Host "Matching archives: $($matches.Count)"

    foreach ($item in $matches) {
        $destination = Join-Path `
            $datasetArchiveFolder `
            $item.FileName

        if (Test-Path $destination) {
            continue
        }

        Write-Host "Downloading $($item.FileName)"

        try {
            Invoke-WebRequest `
                -Uri $item.Url `
                -OutFile "$destination.partial"

            Move-Item `
                -Path "$destination.partial" `
                -Destination $destination
        }
        catch {
            Remove-Item "$destination.partial" `
                -Force `
                -ErrorAction SilentlyContinue

            Write-Warning "Failed to download $($item.Url): $_"
        }
    }
}

Write-Host ""
Write-Host "Download pass complete."
Write-Host "Archives: $archiveRoot"
