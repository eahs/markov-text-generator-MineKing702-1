using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarkovTextGenerator;

public class Tokenizer
{
    private Dictionary<string, int> _wordToToken = new();
    private Dictionary<int, string> _tokenToWord = new();

    private int _nextToken = 0;

    public int GetToken(string word)
    {
        word = word.Trim();

        if (!_wordToToken.ContainsKey(word))
        {
            _wordToToken[word] = _nextToken;
            _tokenToWord[_nextToken] = word;
            _nextToken++;
        }

        return _wordToToken[word];
    }

    public string GetWord(int token)
    {
        return _tokenToWord[token];
    }
}