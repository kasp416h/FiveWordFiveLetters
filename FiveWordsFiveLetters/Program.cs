class Program
{
    private static int fiveLetterWordsLength = 25;
    private static List<string> foundStuff = [];
    private static string[] words = [];
    private static List<int> masks = [];


    static void Main(string[] args)
    {
        var watch = new System.Diagnostics.Stopwatch();

        words = LoadWords();

        watch.Start();

        for (int i = 0; i < words.Length; i++)
        {
            string word = words[i];

            if (word.Length != 5)
            {
                masks.Add(0);
                continue;
            }

            int mask = GetBitMask(word);
            if (mask == 0)
            {
                masks.Add(0);
                continue;
            }

            masks.Add(mask);
        }

        for (int i = 0; i < masks.Count; i++)
        {
            FindFiveLetterWords(words[i], masks[i], i + 1);
        }

        Console.WriteLine(foundStuff.Count());

        watch.Stop();

        Console.WriteLine("Time {0} Ticks; {1} ms", watch.ElapsedTicks, watch.ElapsedMilliseconds);
    }

    private static void FindFiveLetterWords(string combinationWord, int combinationMask, int startingIndex)
    {
        for (int i = startingIndex; i < masks.Count; i++)
        {
            if (words[i].Length != 5) continue;

            if ((combinationMask & masks[i]) != 0) continue;

            int newMask = combinationMask | masks[i];
            string newCombinationWord = combinationWord + words[i];

            if (newCombinationWord.Length == fiveLetterWordsLength)
            {
                Console.WriteLine(newCombinationWord);
                foundStuff.Add(newCombinationWord);
                break;
            }
            else
            {
                FindFiveLetterWords(newCombinationWord, newMask, i + 1);
            }

        }
    }

    private static int GetBitMask(string word)
    {
        int mask = 0;

        foreach (char c in word)
        {
            int bit = 1 << (c - 'a');

            if ((mask & bit) != 0) return 0;

            mask |= bit;
        }

        return mask;
    }

    private static string[] LoadWords()
    {
        string[] words = File.ReadAllLines("./alpha_words.txt");
        return words;
    }
}