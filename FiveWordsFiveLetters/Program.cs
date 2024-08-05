using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

class Program
{
    private static readonly int WordLength = 5;
    private static readonly int TargetLength = WordLength * WordLength;
    private static ConcurrentBag<string> foundStuff = new ConcurrentBag<string>();
    private static string[] words;
    private static int[] masks;
    private static Dictionary<char, int> charFrequency;
    private static readonly int maxDegreeOfParallelism = Environment.ProcessorCount;


    static void Main(string[] args)
    {
        var watch = new Stopwatch();
        words = LoadWords();
        watch.Start();

        CalculateCharFrequency();

        words = words.Where(w => w.Length == WordLength && w.Distinct().Count() == WordLength)
                     .OrderByDescending(word => word.Sum(c => charFrequency[c])).ToArray();

        masks = new int[words.Length];
        Parallel.For(0, words.Length, i =>
        {
            masks[i] = GetBitMask(words[i]);
        });

        var semaphore = new SemaphoreSlim(maxDegreeOfParallelism);
        var tasks = new List<Task>();

        for (int i = masks.Length - 1; i >= 0; i--)
        {
            if (masks[i] != 0)
            {
                int index = i;
                semaphore.Wait();
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        FindFiveLetterWords(new List<int> { index }, masks[index], index - 1);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }
        }

        Task.WaitAll(tasks.ToArray());

        Console.WriteLine(foundStuff.Count());
        watch.Stop();
        Console.WriteLine("Time {0} Ticks; {1} ms", watch.ElapsedTicks, watch.ElapsedMilliseconds);
    }

    private static void FindFiveLetterWords(List<int> combinationIndices, int combinationMask, int startingIndex)
    {
        if (combinationIndices.Count == WordLength)
        {
            string combinationWord = string.Join(" ", combinationIndices.Select(index => words[index]));
            Console.WriteLine(combinationWord);
            foundStuff.Add(combinationWord);
            return;
        }

        for (int i = startingIndex; i >= 0; i--)
        {
            if (masks[i] == 0 || (combinationMask & masks[i]) != 0) continue;

            int newMask = combinationMask | masks[i];
            var newCombinationIndices = new List<int>(combinationIndices) { i };

            if (IsPromising(combinationIndices.Count + 1, newMask, i))
            {
                FindFiveLetterWords(newCombinationIndices, newMask, i - 1);
            }
        }
    }

    private static bool IsPromising(int currentCount, int newMask, int maxIndex)
    {
        int remainingWordsNeeded = WordLength - currentCount;
        int uniqueLettersNeeded = TargetLength - CountBits(newMask);

        if (uniqueLettersNeeded > remainingWordsNeeded * WordLength)
        {
            return false;
        }

        int remainingWordsCount = maxIndex;
        return remainingWordsCount >= remainingWordsNeeded;
    }

    private static int CountBits(int n)
    {
        return System.Numerics.BitOperations.PopCount((uint)n);
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
        return File.ReadAllLines("./alpha_words.txt");
    }
}