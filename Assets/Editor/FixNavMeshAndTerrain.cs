using UnityEngine;
using UnityEditor;
using UnityEditor.AI;

public class FixNavMeshAndTerrain : EditorWindow
{
    [MenuItem("Tools/Luminang/Fix NavMesh and Terrain")]
    public static void Fix()
    {
        // 1. Fix Terrain Colliders (MeshColliders are not supported on Terrains)
        Terrain[] terrains = Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int terrainFixes = 0;
        foreach (Terrain t in terrains)
        {
            MeshCollider mc = t.GetComponent<MeshCollider>();
            if (mc != null)
            {
                DestroyImmediate(mc);
                terrainFixes++;
            }
            if (t.GetComponent<TerrainCollider>() == null)
            {
                t.gameObject.AddComponent<TerrainCollider>().terrainData = t.terrainData;
            }
        }
        
        if (terrainFixes > 0)
        {
            Debug.Log($"<color=green>SUCCESS: Fixed {terrainFixes} Terrain(s) by replacing MeshCollider with TerrainCollider!</color>");
        }

        // 2. Clear and Rebuild the NavMesh
        Debug.Log("Rebuilding NavMesh, this might take a few seconds...");
#pragma warning disable 0618
        UnityEditor.AI.NavMeshBuilder.ClearAllNavMeshes();
        UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
#pragma warning restore 0618
        
        Debug.Log("<color=green>SUCCESS: NavMesh successfully rebuilt!</color>");
    }
}
