using UnityEngine;
using System.Threading.Tasks;
using Supabase.Gotrue;
using Newtonsoft.Json;

public class UserProfileManager : MonoBehaviour
{
    public static UserProfileManager Instance { get; private set; }

    /// <summary>Fired whenever the player's coin balance changes locally. Subscribe to refresh coin UI.</summary>
    public static System.Action<int> OnCoinsChanged;

    /// <summary>Fired after any objective(s) are successfully saved. Subscribe to refresh progress UI.</summary>
    public static System.Action OnObjectivesCompleted;

    public ProfileModel CurrentProfile { get; private set; } = new ProfileModel
    {
        Id = "guest-123",
        Username = "Guest Player",
        Email = "guest@example.com",
        HasCreatedCharacter = true,
        HasCompletedTutorial = true,
        HasSeenPrologue = true
    };

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private Coroutine _heartbeatCoroutine;

    public async Task FetchProfile()
    {
        try
        {
            var user = SupabaseManager.Instance.client.Auth.CurrentUser;
            if (user == null) return;

            var response = await SupabaseManager.Instance.client
                .From<ProfileModel>()
                .Where(x => x.Id == user.Id)
                .Single();

            if (response != null)
            {
                CurrentProfile = response;
                Debug.Log($"[UserProfile] Profile fetched for: {CurrentProfile.Username}");
                StartHeartbeat(); // Begin pinging Supabase every 3 minutes
            }
        }
        catch (System.Exception ex)
        {
            Debug.Log("[UserProfile] No profile found or error: " + ex.Message);
        }
    }

    public void StartHeartbeat()
    {
        if (_heartbeatCoroutine != null) StopCoroutine(_heartbeatCoroutine);
        _heartbeatCoroutine = StartCoroutine(HeartbeatRoutine());
    }

    public void StopHeartbeat()
    {
        if (_heartbeatCoroutine != null) StopCoroutine(_heartbeatCoroutine);
    }

    private System.Collections.IEnumerator HeartbeatRoutine()
    {
        // 180 seconds = 3 minutes
        var wait = new WaitForSeconds(180f); 
        
        while (true)
        {
            // Do it immediately upon starting, then every 3 minutes
            if (CurrentProfile != null && CurrentProfile.Id != "guest-123")
            {
                _ = UpdateLastActiveAsync();
            }
            yield return wait;
        }
    }

    private async Task UpdateLastActiveAsync()
    {
        try
        {
            CurrentProfile.LastActive = System.DateTime.UtcNow;
            
            // Only update the LastActive column instead of uploading the whole profile
            await SupabaseManager.Instance.client
                .From<ProfileModel>()
                .Where(x => x.Id == CurrentProfile.Id)
                .Set(x => x.LastActive, CurrentProfile.LastActive)
                .Update();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[UserProfile] Failed to send heartbeat: " + ex.Message);
        }
    }

    public async Task UpdateProfile(ProfileModel updates)
    {
        try
        {
            await SupabaseManager.Instance.client.From<ProfileModel>().Upsert(updates);
            
            // Update local cache
            if (CurrentProfile == null) CurrentProfile = updates;
            else
            {
                // Sync fields if it's the same ID
                if (CurrentProfile.Id == updates.Id)
                {
                    if (updates.Username != null) CurrentProfile.Username = updates.Username;
                    if (updates.Email != null) CurrentProfile.Email = updates.Email;
                    if (updates.EquippedOutfit != null) CurrentProfile.EquippedOutfit = updates.EquippedOutfit;
                    CurrentProfile.HasCreatedCharacter = updates.HasCreatedCharacter;
                    CurrentProfile.HasCompletedTutorial = updates.HasCompletedTutorial;
                    CurrentProfile.HasSeenPrologue = updates.HasSeenPrologue;
                    CurrentProfile.HasSeenIlocosIntro = updates.HasSeenIlocosIntro;
                    CurrentProfile.HasSeenCebuIntro = updates.HasSeenCebuIntro;
                    CurrentProfile.UsernameFinalizedAt = updates.UsernameFinalizedAt;
                    CurrentProfile.Coins = updates.Coins;
                    CurrentProfile.OverallCoins = updates.OverallCoins;
                }
            }
            Debug.Log("[UserProfile] Profile updated successfully.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[UserProfile] Error updating profile: " + ex.Message);
            throw; // Re-throw so callers know it failed!
        }
    }

    public async Task SetPrologueSeen(bool seen)
    {
        if (CurrentProfile == null) return;
        CurrentProfile.HasSeenPrologue = seen;
        await UpdateProfile(CurrentProfile);
    }

