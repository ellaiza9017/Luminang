using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using TMPro;

/// <summary>
/// Genshin Impact Style Quest Path Tracker.
/// Renders a scattered corridor of glowing golden sparkles floating in place at ground level.
/// Mobile (APK) optimized with smooth frame-by-frame start-point tracking and throttled NavMesh path calculations.
/// </summary>
public class QuestPathTracker : MonoBehaviour
{
    public static QuestPathTracker Instance { get; private set; }

    [Header("Player & Target Sync")]
    [Tooltip("Reference to the player Transform. If null, automatically finds GameObject with tag 'Player'.")]
    public Transform playerTransform;

    [Tooltip("Offset above ground for player's feet (0.05 = right at ground level).")]
    public float groundYOffset = 0.05f;

    [Tooltip("Hide path when player is within this distance of the target.")]
    public float stopDistance = 3f;

    [Header("Mobile Pathfinding & Smoothness Optimization")]
    [Tooltip("How often to recalculate the NavMesh ground path in seconds.")]
    public float pathUpdateInterval = 0.15f;

    [Tooltip("Minimum distance player must move before recalculating path immediately.")]
    public float minMoveDistanceToRecalculate = 0.3f;

    [Tooltip("Speed of smooth path bending when player moves or rotates sideways.")]
    public float pathSmoothSpeed = 16f;

    [Tooltip("Max path distance ahead to render sparkles (meters).")]
    public float maxTrailDistance = 25f;

    [Header("Sparkle Visuals (Hovering Style)")]
    [Tooltip("ParticleSystem for golden sparkles. If null, will automatically create one.")]
    public ParticleSystem sparkleParticles;

    [Tooltip("Total number of active floating sparkles along the path.")]
    public int particleCount = 35;

    [Tooltip("Scatter width of the sparkle trail corridor on the ground.")]
    public float pathScatterWidth = 0.6f;

    [Tooltip("Base size of each glowing sparkle particle.")]
    public float sparkleSize = 0.35f;

    [Tooltip("Speed of independent hovering animation.")]
    public float hoverSpeed = 2.5f;

    [Tooltip("Vertical height range of hovering in place.")]
    public float hoverAmount = 0.12f;

    public Color sparkleColorStart = new Color(1f, 0.9f, 0.4f, 1f); // Bright Golden Yellow
    public Color sparkleColorEnd = new Color(1f, 0.6f, 0.1f, 0.8f);   // Warm Amber Gold

    [Header("Optional Line Ribbon")]
    [Tooltip("Enable if you also want a line underneath the sparkles (Disabled by default).")]
    public bool showLineRenderer = false;

    public LineRenderer pathLineRenderer;
    public float lineWidth = 0.15f;

    [Header("UI Distance Display (Optional)")]
    public TextMeshProUGUI distanceText;

    // Deterministic Scatter Seeds
    private struct ParticleSeed
    {
        public float scatterX;
        public float scatterZ;
        public float phase;
        public float sizeMult;
    }

    private ParticleSeed[] _seeds;
    private QuestTargetMarker _activeMarker;
    private NavMeshPath _navMeshPath;
    private readonly List<Vector3> _pathPoints = new List<Vector3>();
    private readonly List<Vector3> _rawTargetPoints = new List<Vector3>();
    private float _nextPathUpdateTime;
    private Vector3 _lastPlayerPos;
    private bool _isVisible = false;
    private ParticleSystem.Particle[] _particleBuffer;
    private static Texture2D _generatedSparkleTex;
    private static Material _generatedSparkleMat;

    private static bool _markersDirty = false;
    
    private TextMeshProUGUI _dynamicDistanceText;

    public static void NotifyMarkersChanged()
    {
        _markersDirty = true;
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(gameObject); return; }

        _navMeshPath = new NavMeshPath();
        _particleBuffer = new ParticleSystem.Particle[80];
        InitializeSeeds(80);

