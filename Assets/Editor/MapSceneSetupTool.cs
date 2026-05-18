using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class MapSceneSetupTool : EditorWindow
{
    [MenuItem("Tools/Map Scene Setup")]
    public static void Open() => GetWindow<MapSceneSetupTool>("Map Scene Setup");

    private MapManager mapManager;
    private MapSceneBootstrap bootstrap;
    private Vector2 scroll;

    void OnFocus() => Scan();

    void Scan()
    {
        mapManager = FindFirstObjectByType<MapManager>();
        bootstrap  = FindFirstObjectByType<MapSceneBootstrap>();
    }

    void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.LabelField("Map Scene Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        if (GUILayout.Button("Quét lại scene")) Scan();

        EditorGUILayout.Space(8);

        // ── MapManager ────────────────────────────────────────────────────────
        DrawSection("MapManager");

        if (mapManager == null)
        {
            DrawStatus(false, "Không có MapManager trong scene");
            if (GUILayout.Button("Tạo MapdataManager"))
            {
                var go = new GameObject("MapdataManager");
                mapManager = go.AddComponent<MapManager>();
                Undo.RegisterCreatedObjectUndo(go, "Create MapManager");
                EditorUtility.SetDirty(go);
            }
        }
        else
        {
            DrawStatus(true, $"MapManager: {mapManager.gameObject.name}");

            // Current Map
            bool hasCurrentMap = mapManager.currentMap != null;
            DrawStatus(hasCurrentMap, hasCurrentMap
                ? $"Current Map: {mapManager.currentMap.mapName}"
                : "Current Map chưa gán");

            // Default Map
            bool hasDefaultMap = mapManager.defaultMap != null;
            if (!hasDefaultMap)
            {
                DrawStatus(false, "Default Map chưa gán");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("  → Auto-set từ Current Map:", GUILayout.Width(220));
                if (GUILayout.Button("Fix") && mapManager.currentMap != null)
                {
                    Undo.RecordObject(mapManager, "Set Default Map");
                    mapManager.defaultMap = mapManager.currentMap;
                    EditorUtility.SetDirty(mapManager);
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                DrawStatus(true, $"Default Map: {mapManager.defaultMap.mapName}");
            }

            // Encounter Rate
            bool rateOk = mapManager.encounterRate > 0f;
            DrawStatus(rateOk, $"Encounter Rate: {mapManager.encounterRate:F4}" + (rateOk ? "" : " — set > 0 để có encounter"));

            // Current Map enemies
            if (mapManager.currentMap != null)
            {
                DrawMapdataSection(mapManager.currentMap);
            }
        }

        EditorGUILayout.Space(8);

        // ── MapSceneBootstrap ─────────────────────────────────────────────────
        DrawSection("MapSceneBootstrap (cho test từ MapScene)");

        if (bootstrap == null)
        {
            DrawStatus(false, "Chưa có Bootstrap trong scene");
            if (GUILayout.Button("Tạo Bootstrap GameObject"))
            {
                var go = new GameObject("Bootstrap");
                bootstrap = go.AddComponent<MapSceneBootstrap>();
                if (mapManager?.currentMap != null)
                    bootstrap.debugMapData = mapManager.currentMap;
                Undo.RegisterCreatedObjectUndo(go, "Create MapSceneBootstrap");
                Selection.activeGameObject = go;
            }
        }
        else
        {
            DrawStatus(true, $"Bootstrap: {bootstrap.gameObject.name}");

            bool hasDebugMap = bootstrap.debugMapData != null;
            if (!hasDebugMap)
            {
                DrawStatus(false, "debugMapData chưa gán");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("  → Auto-set từ MapManager.currentMap:", GUILayout.Width(270));
                if (GUILayout.Button("Fix") && mapManager?.currentMap != null)
                {
                    Undo.RecordObject(bootstrap, "Set Bootstrap DebugMap");
                    bootstrap.debugMapData = mapManager.currentMap;
                    EditorUtility.SetDirty(bootstrap);
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                DrawStatus(true, $"debugMapData: {bootstrap.debugMapData.mapName}");
            }
        }

        EditorGUILayout.Space(8);

        // ── Fix All ───────────────────────────────────────────────────────────
        DrawSection("Fix tất cả một lần");
        if (GUILayout.Button("Auto Fix All", GUILayout.Height(30)))
            FixAll();

        EditorGUILayout.EndScrollView();
    }

    void DrawMapdataSection(Mapdata map)
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField($"  Mapdata: {map.mapName}", EditorStyles.miniBoldLabel);

        bool hasEnemies = map.possibleEnemies != null && map.possibleEnemies.Count > 0;
        DrawStatus(hasEnemies, hasEnemies
            ? $"  possibleEnemies: {map.possibleEnemies.Count} enemy"
            : "  possibleEnemies trống — sẽ không có encounter");

        if (!hasEnemies)
        {
            if (GUILayout.Button("  Mở Mapdata trong Inspector"))
                Selection.activeObject = map;
        }

        if (hasEnemies)
        {
            int nullCount = 0;
            foreach (var e in map.possibleEnemies) if (e == null) nullCount++;
            if (nullCount > 0)
                DrawStatus(false, $"  {nullCount} slot trong possibleEnemies bị null");
        }

        bool hasLevel = map.enemyLevel > 0;
        DrawStatus(hasLevel, $"  enemyLevel: {map.enemyLevel}" + (hasLevel ? "" : " — set > 0"));
    }

    void FixAll()
    {
        Scan();

        if (mapManager == null)
        {
            var go = new GameObject("MapdataManager");
            mapManager = go.AddComponent<MapManager>();
            Undo.RegisterCreatedObjectUndo(go, "Create MapManager");
        }

        if (mapManager.defaultMap == null && mapManager.currentMap != null)
        {
            Undo.RecordObject(mapManager, "Fix Default Map");
            mapManager.defaultMap = mapManager.currentMap;
            EditorUtility.SetDirty(mapManager);
        }

        if (bootstrap == null)
        {
            var go = new GameObject("Bootstrap");
            bootstrap = go.AddComponent<MapSceneBootstrap>();
            Undo.RegisterCreatedObjectUndo(go, "Create Bootstrap");
        }

        if (bootstrap.debugMapData == null && mapManager.currentMap != null)
        {
            Undo.RecordObject(bootstrap, "Fix Bootstrap DebugMap");
            bootstrap.debugMapData = mapManager.currentMap;
            EditorUtility.SetDirty(bootstrap);
        }

        Scan();
        Debug.Log("[MapSetupTool] Auto Fix All hoàn tất.");
    }

    static void DrawSection(string title)
    {
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        var rect = GUILayoutUtility.GetLastRect();
        rect.y += EditorGUIUtility.singleLineHeight - 2;
        rect.height = 1;
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
        EditorGUILayout.Space(2);
    }

    static void DrawStatus(bool ok, string msg)
    {
        Color prev = GUI.color;
        GUI.color = ok ? new Color(0.4f, 1f, 0.4f) : new Color(1f, 0.4f, 0.4f);
        EditorGUILayout.LabelField((ok ? "✓ " : "✗ ") + msg);
        GUI.color = prev;
    }
}
