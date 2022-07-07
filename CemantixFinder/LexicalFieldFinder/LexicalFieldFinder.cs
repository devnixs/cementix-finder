using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Web;
using CemantixFinder.Referential;

namespace CemantixFinder;

public interface ILexicalFieldFinder
{
    string Language { get; }
    Task<string[]> GetRelatedWords(string word);
}

public class FrenchLexicalFieldFinder : ILexicalFieldFinder
{
    private readonly IHttpClientFactory _httpClientFactory;
    public string Language => Languages.French;

    public static string HttpClientName = "rimessolides.com";
    
    public FrenchLexicalFieldFinder(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string[]> GetRelatedWords(string word)
    {
       // var baseUrl = "https://bf37-176-161-235-14.ngrok.io/test";
        var baseUrl = "https://www.rimessolides.com/motscles.aspx?m=";

        var client = _httpClientFactory.CreateClient(HttpClientName);
        var encodedWord = HttpUtility.UrlEncode(word);
        using (var request = new HttpRequestMessage(HttpMethod.Get, new Uri(baseUrl + encodedWord)))
        {
            request.Headers.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml");
            request.Headers.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate");
            request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 6.2; WOW64; rv:19.0) Gecko/20100101 Firefox/19.0");
            request.Headers.TryAddWithoutValidation("Accept-Charset", "ISO-8859-1");


            using var response = await client.SendAsync(request);
            var html = await response.Content.ReadAsStringAsync();

            var matches = Regex.Matches(html, "class= ?\"l-black ?\" href= ?\".+?\">(.+?)<\\/a>");
            return matches.Select(i => i.Groups[1].Value.ToLowerInvariant()).ToArray();
        }
    }
}