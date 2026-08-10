using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class FixTranslateButton : EditorWindow
{
    [MenuItem("Tools/Fix Translate Buttons")]
    public static void Fix()
    {
        var buttons = Resources.FindObjectsOfTypeAll<Button>();
        int fixedCount = 0;
        foreach (var b in buttons)
        {
            if (b.name.Contains("Translate") && b.gameObject.scene.isLoaded)
            {
                Debug.Log($"--- Fixing {b.name} on {b.transform.parent.name} ---");
                
                // 1. Set as last sibling (guarantees it renders on top of the chat bubble)
                b.transform.SetAsLastSibling();

                // 2. Make sure the button itself can be clicked
                var btnImage = b.GetComponent<Image>();
                if (btnImage != null) btnImage.raycastTarget = true;
                b.interactable = true;

                // 3. Scan all parents and siblings to find what is blocking it
                Transform t = b.transform.parent;
                while(t != null && t.GetComponent<Canvas>() == null)
                {
                    // Fix any Canvas Groups that are disabling clicks
                    var cg = t.GetComponent<CanvasGroup>();
                    if (cg != null)
                    {
                        cg.blocksRaycasts = true;
                        cg.interactable = true;
                    }

                    // Disable Raycast Target on all other things in the chat bubble (like the Text)
                    foreach (Transform sibling in t)
                    {
                        if (sibling != b.transform)
                        {
                            var sImg = sibling.GetComponent<Image>();
                            if (sImg != null && sImg.raycastTarget) sImg.raycastTarget = false;
                            
                            var sText = sibling.GetComponent<TextMeshProUGUI>();
                            if (sText != null && sText.raycastTarget) sText.raycastTarget = false;
                        }
                    }

                    t = t.parent;
                }
                fixedCount++;
            }
        }
        Debug.Log($"Successfully forced {fixedCount} Translate buttons to be clickable!");
    }
}
