using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Runtime STT Debug Tool for testing Speech-to-Text, phrase evaluation, and dialogue progression.
/// Loads phrases from LuminangPhrases.json and allows real-time checklist tracking,
/// phrase search, and 1-click simulation of speech recognition during gameplay.
/// 
/// Accessible via the floating [🧪 STT Debug] button on screen or by pressing F12 / ~ (Backquote).
/// </summary>
public class STTDebugTool : MonoBehaviour
{
    public static STTDebugTool Instance { get; private set; }

    [Header("Shortcut Settings")]
    public KeyCode toggleKey = KeyCode.F12;
    public KeyCode alternateToggleKey = KeyCode.BackQuote;

    // ── Internal Data Models ───────────────────────────────────────
    [Serializable]
    public class PhraseData
    {
        public string id;
        public string category;
        public string type;
        public string english;
        public string ilokano;
        public string cebuano;
    }

    [Serializable]
    public class PhraseListContainer
    {
        public List<PhraseData> phrases;
    }

    // ── State Variables ─────────────────────────────────────────────
    private bool _showDebugWindow = false;
    private Rect _windowRect = new Rect(20, 50, 480, 600);
    private Vector2 _scrollPosition = Vector2.zero;

    private List<PhraseData> _allPhrases = new List<PhraseData>();
    private List<string> _categories = new List<string>() { "All" };
    private HashSet<string> _testedPhraseIds = new HashSet<string>();

    private string _searchQuery = "";
    private int _selectedCategoryIndex = 0;
    private int _selectedLanguageMode = 0; // 0 = Cebuano, 1 = Ilokano, 2 = Both

    private GUIStyle _headerStyle;
    private GUIStyle _boxStyle;
    private GUIStyle _titleStyle;
    private GUIStyle _activeSttStyle;
    private bool _stylesInitialized = false;

