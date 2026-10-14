
param(
    # Number of hours of recent GDELT archives to look for.
    # Defaults to the previous 24 hours.
    [int]$HoursBack = 24
)

# Stop execution when a PowerShell command produces a terminating error.
$ErrorActionPreference = "Stop"

# Resolve the repository root relative to this script's directory.
$root = Split-Path -Parent $PSScriptRoot
$dataRoot = Join-Path $root "research-data"

# Define locations for downloaded ZIP archives and the GDELT master file list.
$archiveRoot = Join-Path $dataRoot "archives"
$masterListPath = Join-Path $dataRoot "MASTERFILELIST.TXT"

# Configure the filename patterns and destination folders for each dataset.
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

# Create the archive directory if it does not already exist.
New-Item -ItemType Directory -Force -Path $archiveRoot |
    Out-Null

# GDELT's master file list contains URLs for available dataset archives.
$masterUrl = "http://data.gdeltproject.org/gdeltv2/masterfilelist.txt"

Write-Host "Downloading GDELT master file list..."

# Download the latest master file list to the research-data directory.
Invoke-WebRequest `
    -Uri $masterUrl `
    -OutFile $masterListPath

# Calculate the UTC cutoff used to select recent archives.
$cutoff = [DateTime]::UtcNow.AddHours(-$HoursBack)

# Load the master file list for filtering by dataset and timestamp.
$lines = Get-Content $masterListPath

foreach ($dataset in $datasets) {
    # Create a separate archive directory for the current dataset.
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

    # Find archives matching the dataset's filename pattern and time window.
    $matches = foreach ($line in $lines) {
        # Master file list entries contain metadata followed by a URL.
        $parts = $line -split "\s+"

        # Ignore entries that do not contain the expected minimum fields.
        if ($parts.Count -lt 3) {
            continue
        }

        # Extract the archive URL and its filename.
        $url = $parts[-1]
        $fileName = [System.IO.Path]::GetFileName($url)

        # Keep only archives belonging to the current dataset.
        if ($fileName -notmatch $dataset.Pattern) {
            continue
        }

        # Extract the 14-digit timestamp at the beginning of the filename.
        if ($fileName -notmatch '^(\d{14})') {
            continue
        }

        # Parse the filename timestamp using the expected GDELT format
        # and convert it to UTC for comparison with the cutoff.
        $timestamp = [DateTime]::ParseExact(
            $Matches[1],
            "yyyyMMddHHmmss",
            [Globalization.CultureInfo]::InvariantCulture,
            [Globalization.DateTimeStyles]::AssumeUniversal
        ).ToUniversalTime()

        # Include archives whose timestamps fall within the requested window.
        if ($timestamp -ge $cutoff) {
            [PSCustomObject]@{
                Timestamp = $timestamp
                Url = $url
                FileName = $fileName
            }
        }
    }

    # Process the newest matching archives first.
    $matches = @($matches | Sort-Object Timestamp -Descending)

    Write-Host "Matching archives: $($matches.Count)"

    foreach ($item in $matches) {
        # Construct the destination path for the current archive.
        $destination = Join-Path `
            $datasetArchiveFolder `
            $item.FileName

        # Skip downloads for archives that already exist locally.
        if (Test-Path $destination) {
            continue
        }

        Write-Host "Downloading $($item.FileName)"

        try {
            # Download to a temporary filename so an incomplete download
            # is not mistaken for a completed archive.
            Invoke-WebRequest `
                -Uri $item.Url `
                -OutFile "$destination.partial"

            # Rename the temporary file after the download succeeds.
            Move-Item `
                -Path "$destination.partial" `
                -Destination $destination
        }
        catch {
            # Remove any partial download left behind by a failed request.
            Remove-Item "$destination.partial" `
                -Force `
                -ErrorAction SilentlyContinue

            # Report the failure and continue processing other archives.
            Write-Warning "Failed to download $($item.Url): $_"
        }
    }
}

# Report completion and the location of the downloaded archives.
Write-Host ""
Write-Host "Download pass complete."
Write-Host "Archives: $archiveRoot"
