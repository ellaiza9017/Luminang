using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Fetches the live leaderboard from Supabase via LeaderboardService,
/// spawns the top 10 rows, manages the 'Your Rank' footer,
/// and handles row clicks to update the Details panel.
/// </summary>
public class LeaderboardManager : MonoBehaviour
{
    [Header("List Settings (LeftGroup)")]
    public Transform listContentParent;
    public GameObject leaderboardRowPrefab;

    [Header("Your Rank Footer")]
    public LeaderboardRowItem yourRankRow;

    [Header("Details Panel (RightGroup)")]
    public LeaderboardDetailsManager detailsManager;

    [Header("Empty States")]
    public GameObject emptyStateLeft;
    public GameObject emptyStateRight;
    public List<GameObject> objectsToHideLeft;
    public List<GameObject> objectsToHideRight;

    private List<LeaderboardEntry> _allEntries = new List<LeaderboardEntry>();
    private LeaderboardRowItem _selectedRow;

    private void Start()
    {
        StartCoroutine(LoadLeaderboardCoroutine());
    }

    private IEnumerator LoadLeaderboardCoroutine()
    {
        // Show loading overlay while fetching
        if (LoadingOverlay.Instance != null) LoadingOverlay.Instance.Show();

        var task = LeaderboardService.Instance.FetchLeaderboardAsync();
        yield return new WaitUntil(() => task.IsCompleted);

        if (LoadingOverlay.Instance != null) LoadingOverlay.Instance.Hide();

        if (task.IsCompletedSuccessfully)
        {
            _allEntries = task.Result;
            DisplayLeaderboard();
        }
        else
        {
            Debug.LogError("[LeaderboardManager] Failed to load leaderboard data.");
        }
    }

    private void DisplayLeaderboard()
    {
        // Clear existing rows
        foreach (Transform child in listContentParent)
            Destroy(child.gameObject);

        if (_allEntries == null || _allEntries.Count == 0)
        {
            if (emptyStateLeft != null) emptyStateLeft.SetActive(true);
            if (emptyStateRight != null) emptyStateRight.SetActive(true);
            if (objectsToHideLeft != null) foreach (var obj in objectsToHideLeft) if (obj != null) obj.SetActive(false);
            if (objectsToHideRight != null) foreach (var obj in objectsToHideRight) if (obj != null) obj.SetActive(false);
            if (detailsManager != null) detailsManager.ClearDetails();
            return;
        }
        
        if (emptyStateLeft != null) emptyStateLeft.SetActive(false);
        if (emptyStateRight != null) emptyStateRight.SetActive(false);
        if (objectsToHideLeft != null) foreach (var obj in objectsToHideLeft) if (obj != null) obj.SetActive(true);
        if (objectsToHideRight != null) foreach (var obj in objectsToHideRight) if (obj != null) obj.SetActive(true);

        // Top 10
        var top10 = _allEntries.Take(10).ToList();
        LeaderboardRowItem firstRow = null;

        for (int i = 0; i < top10.Count; i++)
        {
            GameObject newRow = Instantiate(leaderboardRowPrefab, listContentParent, false);
            LeaderboardRowItem rowScript = newRow.GetComponent<LeaderboardRowItem>();
            if (rowScript != null)
            {
                rowScript.Setup(top10[i], this);
                if (i == 0) firstRow = rowScript;
            }
        }

        // Your Rank footer — always the current player
        var currentPlayerEntry = _allEntries.FirstOrDefault(e => e.IsCurrentPlayer);
        if (yourRankRow != null)
        {
            if (currentPlayerEntry != null)
            {
                yourRankRow.gameObject.SetActive(true);
                yourRankRow.Setup(currentPlayerEntry, this, isFooterRow: true);
            }
            else
            {
                yourRankRow.gameObject.SetActive(false);
            }
        }

        // Default auto-select the current player's row to fill the details panel
        if (yourRankRow != null && yourRankRow.gameObject.activeInHierarchy && currentPlayerEntry != null)
        {
            SelectRow(yourRankRow, currentPlayerEntry);
        }
        else if (firstRow != null)
        {
            SelectRow(firstRow, top10[0]);
        }
    }

    /// <summary>Called by LeaderboardRowItem.OnClick — selects the row and updates the details panel.</summary>
    public void SelectRow(LeaderboardRowItem clickedRow, LeaderboardEntry entry)
    {
        if (_selectedRow != null) _selectedRow.SetSelected(false);
        _selectedRow = clickedRow;
        _selectedRow.SetSelected(true);

        if (detailsManager != null)
            detailsManager.DisplayPlayerDetails(entry);
    }
}

