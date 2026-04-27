using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public static class FuzzyMatcher
{
    private static readonly string[] FillerWords = { "po", "sir", "maam", "m'am", "ahm", "uhm" };

    public static string Normalize(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";

        // Lowercase and remove punctuation (including underscores used as placeholders)
        string result = input.ToLower();
        result = Regex.Replace(result, @"[^\w\s]", "");
        result = result.Replace("_", ""); // Explicitly remove underscores if they survived regex

        // Split into words
        var words = result.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).ToList();

        // Remove filler words
        words.RemoveAll(w => FillerWords.Contains(w));

        return string.Join(" ", words).Trim();
    }

    public static float GetSimilarity(string source, string target)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target)) return 0;

        source = Normalize(source);
        target = Normalize(target);

        if (source == target) return 1.0f;

        int distance = LevenshteinDistance(source, target);
        int maxLength = Math.Max(source.Length, target.Length);

        return 1.0f - ((float)distance / maxLength);
    }

    private static int LevenshteinDistance(string s, string t)
    {
        int n = s.Length;
        int m = t.Length;
        int[,] d = new int[n + 1, m + 1];

        if (n == 0) return m;
        if (m == 0) return n;

        for (int i = 0; i <= n; d[i, 0] = i++) ;
        for (int j = 0; j <= m; d[0, j] = j++) ;

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }
        return d[n, m];
    }
}
