using System;
using System.Collections.Generic;

/// <summary>
/// Represents a single ranked player entry on the leaderboard.
/// All data is derived from ProfileModel (the Supabase profiles table).
/// Computed at runtime by LeaderboardService — never loaded from a JSON file.
/// </summary>
public class LeaderboardEntry
{
    // --- Identity ---
    public string ProfileId { get; set; }
    public string Username { get; set; }
    public string AvatarUrl { get; set; }

    // --- Raw data from ProfileModel ---
    public int OverallCoins { get; set; }
    public DateTime? UsernameFinalizedAt { get; set; }
    public DateTime? LastActive { get; set; }

    // --- Computed from JSONB arrays ---
    public int IlokanoObjectivesCompleted { get; set; }
    public int CebuanoObjectivesCompleted { get; set; }
    public int TotalObjectivesCompleted => IlokanoObjectivesCompleted + CebuanoObjectivesCompleted;

    public int IlokanoLessonsCompleted { get; set; }
    public int CebuanoLessonsCompleted { get; set; }

    public List<string> UnlockedPhrasesIlokano { get; set; } = new List<string>();
    public List<string> UnlockedPhrasesCebuano { get; set; } = new List<string>();
    public int TotalPhrasesUnlocked => (UnlockedPhrasesIlokano?.Count ?? 0) + (UnlockedPhrasesCebuano?.Count ?? 0);

    // --- Progress % ---
    public float IlokanoProgress { get; set; }   // 0-100
    public float CebuanoProgress { get; set; }   // 0-100
    public float OverallProgress { get; set; }   // 0-100 (average)

    // --- Leaderboard state ---
    public int Rank { get; set; }
    public bool IsCurrentPlayer { get; set; }
}

