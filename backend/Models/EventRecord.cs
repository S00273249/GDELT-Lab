namespace GDELT_Lab.Models;

/// <summary>
/// Represents an event record from the GDELT Events dataset.
/// Source values are stored as strings to preserve the original data
/// and avoid unnecessary conversions during ingestion.
/// </summary>
public class EventRecord
{
    // Unique identifier for the event.
    public string GlobalEventId { get; set; } = string.Empty;

    // Date components associated with the event.
    public string? Day { get; set; }
    public string? MonthYear { get; set; }
    public string? Year { get; set; }
    public string? FractionDate { get; set; }

    // Actor 1 identification and classification.
    public string? Actor1Code { get; set; }
    public string? Actor1Name { get; set; }
    public string? Actor1CountryCode { get; set; }
    public string? Actor1KnownGroupCode { get; set; }
    public string? Actor1EthnicCode { get; set; }
    public string? Actor1Religion1Code { get; set; }
    public string? Actor1Religion2Code { get; set; }
    public string? Actor1Type1Code { get; set; }
    public string? Actor1Type2Code { get; set; }
    public string? Actor1Type3Code { get; set; }

    // Actor 2 identification and classification.
    public string? Actor2Code { get; set; }
    public string? Actor2Name { get; set; }
    public string? Actor2CountryCode { get; set; }
    public string? Actor2KnownGroupCode { get; set; }
    public string? Actor2EthnicCode { get; set; }
    public string? Actor2Religion1Code { get; set; }
    public string? Actor2Religion2Code { get; set; }
    public string? Actor2Type1Code { get; set; }
    public string? Actor2Type2Code { get; set; }
    public string? Actor2Type3Code { get; set; }

    // Event classification and statistical measures.
    public string? IsRootEvent { get; set; }
    public string? EventCode { get; set; }
    public string? EventBaseCode { get; set; }
    public string? EventRootCode { get; set; }
    public string? QuadClass { get; set; }
    public string? GoldsteinScale { get; set; }
    public string? NumMentions { get; set; }
    public string? NumSources { get; set; }
    public string? NumArticles { get; set; }
    public string? AvgTone { get; set; }

    // Geographic information associated with Actor 1.
    public string? Actor1GeoType { get; set; }
    public string? Actor1GeoFullName { get; set; }
    public string? Actor1GeoCountryCode { get; set; }
    public string? Actor1GeoAdm1Code { get; set; }
    public string? Actor1GeoAdm2Code { get; set; }
    public string? Actor1GeoLat { get; set; }
    public string? Actor1GeoLong { get; set; }
    public string? Actor1GeoFeatureId { get; set; }

    // Geographic information associated with Actor 2.
    public string? Actor2GeoType { get; set; }
    public string? Actor2GeoFullName { get; set; }
    public string? Actor2GeoCountryCode { get; set; }
    public string? Actor2GeoAdm1Code { get; set; }
    public string? Actor2GeoAdm2Code { get; set; }
    public string? Actor2GeoLat { get; set; }
    public string? Actor2GeoLong { get; set; }
    public string? Actor2GeoFeatureId { get; set; }

    // Geographic information associated with the event action.
    public string? ActionGeoType { get; set; }
    public string? ActionGeoFullName { get; set; }
    public string? ActionGeoCountryCode { get; set; }
    public string? ActionGeoAdm1Code { get; set; }
    public string? ActionGeoAdm2Code { get; set; }
    public string? ActionGeoLat { get; set; }
    public string? ActionGeoLong { get; set; }
    public string? ActionGeoFeatureId { get; set; }

    // Record creation date and originating source URL.
    public string? DateAdded { get; set; }
    public string? SourceUrl { get; set; }
}