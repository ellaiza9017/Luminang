using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RegionSelectionManager : MonoBehaviour
{
    public static RegionSelectionManager Instance { get; private set; }

    [Header("Responsive Zoom")]
    public RectTransform mapContainer; 
    public RegionInfoPanel infoPanel; // Your new custom panel!
    public PlayerInfoPanel playerInfoPanel; // The profile panel at the top-left
    public HUDGroupManager hudGroupManager; // The settings/journal buttons at the right

    [Tooltip("Offset from center (-0.5 to 0.5). -0.1666 matches exactly 1/3 from the left of the screen.")]
    public float horizontalOffsetPercent = -0.1666f; 
    public float zoomDuration = 0.5f;
    public AnimationCurve zoomCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    private Vector2 originalPos;
    private Vector3 originalScale;
    private bool isZoomed = false;
    private Coroutine zoomCoroutine;
    private RegionClickable currentRegion; // Track the selection

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        if (mapContainer != null)
        {
            originalPos = mapContainer.anchoredPosition;
            originalScale = mapContainer.localScale;
        }
    }

    public void SelectRegion(RegionClickable region)
    {
        if (isZoomed) return;
        
        isZoomed = true;
        currentRegion = region;
        currentRegion.SetSelected(true); // Show the glow!

        if (zoomCoroutine != null) StopCoroutine(zoomCoroutine);
        
        // Responsive Math: Calculate exactly where the island should go (1/3 from left)
        float canvasWidth = mapContainer.GetComponentInParent<Canvas>().GetComponent<RectTransform>().rect.width;
        float xOffset = canvasWidth * horizontalOffsetPercent;

        // Position the map so the island ends up at the desired screen coordinates
        Vector2 regionPos = region.data.zoomPosition == Vector3.zero 
            ? region.GetComponent<RectTransform>().anchoredPosition 
            : (Vector2)region.data.zoomPosition;

        Vector2 targetPos = new Vector2(xOffset, 0) 
                          - (regionPos * region.data.zoomOrthographicSize) 
                          + region.data.zoomOffsetOverride;
        
        zoomCoroutine = StartCoroutine(ZoomUI(targetPos, Vector3.one * region.data.zoomOrthographicSize));
        
        // Calculate the screen position of the clicked island
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, region.GetComponent<RectTransform>().position);
        Vector2 localPoint;
        
        // Convert screen position to local position relative to the InfoPanel's parent
        RectTransform parentRect = infoPanel.GetComponent<RectTransform>().parent as RectTransform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, null, out localPoint);

        // Calculate the responsive target position for the panel (beside the island)
        // The island center is at 'horizontalOffsetPercent'. The right screen edge is at +0.5.
        // Assuming your panel is anchored to the middle-Right (X=1, Y=0.5):
        float panelTargetX = canvasWidth * (horizontalOffsetPercent / 2f - 0.25f); 
        Vector2 targetPanelPos = new Vector2(panelTargetX, 0); // Y=0 centers it vertically

        // Show your NEW Custom Panel with its starting position AND its dynamic target!
        if (infoPanel != null) infoPanel.Show(region, localPoint, targetPanelPos);

        // Slide the clouds back to create space for the panel!
        if (MapTransitionManager.Instance != null) MapTransitionManager.Instance.SetPanelFocus(true);

        // Slide out UI panels to clear space!
        if (playerInfoPanel != null) playerInfoPanel.Hide();
        if (hudGroupManager != null) hudGroupManager.Hide();

    }

    public void ResetZoom()
    {
        if (!isZoomed) return;
        
        isZoomed = false;
        if (currentRegion != null)
        {
            currentRegion.SetSelected(false); // Hide the glow!
            currentRegion = null;
        }

        // Hide your Custom Panel!
        if (infoPanel != null) infoPanel.Hide();

        // Slide the clouds back in to the normal "Map View"!
        if (MapTransitionManager.Instance != null) MapTransitionManager.Instance.SetPanelFocus(false);

        // Slide UI panels back in!
        if (playerInfoPanel != null) playerInfoPanel.Show();
        if (hudGroupManager != null) hudGroupManager.Show();


        if (zoomCoroutine != null) StopCoroutine(zoomCoroutine);
        zoomCoroutine = StartCoroutine(ZoomUI(originalPos, originalScale));
    }

    public void OnStartRegion(RegionClickable region)
    {
        Debug.Log($"[Map] Starting region: {region.data.regionName}");
        StartCoroutine(TransitionToTutorial());
    }

    private IEnumerator TransitionToTutorial()
    {
        // 1. Close the Map (Gathers clouds)
        if (MapTransitionManager.Instance != null)
        {
            MapTransitionManager.Instance.CloseMap();
            // Wait for the clouds and the "stagger" effect to fully cover the screen
            float totalWait = MapTransitionManager.Instance.transitionDuration + MapTransitionManager.Instance.staggerStrength;
            yield return new WaitForSeconds(totalWait);
        }

        // 2. Decide where to go based on progress
        string sceneToLoad = "TutorialScene";
        if (UserProfileManager.Instance != null && UserProfileManager.Instance.CurrentProfile != null)
        {
            if (UserProfileManager.Instance.CurrentProfile.HasCompletedTutorial)
            {
                if (currentRegion != null && currentRegion.data != null)
                {
                    string region = currentRegion.data.regionName.ToLower();
                    if (region.Contains("ilocos") || region.Contains("crisologo"))
                    {
                        if (!UserProfileManager.Instance.CurrentProfile.HasSeenIlocosIntro)
                            sceneToLoad = "IlocosIntroScene";
                        else
                            sceneToLoad = "Calle_Crisologo";
                    }
                    else if (region.Contains("cebu") || region.Contains("magellan"))
                    {
                        if (!UserProfileManager.Instance.CurrentProfile.HasSeenCebuIntro)
                            sceneToLoad = "CebuIntroScene"; // For when you make this later
                        else
                            sceneToLoad = "Magellan's_Cross";
                    }
                    else
                    {
                        sceneToLoad = "Calle_Crisologo";
                    }
                }
                else
                {
                    sceneToLoad = "Calle_Crisologo";
                }
                Debug.Log($"[Map] Tutorial already completed. Skipping to {sceneToLoad}...");
            }
        }

        Debug.Log($"[Map] Transitioning to {sceneToLoad}...");
        
        // Find the SceneLoader in the scene and use it
        var loader = FindFirstObjectByType<SceneLoader>();
        if (loader != null)
        {
            loader.LoadScene(sceneToLoad);
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneToLoad);
        }
    }

    private IEnumerator ZoomUI(Vector2 targetPos, Vector3 targetScale)
    {
        float elapsed = 0;
        Vector2 startPos = mapContainer.anchoredPosition;
        Vector3 startScale = mapContainer.localScale;

        while (elapsed < zoomDuration)
        {
            elapsed += Time.deltaTime;
            float t = zoomCurve.Evaluate(elapsed / zoomDuration);
            
            mapContainer.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            mapContainer.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }
        
        mapContainer.anchoredPosition = targetPos;
        mapContainer.localScale = targetScale;
        zoomCoroutine = null;
    }
}
