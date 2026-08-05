using UnityEngine;
using System.Threading.Tasks;
using Supabase.Gotrue;
using Newtonsoft.Json;

public class UserProfileManager : MonoBehaviour
{
    public static UserProfileManager Instance { get; private set; }

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
}
