namespace CemantixFinder;

public class Runner
{
    private readonly ILexicalFieldFinder _lexicalFieldFinder;
    private readonly GameClient _gameClient;

    public Runner(ILexicalFieldFinder lexicalFieldFinder, GameClient gameClient)
    {
        _lexicalFieldFinder = lexicalFieldFinder;
        _gameClient = gameClient;
    }


    public async Task Run(string startingWord)
    {
        decimal currentBestShot = 0m;
        var startTime = DateTimeOffset.UtcNow;
        var pendingScores = new Dictionary<string, WordInfo>
        {
            {startingWord, new(startingWord, null, -20)},
        };

        var pendingLexicalField = new Dictionary<string, WordInfo>
        {
        };
        var totalRequests = 0;

        var closedList = new Dictionary<string, WordInfo>();

        while (true)
        {
            while (pendingScores.Any())
            {
                var entry = pendingScores.MaxBy(i => i.Value.Heuristic).Value;
                pendingScores.Remove(entry.Name);
                totalRequests++;
                var scoring = await _gameClient.GetWordScore(entry.Name);
                entry = entry with {Score = scoring.Score};

                if (scoring.Score == 100)
                {
                    ConsoleWrite("###############################", ConsoleColor.Cyan);
                    ConsoleWrite($"Solution: {entry.Name} trouvée en {totalRequests} coups et {(DateTimeOffset.UtcNow-startTime).TotalSeconds :0} secondes", ConsoleColor.Cyan);
                    ConsoleWrite(GetWordChain(entry, closedList), ConsoleColor.Cyan);
                    ConsoleWrite("###############################", ConsoleColor.Cyan);

                    return;
                }

                pendingLexicalField.Add(entry.Name, entry);

                if (scoring.Score > currentBestShot)
                {
                    currentBestShot = scoring.Score;
                    ConsoleWrite($"{GetWordChain(entry, closedList)} = {entry.Score}", ConsoleColor.DarkRed);
                    break;
                }
                else
                {
                    ConsoleWrite($"{GetWordChain(entry, closedList)} = {entry.Score}");
                }
            }

            if (pendingLexicalField.Any())
            {
                var entry = pendingLexicalField.MaxBy(i => i.Value.Score).Value;
                pendingLexicalField.Remove(entry.Name);
                var words = await _lexicalFieldFinder.GetRelatedWords(entry.Name);
                if (words.Length == 0)
                {
                    ConsoleWrite($"aucun mot trouvé similaire à {entry.Name}", ConsoleColor.Yellow);
                }
                else
                {
                    ConsoleWrite($"{words.Length} mots trouvés similaires à {entry.Name}", ConsoleColor.Green);
                }

                foreach (var word in words)
                {
                    if (pendingScores.ContainsKey(word) || pendingLexicalField.ContainsKey(word) || closedList.ContainsKey(word))
                    {
                        continue;
                    }

                    pendingScores.Add(word, new WordInfo(word, null, entry.Score.Value)
                    {
                        PreviousWord = entry.Name
                    });
                }

                closedList.Add(entry.Name, entry);
            }

            if (!pendingScores.Any() && !pendingLexicalField.Any())
            {
                break;
            }
        }

        var closestFinds = closedList.OrderByDescending(i => i.Value.Score).Take(20).ToArray();
        ConsoleWrite("Solution non trouvée.");
        if (closestFinds.Any())
        {
            ConsoleWrite("Mots les plus proches:");
            foreach (var closestFind in closestFinds)
            {
                ConsoleWrite($"{closestFind.Key} => {closestFind.Value.Score}");
            }
        }
    }

    private string GetWordChain(WordInfo word, Dictionary<string, WordInfo> closedList)
    {
        var backTrace = new List<string>();
        var current = word;

        while (current != null)
        {
            backTrace.Add(current.Name);
            current = !string.IsNullOrEmpty(current.PreviousWord) && closedList.ContainsKey(current.PreviousWord) ? closedList[current.PreviousWord] : null;
        }

        backTrace.Reverse();
        var trace = string.Join(" => ", backTrace);
        return trace;
    }

    private void ConsoleWrite(string message, ConsoleColor? fg = null, ConsoleColor? bg = null)
    {
        if (bg != null)
        {
            Console.BackgroundColor = bg.Value;
        }

        if (fg != null)
        {
            Console.ForegroundColor = fg.Value;
        }

        Console.WriteLine(message);
        Console.ResetColor();
    }
}

public record WordInfo(string Name, decimal? Score, decimal Heuristic)
{
    public string PreviousWord { get; set; }
}