    // ── Auto Initialization ─────────────────────────────────────────
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitialize()
    {
        if (Instance == null && FindFirstObjectByType<STTDebugTool>() == null)
        {
            GameObject debugObj = new GameObject("[STTDebugTool]");
            debugObj.AddComponent<STTDebugTool>();
            DontDestroyOnLoad(debugObj);
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadPhrasesFromJSON();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        CheckToggleShortcut();
    }

    private void CheckToggleShortcut()
    {
        try
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.f12Key.wasPressedThisFrame || keyboard.backquoteKey.wasPressedThisFrame)
                {
                    _showDebugWindow = !_showDebugWindow;
                }

                // Hotkey F11 or P key to INSTANTLY auto-pass the current active STT word
                if (keyboard.f11Key.wasPressedThisFrame || keyboard.pKey.wasPressedThisFrame)
                {
                    TriggerInstantAutoPass();
                }
                return;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(toggleKey) || Input.GetKeyDown(alternateToggleKey))
            {
                _showDebugWindow = !_showDebugWindow;
            }
            if (Input.GetKeyDown(KeyCode.F11) || Input.GetKeyDown(KeyCode.P))
            {
                TriggerInstantAutoPass();
            }
#endif
        }
        catch (Exception)
        {
            // Silently ignore input mode exceptions
        }
    }

    public void TriggerInstantAutoPass()
    {
        string word = GetActiveSTTWord();
        if (string.IsNullOrEmpty(word))
        {
            Debug.LogWarning("[STTDebugTool] Auto-pass key pressed, but no active STT target word was found!");
            return;
        }

        Debug.Log($"<color=cyan>[STTDebugTool] ⚡ Instant Auto-Pass triggered for word: '{word}'!</color>");
        SimulatePassActiveSTT(word);
    }

    private void LoadPhrasesFromJSON()
    {
        TextAsset jsonAsset = Resources.Load<TextAsset>("LuminangPhrases");
        if (jsonAsset != null && !string.IsNullOrEmpty(jsonAsset.text))
        {
            try
            {
                PhraseListContainer container = JsonUtility.FromJson<PhraseListContainer>(jsonAsset.text);
                if (container != null && container.phrases != null)
                {
                    _allPhrases = container.phrases;
                    var catSet = new HashSet<string>();
                    foreach (var p in _allPhrases)
                    {
                        if (!string.IsNullOrEmpty(p.category)) catSet.Add(p.category);
                    }
                    _categories = new List<string>() { "All" };
                    _categories.AddRange(catSet);
                    Debug.Log($"[STTDebugTool] Successfully loaded {_allPhrases.Count} phrases from LuminangPhrases.json across {catSet.Count} categories.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[STTDebugTool] Error parsing LuminangPhrases.json: {ex.Message}");
            }
        }
        else
        {
            Debug.LogWarning("[STTDebugTool] Could not load LuminangPhrases.json from Resources!");
        }
    }

    private void InitStyles()
    {
        if (_stylesInitialized) return;

        _headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.yellow }
        };

        _boxStyle = new GUIStyle(GUI.skin.box)
        {
            padding = new RectOffset(8, 8, 8, 8)
        };

        _titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };

        _activeSttStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = Texture2D.linearGrayTexture }
        };

        _stylesInitialized = true;
    }

    private void OnGUI()
    {
        InitStyles();

        // Render on top of Canvas UI and catch clicks
        GUI.depth = -10000;

        // Auto-scale IMGUI for Device Simulator & high resolution screens
        Matrix4x4 origMatrix = GUI.matrix;
        float baseHeight = 720f;
        float scale = Mathf.Max(1.0f, (float)Screen.height / baseHeight);
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1.0f));

        // ── Floating Toggle Button (Top left) ───────────────────────
        Color prevBg = GUI.backgroundColor;
        GUI.backgroundColor = _showDebugWindow ? new Color(1f, 0.4f, 0.4f) : new Color(0.2f, 0.8f, 0.4f);
        if (GUI.Button(new Rect(10, 10, 140, 36), _showDebugWindow ? "❌ Close Debug" : "🧪 STT Debug"))
        {
            _showDebugWindow = !_showDebugWindow;
        }
        GUI.backgroundColor = prevBg;

        if (_showDebugWindow)
        {
            float scaledWidth = (Screen.width / scale) - 30;
            float scaledHeight = (Screen.height / scale) - 60;
            _windowRect.width = Mathf.Clamp(scaledWidth, 360, 540);
            _windowRect.height = Mathf.Clamp(scaledHeight, 380, 660);

            _windowRect = GUI.Window(998877, _windowRect, DrawDebugWindow, "🧪 Luminang STT Real-Time Debug Tool");
        }

        GUI.matrix = origMatrix;
    }

    private void DrawDebugWindow(int windowID)
    {
        GUI.DragWindow(new Rect(0, 0, _windowRect.width - 30, 25));

        GUILayout.Space(22);

        // ── 1. ACTIVE STT STATUS BANNER ─────────────────────────────────
        string activeTargetWord = GetActiveSTTWord();
        bool hasActiveStt = !string.IsNullOrEmpty(activeTargetWord);

        GUILayout.BeginVertical("box");
        GUILayout.BeginHorizontal();
        GUILayout.Label("Active STT Target:", _headerStyle);
        if (hasActiveStt)
        {
            GUILayout.Label($"<b><color=#55FF55>\"{activeTargetWord}\"</color></b>", _headerStyle);
        }
        else
        {
            GUILayout.Label("<color=#AAAAAA>(None - Waiting for STT Node)</color>");
        }
        GUILayout.EndHorizontal();

        if (hasActiveStt)
        {
            Color origColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.2f, 0.9f, 0.3f);
            if (GUILayout.Button($"⚡ AUTO-PASS ACTIVE STT: \"{activeTargetWord}\"", GUILayout.Height(36)))
            {
                SimulatePassActiveSTT(activeTargetWord);
            }
            GUI.backgroundColor = origColor;
        }
        GUILayout.EndVertical();

        GUILayout.Space(5);

        // ── 2. SEARCH & FILTER CONTROLS ────────────────────────────────
        GUILayout.BeginVertical("box");
        
        // Search bar
        GUILayout.BeginHorizontal();
        GUILayout.Label("🔍 Search:", GUILayout.Width(70));
        _searchQuery = GUILayout.TextField(_searchQuery);
        if (GUILayout.Button("X", GUILayout.Width(25))) _searchQuery = "";
        GUILayout.EndHorizontal();

        // Category filter
        GUILayout.BeginHorizontal();
        GUILayout.Label("Category:", GUILayout.Width(70));
        _selectedCategoryIndex = Mathf.Clamp(_selectedCategoryIndex, 0, _categories.Count - 1);
        for (int i = 0; i < _categories.Count; i++)
        {
            bool isSelected = i == _selectedCategoryIndex;
            Color btnColor = GUI.backgroundColor;
            if (isSelected) GUI.backgroundColor = new Color(0.3f, 0.7f, 1f);
            if (GUILayout.Button(_categories[i], GUILayout.Height(22)))
            {
                _selectedCategoryIndex = i;
            }
            GUI.backgroundColor = btnColor;
        }
        GUILayout.EndHorizontal();

        // Language filter
        GUILayout.BeginHorizontal();
        GUILayout.Label("Language:", GUILayout.Width(70));
        string[] langs = { "Cebuano", "Ilokano", "Both" };
        for (int l = 0; l < langs.Length; l++)
        {
            bool isSelected = l == _selectedLanguageMode;
            Color btnColor = GUI.backgroundColor;
            if (isSelected) GUI.backgroundColor = new Color(1f, 0.8f, 0.3f);
            if (GUILayout.Button(langs[l], GUILayout.Height(20)))
            {
                _selectedLanguageMode = l;
            }
            GUI.backgroundColor = btnColor;
        }
        GUILayout.EndHorizontal();

        // Checklist Progress Summary
        int testedCount = _testedPhraseIds.Count;
        int totalCount = _allPhrases.Count;
        float percent = totalCount > 0 ? (float)testedCount / totalCount * 100f : 0f;
        
        GUILayout.BeginHorizontal();
        GUILayout.Label($"Progress: <b>{testedCount} / {totalCount}</b> tested ({percent:F0}%)");
        if (GUILayout.Button("Reset Checklist", GUILayout.Width(110)))
        {
            _testedPhraseIds.Clear();
        }
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();

        GUILayout.Space(5);

        // ── 3. PHRASE CHECKLIST TABLE ──────────────────────────────────
        _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

        string selCat = _categories[_selectedCategoryIndex];
        var filteredPhrases = _allPhrases.Where(p =>
        {
            if (selCat != "All" && !string.Equals(p.category, selCat, StringComparison.OrdinalIgnoreCase))
                return false;

            if (!string.IsNullOrEmpty(_searchQuery))
            {
                string q = _searchQuery.Trim().ToLower();
                bool matchEng = !string.IsNullOrEmpty(p.english) && p.english.ToLower().Contains(q);
                bool matchCeb = !string.IsNullOrEmpty(p.cebuano) && p.cebuano.ToLower().Contains(q);
                bool matchIlo = !string.IsNullOrEmpty(p.ilokano) && p.ilokano.ToLower().Contains(q);
                if (!matchEng && !matchCeb && !matchIlo) return false;
            }

            return true;
        }).ToList();

        if (filteredPhrases.Count == 0)
        {
            GUILayout.Label("<i>No matching phrases found.</i>");
        }

        foreach (var phrase in filteredPhrases)
        {
            bool isChecked = _testedPhraseIds.Contains(phrase.id);
            bool isMatchingActiveTarget = hasActiveStt && (
                string.Equals(phrase.cebuano, activeTargetWord, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(phrase.ilokano, activeTargetWord, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(phrase.english, activeTargetWord, StringComparison.OrdinalIgnoreCase)
            );

            Color defaultBg = GUI.backgroundColor;
            if (isMatchingActiveTarget)
            {
                GUI.backgroundColor = new Color(0.2f, 0.8f, 0.4f, 1f); // Highlight active target green
            }

            GUILayout.BeginVertical("box");

            GUILayout.BeginHorizontal();

            // Checkbox
            bool newChecked = GUILayout.Toggle(isChecked, "", GUILayout.Width(20));
            if (newChecked != isChecked)
            {
                if (newChecked) _testedPhraseIds.Add(phrase.id);
                else _testedPhraseIds.Remove(phrase.id);
            }

            // Phrase Label
            string cebText = !string.IsNullOrEmpty(phrase.cebuano) ? phrase.cebuano : "-";
            string iloText = !string.IsNullOrEmpty(phrase.ilokano) ? phrase.ilokano : "-";
            string engText = !string.IsNullOrEmpty(phrase.english) ? phrase.english : "-";

            string displayPhrase = "";
            if (_selectedLanguageMode == 0) displayPhrase = $"<b>{cebText}</b> <color=#AAAAAA>({engText})</color>";
            else if (_selectedLanguageMode == 1) displayPhrase = $"<b>{iloText}</b> <color=#AAAAAA>({engText})</color>";
            else displayPhrase = $"Ceb: <b>{cebText}</b> | Ilo: <b>{iloText}</b> <color=#AAAAAA>({engText})</color>";

            if (isMatchingActiveTarget)
            {
                displayPhrase = "⭐ <b>[CURRENT TARGET]</b> " + displayPhrase;
            }

            GUILayout.Label(displayPhrase, GUILayout.ExpandWidth(true));

            // Test / Simulate Button
            if (GUILayout.Button("TEST", GUILayout.Width(60), GUILayout.Height(24)))
            {
                _testedPhraseIds.Add(phrase.id);
                string testWord = _selectedLanguageMode == 1 ? phrase.ilokano : phrase.cebuano;
                if (string.IsNullOrEmpty(testWord)) testWord = phrase.english;
                SimulatePassActiveSTT(testWord);
            }

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUI.backgroundColor = defaultBg;
        }

        GUILayout.EndScrollView();
    }

    // ── Helper Methods ─────────────────────────────────────────────

    private string GetActiveSTTWord()
    {
        if (TeachingOverlayPanel.Instance != null && TeachingOverlayPanel.Instance.gameObject.activeInHierarchy)
        {
            // Access targetWord via DialogueManager if available
            if (DialogueManager.Instance != null && DialogueManager.Instance.PendingSTTChoice != null)
            {
                return DialogueManager.Instance.PendingSTTChoice.expectedSTTWord;
            }
        }

        // Check Fishing Game STT Manager
        if (FishingSTTManager.Instance != null && FishingSTTManager.Instance.IsSTTActive)
        {
            return FishingSTTManager.Instance.TargetWord;
        }

        if (DialogueManager.Instance != null && DialogueManager.Instance.PendingSTTChoice != null)
        {
            return DialogueManager.Instance.PendingSTTChoice.expectedSTTWord;
        }

        return "";
    }

    private void SimulatePassActiveSTT(string spokenWord)
    {
        Debug.Log($"<color=cyan>[STTDebugTool] Simulating STT Recognition for word: '{spokenWord}'...</color>");

        // If an in-scene lesson is active, route through InSceneLessonController so
        // "Great job!", agree animation, and all lesson hooks fire correctly.
        if (InSceneLessonController.Instance != null && InSceneLessonController.Instance.IsLessonActive)
        {
            InSceneLessonController.Instance.SimulateSuccess(spokenWord);
        }
        else if (TeachingOverlayPanel.Instance != null && TeachingOverlayPanel.Instance.gameObject.activeInHierarchy)
        {
            TeachingOverlayPanel.Instance.HandleSuccess(spokenWord);
        }
        else if (FishingSTTManager.Instance != null && FishingSTTManager.Instance.IsSTTActive)
        {
            FishingSTTManager.Instance.SimulateSuccess(spokenWord);
        }
        else if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.CompleteSTT(true, "");
        }
        else
        {
            Debug.LogWarning("[STTDebugTool] Could not pass STT challenge: no active handler found.");
        }
    }
}
