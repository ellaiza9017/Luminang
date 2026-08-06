using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ObjectiveItem
{
    public string id;
    public string objective;
}

[System.Serializable]
public class ObjectiveCategory
{
    public string category;
    public List<ObjectiveItem> items;
}

[System.Serializable]
public class ObjectivesData
{
    public List<ObjectiveCategory> objectives;
}

public static class ProgressCalculator
{
    private static Dictionary<string, ObjectivesData> _cachedObjectives = new Dictionary<string, ObjectivesData>();

    private static ObjectivesData GetObjectivesData(string languageKey)
    {
        string safeKey = languageKey.ToLower();
        if (_cachedObjectives.ContainsKey(safeKey))
        {
            return _cachedObjectives[safeKey];
        }

        string resourceName = safeKey == "ilokano" ? "Ilokano Objectives" : "Cebuano Objectives";
        TextAsset jsonAsset = Resources.Load<TextAsset>(resourceName);
        
        if (jsonAsset != null)
        {
            ObjectivesData data = JsonUtility.FromJson<ObjectivesData>(jsonAsset.text);
            _cachedObjectives[safeKey] = data;
            return data;
        }

        Debug.LogError($"[ProgressCalculator] Failed to load {resourceName}.json from Resources!");
        return null;
    }

    /// <summary>
    /// Checks if a lesson category has all of its objectives completed.
    /// </summary>
    public static bool IsLessonCompleted(string languageKey, string categoryKey, List<string> completedObjectiveIds)
    {
        if (completedObjectiveIds == null || completedObjectiveIds.Count == 0) return false;

        ObjectivesData data = GetObjectivesData(languageKey);
        if (data == null) return false;

        foreach (var cat in data.objectives)
        {
            if (string.Equals(cat.category, categoryKey, System.StringComparison.OrdinalIgnoreCase))
            {
                if (cat.items == null || cat.items.Count == 0) return true; // Empty category counts as complete

                foreach (var item in cat.items)
                {
                    if (!completedObjectiveIds.Contains(item.id))
                    {
                        return false; // Found an objective that is NOT completed
                    }
                }
                return true; // All objectives for this category are completed
            }
        }

        return false; // Category not found
    }

    // Minimal classes to parse valid lessons from LessonsData.json
    [System.Serializable] private class MinLesson { public string categoryKey; }
    [System.Serializable] private class MinChapter { public List<MinLesson> lessons; }
    [System.Serializable] private class MinLanguage { public string languageKey; public List<MinChapter> chapters; }
    [System.Serializable] private class MinLessonsData { public List<MinLanguage> languages; }

    /// <summary>
    /// Counts how many valid lessons have all their objectives completed.
    /// Only counts categories that actually exist in LessonsData.json (ignores "Intro" etc).
    /// </summary>
    public static int GetCompletedLessonsCount(string languageKey, List<string> completedObjectiveIds)
    {
        if (completedObjectiveIds == null || completedObjectiveIds.Count == 0) return 0;

        TextAsset lessonsAsset = Resources.Load<TextAsset>("LessonsData");
        if (lessonsAsset == null) return 0;

        MinLessonsData lessonsData = JsonUtility.FromJson<MinLessonsData>(lessonsAsset.text);
        if (lessonsData == null || lessonsData.languages == null) return 0;

        int completedCount = 0;
        foreach (var lang in lessonsData.languages)
        {
            if (string.Equals(lang.languageKey, languageKey, System.StringComparison.OrdinalIgnoreCase))
            {
                if (lang.chapters != null)
                {
                    foreach (var chap in lang.chapters)
                    {
                        if (chap.lessons != null)
                        {
                            foreach (var lesson in chap.lessons)
                            {
                                if (IsLessonCompleted(languageKey, lesson.categoryKey, completedObjectiveIds))
                                {
                                    completedCount++;
                                }
                            }
                        }
                    }
                }
                break;
            }
        }

        return completedCount;
    }
}
