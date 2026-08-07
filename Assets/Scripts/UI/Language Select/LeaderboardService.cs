using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Fetches player profiles from Supabase and produces a sorted leaderboard list.
/// Ranking criteria (in order):
///   1. Total completed objectives (ilokano + cebuano)
///   2. Overall coins (lifetime)
///   3. Total unlocked phrases (ilokano + cebuano)
///   4. Username finalized at (earlier = higher rank, rewards loyalty)
///   5. Username alphabetical (A→Z, deterministic final fallback)
/// </summary>
public class LeaderboardService : MonoBehaviour
{
    public static LeaderboardService Instance { get; private set; }

    /// <summary>
    /// True total number of objectives per language, counted directly from the Objectives JSON files.
    /// </summary>

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// Fetches all profiles from Supabase, maps them to LeaderboardEntry,
    /// sorts by the 5-criteria ranking chain, and returns the full sorted list.
    /// The caller can take the top 10 and find the current player row.
    /// </summary>
    public async Task<List<LeaderboardEntry>> FetchLeaderboardAsync()
    {
        var result = new List<LeaderboardEntry>();

        try
        {
            var currentUser = SupabaseManager.Instance.client.Auth.CurrentUser;

            // Fetch all profiles
            var response = await SupabaseManager.Instance.client
                .From<ProfileModel>()
                .Get();

            var profiles = response?.Models;
            if (profiles == null || profiles.Count == 0)
            {
                Debug.LogWarning("[LeaderboardService] No profiles returned from Supabase.");
                return result;
            }

            foreach (var profile in profiles)
            {
                // Skip banned/suspended players
                if (profile.Status != null && profile.Status.ToLower() == "suspended") continue;
                // Skip players who haven't created a character yet (haven't really started)
                if (!profile.HasCreatedCharacter) continue;

                int iloObjectives = profile.CompletedObjectivesIlokano?.Count ?? 0;
                int cebObjectives = profile.CompletedObjectivesCebuano?.Count ?? 0;
                int iloPhrases    = profile.UnlockedPhrasesIlokano?.Count ?? 0;
                int cebPhrases    = profile.UnlockedPhrasesCebuano?.Count ?? 0;

                int maxIlokano = ProgressCalculator.GetTotalObjectivesCount("ilokano");
                int maxCebuano = ProgressCalculator.GetTotalObjectivesCount("cebuano");
                int maxTotal = maxIlokano + maxCebuano;

                float iloProgress = maxIlokano > 0 ? (iloObjectives / (float)maxIlokano) * 100f : 0f;
                float cebProgress = maxCebuano > 0 ? (cebObjectives / (float)maxCebuano) * 100f : 0f;
                float overallProgress = maxTotal > 0 ? ((iloObjectives + cebObjectives) / (float)maxTotal) * 100f : 0f;

                var entry = new LeaderboardEntry
                {
                    ProfileId                  = profile.Id,
                    Username                   = profile.Username ?? "Unknown",
                    AvatarUrl                  = profile.AvatarUrl,
                    OverallCoins               = profile.OverallCoins,
                    UsernameFinalizedAt        = profile.UsernameFinalizedAt,
                    LastActive                 = profile.LastActive,
                    IlokanoObjectivesCompleted = iloObjectives,
                    CebuanoObjectivesCompleted = cebObjectives,
                    IlokanoLessonsCompleted    = ProgressCalculator.GetCompletedLessonsCount("ilokano", profile.CompletedObjectivesIlokano),
                    CebuanoLessonsCompleted    = ProgressCalculator.GetCompletedLessonsCount("cebuano", profile.CompletedObjectivesCebuano),
                    UnlockedPhrasesIlokano     = profile.UnlockedPhrasesIlokano ?? new List<string>(),
                    UnlockedPhrasesCebuano     = profile.UnlockedPhrasesCebuano ?? new List<string>(),
                    IlokanoProgress            = iloProgress,
                    CebuanoProgress            = cebProgress,
                    OverallProgress            = overallProgress,
                    IsCurrentPlayer            = currentUser != null && profile.Id == currentUser.Id
                };

                result.Add(entry);
            }

            // Sort by the 5-criteria chain
            result = result.OrderByDescending(e => e.TotalObjectivesCompleted)
                           .ThenByDescending(e => e.OverallCoins)
                           .ThenByDescending(e => e.TotalPhrasesUnlocked)
                           .ThenBy(e => e.UsernameFinalizedAt ?? DateTime.MaxValue) // earlier = better
                           .ThenBy(e => e.Username)
                           .ToList();

            // Assign ranks after sorting
            for (int i = 0; i < result.Count; i++)
                result[i].Rank = i + 1;

            Debug.Log($"[LeaderboardService] Fetched and ranked {result.Count} players.");
        }
        catch (Exception ex)
        {
            Debug.LogError("[LeaderboardService] Failed to fetch leaderboard: " + ex.Message);
        }

        return result;
    }
}