        EnsureComponentsExist();
    }

    private void InitializeSeeds(int count)
    {
        _seeds = new ParticleSeed[count];
        Random.State prevState = Random.state;
        Random.InitState(1337);

        for (int i = 0; i < count; i++)
        {
            Vector2 circle = Random.insideUnitCircle;
            _seeds[i] = new ParticleSeed
            {
                scatterX = circle.x,
                scatterZ = circle.y,
                phase = Random.Range(0f, Mathf.PI * 2f),
                sizeMult = Random.Range(0.75f, 1.25f)
            };
        }

        Random.state = prevState;
    }

    private void OnEnable()
    {
        ObjectiveManager.OnObjectiveChanged += HandleObjectiveChanged;
        
        if (ObjectiveManager.Instance != null)
        {
            HandleObjectiveChanged(ObjectiveManager.Instance.CurrentObjective);
        }
    }

    private void OnDisable()
    {
        ObjectiveManager.OnObjectiveChanged -= HandleObjectiveChanged;
        SetVisibility(false);
    }

    private void Start()
    {
        FindPlayer();
        FindMatchingTarget(ObjectiveManager.Instance != null ? ObjectiveManager.Instance.CurrentObjective : "");
    }

    private void FindPlayer()
    {
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
        }
    }

    private void HandleObjectiveChanged(string newObjective)
    {
        FindMatchingTarget(newObjective);
    }

    public void FindMatchingTarget(string objectiveName)
    {
        _activeMarker = null;

        if (string.IsNullOrEmpty(objectiveName))
        {
            SetVisibility(false);
            return;
        }

        // 1. Search registered QuestTargetMarkers
        IReadOnlyList<QuestTargetMarker> markers = QuestTargetMarker.AllMarkers;
        for (int i = 0; i < markers.Count; i++)
        {
            if (markers[i] != null && markers[i].MatchesObjective(objectiveName))
            {
                _activeMarker = markers[i];
                break;
            }
        }

        // 2. Fallback: Search QuestIndicator objects
        if (_activeMarker == null)
        {
            QuestIndicator[] indicators = FindObjectsByType<QuestIndicator>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < indicators.Length; i++)
            {
                if (indicators[i] != null && 
                    !string.IsNullOrEmpty(indicators[i].requiredObjective) &&
                    objectiveName.StartsWith(indicators[i].requiredObjective.Trim(), System.StringComparison.OrdinalIgnoreCase))
                {
                    QuestTargetMarker dynamicMarker = indicators[i].GetComponent<QuestTargetMarker>();
                    if (dynamicMarker == null)
                    {
                        dynamicMarker = indicators[i].gameObject.AddComponent<QuestTargetMarker>();
                        dynamicMarker.requiredObjective = indicators[i].requiredObjective;
                    }
                    _activeMarker = dynamicMarker;
                    break;
                }
            }
        }

        _markersDirty = false;
        bool hasTarget = _activeMarker != null;
        if (hasTarget)
        {
            RecalculatePath();
        }

        SetVisibility(hasTarget);
    }

    private void Update()
    {
        if (playerTransform == null)
        {
            FindPlayer();
            if (playerTransform == null) return;
        }

        if (_markersDirty)
        {
            FindMatchingTarget(ObjectiveManager.Instance != null ? ObjectiveManager.Instance.CurrentObjective : "");
        }

        if (_activeMarker == null)
        {
            if (_isVisible) SetVisibility(false);
            return;
        }

        // Check dialogue status (Hide path during dialogue)
        bool isInDialogue = DialogueManager.Instance != null && DialogueManager.Instance.IsInDialogue;
        float distToTarget = Vector3.Distance(playerTransform.position, _activeMarker.TargetPosition);
        
        bool shouldShow = !isInDialogue && distToTarget > stopDistance;

        if (shouldShow != _isVisible)
        {
            SetVisibility(shouldShow);
        }

        if (!_isVisible) return;

        // Update UI distance (Dynamically create it if missing)
        if (distanceText == null && _dynamicDistanceText == null) EnsureDistanceTextExists();

        TextMeshProUGUI activeText = distanceText != null ? distanceText : _dynamicDistanceText;
        if (activeText != null)
        {
            activeText.text = $"{Mathf.RoundToInt(distToTarget)}m";
        }

        // Throttled NavMesh recalculation check
        float movedDist = Vector3.Distance(playerTransform.position, _lastPlayerPos);
        if (Time.time >= _nextPathUpdateTime || movedDist >= minMoveDistanceToRecalculate)
        {
            _nextPathUpdateTime = Time.time + pathUpdateInterval;
            _lastPlayerPos = playerTransform.position;
            RecalculatePath();
        }

        // EVERY FRAME: Smoothly update player feet start position & lerp path points
        UpdateSmoothPath();

        UpdateHoveringParticles();
    }

    private void RecalculatePath()
    {
        if (playerTransform == null || _activeMarker == null) return;

        Vector3 startPos = GetPlayerFeetPosition();
        Vector3 targetPos = _activeMarker.TargetPosition;

        // Snap start and target positions to the nearest NavMesh surface
        if (NavMesh.SamplePosition(startPos, out NavMeshHit startHit, 10.0f, NavMesh.AllAreas))
        {
            startPos = startHit.position;
        }
        if (NavMesh.SamplePosition(targetPos, out NavMeshHit targetHit, 10.0f, NavMesh.AllAreas))
        {
            targetPos = targetHit.position;
        }

        _rawTargetPoints.Clear();

        // Always use the fallback straight-line method to go through objects
        int samples = 8;
        for (int i = 0; i <= samples; i++)
        {
            float t = i / (float)samples;
            Vector3 lerpPos = Vector3.Lerp(startPos, targetPos, t);
            
            // Sample the navmesh vertically to stay flat against the ground
            if (NavMesh.SamplePosition(lerpPos, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
            {
                lerpPos = hit.position + Vector3.up * groundYOffset;
            }
            else
            {
                lerpPos.y = startPos.y + groundYOffset; // Keep it flat if all else fails
            }
            
            _rawTargetPoints.Add(lerpPos);
        }

        FilterTargetPointsToMaxDistance(maxTrailDistance);

        if (_pathPoints.Count == 0)
        {
            _pathPoints.AddRange(_rawTargetPoints);
        }
    }

    private Vector3 GetPlayerFeetPosition()
    {
        Vector3 startPos = playerTransform.position;
        int layerMask = ~(1 << playerTransform.gameObject.layer); // Ignore the player's own layer
        
        if (Physics.Raycast(startPos + Vector3.up * 1f, Vector3.down, out RaycastHit feetHit, 5f, layerMask))
        {
            return feetHit.point + Vector3.up * groundYOffset;
        }
        return startPos + Vector3.up * groundYOffset;
    }

    private void UpdateSmoothPath()
    {
        if (_rawTargetPoints.Count == 0) return;

        // Ensure path points list matches target count
        while (_pathPoints.Count < _rawTargetPoints.Count)
        {
            _pathPoints.Add(_rawTargetPoints[_pathPoints.Count]);
        }
        while (_pathPoints.Count > _rawTargetPoints.Count)
        {
            _pathPoints.RemoveAt(_pathPoints.Count - 1);
        }

        // 1. Instant update of path start point at player's feet every frame
        Vector3 currentFeet = GetPlayerFeetPosition();
        _pathPoints[0] = currentFeet;
        _rawTargetPoints[0] = currentFeet;

        // 2. Smoothly lerp intermediate path points to eliminate turns/sideway jerks
        float dtLerp = Mathf.Clamp01(Time.deltaTime * pathSmoothSpeed);
        for (int i = 1; i < _pathPoints.Count; i++)
        {
            _pathPoints[i] = Vector3.Lerp(_pathPoints[i], _rawTargetPoints[i], dtLerp);
        }

        // Update optional LineRenderer
        if (pathLineRenderer != null && showLineRenderer)
        {
            pathLineRenderer.positionCount = _pathPoints.Count;
            for (int i = 0; i < _pathPoints.Count; i++)
            {
                pathLineRenderer.SetPosition(i, _pathPoints[i]);
            }
        }
    }

    private void FilterTargetPointsToMaxDistance(float maxDist)
    {
        if (_rawTargetPoints.Count < 2) return;

        float accumulatedLength = 0f;
        int maxIndex = _rawTargetPoints.Count - 1;

        for (int i = 0; i < _rawTargetPoints.Count - 1; i++)
        {
            float segLen = Vector3.Distance(_rawTargetPoints[i], _rawTargetPoints[i + 1]);
            if (accumulatedLength + segLen > maxDist)
            {
                float remain = maxDist - accumulatedLength;
                float t = remain / segLen;
                _rawTargetPoints[i + 1] = Vector3.Lerp(_rawTargetPoints[i], _rawTargetPoints[i + 1], t);
                maxIndex = i + 1;
                break;
            }
            accumulatedLength += segLen;
        }

        if (maxIndex < _rawTargetPoints.Count - 1)
        {
            _rawTargetPoints.RemoveRange(maxIndex + 1, _rawTargetPoints.Count - (maxIndex + 1));
        }
    }

    private void UpdateHoveringParticles()
    {
        if (_pathPoints.Count < 2 || sparkleParticles == null) return;

        float totalPathLength = GetPathLength();
        if (totalPathLength < 0.1f) return;

        int activeCount = Mathf.Clamp(particleCount, 5, _particleBuffer.Length);
        if (_seeds == null || _seeds.Length < activeCount)
        {
            InitializeSeeds(activeCount);
        }

        for (int i = 0; i < activeCount; i++)
        {
            float fraction = (float)i / (activeCount - 1);
            float distanceAlongPath = fraction * totalPathLength;

            Vector3 pos = EvaluatePositionOnPath(distanceAlongPath);

            pos.x += _seeds[i].scatterX * pathScatterWidth;
            pos.z += _seeds[i].scatterZ * pathScatterWidth;

            float phase = _seeds[i].phase;
            float hoverY = Mathf.Sin(Time.time * hoverSpeed + phase) * hoverAmount;
            pos.y += hoverY;

            float twinkle = 0.5f + 0.5f * Mathf.Sin((Time.time * hoverSpeed * 1.4f) + phase * 2f);
            
            // Stay fully visible near the player, and only fade out at the very end of the trail
            float fadeEdge = Mathf.Clamp01((1.0f - fraction) * 4f);

            Color col = Color.Lerp(sparkleColorStart, sparkleColorEnd, fraction);
            col.a = fadeEdge * twinkle * 0.95f;

            _particleBuffer[i].position = pos;
            _particleBuffer[i].startColor = col;
            _particleBuffer[i].startSize = sparkleSize * _seeds[i].sizeMult * (0.8f + twinkle * 0.4f);
            _particleBuffer[i].remainingLifetime = 1f;
            _particleBuffer[i].startLifetime = 1f;
        }

        sparkleParticles.SetParticles(_particleBuffer, activeCount);
    }

    private float GetPathLength()
    {
        float len = 0f;
        for (int i = 0; i < _pathPoints.Count - 1; i++)
        {
            len += Vector3.Distance(_pathPoints[i], _pathPoints[i + 1]);
        }
        return len;
    }

    private Vector3 EvaluatePositionOnPath(float distanceAlongPath)
    {
        if (_pathPoints.Count == 0) return Vector3.zero;
        if (_pathPoints.Count == 1) return _pathPoints[0];

        float currentDist = 0f;
        for (int i = 0; i < _pathPoints.Count - 1; i++)
        {
            float segLen = Vector3.Distance(_pathPoints[i], _pathPoints[i + 1]);
            if (currentDist + segLen >= distanceAlongPath)
            {
                float t = (distanceAlongPath - currentDist) / segLen;
                return Vector3.Lerp(_pathPoints[i], _pathPoints[i + 1], t);
            }
            currentDist += segLen;
        }

        return _pathPoints[_pathPoints.Count - 1];
    }

    private void EnsureDistanceTextExists()
    {
        if (distanceText != null || _dynamicDistanceText != null) return;
        if (ObjectiveManager.Instance == null || ObjectiveManager.Instance.objectiveText == null) return;

        TextMeshProUGUI objText = ObjectiveManager.Instance.objectiveText;

        // Create the new text object
        GameObject distObj = new GameObject("QuestDistanceText");
        distObj.transform.SetParent(objText.transform.parent, false);

        _dynamicDistanceText = distObj.AddComponent<TextMeshProUGUI>();
        
        // Copy styling
        _dynamicDistanceText.font = objText.font;
        _dynamicDistanceText.fontSize = objText.fontSize * 0.9f; 
        _dynamicDistanceText.alignment = objText.alignment;
        _dynamicDistanceText.color = Color.yellow;
        
        // Positioning
        RectTransform rt = _dynamicDistanceText.GetComponent<RectTransform>();
        RectTransform objRt = objText.GetComponent<RectTransform>();
        
        // Match anchors/pivot
        rt.anchorMin = objRt.anchorMin;
        rt.anchorMax = objRt.anchorMax;
        rt.pivot = objRt.pivot;
        
        // Position directly below ObjectiveText
        rt.anchoredPosition = objRt.anchoredPosition + new Vector2(0, -35f);
        rt.sizeDelta = objRt.sizeDelta;
        
        _dynamicDistanceText.gameObject.SetActive(_isVisible);
    }

    private void SetVisibility(bool visible)
    {
        _isVisible = visible;

        if (pathLineRenderer != null)
        {
            pathLineRenderer.enabled = visible && showLineRenderer;
        }

        if (sparkleParticles != null)
        {
            if (!visible) sparkleParticles.Clear();
            var main = sparkleParticles.main;
            main.loop = visible;
        }

        if (distanceText != null)
        {
            distanceText.gameObject.SetActive(visible);
        }
        if (_dynamicDistanceText != null)
        {
            _dynamicDistanceText.gameObject.SetActive(visible);
        }
    }

    private void EnsureComponentsExist()
    {
        if (pathLineRenderer == null)
        {
            pathLineRenderer = GetComponent<LineRenderer>();
            if (pathLineRenderer == null)
            {
                pathLineRenderer = gameObject.AddComponent<LineRenderer>();
            }
        }

        pathLineRenderer.startWidth = lineWidth;
        pathLineRenderer.endWidth = lineWidth * 0.3f;
        pathLineRenderer.startColor = sparkleColorStart;
        pathLineRenderer.endColor = sparkleColorEnd;
        pathLineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        pathLineRenderer.receiveShadows = false;
        pathLineRenderer.useWorldSpace = true;
        pathLineRenderer.enabled = showLineRenderer;

        if (sparkleParticles == null)
        {
            sparkleParticles = GetComponentInChildren<ParticleSystem>();
            if (sparkleParticles == null)
            {
                GameObject particleObj = new GameObject("SparkleParticleTrail");
                particleObj.transform.SetParent(transform);
                particleObj.transform.localPosition = Vector3.zero;
                sparkleParticles = particleObj.AddComponent<ParticleSystem>();
            }
        }

        var main = sparkleParticles.main;
        main.maxParticles = 80;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startSpeed = 0f;
        main.startSize = sparkleSize;

        var shape = sparkleParticles.shape;
        shape.enabled = false;

        var emission = sparkleParticles.emission;
        emission.enabled = false;

        ParticleSystemRenderer psRenderer = sparkleParticles.GetComponent<ParticleSystemRenderer>();
        if (psRenderer != null)
        {
            psRenderer.material = GetOrCreateSparkleMaterial();
        }
    }

    private static Material GetOrCreateSparkleMaterial()
    {
        if (_generatedSparkleMat != null) return _generatedSparkleMat;

        Shader shader = Shader.Find("Mobile/Particles/Additive");
        if (shader == null) shader = Shader.Find("Particles/Additive");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        _generatedSparkleMat = new Material(shader);
        _generatedSparkleMat.mainTexture = GetOrCreateSparkleTexture();
        
        if (_generatedSparkleMat.HasProperty("_TintColor"))
        {
            _generatedSparkleMat.SetColor("_TintColor", Color.white);
        }

        return _generatedSparkleMat;
    }

    private static Texture2D GetOrCreateSparkleTexture()
    {
        if (_generatedSparkleTex != null) return _generatedSparkleTex;

        int res = 64;
        _generatedSparkleTex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[res * res];
        Vector2 center = new Vector2(res / 2f, res / 2f);
        float maxRadius = res / 2f;

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                Vector2 pos = new Vector2(x, y);
                float dist = Vector2.Distance(pos, center) / maxRadius;
                
                float dx = Mathf.Abs(x - center.x) / maxRadius;
                float dy = Mathf.Abs(y - center.y) / maxRadius;
                float starCross = Mathf.Pow(Mathf.Clamp01(1f - dx * 2.5f), 3f) * Mathf.Pow(Mathf.Clamp01(1f - dy * 2.5f), 3f);

                float coreGlow = Mathf.Pow(Mathf.Clamp01(1f - dist * 1.5f), 2.5f);

                float intensity = Mathf.Clamp01(starCross * 2f + coreGlow);
                pixels[y * res + x] = new Color(1f, 1f, 1f, intensity);
            }
        }

        _generatedSparkleTex.SetPixels(pixels);
        _generatedSparkleTex.Apply();
        return _generatedSparkleTex;
    }
}
