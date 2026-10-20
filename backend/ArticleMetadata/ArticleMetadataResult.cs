namespace ArticleMetadata;

/// <summary>
/// Holds the metadata extracted from one publisher page.
/// This type is independent of the API and database so it can be reused by
/// the console tester and the sample-dataset preparation process.
/// </summary>
public class ArticleMetadataResult
{
    /// <summary>The original URL that was submitted for extraction.</summary>
    public string ArticleUrl { get; set; } = string.Empty;

    /// <summary>The HTML document's title, if the page was retrieved.</summary>
    public string? PageTitle { get; set; }

    /// <summary>The best headline found using the configured fallback order.</summary>
    public string? Headline { get; set; }

    /// <summary>Identifies the source used for the headline.</summary>
    public string? HeadlineSource { get; set; }

    /// <summary>The publisher's description, if one could be found.</summary>
    public string? Description { get; set; }

    /// <summary>Identifies the source used for the description.</summary>
    public string? DescriptionSource { get; set; }

    /// <summary>The selected display image, including any GDELT fallback.</summary>
    public string? ImageUrl { get; set; }

    /// <summary>Identifies the source used for the selected image.</summary>
    public string? ImageSource { get; set; }

    /// <summary>True when the publisher page was retrieved and parsed.</summary>
    public bool PageRetrieved { get; set; }

    /// <summary>The HTTP status code, when an HTTP response was received.</summary>
    public int? HttpStatusCode { get; set; }

    /// <summary>Number of characters in the downloaded HTML document.</summary>
    public int HtmlLength { get; set; }

    /// <summary>Outcome such as Success, HttpError, Timeout or InvalidUrl.</summary>
    public string Status { get; set; } = "Pending";

    /// <summary>Error details, if the request or extraction encountered a problem.</summary>
    public string? ErrorMessage { get; set; }
}
