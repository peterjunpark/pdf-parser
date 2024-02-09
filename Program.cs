using System.Globalization;
using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

const string TicketsDir = "./Tickets";

try
{
    var ticketsAggregator = new TicketsAggregator(TicketsDir);

    ticketsAggregator.Run();
}
catch (Exception ex)
{
    Console.WriteLine("An exception occurred.\nException message: " + ex.Message);
}

internal class TicketsAggregator(string ticketsDir)
{
    private readonly string _ticketsDir = ticketsDir;
    private readonly Dictionary<string, CultureInfo> _domainToCultureMap = new()
    {
        [".com"] = new CultureInfo("en-US"),
        [".fr"] = new CultureInfo("fr-FR"),
        [".jp"] = new CultureInfo("ja-JP")
    };

    public void Run()
    {
        StringBuilder _stringBuilder = new();
        foreach (var filePath in Directory.GetFiles(_ticketsDir, "*.pdf"))
        {
            using PdfDocument document = PdfDocument.Open(filePath);
            int pageCount = document.NumberOfPages;
            Page page = document.GetPage(1);
            var lines = ProcessPage(page);
            _stringBuilder.AppendLine(string.Join(Environment.NewLine, lines));
        }
        WriteToFile(_stringBuilder.ToString());
        Console.WriteLine("Press any key to close.");
        Console.ReadKey();
    }

    private void WriteToFile(string text)
    {
        var outputPath = Path.Combine(_ticketsDir, "aggregatedTickets.txt");
        File.WriteAllText(outputPath, text);
        Console.WriteLine("Output saved to " + outputPath);
    }

    private IEnumerable<string> ProcessPage(Page page)
    {
        string text = page.Text;
        var split = text.Split(new[] { "Title:", "Date:", "Time:", "Visit us:" }, StringSplitOptions.None);

        var domain = GetDomainFromUrl(split.Last());
        var culture = _domainToCultureMap[domain];

        for (int i = 1; i < split.Length - 3; i += 3)
        {
            yield return ProcessTicket(split, culture, i);
        }
    }

    private static string ProcessTicket(string[] split, CultureInfo culture, int i)
    {
        string title = split[i];
        string dateAsString = split[i + 1];
        string timeAsString = split[i + 2];

        DateOnly date = DateOnly.Parse(dateAsString, culture);
        TimeOnly time = TimeOnly.Parse(timeAsString, culture);

        var dateAsStringInvariant = date.ToString(CultureInfo.InvariantCulture);
        var timeAsStringInvariant = time.ToString(CultureInfo.InvariantCulture);

        var ticketData = $"{title,-40}| {dateAsStringInvariant} | {timeAsStringInvariant}";
        return ticketData;
    }

    private static string GetDomainFromUrl(string url)
    {
        int lastDotIndex = url.LastIndexOf('.');
        return url[lastDotIndex..];
    }
}
