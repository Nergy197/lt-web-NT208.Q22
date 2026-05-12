using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class MapDataCreator : EditorWindow
{
    private enum Tab { Monster, Map, SceneScanner }
    private Tab currentTab;

    // --- Monster Creator State ---
    private string enemyName = "New Monster";
    private int hp = 50;
    private int atk = 10;
    private int def = 5;
    private int spd = 5;
    private Sprite monsterSprite;
    private EnemyAttackData defaultAttack;

    // --- Map Creator State ---
    private string mapName = "New Map";
    private int mapId = 1;
    private int enemyLevel = 1;
    private List<EnemyData> mapEnemies = new List<EnemyData>();
    private Vector2 scrollPos;

    // --- Scene Scanner State ---
    private List<EnemyData> sceneEnemies = new List<EnemyData>();

    [MenuItem("Tools/Map & Monster Creator")]
    public static void ShowWindow()
    {
        GetWindow<MapDataCreator>("Map & Monster Creator");
    }

    private void OnEnable()
    {
        // Try to find a default attack
        if (defaultAttack == null)
        {
            string[] guids = AssetDatabase.FindAssets("Skill_DanhThuong");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                defaultAttack = AssetDatabase.LoadAssetAtPath<EnemyAttackData>(path);
            }
        }
    }

    private void OnGUI()
    {
        currentTab = (Tab)GUILayout.Toolbar((int)currentTab, new string[] { "Monster Creator", "Map Data Creator", "Scene Scanner" });

        EditorGUILayout.Space();

        switch (currentTab)
        {
            case Tab.Monster:
                DrawMonsterCreator();
                break;
            case Tab.Map:
                DrawMapCreator();
                break;
            case Tab.SceneScanner:
                DrawSceneScanner();
                break;
        }
    }

    private void DrawMonsterCreator()
    {
        EditorGUILayout.LabelField("Create New Monster Asset & Prefab", EditorStyles.boldLabel);
        
        enemyName = EditorGUILayout.TextField("Monster Name", enemyName);
        hp = EditorGUILayout.IntField("Base HP", hp);
        atk = EditorGUILayout.IntField("Base Atk", atk);
        def = EditorGUILayout.IntField("Base Def", def);
        spd = EditorGUILayout.IntField("Base Spd", spd);
        
        monsterSprite = (Sprite)EditorGUILayout.ObjectField("Monster Sprite", monsterSprite, typeof(Sprite), false);
        defaultAttack = (EnemyAttackData)EditorGUILayout.ObjectField("Default Attack", defaultAttack, typeof(EnemyAttackData), false);

        EditorGUILayout.Space();

        if (GUILayout.Button("Create Monster", GUILayout.Height(40)))
        {
            if (monsterSprite == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a Sprite first!", "OK");
                return;
            }
            CreateMonsterAsset();
        }
    }

    private void CreateMonsterAsset()
    {
        // 1. Create Folders
        string prefabFolder = "Assets/Generated/Prefabs/Enemies";
        string dataFolder = "Assets/Generated/Data/Enemies";
        EnsureFolder(prefabFolder);
        EnsureFolder(dataFolder);

        // 2. Create Prefab
        GameObject go = new GameObject(enemyName + "_Variant");
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = monsterSprite;
        
        string prefabPath = $"{prefabFolder}/{enemyName}_Variant.prefab";
        prefabPath = AssetDatabase.GenerateUniqueAssetPath(prefabPath);
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        DestroyImmediate(go);

        // 3. Create EnemyData ScriptableObject
        EnemyData data = ScriptableObject.CreateInstance<EnemyData>();
        data.entityName = enemyName;
        data.baseHP = hp;
        data.baseAtk = atk;
        data.baseDef = def;
        data.baseSpd = spd;
        data.battlePrefab = prefab;
        
        if (defaultAttack != null)
        {
            data.attacks = new List<EnemyAttackData> { defaultAttack };
        }

        string dataPath = $"{dataFolder}/{enemyName}.asset";
        dataPath = AssetDatabase.GenerateUniqueAssetPath(dataPath);
        AssetDatabase.CreateAsset(data, dataPath);
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("Success", $"Created Monster: {enemyName}\nPrefab: {prefabPath}\nData: {dataPath}", "OK");
        
        // Auto-add to map list if we are in Map tab
        if (!mapEnemies.Contains(data)) mapEnemies.Add(data);
    }

    private void DrawMapCreator()
    {
        EditorGUILayout.LabelField("Generate MapData ScriptableObject", EditorStyles.boldLabel);

        mapName = EditorGUILayout.TextField("Map Name", mapName);
        mapId = EditorGUILayout.IntField("Map ID", mapId);
        enemyLevel = EditorGUILayout.IntField("Base Enemy Level", enemyLevel);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Possible Enemies in this Map:");
        
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(200));
        for (int i = 0; i < mapEnemies.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            mapEnemies[i] = (EnemyData)EditorGUILayout.ObjectField(mapEnemies[i], typeof(EnemyData), false);
            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                mapEnemies.RemoveAt(i);
                break;
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("Add Enemy Slot"))
        {
            mapEnemies.Add(null);
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Generate MapData Asset", GUILayout.Height(40)))
        {
            CreateMapAsset();
        }
    }

    private void CreateMapAsset()
    {
        string folder = "Assets/Generated/Data/Maps";
        EnsureFolder(folder);

        Mapdata data = ScriptableObject.CreateInstance<Mapdata>();
        data.mapName = mapName;
        data.mapId = mapId;
        data.enemyLevel = enemyLevel;
        data.possibleEnemies = new List<EnemyData>(mapEnemies.Where(e => e != null));

        string path = $"{folder}/{mapName}_Data.asset";
        path = AssetDatabase.GenerateUniqueAssetPath(path);
        AssetDatabase.CreateAsset(data, path);
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("Success", $"Created MapData at {path}", "OK");
    }

    private void DrawSceneScanner()
    {
        EditorGUILayout.LabelField("Scan Current Scene for Monster Data", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("This will find all 'EnemyTrigger' components in the active scene and list the 'EnemyData' they use.", MessageType.Info);

        if (GUILayout.Button("Scan Active Scene", GUILayout.Height(30)))
        {
            ScanScene();
        }

        if (sceneEnemies.Count > 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Found {sceneEnemies.Count} Unique Enemies:");
            foreach (var enemy in sceneEnemies)
            {
                EditorGUILayout.ObjectField(enemy, typeof(EnemyData), false);
            }

            if (GUILayout.Button("Copy to Map Data Creator"))
            {
                mapEnemies = new List<EnemyData>(sceneEnemies);
                currentTab = Tab.Map;
            }
        }
    }

    private void ScanScene()
    {
        sceneEnemies.Clear();
        EnemyTrigger[] triggers = Object.FindObjectsByType<EnemyTrigger>(FindObjectsSortMode.None);
        
        HashSet<EnemyData> uniqueEnemies = new HashSet<EnemyData>();
        foreach (var t in triggers)
        {
            if (t.enemyData != null)
                uniqueEnemies.Add(t.enemyData);
        }
        
        sceneEnemies = uniqueEnemies.ToList();
        Debug.Log($"[Scanner] Found {triggers.Length} triggers and {sceneEnemies.Count} unique enemy types.");
    }

    private void EnsureFolder(string path)
    {
        string[] folders = path.Split('/');
        string current = "";
        foreach (var f in folders)
        {
            if (string.IsNullOrEmpty(current))
                current = f;
            else
            {
                string parent = current;
                current = parent + "/" + f;
                if (!AssetDatabase.IsValidFolder(current))
                {
                    AssetDatabase.CreateFolder(parent, f);
                }
            }
        }
    }
}
