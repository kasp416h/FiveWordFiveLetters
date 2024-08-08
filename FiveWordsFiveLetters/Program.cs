using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

class Program
{
    private static int WordLength;
    private static int NumberOfWords;
    private static int TargetLength;
    private static ConcurrentBag<string> foundStuff = new ConcurrentBag<string>();
    private static string[] words;
    private static int[] masks;
    private static Dictionary<char, int> charFrequency;

    static void Main(string[] args)
    {
        Console.WriteLine("How many letters do you want in each word?");
        WordLength = int.Parse(Console.ReadLine());

        Console.WriteLine("How many words do you want to find?");
        NumberOfWords = int.Parse(Console.ReadLine());

        TargetLength = WordLength * NumberOfWords;

        var watch = new Stopwatch();
        words = LoadWords();
        watch.Start();

        CalculateCharFrequency();

        words = words.Where(w => w.Length == WordLength && w.Distinct().Count() == WordLength)
                     .OrderBy(word => word.Sum(c => charFrequency[c])).ToArray();

        masks = new int[words.Length];
        Parallel.For(0, words.Length, i =>
        {
            masks[i] = GetBitMask(words[i]);
        });

        var tasks = new List<Task>();

        for (int i = 0; i < masks.Length; i++)
        {
            if (masks[i] != 0)
            {
                int index = i;
                tasks.Add(Task.Run(() => FindFiveLetterWords(new int[NumberOfWords], 0, masks[index], index)));
            }
        }

        Task.WaitAll(tasks.ToArray());

        Console.WriteLine(foundStuff.Count());
        watch.Stop();
        Console.WriteLine("Time {0} Ticks; {1} ms", watch.ElapsedTicks, watch.ElapsedMilliseconds);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void FindFiveLetterWords(int[] combinationIndices, int depth, int combinationMask, int startingIndex)
    {
        combinationIndices[depth] = startingIndex;

        if (depth == NumberOfWords - 1)
        {
            var builder = new System.Text.StringBuilder();
            for (int i = 0; i < NumberOfWords; i++)
            {
                if (i > 0) builder.Append(' ');
                builder.Append(words[combinationIndices[i]]);
            }
            Console.WriteLine(builder.ToString());
            foundStuff.Add(builder.ToString());
            return;
        }

        for (int i = startingIndex - 1; i >= 0; i--)
        {
            if (masks[i] == 0 || (combinationMask & masks[i]) != 0) continue;

            int newMask = combinationMask | masks[i];
            if (IsPromising(depth + 1, newMask, i))
            {
                FindFiveLetterWords(combinationIndices, depth + 1, newMask, i);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsPromising(int currentCount, int newMask, int maxIndex)
    {
        int remainingWordsNeeded = NumberOfWords - currentCount;
        int uniqueLettersNeeded = TargetLength - CountBits(newMask);

        if (uniqueLettersNeeded > remainingWordsNeeded * WordLength)
        {
            return false;
        }

        int remainingWordsCount = maxIndex + 1;
        return remainingWordsCount >= remainingWordsNeeded;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CountBits(int n)
    {
        return BitOperations.PopCount((uint)n);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    private static void CalculateCharFrequency()
    {
        charFrequency = new Dictionary<char, int>();
        foreach (var word in words)
        {
            foreach (var c in word)
            {
                if (charFrequency.ContainsKey(c))
                {
                    charFrequency[c]++;
                }
                else
                {
                    charFrequency[c] = 1;
                }
            }
        }
    }

    private static string[] LoadWords()
    {
        string directory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        string filePath = Path.Combine(directory, "alpha_words.txt");
        return File.ReadAllLines(filePath);
    }
}