    public async Task SetIlocosIntroSeen(bool seen)
    {
        if (CurrentProfile == null) return;
        CurrentProfile.HasSeenIlocosIntro = seen;
        await UpdateProfile(CurrentProfile);
    }

    public async Task SetCebuIntroSeen(bool seen)
    {
        if (CurrentProfile == null) return;
        CurrentProfile.HasSeenCebuIntro = seen;
        await UpdateProfile(CurrentProfile);
    }

    public async Task SetTutorialCompleted(bool completed)
    {
        if (CurrentProfile == null) return;
        CurrentProfile.HasCompletedTutorial = completed;
        await UpdateProfile(CurrentProfile);
    }

    [System.Serializable]
    private class PhraseDataList
    {
        public System.Collections.Generic.List<PhraseItem> phrases;
    }
    
    [System.Serializable]
    private class PhraseItem
    {
        public string id;
        public string category;
    }

    private void UnlockPhrasesForCategory(string category, string language)
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("LuminangPhrases");
        if (jsonFile == null) return;
        
        PhraseDataList data = JsonUtility.FromJson<PhraseDataList>(jsonFile.text);
        if (data == null || data.phrases == null) return;

        var unlockedList = (language == "Cebuano") 
            ? CurrentProfile.UnlockedPhrasesCebuano 
            : CurrentProfile.UnlockedPhrasesIlokano;
            
        if (unlockedList == null)
        {
            unlockedList = new System.Collections.Generic.List<string>();
            if (language == "Cebuano") CurrentProfile.UnlockedPhrasesCebuano = unlockedList;
            else CurrentProfile.UnlockedPhrasesIlokano = unlockedList;
        }

