using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class OptionsMenuManager : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_Text usernameText;
    public Button logoutButton;

    [Header("Scene Settings")]
    public string loginSceneName = "LoginScene";

    async void Start()
    {
        // 1. Initial UI state
        if (usernameText != null) usernameText.text = "Loading...";

        // 2. Fetch the player's profile data
        await FetchUserProfile();

        // 3. Hook up Logout button
        if (logoutButton != null)
        {
            logoutButton.onClick.AddListener(OnLogoutClicked);
        }
    }

    private async Task FetchUserProfile()
    {
        try
        {
            var currentUser = SupabaseManager.Instance.client.Auth.CurrentUser;
            if (currentUser != null)
            {
                // Fetch the username from our 'profiles' table
                var result = await SupabaseManager.Instance.client
                    .From<ProfileModel>()
                    .Filter("id", Postgrest.Constants.Operator.Equals, currentUser.Id)
                    .Single();

                if (result != null && !string.IsNullOrEmpty(result.Username))
                {
                    usernameText.text = result.Username;
                }
                else
                {
                    usernameText.text = "Player";
                }
            }
            else
            {
                usernameText.text = "Guest";
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Options] Failed to fetch profile: {ex.Message}");
            usernameText.text = "Error";
        }
    }

    private async void OnLogoutClicked()
    {
        Debug.Log("[Options] Logging out...");
        
        // 1. Clear local offline backup so it doesn't get pushed to the next account
        if (UserProfileManager.Instance != null)
        {
            UserProfileManager.Instance.ClearLocalBackup();
        }

        // 2. Clear lingering PlayerPrefs that are tied to the specific account
        PlayerPrefs.DeleteKey("CurrentObjective");
        PlayerPrefs.DeleteKey("FinalAssessment_Completed");
        PlayerPrefs.Save();

        // 3. Sign out from Supabase
        await SupabaseManager.Instance.client.Auth.SignOut();

        // 4. Return to the Login screen
        SceneManager.LoadScene(loginSceneName);
    }
}

