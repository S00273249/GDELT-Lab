/*
 * Project 300 - Article Metadata Tester
 *
 * The PowerShell script below tests metadata extraction against multiple
 * publishers. It runs this console program once for each article URL.
 * Keep it commented out because it is a PowerShell script, not C# code.
 */

/*
$urls = @(
    # First three: BBC, The Guardian, and a second RTÉ article
    "https://www.bbc.co.uk/news/articles/c680zyz2m5lro"
    "https://www.theguardian.com/music/2026/oct/12/george-fenton-obituary"
    "https://www.rte.ie/news/regional/2026/1012/1595044-galway-culture-company/"

    # Top 10 publishers in your GDELT snapshot
    "https://941thebeat.iheart.com/content/2026-10-12-blueface-proposes-to-chrisean-rock-at-arizona-nightclub/"
    "https://economictimes.indiatimes.com/news/international/us/diddy-rushed-out-of-prison-after-head-injury-as-rapper-receives-emergency-medical-treatment/articleshow/134900669.cms"
    "https://www.aol.co.uk/articles/labours-troubles-bill-put-elderly-183000000.html"
    "https://www.boredpanda.com/hilarious-signs-ruined-misused-quotation-marks/?cexp_id=162868&_f=homepage_featured&cexp_var=0"
    "https://www.manilatimes.net/2026/10/13/tmt-newswire/globenewswire/fixgo-announces-nationwide-shipping-of-auto-parts-from-california-warehouse/2444062"
    "https://www.aol.com/articles/crowdstrike-124-2026-fortinet-142-190017000.html"
    "https://www.ghanamma.com/2026/10/12/ohemaa-woyeje-finally-breaks-silence-on-why-angel-fm-tv-accra-went-off-air/"
    "https://www.thehindu.com/news/cities/Delhi/delhi-cm-launches-portal-for-durga-e-auto-scheme-for-women-transgender-persons/article71575898.ece"
    "http://www.bignewsnetwork.com/news/279369248/leaning-into-tight-ends-in-look-at-lions-snaps"
    "https://aninews.in/news/national/general-news/216-households-in-valmiki-camp-to-get-tap-water-connections-delhi-mayor-pravesh-wahi-inaugurates-8216har-ghar-nal-yojana821720261012224334/"
)

foreach ($url in $urls) {
    Write-Host "`n============================================" -ForegroundColor Cyan
    Write-Host "Testing: $url" -ForegroundColor Yellow
    Write-Host "============================================" -ForegroundColor Cyan

    dotnet run -- "$url"
}
*/

using ArticleMetadata;

// Use the URL supplied as the first command-line argument.
// If no URL is supplied, test the default RTÉ article instead.
var articleUrl = args.Length > 0
    ? args[0]
    : "https://www.rte.ie/news/business/2026/1012/1595068-openai-jobs-ireland/";

// An optional second argument supplies the image URL from GDELT.
// This allows us to test the image fallback when a publisher page is blocked.
var gdeltImage = args.Length > 1 ? args[1] : null;

Console.WriteLine("Project 300 - Article Metadata Tester");
Console.WriteLine("======================================");
Console.WriteLine($"URL: {articleUrl}");

if (!string.IsNullOrWhiteSpace(gdeltImage))
{
    Console.WriteLine($"GDELT image: {gdeltImage}");
}

// The extraction logic lives in the reusable ArticleMetadata library.
// This console project remains a convenient way to test one URL at a time.
using var extractor = new ArticleMetadataExtractor();

try
{
    var result = await extractor.ExtractAsync(articleUrl, gdeltImage);

    Console.WriteLine();
    Console.WriteLine($"Status: {result.Status}");
    Console.WriteLine($"Page retrieved: {result.PageRetrieved}");
    Console.WriteLine($"HTTP status: {(result.HttpStatusCode is int statusCode ? statusCode.ToString() : "(unavailable)")}");
    Console.WriteLine($"HTML downloaded: {result.HtmlLength:N0} characters");

    // Print each extracted value alongside its source so the fallback order
    // can be checked while testing different publishers.
    PrintField("Page title", result.PageTitle);
    PrintField("Headline", result.Headline);
    PrintField("Headline source", result.HeadlineSource);
    PrintField("Description", result.Description);
    PrintField("Description source", result.DescriptionSource);
    PrintField("Image", result.ImageUrl);
    PrintField("Image source", result.ImageSource);

    if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
    {
        Console.WriteLine();
        Console.WriteLine($"Error: {result.ErrorMessage}");
    }
}
catch (OperationCanceledException)
{
    // The caller can cancel an extraction. Do not disguise cancellation as
    // a publisher error; allow it to be handled by the calling process.
    Console.WriteLine("The metadata extraction was cancelled.");
}

// Prints one result field in a consistent, readable format.
// Missing values are shown explicitly instead of appearing as blank output.
static void PrintField(string label, string? value)
{
    Console.WriteLine($"{label}: {Show(value)}");
}

// Replaces missing values with readable text in the console output.
static string Show(string? value)
{
    return string.IsNullOrWhiteSpace(value) ? "(not found)" : value;
}
