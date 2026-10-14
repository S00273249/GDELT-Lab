$ErrorActionPreference = "Stop"

$inputPath = "research-data\reference-data\LOOKUP-GKGTHEMES.TXT"
$outputPath = "research-data\reference-data\gkg-theme-dictionary.json"

if (-not (Test-Path $inputPath)) {
    throw "Lookup file not found: $inputPath"
}

function ConvertTo-ReadableWords([string]$text) {
    $text = $text -replace "_+", " "
    $text = $text.Trim().ToLowerInvariant()
    $text = (Get-Culture).TextInfo.ToTitleCase($text)

    # Restore common abbreviations, names and known joined words.
    $replacements = [ordered]@{
        "Ict"               = "ICT"
        "Us "                = "U.S. "
        "Us-"                = "U.S.-"
        "Epu "               = "EPU "
        "Ungp "              = "UNGP "
        "Crisislex"          = "CrisisLex"
        "Msm"                = "MSM"
        "Wellbeing"          = "Well-being"
        "Dieselprice"        = "Diesel Price"
        "Gasolineprice"      = "Gasoline Price"
        "Heatingoil"         = "Heating Oil"
        "Fuelprices"         = "Fuel Prices"
        "Worldlanguages"     = "World Languages"
        "Pointsofinterest"   = "Points of Interest"
        "Worldarachnids"     = "World Arachnids"
        "Worldinsects"       = "World Insects"
        "Worldbirds"         = "World Birds"
        "Worldfish"          = "World Fish"
        "Worldreptiles"      = "World Reptiles"
        "Worldcrustaceans"   = "World Crustaceans"
        "Chronicdisease"     = "Chronic Disease"
        "Foodstaples"        = "Food Staples"
        "Aidgroups"          = "Aid Groups"
        "Specialissues"      = "Special Issues"
        "And"                = "and"
        "Of"                 = "of"
        "For"                = "for"
        "The"                = "the"
    }

    foreach ($key in $replacements.Keys) {
        $text = $text.Replace($key, $replacements[$key])
    }

    # Preserve conventional capitalization after title casing.
    $text = $text -replace '\bMsm\b', 'MSM'
    $text = $text -replace '\bIct\b', 'ICT'
    $text = $text -replace '\bUs\b', 'U.S.'
    $text = $text -replace '\bUn\b', 'UN'
    $text = $text -replace '\bEpu\b', 'EPU'
    $text = $text -replace '\bUngp\b', 'UNGP'

    $joinedWordReplacements = [ordered]@{
        "Updatessympathy" = "Updates Sympathy"
        "Dieselprice"     = "Diesel Price"
        "Gasolineprice"   = "Gasoline Price"
        "Heatingoil"      = "Heating Oil"
        "Fuelprices"      = "Fuel Prices"
        "Worldlanguages"  = "World Languages"
        "Worldcrustaceans" = "World Crustaceans"
        "Worldarachnids"  = "World Arachnids"
        "Worldinsects"    = "World Insects"
        "Worldbirds"      = "World Birds"
        "Worldfish"       = "World Fish"
        "Worldreptiles"   = "World Reptiles"
        "Foodstaples"     = "Food Staples"
        "Aidgroups"       = "Aid Groups"
        "Specialissues"   = "Special Issues"
        "Chronicdisease"  = "Chronic Disease"
    }

    foreach ($key in $joinedWordReplacements.Keys) {
        $text = $text.Replace($key, $joinedWordReplacements[$key])
    }

    # Collapse adjacent repeated words, e.g. "Migration Fear Fear".
    $text = $text -replace '\b(\w+)(\s+\1\b)+', '$1'

    return $text
}

