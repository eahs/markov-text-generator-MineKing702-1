namespace MarkovTextGenerator;

public class Chain
{
    public Dictionary<int, List<Word>> Words { get; set; } = new();
    private readonly Dictionary<int, int> _sums = new();

    private readonly Random _rand = new(System.Environment.TickCount);

    private List<int> Starters = new();

    private string CurrentSentence = "";

    private Tokenizer tokenizer = new();

    /// <summary>
    /// Returns a random starting word
    /// </summary>
    public string GetRandomStartingWord()
    {
        int token = Starters[_rand.Next() % Starters.Count];
        return tokenizer.GetWord(token);
    }

    /// <summary>
    /// Adds a sentence to the chain
    /// </summary>
    public void AddSentence(string? sentence)
    {
        string[] wordArray = sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        int startToken = tokenizer.GetToken(wordArray[0]);
        Starters.Add(startToken);

        for (int i = 0; i < wordArray.Length - 1; i++)
        {
            int token1 = tokenizer.GetToken(wordArray[i]);
            int token2 = tokenizer.GetToken(wordArray[i + 1]);

            Word word2 = new Word(token2);

            List<string> phrases = new();

            for (int j = 0; j < i; j++)
            {
                string curPhrase = "";

                for (int k = j; k < i; k++)
                {
                    curPhrase += wordArray[k] + " ";
                }

                phrases.Add(curPhrase);
            }

            word2.Phrases = phrases;

            AddPair(token1, word2);
        }

        int lastToken = tokenizer.GetToken(wordArray[wordArray.Length - 1]);
        int endToken = tokenizer.GetToken("");

        AddPair(lastToken, new Word(endToken));
    }

    /// <summary>
    /// Adds a pair of tokens to the chain
    /// </summary>
    public void AddPair(int word, Word word2)
    {
        if (!Words.ContainsKey(word))
        {
            _sums.Add(word, 1);

            Words[word] = new List<Word>();
            Words[word].Add(word2);
        }
        else
        {
            bool found = false;

            foreach (Word existing in Words[word])
            {
                if (existing.Token == word2.Token)
                {
                    found = true;

                    existing.Count++;
                    _sums[word]++;

                    // Merge phrase lists instead of losing them
                    foreach (var phrase in word2.Phrases)
                    {
                        if (!existing.Phrases.Contains(phrase))
                        {
                            existing.Phrases.Add(phrase);
                        }
                    }

                    break;
                }
            }

            if (!found)
            {
                Words[word].Add(word2);
                _sums[word]++;
            }
        }
    }

    /// <summary>
    /// Returns next token based on probabilities
    /// </summary>
    public int GetNextWord(int word)
    {
        if (Words.TryGetValue(word, out List<Word>? value))
        {
            List<Word> choices = value;

            double[] scores = new double[choices.Count];

            for (int i = 0; i < choices.Count; i++)
            {
                string nextWord = tokenizer.GetWord(choices[i].Token);
                string currentWord = tokenizer.GetWord(word);

                if (nextWord.ToLower().Trim() == currentWord.ToLower().Trim() || nextWord.Trim() == "")
                {
                    continue;
                }

                int score = 0;

                score += 5 * choices[i].Count;

                List<string> phrases = choices[i].Phrases;

                for (int j = 0; j < phrases.Count; j++)
                {
                    if (CurrentSentence.Contains(phrases[j]))
                    {
                        score += 10 * phrases[j].Split(" ").Count();
                        break;
                    }
                }

                scores[i] = score * choices[i].Probability;
            }

            double total = scores.Sum();

            if (total == 0)
                return choices[Random.Shared.Next(choices.Count)].Token;

            double[] probabilities = new double[scores.Length];

            for (int i = 0; i < scores.Length; i++)
            {
                probabilities[i] = scores[i] / total;
            }

            double rand = Random.Shared.NextDouble();
            double cumulative = 0;

            for (int i = 0; i < probabilities.Length; i++)
            {
                cumulative += probabilities[i];

                if (rand <= cumulative)
                {
                    return choices[i].Token;
                }
            }

            return choices.Last().Token;
        }

        return tokenizer.GetToken("idkbbq");
    }

    /// <summary>
    /// Generates a sentence starting from a word
    /// </summary>
    public string GenerateSentence(string startingWord)
    {
        int startToken = tokenizer.GetToken(startingWord);

        CurrentSentence = startingWord;

        int lastWord = startToken;
        int nextWord;

        int count = 0;
        int maxLength = 20;

        do
        {
            nextWord = GetNextWord(lastWord);

            string nextWordStr = tokenizer.GetWord(nextWord);

            CurrentSentence += " " + nextWordStr;

            lastWord = nextWord;

            count++;
            if (count >= maxLength)
                break;

        } while (tokenizer.GetWord(nextWord) != "");

        return CurrentSentence;
    }

    /// <summary>
    /// Updates probabilities for each word chain
    /// </summary>
    public void UpdateProbabilities()
    {
        foreach (int word in Words.Keys)
        {
            foreach (Word s in Words[word])
            {
                s.Probability = (double)s.Count / _sums[word];
            }
        }
    }
}