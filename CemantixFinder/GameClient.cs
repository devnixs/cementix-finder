using System.Text.Json;
using System.Text.Json.Serialization;

namespace CemantixFinder;

public class GameClient
{
    private string _clientAddress = "https://cemantix.herokuapp.com/score";
    public static string ClientName = "Cemantix";

    private readonly IHttpClientFactory _httpClientFactory;

    public GameClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<WordScore> GetWordScore(string word)
    {
        var httpClient = _httpClientFactory.CreateClient(ClientName);

        using (var request = new HttpRequestMessage(HttpMethod.Post, new Uri(_clientAddress)))
        {
            var body = new List<KeyValuePair<string, string>>
            {
                new("word", word),
            };
            request.Headers.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml");
            request.Headers.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate");
            request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 6.2; WOW64; rv:19.0) Gecko/20100101 Firefox/19.0");
            request.Headers.TryAddWithoutValidation("Accept-Charset", "ISO-8859-1");

            request.Content = new FormUrlEncodedContent(body);

            using var response = await httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();
            try
            {
                var decoded = JsonSerializer.Deserialize<WordScore>(json);
                if (decoded == null)
                {
                    return new WordScore()
                    {
                        Score = 0,
                    };
                }

                return decoded with {Score = decoded.Score * 100};
            }
            catch (Exception)
            {
                return new WordScore()
                {
                    Score = 0,
                };
            }
        }
    }
}

public record WordScore
{
    [JsonPropertyName("score")]
    public decimal Score { get; set; }
}