namespace MarkovTextGenerator;

public class Chain
{
    // (token1, token2, token3) -> list of possible next words
    public Dictionary<(int, int, int), List<Word>> Words { get; set; } = new();

    private readonly Dictionary<(int, int, int), int> _sums = new();

    private readonly Random _rand = new(Environment.TickCount);

    // Token storage
    private Dictionary<string, int> _tokenLookup = new();
    private Dictionary<int, string> _reverseLookup = new();
    private int _nextToken = 0;

    private List<(int, int, int)> Starters = new();

    private string CurrentSentence = "";

    // ================= TOKEN HELPERS =================

    private int GetToken(string word)
    {
        word = word.Trim().ToLower();

        if (_tokenLookup.TryGetValue(word, out int token))
            return token;

        token = _nextToken++;

        _tokenLookup[word] = token;
        _reverseLookup[token] = word;

        return token;
    }

    private string GetWord(int token)
    {
        if (_reverseLookup.TryGetValue(token, out string? value))
            return value;

        return "";
    }

    // ================= TRAINING =================

    public void AddSentence(string sentence)
    {
        if (string.IsNullOrWhiteSpace(sentence))
            return;

        string[] words = sentence
            .ToLower()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (words.Length < 4)
            return;

        int[] tokens = words.Select(GetToken).ToArray();

        // Store starter triple
        Starters.Add((tokens[0], tokens[1], tokens[2]));

        // Build 3-gram chain
        for (int i = 0; i < tokens.Length - 3; i++)
        {
            var key = (tokens[i], tokens[i + 1], tokens[i + 2]);

            Word nextWord = new Word(tokens[i + 3]);

            List<string> phrases = new();

            for (int j = 0; j < i; j++)
            {
                string phrase = "";

                for (int k = j; k < i; k++)
                {
                    phrase += words[k] + " ";
                }

                phrases.Add(phrase.Trim());
            }

            nextWord.Phrases = phrases;

            AddPair(key, nextWord);
        }

        var endKey = (tokens[^3], tokens[^2], tokens[^1]);
        AddPair(endKey, new Word(GetToken("")));
    }

    private void AddPair((int, int, int) key, Word word)
    {
        if (!Words.ContainsKey(key))
        {
            Words[key] = new List<Word>();
            _sums[key] = 1;
            Words[key].Add(word);
            return;
        }

        bool found = false;

        foreach (var existing in Words[key])
        {
            if (existing.Token == word.Token)
            {
                existing.Count++;
                _sums[key]++;

                // Merge phrases
                foreach (var p in word.Phrases)
                {
                    if (!existing.Phrases.Contains(p))
                        existing.Phrases.Add(p);
                }

                found = true;
                break;
            }
        }

        if (!found)
        {
            Words[key].Add(word);
            _sums[key]++;
        }
    }

    // ================= GENERATION =================

    public string GetRandomStartingWord()
    {
        var starter = Starters[_rand.Next(Starters.Count)];

        return $"{GetWord(starter.Item1)} {GetWord(starter.Item2)} {GetWord(starter.Item3)}";
    }

    public string GenerateSentence(string startingWords)
    {
        string[] parts = startingWords
            .ToLower()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 3)
            return startingWords;

        int t1 = GetToken(parts[0]);
        int t2 = GetToken(parts[1]);
        int t3 = GetToken(parts[2]);

        CurrentSentence = $"{parts[0]} {parts[1]} {parts[2]}";

        int maxLength = 30;

        while (maxLength-- > 0)
        {
            string next = GetNextWord(t1, t2, t3);

            if (string.IsNullOrEmpty(next))
                break;

            CurrentSentence += " " + next;

            int nextToken = GetToken(next);

            t1 = t2;
            t2 = t3;
            t3 = nextToken;
        }

        return CurrentSentence;
    }

    private string GetNextWord(int t1, int t2, int t3)
    {
        var key = (t1, t2, t3);

        if (!Words.TryGetValue(key, out List<Word>? choices))
            return "";

        double[] scores = new double[choices.Count];

        for (int i = 0; i < choices.Count; i++)
        {
            string wordText = GetWord(choices[i].Token);

            if (wordText == "")
                continue;

            double score = 5 * choices[i].Count;

            foreach (var phrase in choices[i].Phrases)
            {
                if (CurrentSentence.Contains(phrase))
                {
                    score += 10 * phrase.Split(' ').Length;
                    break;
                }
            }

            scores[i] = score * choices[i].Probability;
        }

        double total = scores.Sum();

        if (total == 0)
            return GetWord(choices[_rand.Next(choices.Count)].Token);

        double rand = _rand.NextDouble();
        double cumulative = 0;

        for (int i = 0; i < scores.Length; i++)
        {
            cumulative += scores[i] / total;

            if (rand <= cumulative)
                return GetWord(choices[i].Token);
        }

        return GetWord(choices.Last().Token);
    }

    // ================= PROBABILITIES =================

    public void UpdateProbabilities()
    {
        foreach (var key in Words.Keys)
        {
            foreach (var word in Words[key])
            {
                word.Probability = (double)word.Count / _sums[key];
            }
        }
    }
}