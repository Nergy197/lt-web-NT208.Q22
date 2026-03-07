using UnityEngine;
using UnityEditor;
using System.IO;

public class CreateGameContentTool
{
    // ==========================================
    // TOOL 1: TẠO 4 NHÂN VẬT (SCRIPTABLE OBJECTS)
    // ==========================================
    [MenuItem("Tools/1. Tự động tạo Data 4 Nhân Vật")]
    public static void CreateFourCharacters()
    {
        string folderPath = "Assets/Data/Characters";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            Directory.CreateDirectory(folderPath);
            AssetDatabase.Refresh();
        }

        string[] classNames = { "Hero", "Mage", "Warrior", "Priest" };
        int[] hps = { 100, 80, 150, 90 };
        int[] atks = { 15, 25, 12, 5 };
        int[] defs = { 10, 5, 20, 8 };
        int[] spds = { 5, 6, 3, 7 };

        for (int i = 0; i < 4; i++)
        {
            string path = $"{folderPath}/{classNames[i]}.asset";

            // Nếu file đã có, bỏ qua đỡ đè dữ liệu cũ
            if (File.Exists(path))
            {
                Debug.Log($"Đã có sẵn: {classNames[i]}. Bỏ qua tạo mới.");
                continue;
            }

            PlayerData newChar = ScriptableObject.CreateInstance<PlayerData>();
            newChar.entityName = classNames[i];
            newChar.baseHP = hps[i];
            newChar.baseAtk = atks[i];
            newChar.baseDef = defs[i];
            newChar.baseSpd = spds[i];
            newChar.maxAP = 100;

            AssetDatabase.CreateAsset(newChar, path);
            Debug.Log($"Đã tạo Asset Nhân vật: {classNames[i]} tại {path}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Hoàn tất tạo 4 nhân vật! Hãy vào Assets/Data/Characters để kiểm tra (và gắn kỹ năng nếu muốn).");
    }

    // ==========================================
    // TOOL 2: SETUP MAP CƠ BẢN (PLAYER + MÔI TRƯỜNG)
    // ==========================================
    [MenuItem("Tools/2. Setup Map Căn Bản Nhanh (Player + Map)")]
    public static void SetupBasicMap()
    {
        // Tạo Player
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            player = new GameObject("Player");
            player.tag = "Player";

            // Gắn components cơ bản
            SpriteRenderer sr = player.AddComponent<SpriteRenderer>();
            sr.color = Color.blue; // Tạm thời để cục hình vuông màu xanh cho dễ nhìn

            // Box Collider làm trigger chạm quái
            BoxCollider2D col = player.AddComponent<BoxCollider2D>();
            // Rigidbody cho PlayerMovement
            Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.freezeRotation = true;

            // Animator (PlayerMovement yêu cầu nhưng ta cứ thêm vào)
            player.AddComponent<Animator>();

            // Script di chuyển
            player.AddComponent<PlayerMovement>();

            // Setup hình ảnh hiển thị cơ bản cho tiện click
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            sr.sprite = sprite;
            player.transform.localScale = new Vector3(1f, 1f, 1);

            Undo.RegisterCreatedObjectUndo(player, "Create Player");
            Debug.Log("Đã tạo Player trên Map thành công.");
        }
        else
        {
            Debug.Log("Đã có sẵn Player, bỏ qua.");
        }

        // Camera follow (Đơn giản - kéo camera làm con của Player)
        Camera cam = Camera.main;
        if (cam != null && cam.transform.parent != player.transform)
        {
            cam.transform.SetParent(player.transform);
            cam.transform.localPosition = new Vector3(0, 0, -10); // Lùi lại để nhìn 2D
        }

        // Tạo Grid & 1 tấm nền đất cho Map
        Grid grid = Object.FindFirstObjectByType<Grid>();
        if (grid == null)
        {
            GameObject gridObj = new GameObject("Grid");
            grid = gridObj.AddComponent<Grid>();

            // Tilemap giả làm nền
            GameObject groundObj = new GameObject("Ground");
            groundObj.transform.SetParent(gridObj.transform);

            SpriteRenderer groundSr = groundObj.AddComponent<SpriteRenderer>();
            groundSr.color = new Color(0.2f, 0.5f, 0.2f); // Màu xanh lá cây (cỏ)
            groundSr.sortingOrder = -10; // Đặt nền ở dưới cùng

            Texture2D texG = new Texture2D(1, 1);
            texG.SetPixel(0, 0, Color.white);
            texG.Apply();
            Sprite spriteG = Sprite.Create(texG, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            groundSr.sprite = spriteG;
            groundObj.transform.localScale = new Vector3(15, 15, 1);

            Undo.RegisterCreatedObjectUndo(gridObj, "Create Environment Grid");
            Debug.Log("Đã tạo Grid và Ground thành công.");
        }

        // Tạo một con quái giả (Enemy Trigger mẫu)
        EnemyTrigger enemyTrigger = Object.FindFirstObjectByType<EnemyTrigger>();
        if (enemyTrigger == null)
        {
            GameObject enemyObj = new GameObject("Enemy_Slime_Sample");
            enemyObj.transform.position = new Vector3(3, 0, 0);

            SpriteRenderer esr = enemyObj.AddComponent<SpriteRenderer>();
            esr.color = Color.red;

            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            esr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);

            BoxCollider2D ecol = enemyObj.AddComponent<BoxCollider2D>();
            ecol.isTrigger = true; // Là trigger để chạm vào thì đánh

            EnemyTrigger et = enemyObj.AddComponent<EnemyTrigger>();
            et.requireInteract = false;

            Undo.RegisterCreatedObjectUndo(enemyObj, "Create Sample Enemy");
            Debug.Log("Đã tạo Enemy Trigger Mẫu thành công.");
        }

        Debug.Log("Hoàn tất Setup Map cơ bản! Hãy kéo Map Manager, Input Controller vào Scene nếu chưa có.");
    }

    // ==========================================
    // TOOL 3: NHÂN BẢN MAP SCENE CÓ SẴN
    // ==========================================
    [MenuItem("Tools/3. Tạo Map Mới (Copy y chang MapScene hiện tại)")]
    public static void DuplicateMapScene()
    {
        string sourcePath = "Assets/Scenes/MapScene.unity";

        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
        {
            Directory.CreateDirectory("Assets/Scenes");
            AssetDatabase.Refresh();
        }

        if (!File.Exists(sourcePath))
        {
            Debug.LogError($"Không tìm thấy file gốc tại {sourcePath}. Vui lòng đảm bảo bạn đang có một scene tên là MapScene.unity nằm trong thư mục Assets/Scenes.");
            return;
        }

        // Tạo tên file mới không bị trùng
        string newPath = AssetDatabase.GenerateUniqueAssetPath("Assets/Scenes/NewMap.unity");

        // Copy file
        AssetDatabase.CopyAsset(sourcePath, newPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Đã tạo một Map mới y chang Map gốc tại: {newPath}");
    }
}
