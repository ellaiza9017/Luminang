using UnityEngine;
using TMPro;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Supabase.Gotrue;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Postgrest.Attributes;
using Postgrest.Models;

public class SignupManager : MonoBehaviour
{
    [Header("UI Fields")]
    public TMP_InputField usernameField;
    public TMP_InputField emailField;
    public TMP_InputField passwordField;

    [Header("Settings")]
    public string loginSceneName = "LoginScene";
    public string mainMenuSceneName = "MainMenuScene";

    [Header("Status")]
    public bool isBusy = false;

    private void Start()
    {
        Debug.Log("[Signup] Manager started. Subscribing to Google Login event...");
        if (SupabaseManager.Instance != null)
        {
            SupabaseManager.Instance.OnGoogleLoginComplete += HandleGoogleSignupComplete;
            Debug.Log("[Signup] Successfully subscribed to Supabase event.");
        }
        else
        {
            Debug.LogError("[Signup] SupabaseManager Instance was null at Start! Searching in scene...");
            var manager = FindFirstObjectByType<SupabaseManager>();
            if (manager != null)
            {
                manager.OnGoogleLoginComplete += HandleGoogleSignupComplete;
                Debug.Log("[Signup] Found and subscribed to SupabaseManager manually.");
            }
        }
    }

    private void OnDestroy()
    {
        if (SupabaseManager.Instance != null)
        {
            SupabaseManager.Instance.OnGoogleLoginComplete -= HandleGoogleSignupComplete;
        }
    }

    public async void OnSignupButtonClicked()
    {
        if (isBusy) return;

        string username = usernameField.text.Trim().ToLower();
        string email = emailField.text.Trim();
        string password = passwordField.text;

        if (!IsValidUsername(username))
        {
            GenericModal.Instance.ShowAlert("Username must be 3-16 characters and contain only lowercase letters and numbers.", "Okay");
            return;
        }
        if (!IsValidEmail(email))
        {
            GenericModal.Instance.ShowAlert("Please enter a valid email address.", "Okay");
            return;
        }
        if (!IsValidPassword(password))
        {
            GenericModal.Instance.ShowAlert("Password must be at least 8 characters, include an uppercase letter, a lowercase letter, and a number.", "Okay");
            return;
        }

        isBusy = true;
        LoadingOverlay.Instance?.Show();

        try
        {
            // 1. Check if Username is taken
            Debug.Log("[Signup] Stage 1: Checking if username is available...");
            bool userExists = await CheckUsernameExists(username);
            if (userExists)
            {
                GenericModal.Instance.ShowAlert($"The username '{username}' is already taken.", "Okay");
                return;
            }

            // 2. Check if Email is already in use
            Debug.Log("[Signup] Stage 2: Checking if email is available...");
            bool emailExists = await CheckEmailExists(email);
            if (emailExists)
            {
                GenericModal.Instance.ShowAlert("This email is already in use! If you used 'Continue with Google', please log in with that instead.", "Okay");
                return;
            }

            // 3. Attempt Signup
            Debug.Log("[Signup] Stage 3: Sending signup request to Supabase...");
            var signupOptions = new SignUpOptions
            {
                Data = new Dictionary<string, object> { { "username", username } }
            };

            var response = await SupabaseManager.Instance.client.Auth.SignUp(email, password, signupOptions);
            
            if (response != null && response.User != null)
            {
                Debug.Log("<color=green>[Signup] Signup call completed successfully.</color>");
                
                // Check if we are already logged in (happens if 'Confirm Signup' is disabled in Supabase)
                var session = SupabaseManager.Instance.client.Auth.CurrentSession;
                if (session != null && session.User != null)
                {
                    Debug.Log("<color=green>[Signup] Auto-login detected! Transitioning to Main Menu...</color>");
                    SceneManager.LoadScene(mainMenuSceneName);
                }
                else
                {
                    GenericModal.Instance.ShowAlert(
                        "Account created! Please verify your email before logging in.", 
                        "Okay", 
                        () => SceneManager.LoadScene(loginSceneName)
                    );
                }
            }
            else
            {
                Debug.LogWarning("[Signup] Signup response or user was null without throwing an exception.");
                GenericModal.Instance.ShowAlert("Account creation failed. Please try again.", "Okay");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Signup] Critical Error: {ex.Message}");
            
            string friendlyMessage = "Could not create account (Network or Server Error).";
            if (ex.Message.Contains("already been registered")) 
                friendlyMessage = "This email is already in use!";
            else if (ex.Message.Contains("Database error"))
                friendlyMessage = "Database error. Please try a different username.";
            else if (ex.Message.Contains("sending the request"))
                friendlyMessage = "Network problem! Please check your internet connection and try again.";

            GenericModal.Instance.ShowAlert(friendlyMessage, "Okay");
        }
        finally
        {
            Debug.Log("[Signup] Cleaning up signup state.");
            LoadingOverlay.Instance?.Hide();
            isBusy = false;
        }
    }

