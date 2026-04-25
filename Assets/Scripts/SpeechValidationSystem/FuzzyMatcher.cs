using System;
using System.Text.RegularExpressions;
using System.Linq;

namespace Luminang.SpeechValidation
{
    public static class FuzzyMatcher
    {
        // Add more filler words here as needed
        private static readonly string[] fillerWords = { "po", "sir", "maam", "uh", "um", "ah", "eh" };

        public static float GetSimilarity(string input, string target)
        {
            string normInput = Normalize(input);
            string normTarget = Normalize(target);

            // Handle the "___" placeholder logic
            if (target.Contains("___"))
            {
                normTarget = normTarget.Replace("___", "").Trim();
                
                // If the target is just empty after removing placeholder
                if (string.IsNullOrEmpty(normTarget)) return 1.0f;

                // For placeholders, we want to match the target prefix/suffix in the input.
                // We'll calculate the similarity against the closest matching substring in the input 
                // of roughly the same length as the normalized target.
                return GetBestSubstringSimilarity(normInput, normTarget);
            }

            return CalculateSimilarity(normInput, normTarget);
        }

        public static string Normalize(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";

            // Convert to lowercase
            text = text.ToLowerInvariant();

            // Remove punctuation using regex
            text = Regex.Replace(text, @"[^\w\s]", "");

            // Split into words, remove fillers, and rejoin
            var words = text.Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var filteredWords = words.Where(w => !fillerWords.Contains(w));

            return string.Join(" ", filteredWords).Trim();
        }

        private static float CalculateSimilarity(string source, string target)
        {
            if (string.IsNullOrEmpty(source) && string.IsNullOrEmpty(target)) return 1.0f;
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target)) return 0.0f;

            int distance = ComputeLevenshteinDistance(source, target);
            int maxLength = Math.Max(source.Length, target.Length);

            return (float)(maxLength - distance) / maxLength;
        }

        private static float GetBestSubstringSimilarity(string source, string target)
        {
            if (source.Length <= target.Length)
            {
                return CalculateSimilarity(source, target);
            }

            // Slide a window of target.Length over source to find best match
            float bestSimilarity = 0f;
            int windowSize = target.Length;
            
            // Allow window size to vary slightly for minor misspellings expanding length
            for (int w = windowSize - 2; w <= windowSize + 2; w++)
            {
                if (w < 1 || w > source.Length) continue;

                for (int i = 0; i <= source.Length - w; i++)
                {
                    string sub = source.Substring(i, w);
                    float sim = CalculateSimilarity(sub, target);
                    if (sim > bestSimilarity)
                    {
                        bestSimilarity = sim;
                    }
                }
            }

            return bestSimilarity;
        }

        private static int ComputeLevenshteinDistance(string a, string b)
        {
            int n = a.Length;
            int m = b.Length;
            int[,] d = new int[n + 1, m + 1];

            if (n == 0) return m;
            if (m == 0) return n;

            for (int i = 0; i <= n; i++) d[i, 0] = i;
            for (int j = 0; j <= m; j++) d[0, j] = j;

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (b[j - 1] == a[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            return d[n, m];
        }
    }
}
