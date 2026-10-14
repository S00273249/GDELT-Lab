$ErrorActionPreference = "Stop"

$gkgDirectory = "research-data\extracted\gkg"
$dictionaryPath = "research-data\reference-data\gkg-theme-dictionary.json"
$outputCsvPath = "research-data\reference-data\gkg-theme-coverage.csv"
$outputJsonPath = "research-data\reference-data\gkg-theme-coverage.json"

# Validate input paths.
if (-not (Test-Path $gkgDirectory -PathType Container)) {
    throw "GKG directory not found: $gkgDirectory"
}

if (-not (Test-Path $dictionaryPath -PathType Leaf)) {
    throw "Theme dictionary not found: $dictionaryPath"
}

Write-Host "Loading theme dictionary..."

# Build a lookup table using case-insensitive theme codes.
$dictionary = @{}

$dictionaryEntries = Get-Content -LiteralPath $dictionaryPath -Raw |
    ConvertFrom-Json

foreach ($entry in $dictionaryEntries) {
    if (-not [string]::IsNullOrWhiteSpace($entry.code)) {
        $dictionary[$entry.code] = $entry
    }
}

Write-Host "Dictionary entries: $($dictionary.Count)"

# Find the extracted GKG files.
$files = @(
    Get-ChildItem -LiteralPath $gkgDirectory -Filter "*.gkg.csv" -File |
        Sort-Object Name
)

if ($files.Count -eq 0) {
    throw "No .gkg.csv files found in: $gkgDirectory"
}

Write-Host "Files to scan: $($files.Count)"
Write-Host ""

# Track local occurrences for each actual theme code.
$themeCounts = [System.Collections.Generic.Dictionary[string, long]]::new(
    [System.StringComparer]::OrdinalIgnoreCase
)

$totalRecords = 0L
$recordsWithoutThemes = 0L
$malformedRecords = 0L
$fileNumber = 0

foreach ($file in $files) {
    $fileNumber++

    Write-Progress `
        -Activity "Scanning GKG theme fields" `
        -Status "$fileNumber of $($files.Count): $($file.Name)" `
        -PercentComplete (($fileNumber / $files.Count) * 100)

    Write-Host "Scanning $($file.Name)..."

    foreach ($line in [System.IO.File]::ReadLines($file.FullName)) {
        if ([string]::IsNullOrWhiteSpace($line)) {
            continue
        }

        $totalRecords++

        # GKG records are tab-separated.
        # Split at most 27 fields so the final field is preserved.
        $fields = $line -split "`t", 27

        # A valid GKG record must contain the V2Themes field.
        # V2Themes is field 7 (zero-based index).
        if ($fields.Count -le 7) {
            $malformedRecords++
            continue
        }

        $themesField = $fields[7]

        if ([string]::IsNullOrWhiteSpace($themesField)) {
            $recordsWithoutThemes++
            continue
        }

        # V2Themes contains semicolon-separated theme codes.
        foreach ($theme in ($themesField -split ';')) {
            $code = $theme.Trim()

            if ([string]::IsNullOrWhiteSpace($code)) {
                continue
            }

            if ($themeCounts.ContainsKey($code)) {
                $themeCounts[$code]++
            }
            else {
                $themeCounts.Add($code, 1L)
            }
        }
    }
}

Write-Progress -Activity "Scanning GKG theme fields" -Completed

Write-Host ""
Write-Host "--- Scan Complete ---"
Write-Host "Records scanned: $totalRecords"
Write-Host "Records without themes: $recordsWithoutThemes"
Write-Host "Malformed records: $malformedRecords"
Write-Host "Distinct local themes: $($themeCounts.Count)"

# Build report rows, one row per distinct local theme.
Write-Host ""
Write-Host "Building coverage report..."