    private bool IsValidUsername(string user)
    {
        if (user.Length < 3 || user.Length > 16) return false;
        return Regex.IsMatch(user, @"^[a-z0-9]+$");
    }

    private bool IsValidEmail(string email)
    {
        return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }

    private bool IsValidPassword(string pass)
    {
        return pass.Length >= 8 && Regex.IsMatch(pass, @"[A-Z]") && Regex.IsMatch(pass, @"[a-z]") && Regex.IsMatch(pass, @"[0-9]");
    }

    private async Task<bool> CheckEmailExists(string email)
    {
        try
        {
            var parameters = new Dictionary<string, object> { { "target_email", email.ToLower() } };
            var rpcResponse = await SupabaseManager.Instance.client.Rpc("check_email_exists", parameters);
            
            // The RPC returns a boolean directly
            if (rpcResponse != null && rpcResponse.Content != null)
            {
                return rpcResponse.Content.ToLower() == "true";
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Signup] RPC Error checking email: {ex.Message}");
        }
        return false;
    }

    private async Task<bool> CheckUsernameExists(string user)
    {
        var result = await SupabaseManager.Instance.client
            .From<ProfileModel>()
            .Filter("username", Postgrest.Constants.Operator.Equals, user)
            .Get();

        return result.Models.Count > 0;
    }

    private bool _waitingForGoogleLogin = false;

    private Coroutine _googleLoginTimeoutCoroutine;

    public void OnContinueWithGoogleButtonClicked()
    {
        if (isBusy) return;

        isBusy = true;
        _waitingForGoogleLogin = true;
        LoadingOverlay.Instance?.Show();

        try
        {
#if UNITY_EDITOR
            if (UnityRedirectListener.Instance != null)
            {
                UnityRedirectListener.Instance.StartEditorListener();
            }
#endif

            var redirectTo = "luminang://callback"; 
#if UNITY_EDITOR
            redirectTo = "http://localhost:54321/"; 
#endif
            string authUrl = $"{SupabaseManager.Instance.supabaseUrl}/auth/v1/authorize?provider=google&redirect_to={redirectTo}";
            
            Debug.Log($"[Signup] Opening Google signup in browser...");
            Application.OpenURL(authUrl);

            // Start a hard 60-second timeout just in case focus detection fails
            if (_googleLoginTimeoutCoroutine != null) StopCoroutine(_googleLoginTimeoutCoroutine);
            _googleLoginTimeoutCoroutine = StartCoroutine(GoogleLoginTimeout());
        }
        catch (System.Exception ex)
        {
            LoadingOverlay.Instance?.Hide();
            Debug.LogError($"[Signup] Google Error: {ex.Message}");
            GenericModal.Instance.ShowAlert("Google signup failed.", "Okay");
            isBusy = false;
            _waitingForGoogleLogin = false;
        }
    }

    private System.Collections.IEnumerator GoogleLoginTimeout()
    {
        yield return new WaitForSeconds(15f);
        if (_waitingForGoogleLogin)
        {
            Debug.Log("[Signup] Google signup timed out after 15 seconds.");
            CancelGoogleLogin();
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && _waitingForGoogleLogin)
        {
            // Give the deep link a short moment to arrive
            StartCoroutine(CheckGoogleLoginCancelled());
        }
    }

    private System.Collections.IEnumerator CheckGoogleLoginCancelled()
    {
        yield return new WaitForSeconds(2f);
        if (_waitingForGoogleLogin)
        {
            Debug.Log("[Signup] User returned but no deep link received. Cancelling loading.");
            CancelGoogleLogin();
        }
    }

    private void CancelGoogleLogin()
    {
        _waitingForGoogleLogin = false;
        if (isBusy)
        {
            isBusy = false;
            LoadingOverlay.Instance?.Hide();
            GenericModal.Instance.ShowAlert("Signup cancelled or timed out. Please try again.", "Okay");
        }
    }

    private async void HandleGoogleSignupComplete(bool success)
    {
        _waitingForGoogleLogin = false;
        Debug.Log($"[Signup] HandleGoogleSignupComplete called. Success: {success}");

        if (!success)
        {
            LoadingOverlay.Instance?.Hide();
            GenericModal.Instance.ShowAlert("Google signup failed or was cancelled.", "Okay");
            isBusy = false;
            return;
        }

        try
        {
            Debug.Log("<color=green>[Signup] Google signup successful! Fetching profile...</color>");
            
            if (UserProfileManager.Instance != null)
            {
                await UserProfileManager.Instance.FetchProfile();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Signup] Error fetching profile after Google signup: {ex.Message}");
        }

        LoadingOverlay.Instance?.Hide();
        SceneManager.LoadScene(mainMenuSceneName);
        isBusy = false;
    }
}

