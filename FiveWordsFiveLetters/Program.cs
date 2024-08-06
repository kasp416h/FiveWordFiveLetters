using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

class Program
{
    private static readonly int WordLength = 5;
    private static readonly int TargetLength = WordLength * WordLength;
    private static ConcurrentBag<string> foundStuff = new ConcurrentBag<string>();
    private static string[] words;
    private static int[] masks;
    private static Dictionary<char, int> charFrequency;

    static void Main(string[] args)
    {
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

        var threads = new List<Thread>();

        for (int i = 0; i < masks.Length; i++)
        {
            if (masks[i] != 0)
            {
                int index = i;
                Thread thread = new Thread(() => FindFiveLetterWords(new Span<int>(new int[WordLength]), 0, masks[index], index));
                threads.Add(thread);
                thread.Start();
            }
        }

        foreach (var thread in threads)
        {
            thread.Join();
        }

        Console.WriteLine(foundStuff.Count());
        watch.Stop();
        Console.WriteLine("Time {0} Ticks; {1} ms", watch.ElapsedTicks, watch.ElapsedMilliseconds);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void FindFiveLetterWords(Span<int> combinationIndices, int depth, int combinationMask, int startingIndex)
    {
        combinationIndices[depth] = startingIndex;

        if (depth == WordLength - 1)
        {
            var builder = new System.Text.StringBuilder();
            for (int i = 0; i < WordLength; i++)
            {
                if (i > 0) builder.Append(' ');
                builder.Append(words[combinationIndices[i]]);
            }
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
        int remainingWordsNeeded = WordLength - currentCount;
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