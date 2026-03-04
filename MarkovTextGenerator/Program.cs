namespace MarkovTextGenerator;

public class Program
{
    static void Main(string[] args)
    {
        Chain chain = new Chain();

        Console.WriteLine("Welcome to Marky Markov's Random Text Generator!");

        LoadText("Sample.txt", chain);

        // Now let's update all the probabilities with the new data
        chain.UpdateProbabilities();

        var startWord = chain.GetRandomStartingWord();
        var sentence = chain.GenerateSentence(startWord);
        Console.WriteLine(sentence);
    }

    static void LoadText(string filename, Chain chain)
    {
        int counter = 0;

        string path = Path.Combine(Environment.CurrentDirectory, $"data\\{filename}");

        var lines = File.ReadAllLines(path);
        foreach (var line in lines)
        {
            chain.AddSentence(line);
            counter++;
        }
    }
}

