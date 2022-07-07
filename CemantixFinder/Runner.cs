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


    public async Task Run()
    {
        decimal currentBestShot = 0m;
        var pendingScores = new Dictionary<string, WordInfo>
        {
            { "stylo",   new("stylo", null, -20) },
        };

        var pendingLexicalField = new Dictionary<string, WordInfo>
        {
        };

        var closedList = new Dictionary<string, WordInfo>();

        while (true)
        {
            while (pendingScores.Any())
            {
                var entry = pendingScores.MaxBy(i => i.Value.Heuristic).Value;
                pendingScores.Remove(entry.Name);
                var scoring = await _gameClient.GetWordScore(entry.Name);
               // await Task.Delay(TimeSpan.FromSeconds(1));
                entry = entry with { Score = scoring.Score };

                if (scoring.Score == 100)
                {
                    ConsoleWrite("###########", ConsoleColor.Cyan);
                    ConsoleWrite("Found word " + entry.Name, ConsoleColor.Cyan);
                    ConsoleWrite("###########", ConsoleColor.Cyan);
                    return;
                }

                pendingLexicalField.Add(entry.Name, entry);

                if (scoring.Score > currentBestShot)
                {
                    currentBestShot = scoring.Score;
                    ConsoleWrite($"{entry.Name} => {entry.Score}", ConsoleColor.DarkRed);
                    break;
                }
                else
                {
                    ConsoleWrite($"\t {entry.Name} => {entry.Score}");
                }
            }

            if (pendingLexicalField.Any())
            {
                var entry = pendingLexicalField.MaxBy(i => i.Value.Score).Value;
                pendingLexicalField.Remove(entry.Name);
                var words = await _lexicalFieldFinder.GetRelatedWords(entry.Name);
                if (words.Length == 0)
                {
                    ConsoleWrite($"\t found {words.Length} words related to {entry.Name}", ConsoleColor.Yellow);
                }
                else
                {
                    ConsoleWrite($"\t found {words.Length} words related to {entry.Name}");
                }
                foreach (var word in words)
                {
                    if (pendingScores.ContainsKey(word) || pendingLexicalField.ContainsKey(word) || closedList.ContainsKey(word))
                    {
                        continue;
                    }

                    pendingScores.Add(word, new WordInfo(word, null, entry.Score.Value));
                }

                closedList.Add(entry.Name, entry);
            }

            if (!pendingScores.Any() && !pendingLexicalField.Any())
            {
                break;
            }
        }

        var closestFinds = closedList.OrderByDescending(i => i.Value.Score).Take(20).ToArray();
        ConsoleWrite("Could not find word. Closest guesses");
        foreach (var closestFind in closestFinds)
        {
            ConsoleWrite($"{closestFind.Key} => {closestFind.Value.Score}");
        }
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
}