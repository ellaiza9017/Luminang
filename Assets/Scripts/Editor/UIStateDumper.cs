using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.IO;
using System.Text;

public class UIStateDumper
{
    [MenuItem("Tools/Dump SariSari UI State")]
    public static void DumpState()
    {
        StringBuilder sb = new StringBuilder();
        
        SariSariGameManager manager = Object.FindObjectOfType<SariSariGameManager>();
        if (manager == null)
        {
            Debug.LogError("Could not find SariSariGameManager in the scene.");
            return;
        }

        sb.AppendLine("--- SariSariGameManager State ---");
        
        sb.AppendLine($"activeSessionRounds count (via reflection): {GetPrivateField<System.Collections.IList>(manager, "activeSessionRounds")?.Count ?? -1}");
        sb.AppendLine($"phraseDictionary count (via reflection): {GetPrivateField<System.Collections.IList>(manager, "phraseDictionary")?.Count ?? -1}");
        sb.AppendLine($"wordBlockPrefab assigned: {(manager.wordBlockPrefab != null ? manager.wordBlockPrefab.name : "NULL")}");
        sb.AppendLine($"wordSlotPrefab assigned: {(manager.wordSlotPrefab != null ? manager.wordSlotPrefab.name : "NULL")}");

        if (manager.wordBoxGroup != null)
        {
            sb.AppendLine("\n[WordBoxGroup]");
            DumpRectTransform(manager.wordBoxGroup as RectTransform, sb);
            DumpLayoutGroup(manager.wordBoxGroup, sb);
            
            sb.AppendLine("  [Children of WordBoxGroup]");
            foreach (Transform child in manager.wordBoxGroup)
            {
                sb.AppendLine($"    - {child.name}");
                DumpRectTransform(child as RectTransform, sb, "      ");
                DumpLayoutGroup(child, sb, "      ");
                DumpLayoutElement(child, sb, "      ");
                DumpCSF(child, sb, "      ");
            }
        }

        if (manager.sentenceBox != null)
        {
            sb.AppendLine("\n[SentenceBox]");
            DumpRectTransform(manager.sentenceBox as RectTransform, sb);
            DumpLayoutGroup(manager.sentenceBox, sb);
            
            sb.AppendLine("  [Children of SentenceBox]");
            foreach (Transform child in manager.sentenceBox)
            {
                sb.AppendLine($"    - {child.name}");
                DumpRectTransform(child as RectTransform, sb, "      ");
                DumpLayoutGroup(child, sb, "      ");
                DumpLayoutElement(child, sb, "      ");
                DumpCSF(child, sb, "      ");
            }
        }
        
        string path = Path.Combine(Application.dataPath, "UI_Dump.txt");
        File.WriteAllText(path, sb.ToString());
        Debug.Log($"UI State dumped to: {path}");
    }

    private static void DumpRectTransform(RectTransform rt, StringBuilder sb, string indent = "  ")
    {
        if (rt == null) return;
        sb.AppendLine($"{indent}Rect: sizeDelta={rt.sizeDelta}, rect={rt.rect}, anchoredPosition={rt.anchoredPosition}, pivot={rt.pivot}, scale={rt.localScale}");
    }

    private static void DumpLayoutGroup(Transform t, StringBuilder sb, string indent = "  ")
    {
        HorizontalLayoutGroup hlg = t.GetComponent<HorizontalLayoutGroup>();
        if (hlg != null)
        {
            sb.AppendLine($"{indent}HorizontalLayoutGroup: ControlSize(W:{hlg.childControlWidth}, H:{hlg.childControlHeight}), ForceExpand(W:{hlg.childForceExpandWidth}, H:{hlg.childForceExpandHeight})");
        }
    }

    private static void DumpLayoutElement(Transform t, StringBuilder sb, string indent = "  ")
    {
        LayoutElement le = t.GetComponent<LayoutElement>();
        if (le != null)
        {
            sb.AppendLine($"{indent}LayoutElement: min(W:{le.minWidth}, H:{le.minHeight}), preferred(W:{le.preferredWidth}, H:{le.preferredHeight}), flex(W:{le.flexibleWidth}, H:{le.flexibleHeight})");
        }
    }
    
    private static void DumpCSF(Transform t, StringBuilder sb, string indent = "  ")
    {
        ContentSizeFitter csf = t.GetComponent<ContentSizeFitter>();
        if (csf != null)
        {
            sb.AppendLine($"{indent}ContentSizeFitter: H:{csf.horizontalFit}, V:{csf.verticalFit}");
        }
    }

    private static T GetPrivateField<T>(object obj, string fieldName)
    {
        var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            return (T)field.GetValue(obj);
        }
        return default(T);
    }
}
