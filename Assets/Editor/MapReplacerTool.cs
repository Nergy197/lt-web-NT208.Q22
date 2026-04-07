using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapReplacerTool : EditorWindow
{
    private GameObject oldMap;
    private GameObject newMapPrefab;
    private string targetSortingLayer = "Environment";

    [MenuItem("Tools/Map Replacer")]
    public static void ShowWindow()
    {
        GetWindow<MapReplacerTool>("Map Replacer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Replace Map Prefab", EditorStyles.boldLabel);

        oldMap = (GameObject)EditorGUILayout.ObjectField("Old Map (In Scene)", oldMap, typeof(GameObject), true);
        newMapPrefab = (GameObject)EditorGUILayout.ObjectField("New Map (Prefab)", newMapPrefab, typeof(GameObject), false);
        targetSortingLayer = EditorGUILayout.TextField("Map Sorting Layer", targetSortingLayer);

        if (GUILayout.Button("Replace and Fix Layers"))
        {
            ReplaceMap();
        }
    }

    private void ReplaceMap()
    {
        if (oldMap == null)
        {
            Debug.LogError("Old Map is not assigned!");
            return;
        }

        if (newMapPrefab == null)
        {
            Debug.LogError("New Map Prefab is not assigned!");
            return;
        }

        if (PrefabUtility.GetPrefabAssetType(newMapPrefab) == PrefabAssetType.NotAPrefab)
        {
            Debug.LogError("New Map is not a prefab!");
            return;
        }

        // Instantiate new map
        GameObject newMapInstance = (GameObject)PrefabUtility.InstantiatePrefab(newMapPrefab);
        newMapInstance.name = newMapPrefab.name; // Remove "(Clone)"

        // Copy transforms
        newMapInstance.transform.position = oldMap.transform.position;
        newMapInstance.transform.rotation = oldMap.transform.rotation;
        newMapInstance.transform.localScale = oldMap.transform.localScale;
        newMapInstance.transform.SetParent(oldMap.transform.parent);
        newMapInstance.transform.SetSiblingIndex(oldMap.transform.GetSiblingIndex());

        // Fix Sorting Layers
        FixSortingLayers(newMapInstance);

        // Register undo operation for the destruction of old map and creation of new one
        Undo.RegisterCreatedObjectUndo(newMapInstance, "Replace Map");
        Undo.DestroyObjectImmediate(oldMap);

        Debug.Log("Map replaced successfully and sorting layers adjusted!", newMapInstance);
        Selection.activeGameObject = newMapInstance;
    }

    private void FixSortingLayers(GameObject mapInstance)
    {
        // Fix for all renderers (TilemapRenderer, SpriteRenderer, etc.)
        Renderer[] allRenderers = mapInstance.GetComponentsInChildren<Renderer>(true);

        int renderersFixed = 0;
        foreach (Renderer rend in allRenderers)
        {
            rend.sortingLayerName = targetSortingLayer;
            string objName = rend.gameObject.name.ToLower();

            if (objName.Contains("ground") || objName.Contains("nền"))
                rend.sortingOrder = -100;
            else if (objName.Contains("path") || objName.Contains("grass"))
                rend.sortingOrder = -90;
            else if (objName.Contains("water") || objName.Contains("nước"))
                rend.sortingOrder = -95;
            else if (objName.Contains("wall") || objName.Contains("tường"))
                rend.sortingOrder = -50;
            else if (objName.Contains("decor") || objName.Contains("prop") || objName.Contains("tree"))
                rend.sortingOrder = -10;
            else 
                rend.sortingOrder = -1; // Default -1 thay vì 0, để luôn rớt xuống dưới Player

            renderersFixed++;
        }

        Debug.Log($"Fixed sorting layers for {renderersFixed} renderers. Set them to '{targetSortingLayer}'.");
    }
}