$report = foreach ($item in $themeCounts.GetEnumerator()) {
    $code = $item.Key
    $localOccurrences = $item.Value

    if ($dictionary.ContainsKey($code)) {
        $entry = $dictionary[$code]

        [PSCustomObject]@{
            code                 = $code
            localOccurrences     = $localOccurrences
            referenceOccurrences = $entry.occurrenceCount
            label                = $entry.label
            labelSource          = $entry.labelSource
            verificationStatus   = $entry.verificationStatus
            dictionaryStatus     = "matched"
        }
    }
    else {
        [PSCustomObject]@{
            code                 = $code
            localOccurrences     = $localOccurrences
            referenceOccurrences = $null
            label                = $null
            labelSource          = $null
            verificationStatus   = $null
            dictionaryStatus     = "missing"
        }
    }
}

# Sort by local occurrence count, most frequent first.
$report = @(
    $report | Sort-Object -Property localOccurrences -Descending
)

# Write the CSV report.
Write-Host "Writing CSV report..."

$report |
    Export-Csv -LiteralPath $outputCsvPath -NoTypeInformation -Encoding utf8

# Calculate summary statistics.
$matchedThemes = @(
    $report | Where-Object { $_.dictionaryStatus -eq "matched" }
)

$missingThemes = @(
    $report | Where-Object { $_.dictionaryStatus -eq "missing" }
)

$matchedOccurrences = [long]0
$missingOccurrences = [long]0

foreach ($row in $matchedThemes) {
    $matchedOccurrences += [long]$row.localOccurrences
}

foreach ($row in $missingThemes) {
    $missingOccurrences += [long]$row.localOccurrences
}

$totalThemeOccurrences = $matchedOccurrences + $missingOccurrences

$coveragePercent = if ($totalThemeOccurrences -gt 0) {
    [Math]::Round(
        ($matchedOccurrences / $totalThemeOccurrences) * 100,
        2
    )
}
else {
    0
}

# Create a compact JSON summary.
# Do not serialize the entire coverage report to JSON:
# millions of local occurrences can consume excessive memory.
$summary = [PSCustomObject]@{
    generatedAt                  = (Get-Date).ToString("o")
    filesScanned                 = $files.Count
    recordsScanned               = $totalRecords
    recordsWithoutThemes         = $recordsWithoutThemes
    malformedRecords             = $malformedRecords
    dictionaryEntries            = $dictionary.Count
    distinctLocalThemes          = $themeCounts.Count
    matchedThemes                = $matchedThemes.Count
    missingThemes                = $missingThemes.Count
    matchedThemeOccurrences      = $matchedOccurrences
    missingThemeOccurrences     = $missingOccurrences
    totalThemeOccurrences        = $totalThemeOccurrences
    occurrenceCoveragePercent    = $coveragePercent
    csvReport                    = $outputCsvPath
}

$summary |
    ConvertTo-Json -Depth 4 |
    Set-Content -LiteralPath $outputJsonPath -Encoding utf8

# Display summary.
Write-Host ""
Write-Host "========== GKG Theme Coverage =========="
Write-Host "Files scanned:                 $($files.Count)"
Write-Host "Records scanned:               $totalRecords"
Write-Host "Records without themes:        $recordsWithoutThemes"
Write-Host "Malformed records:             $malformedRecords"
Write-Host "Distinct local themes:         $($themeCounts.Count)"
Write-Host "Dictionary entries:            $($dictionary.Count)"
Write-Host "Matched themes:                $($matchedThemes.Count)"
Write-Host "Missing themes:                $($missingThemes.Count)"
Write-Host "Matched occurrences:           $matchedOccurrences"
Write-Host "Missing occurrences:           $missingOccurrences"
Write-Host "Occurrence coverage:            $coveragePercent%"
Write-Host ""
Write-Host "CSV report:  $outputCsvPath"
Write-Host "JSON summary: $outputJsonPath"

# Show the most frequent genuinely missing themes.
Write-Host ""
Write-Host "--- Most Frequent Missing Themes ---"

$missingThemes |
    Select-Object -First 30 code, localOccurrences |
    Format-Table -AutoSize

# Show the most frequent matched themes whose labels are rule-generated.
Write-Host ""
Write-Host "--- Most Frequent Rule-Generated Labels ---"

$matchedThemes |
    Where-Object { $_.labelSource -eq "rule-generated" } |
    Select-Object -First 30 code, localOccurrences, label |
    Format-Table -AutoSize