function Get-ThemeLabel([string]$code) {
    # Exact mappings for known family-level codes.
    $exactLabels = @{
        "TAX_FNCACT"                  = "Functional Role"
        "TAX_ETHNICITY"               = "Ethnicity"
        "TAX_WORLDLANGUAGES"          = "World Languages"
        "TAX_WORLDMAMMALS"             = "World Mammals"
        "TAX_WORLDINSECTS"             = "World Insects"
        "TAX_WORLDBIRDS"               = "World Birds"
        "TAX_WORLDFISH"                = "World Fish"
        "TAX_WORLDREPTILES"            = "World Reptiles"
        "TAX_WORLDCRUSTACEANS"         = "World Crustaceans"
        "TAX_WORLDARACHNIDS"           = "World Arachnids"
        "TAX_DISEASE"                  = "Disease"
        "TAX_RELIGION"                 = "Religion"
        "TAX_ECON"                     = "Economic Topic"
        "TAX_FOODSTAPLES"              = "Food Staples"
        "TAX_POLITICAL_PARTY"          = "Political Party"
        "SOC_POINTSOFINTEREST"         = "Points of Interest"
        "CRISISLEX_CRISISLEXREC"       = "CrisisLex: Crisis-Related Term"
        "EPU_POLICY"                  = "Economic Policy"
        "EPU_ECONOMY"                 = "Economy"
        "EPU_UNCERTAINTY"             = "Economic Policy Uncertainty"
        "EPU_POLICY_POLICY"           = "Economic Policy"
        "USPEC_POLICY1"                = "U.S.-Specific: Policy 1"
        "USPEC_UNCERTAINTY1"           = "U.S.-Specific: Uncertainty 1"
        "USPEC_POLITICS_GENERAL1"      = "U.S.-Specific: General Politics 1"
        "MEDIA_MSM"                   = "Mainstream Media"
        "ECON_DIESELPRICE"             = "Economy: Diesel Price"
        "ECON_GASOLINEPRICE"           = "Economy: Gasoline Price"
        "ECON_HEATINGOIL"              = "Economy: Heating Oil"
        "FUELPRICES"                   = "Fuel Prices"
        "EPU_ECONOMY_HISTORIC"         = "Historical Economic Conditions"
    }

    if ($exactLabels.ContainsKey($code)) {
        return $exactLabels[$code]
    }

    # World Bank codes embed a numeric identifier and a readable name.
    if ($code -match '^WB_\d+_(.+)$') {
        return ConvertTo-ReadableWords $Matches[1]
    }

    # Match specific families before their general parent families.
    $rules = @(
        @{ Pattern = '^TAX_FNCACT_(.+)$';          Label = 'Functional Role' }
        @{ Pattern = '^TAX_WORLDLANGUAGES_(.+)$';  Label = 'Language' }
        @{ Pattern = '^TAX_ETHNICITY_(.+)$';       Label = 'Ethnicity' }
        @{ Pattern = '^TAX_WORLDMAMMALS_(.+)$';    Label = 'Mammal' }
        @{ Pattern = '^TAX_WORLDFISH_(.+)$';       Label = 'Fish' }
        @{ Pattern = '^TAX_WORLDINSECTS_(.+)$';    Label = 'Insect' }
        @{ Pattern = '^TAX_WORLDBIRDS_(.+)$';      Label = 'Bird' }
        @{ Pattern = '^TAX_WORLDREPTILES_(.+)$';   Label = 'Reptile' }
        @{ Pattern = '^TAX_WORLDCRUSTACEANS_(.+)$'; Label = 'Crustacean' }
        @{ Pattern = '^TAX_WORLDARACHNIDS_(.+)$';  Label = 'Arachnid' }
        @{ Pattern = '^TAX_DISEASE_(.+)$';         Label = 'Disease' }
        @{ Pattern = '^TAX_CHRONICDISEASE_(.+)$';  Label = 'Chronic Disease' }
        @{ Pattern = '^TAX_RELIGION_(.+)$';        Label = 'Religion' }
        @{ Pattern = '^TAX_TERROR_GROUP_(.+)$';    Label = 'Terror Group' }
        @{ Pattern = '^TAX_AIDGROUPS_(.+)$';       Label = 'Aid Group' }
        @{ Pattern = '^TAX_SPECIAL_ISSUES_(.+)$';  Label = 'Special Issue' }
        @{ Pattern = '^TAX_POLITICAL_PARTY_(.+)$'; Label = 'Political Party' }
        @{ Pattern = '^TAX_MILITARY_TITLE_(.+)$';  Label = 'Military Title' }
        @{ Pattern = '^TAX_WEAPONS_(.+)$';         Label = 'Weapon' }
        @{ Pattern = '^TAX_FOODSTAPLES_(.+)$';     Label = 'Food Staple' }
        @{ Pattern = '^TAX_ECON_(.+)$';            Label = 'Economic Topic' }
        @{ Pattern = '^TAX_(.+)$';                Label = 'Taxonomy' }
        @{ Pattern = '^CRISISLEX_C(\d+)_(.+)$';    Label = 'CrisisLex Category' }
        @{ Pattern = '^CRISISLEX_T(\d+)_(.+)$';    Label = 'CrisisLex Term' }
        @{ Pattern = '^CRISISLEX_O(\d+)_(.+)$';    Label = 'CrisisLex Other' }
        @{ Pattern = '^CRISISLEX_(.+)$';           Label = 'CrisisLex' }
        @{ Pattern = '^EPU_POLICY_(.+)$';          Label = 'Economic Policy' }
        @{ Pattern = '^EPU_CATS_(.+)$';            Label = 'Economic Policy Uncertainty Category' }
        @{ Pattern = '^EPU_ECONOMY_(.+)$';         Label = 'Economic Topic' }
        @{ Pattern = '^EPU_(.+)$';                 Label = 'Economic Policy Uncertainty' }
        @{ Pattern = '^UNGP_(.+)$';                Label = 'UN Guiding Principles' }
        @{ Pattern = '^USPEC_POLITICS_(.+)$';      Label = 'U.S.-Specific Politics' }
        @{ Pattern = '^USPEC_(.+)$';               Label = 'U.S.-Specific' }
        @{ Pattern = '^SOC_POINTSOFINTEREST_(.+)$'; Label = 'Point of Interest' }
        @{ Pattern = '^SOC_(.+)$';                 Label = 'Social' }
        @{ Pattern = '^ECON_(.+)$';                Label = 'Economy' }
        @{ Pattern = '^NATURAL_DISASTER_(.+)$';    Label = 'Natural Disaster' }
        @{ Pattern = '^NATURAL_DISASTER$';         Label = 'Natural Disaster' }
        @{ Pattern = '^MEDIA_(.+)$';               Label = 'Media' }
        @{ Pattern = '^ENV_(.+)$';                 Label = 'Environment' }
    )

    foreach ($rule in $rules) {
        if ($code -match $rule.Pattern) {
            # Capture the suffix from the rule match.
            $suffix = $Matches[$Matches.Count - 1]

            # For CrisisLex, exclude the numeric category identifier.
            if ($code -match '^CRISISLEX_[CTO]\d+_(.+)$') {
                $suffix = $Matches[1]
            }

            $readable = ConvertTo-ReadableWords $suffix
            return "$($rule.Label): $readable"
        }
    }

    # Generic fallback: a readable rendering, not a verified definition.
    return ConvertTo-ReadableWords $code
}