        foreach (var phrase in data.phrases)
        {
            if (phrase.category == category && !unlockedList.Contains(phrase.id))
            {
                unlockedList.Add(phrase.id);
            }
        }
    }

    // ── Local PlayerPrefs Backup Helpers ──────────────────────────────────────
    // These are written EVERY time an objective completes so progress survives
    // even if the Supabase push fails (bad network, app crash, etc.).

    private static string LocalBackupKey(string language) =>
        language == "Cebuano" ? "LocalCompletedCebuano" : "LocalCompletedIlokano";

    private void SaveLocalBackup(string language, System.Collections.Generic.List<string> ids)
    {
        PlayerPrefs.SetString(LocalBackupKey(language), string.Join(",", ids));
        PlayerPrefs.Save();
    }

    private System.Collections.Generic.List<string> LoadLocalBackup(string language)
    {
        string raw = PlayerPrefs.GetString(LocalBackupKey(language), "");
        if (string.IsNullOrEmpty(raw)) return new System.Collections.Generic.List<string>();
        return new System.Collections.Generic.List<string>(raw.Split(','));
    }

    /// <summary>
    /// Called on game startup. Compares local PlayerPrefs backup against the
    /// Supabase data that was just fetched. If the local backup has MORE entries,
    /// the missing ones are pushed UP to Supabase so the player never gets rolled back.
    /// </summary>
    public async Task SyncLocalObjectivesWithCloud()
    {
        if (CurrentProfile == null || CurrentProfile.Id == "guest-123") return;

        foreach (string lang in new[] { "Cebuano", "Ilokano" })
        {
            var localIds  = LoadLocalBackup(lang);
            bool isCeb    = lang == "Cebuano";
            var cloudIds  = isCeb ? CurrentProfile.CompletedObjectivesCebuano
                                  : CurrentProfile.CompletedObjectivesIlokano;
            if (cloudIds == null) cloudIds = new System.Collections.Generic.List<string>();

            // Find any IDs that are in the local backup but missing from the cloud
            var missing = new System.Collections.Generic.List<string>();
            foreach (string id in localIds)
                if (!cloudIds.Contains(id)) missing.Add(id);

            if (missing.Count > 0)
            {
                Debug.Log($"[UserProfile] SyncLocalObjectivesWithCloud: Found {missing.Count} locally-saved but cloud-missing objectives in {lang}. Pushing them up now.");
                await BulkMarkObjectivesCompleted(missing, lang);
            }
            else
            {
                Debug.Log($"[UserProfile] SyncLocalObjectivesWithCloud: {lang} is in sync.");
                // Also ensure the local backup reflects cloud data (for fresh installs / new devices)
                foreach (string id in cloudIds)
                    if (!localIds.Contains(id)) localIds.Add(id);
                SaveLocalBackup(lang, localIds);
            }
        }
    }

    // ── Category unlock helper (shared) ──────────────────────────────────────
    private string GetCategoryForObjective(string objectiveId, bool isCebuano)
    {
        if (!isCebuano) return null; // Ilokano mapping can be added later
        switch (objectiveId)
        {
            case "ceb_03": return "Greetings";
            case "ceb_05": return "Gratitude";
            case "ceb_07": return "Responses";
            case "ceb_09": return "Identity";
            case "ceb_12": return "Requests";
            case "ceb_15": return "Directions";
            case "ceb_17": return "Count";
            case "ceb_21": return "Action Verbs";
            case "ceb_23": return "Linking Verbs";
            case "ceb_25": return "Pronouns";
            case "ceb_27": return "Interrogatives";
            default: return null;
        }
    }

    /// <summary>
    /// Saves multiple completed objective IDs in a SINGLE Supabase call.
    /// Avoids the race condition caused by rapid sequential updates.
    /// </summary>
    public async Task BulkMarkObjectivesCompleted(System.Collections.Generic.List<string> objectiveIds, string language)
    {
        if (CurrentProfile == null || CurrentProfile.Id == "guest-123" || objectiveIds == null || objectiveIds.Count == 0) return;

        bool isCebuano = language.Equals("Cebuano", System.StringComparison.OrdinalIgnoreCase);
        var list = isCebuano ? CurrentProfile.CompletedObjectivesCebuano : CurrentProfile.CompletedObjectivesIlokano;
        if (list == null) list = new System.Collections.Generic.List<string>();

        bool anyNew = false;
        bool categoryUnlocked = false;

        foreach (string objectiveId in objectiveIds)
        {
            if (!list.Contains(objectiveId))
            {
                list.Add(objectiveId);
                anyNew = true;
                Debug.Log($"[UserProfile] BulkMark: Queuing '{objectiveId}' for save.");

                string cat = GetCategoryForObjective(objectiveId, isCebuano);
                if (cat != null)
                {
                    UnlockPhrasesForCategory(cat, language);
                    categoryUnlocked = true;
                    Debug.Log($"[UserProfile] BulkMark: Unlocked phrases for '{cat}'.");
                }
            }
        }

        if (!anyNew) { Debug.Log("[UserProfile] BulkMark: All objectives already saved."); return; }

        // Update local profile reference and write backup BEFORE pushing to Supabase
        if (isCebuano) CurrentProfile.CompletedObjectivesCebuano = list;
        else CurrentProfile.CompletedObjectivesIlokano = list;
        SaveLocalBackup(language, list);

        try
        {
            if (isCebuano)
            {
                var q = SupabaseManager.Instance.client.From<ProfileModel>()
                    .Where(x => x.Id == CurrentProfile.Id)
                    .Set(x => x.CompletedObjectivesCebuano, list);
                if (categoryUnlocked) q = q.Set(x => x.UnlockedPhrasesCebuano, CurrentProfile.UnlockedPhrasesCebuano);
                await q.Update();
            }
            else
            {
                var q = SupabaseManager.Instance.client.From<ProfileModel>()
                    .Where(x => x.Id == CurrentProfile.Id)
                    .Set(x => x.CompletedObjectivesIlokano, list);
                if (categoryUnlocked) q = q.Set(x => x.UnlockedPhrasesIlokano, CurrentProfile.UnlockedPhrasesIlokano);
                await q.Update();
            }
            Debug.Log($"<color=green>[UserProfile] Bulk saved {objectiveIds.Count} objectives to Supabase ({language}).</color>");
            OnObjectivesCompleted?.Invoke(); // Notify UI (e.g. PlayerInfoPanel progress bar)
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[UserProfile] BulkMark Supabase push failed: {ex.Message}");
            // PlayerPrefs backup is already written, so progress is safe locally
        }
    }

    /// <summary>
    /// Auto-saves a single completed objective ID to Supabase (per-objective, not bulk).
    /// Adds it to the local profile immediately, then pushes only the changed column.
    /// Also unlocks associated phrases for the completed category in bulk.
    /// </summary>
    public async Task MarkObjectiveCompleted(string objectiveId, string language)
    {
        if (CurrentProfile == null || CurrentProfile.Id == "guest-123")
        {
            Debug.LogWarning("[UserProfile] MarkObjectiveCompleted skipped — no logged-in profile.");
            return;
        }

        bool isCebuano = language.Equals("Cebuano", System.StringComparison.OrdinalIgnoreCase);

        // 1. Update local cache immediately so SyncObjectiveWithDatabase sees it right away
        var list = isCebuano ? CurrentProfile.CompletedObjectivesCebuano : CurrentProfile.CompletedObjectivesIlokano;
        if (list == null) list = new System.Collections.Generic.List<string>();

        if (!list.Contains(objectiveId))
        {
            list.Add(objectiveId);
            if (isCebuano) CurrentProfile.CompletedObjectivesCebuano = list;
            else CurrentProfile.CompletedObjectivesIlokano = list;
        }
        else
        {
            Debug.Log($"[UserProfile] Objective '{objectiveId}' already marked complete. Skipping save.");
            return;
        }

        // 1.5 Write to local PlayerPrefs backup IMMEDIATELY (survives network failures)
        SaveLocalBackup(language, list);

        // 1.6 Map Objective to Category and unlock phrases
        string categoryToUnlock = GetCategoryForObjective(objectiveId, isCebuano);

        if (categoryToUnlock != null)
        {
            UnlockPhrasesForCategory(categoryToUnlock, language);
            Debug.Log($"[UserProfile] Unlocked phrases for category '{categoryToUnlock}' in {language}.");
        }

        // 2. Push relevant columns to Supabase
        try
        {
            if (isCebuano)
            {
                var query = SupabaseManager.Instance.client
                    .From<ProfileModel>()
                    .Where(x => x.Id == CurrentProfile.Id)
                    .Set(x => x.CompletedObjectivesCebuano, CurrentProfile.CompletedObjectivesCebuano);
                    
                if (categoryToUnlock != null)
                {
                    query = query.Set(x => x.UnlockedPhrasesCebuano, CurrentProfile.UnlockedPhrasesCebuano);
                }
                
                await query.Update();
            }
            else
            {
                var query = SupabaseManager.Instance.client
                    .From<ProfileModel>()
                    .Where(x => x.Id == CurrentProfile.Id)
                    .Set(x => x.CompletedObjectivesIlokano, CurrentProfile.CompletedObjectivesIlokano);
                    
                if (categoryToUnlock != null)
                {
                    query = query.Set(x => x.UnlockedPhrasesIlokano, CurrentProfile.UnlockedPhrasesIlokano);
                }
                
                await query.Update();
            }
            Debug.Log($"<color=green>[UserProfile] Objective '{objectiveId}' (and phrases if any) saved to Supabase ({language}).</color>");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[UserProfile] Failed to save objective '{objectiveId}': {ex.Message}");
        }
    }


    public EquippedOutfitData GetEquippedOutfitData()
    {
        if (CurrentProfile == null || CurrentProfile.EquippedOutfit == null) return null;

        try 
        {
            // If it's already the right type, just return it
            if (CurrentProfile.EquippedOutfit is EquippedOutfitData data) return data;

            // Otherwise, deserialize from JSON (handling the object type from Supabase)
            string json = JsonConvert.SerializeObject(CurrentProfile.EquippedOutfit);
            return JsonConvert.DeserializeObject<EquippedOutfitData>(json);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[UserProfile] Failed to parse outfit data: " + ex.Message);
            return null;
        }
    }
    public async Task AddCoins(int amount)
    {
        if (amount <= 0) return;

        // 1. Always update PlayerPrefs first
        int currentPrefsCoins = PlayerPrefs.GetInt("PlayerCoins", 0);
        PlayerPrefs.SetInt("PlayerCoins", currentPrefsCoins + amount);
        PlayerPrefs.Save();

        // 2. If it's a guest, fire the event and stop here
        if (CurrentProfile == null || CurrentProfile.Id == "guest-123")
        {
            OnCoinsChanged?.Invoke(currentPrefsCoins + amount);
            return;
        }

        // 3. Update memory profile
        CurrentProfile.Coins += amount;
        CurrentProfile.OverallCoins += amount;

        // 4. Fire the event immediately so UI updates without waiting for Supabase
        OnCoinsChanged?.Invoke(CurrentProfile.Coins);
        try
        {
            await SupabaseManager.Instance.client
                .From<ProfileModel>()
                .Where(x => x.Id == CurrentProfile.Id)
                .Set(x => x.Coins, CurrentProfile.Coins)
                .Set(x => x.OverallCoins, CurrentProfile.OverallCoins)
                .Update();
            Debug.Log($"<color=green>[UserProfile] Synchronized +{amount} coins to cloud. Total: {CurrentProfile.Coins}</color>");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[UserProfile] Failed to synchronize +{amount} coins: {ex.Message}");
        }
    }
}
