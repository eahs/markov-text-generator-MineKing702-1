namespace MarkovTextGenerator;

public class Word
{
    public int Count { get; set; } = 1;

    public double Probability { get; set; } = 0.0;

    public int Token { get; }

    public Word(int token)
    {
        Token = token;
    }

    public List<string> Phrases { get; set; } = new();

    public override string ToString() => Token.ToString();
}