# Codes assigned an explicit label in Get-ThemeLabel().
# Kept here separately because function-local variables are not accessible
# from the main script scope.
$explicitLabelCodes = @(
    "TAX_FNCACT",
    "TAX_ETHNICITY",
    "TAX_WORLDLANGUAGES",
    "TAX_WORLDMAMMALS",
    "TAX_WORLDINSECTS",
    "TAX_WORLDBIRDS",
    "TAX_WORLDFISH",
    "TAX_WORLDREPTILES",
    "TAX_WORLDCRUSTACEANS",
    "TAX_WORLDARACHNIDS",
    "TAX_DISEASE",
    "TAX_RELIGION",
    "TAX_ECON",
    "TAX_FOODSTAPLES",
    "TAX_POLITICAL_PARTY",
    "SOC_POINTSOFINTEREST",
    "CRISISLEX_CRISISLEXREC",
    "EPU_POLICY",
    "EPU_ECONOMY",
    "EPU_UNCERTAINTY",
    "EPU_POLICY_POLICY",
    "USPEC_POLICY1",
    "USPEC_UNCERTAINTY1",
    "USPEC_POLITICS_GENERAL1",
    "MEDIA_MSM",
    "ECON_DIESELPRICE",
    "ECON_GASOLINEPRICE",
    "ECON_HEATINGOIL",
    "FUELPRICES",
    "EPU_ECONOMY_HISTORIC"
)

$entries = [System.Collections.Generic.List[object]]::new()

$seen = [System.Collections.Generic.HashSet[string]]::new(
    [System.StringComparer]::OrdinalIgnoreCase
)

$resolvedInputPath = (Resolve-Path $inputPath).Path

foreach ($line in [System.IO.File]::ReadLines($resolvedInputPath)) {
    if ([string]::IsNullOrWhiteSpace($line)) {
        continue
    }

    $parts = $line -split "`t", 2

    if ($parts.Count -ne 2) {
        continue
    }

    $code = $parts[0].Trim()
    $count = 0L

    if (
        -not $code -or
        -not [long]::TryParse($parts[1].Trim(), [ref]$count)
    ) {
        continue
    }

    if (-not $seen.Add($code)) {
        continue
    }

    $label = Get-ThemeLabel $code

    $labelSource = if ($code -match '^WB_\d+_.+$') {
        "embedded-name"
    }
    elseif ($code -in $explicitLabelCodes) {
        "explicit-label"
    }
    else {
        "rule-generated"
    }

    $entries.Add([PSCustomObject]@{
        code               = $code
        label              = $label
        occurrenceCount    = $count
        labelSource        = $labelSource
        verificationStatus = "unverified"
    })
}

# Ensure the destination directory exists.
$outputDirectory = Split-Path -Parent $outputPath

if ($outputDirectory -and -not (Test-Path $outputDirectory)) {
    New-Item -ItemType Directory -Path $outputDirectory -Force |
        Out-Null
}

$json = ConvertTo-Json -InputObject @($entries) -Depth 4

[System.IO.File]::WriteAllText(
    $outputPath,
    $json,
    [System.Text.UTF8Encoding]::new($false)
)

Write-Host "`n--- Theme Dictionary Generated ---"
Write-Host "Entries: $($entries.Count)"
Write-Host "Output:  $outputPath"
Write-Host "`n--- Sample Labels ---"

$sampleCodes = @(
    "TAX_FNCACT",
    "TAX_FNCACT_PRESIDENT",
    "TAX_ETHNICITY",
    "TAX_WORLDLANGUAGES_PORTUGUESE",
    "SOC_POINTSOFINTEREST",
    "SOC_POINTSOFINTEREST_SCHOOL",
    "CRISISLEX_C07_SAFETY",
    "CRISISLEX_T03_DEAD",
    "EPU_POLICY",
    "EPU_POLICY_GOVERNMENT",
    "EPU_POLICY_POLICY",
    "EPU_CATS_MIGRATION_FEAR_FEAR",
    "USPEC_POLITICS_GENERAL1",
    "WB_696_PUBLIC_SECTOR_MANAGEMENT",
    "TAX_ECON_PRICE",
    "ECON_DIESELPRICE",
    "ECON_GASOLINEPRICE",
    "ECON_HEATINGOIL",
    "FUELPRICES",
    "MEDIA_MSM",
    "TAX_FOODSTAPLES",
    "TAX_FOODSTAPLES_RICE",
    "EDUCATION"
)

$entries |
    Where-Object { $_.code -in $sampleCodes } |
    Format-Table code, label, labelSource, verificationStatus -AutoSize