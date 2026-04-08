using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Editor Tools cho hệ thống Teleport Portal.
/// Truy cập từ thanh menu: Tools/Teleport Portal/...
/// </summary>
public class TeleportSetupTool
{
    // ================================================================
    //  TOOL 1: TẠO MỘT CẶP CỔNG DỊCH CHUYỂN 2 CHIỀU
    // ================================================================
    [MenuItem("Tools/Teleport Portal/1. Tạo Cặp Cổng 2 Chiều (A ↔ B)")]
    public static void CreateTeleportPair()
    {
        // ---- Tạo Parent để gom cổng cho gọn Hierarchy ----
        string groupName = "TeleportPair_" + System.DateTime.Now.ToString("HHmmss");
        GameObject group = new GameObject(groupName);
        Undo.RegisterCreatedObjectUndo(group, "Create Teleport Pair");

        // ---- Portal A ----
        GameObject portalA = CreatePortalObject("Portal_A", new Vector3(-3, 0, 0), group.transform);
        TeleportPortal scriptA = portalA.GetComponent<TeleportPortal>();

        // ---- Spawn Point A (điểm xuất hiện khi từ B trở về) ----
        GameObject spawnA = CreateSpawnPoint("SpawnPoint_A", new Vector3(-2.5f, 0, 0), group.transform);

        // ---- Portal B ----
        GameObject portalB = CreatePortalObject("Portal_B", new Vector3(3, 0, 0), group.transform);
        TeleportPortal scriptB = portalB.GetComponent<TeleportPortal>();

        // ---- Spawn Point B (điểm xuất hiện khi từ A qua B) ----
        GameObject spawnB = CreateSpawnPoint("SpawnPoint_B", new Vector3(3.5f, 0, 0), group.transform);

        // ---- Liên kết chéo: A → SpawnB, B → SpawnA ----
        scriptA.destination = spawnB.transform;
        scriptB.destination = spawnA.transform;

        // Đánh dấu scene đã thay đổi
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = group;

        Debug.Log($"[TeleportSetupTool] Đã tạo cặp cổng 2 chiều: {portalA.name} ↔ {portalB.name}.\n" +
                  $"Di chuyển các GameObject tới vị trí mong muốn, rồi kéo thả Mapdata vào 'Target Map Data' nếu cần.");
    }

    // ================================================================
    //  TOOL 2: TẠO CỔNG MỘT CHIỀU (VD: HỐ RƠI, ĐƯỜNG HẦM MỘT CHIỀU)
    // ================================================================
    [MenuItem("Tools/Teleport Portal/2. Tạo Cổng Một Chiều (A → B)")]
    public static void CreateOneWayPortal()
    {
        string groupName = "TeleportOneWay_" + System.DateTime.Now.ToString("HHmmss");
        GameObject group = new GameObject(groupName);
        Undo.RegisterCreatedObjectUndo(group, "Create One-Way Portal");

        GameObject portalA = CreatePortalObject("Portal_Enter", new Vector3(-3, 0, 0), group.transform);
        GameObject spawnB = CreateSpawnPoint("SpawnPoint_Exit", new Vector3(3, 0, 0), group.transform);

        portalA.GetComponent<TeleportPortal>().destination = spawnB.transform;

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = group;

        Debug.Log("[TeleportSetupTool] Đã tạo cổng một chiều. Di chuyển Portal_Enter và SpawnPoint_Exit tới vị trí cần thiết.");
    }

    // ================================================================
    //  TOOL 3: KIỂM TRA LỖI TẤT CẢ PORTAL TRONG SCENE
    // ================================================================
    [MenuItem("Tools/Teleport Portal/3. Kiểm Tra Lỗi Tất Cả Portal")]
    public static void ValidateAllPortals()
    {
        TeleportPortal[] portals = Object.FindObjectsByType<TeleportPortal>(FindObjectsSortMode.None);
        SavePoint[] savePoints = Object.FindObjectsByType<SavePoint>(FindObjectsSortMode.None);

        if (portals.Length == 0)
        {
            Debug.Log("[TeleportSetupTool] ✅ Không tìm thấy TeleportPortal nào trong scene.");
            return;
        }

        int errorCount = 0;
        int warningCount = 0;
        List<string> report = new List<string>();

        report.Add($"======= BÁO CÁO KIỂM TRA PORTAL ({portals.Length} cổng) =======\n");

        foreach (var portal in portals)
        {
            string name = portal.gameObject.name;

            // Lỗi 1: Destination null
            if (portal.destination == null)
            {
                report.Add($"❌ LỖI [{name}]: 'Destination' chưa được gán! Player sẽ không thể dịch chuyển.");
                errorCount++;
            }

            // Lỗi 2: Destination trỏ chính mình
            if (portal.destination != null && portal.destination == portal.transform)
            {
                report.Add($"❌ LỖI [{name}]: 'Destination' đang trỏ về chính nó! Sẽ teleport tại chỗ.");
                errorCount++;
            }

            // Lỗi 3: Collider không phải Trigger
            BoxCollider2D col = portal.GetComponent<BoxCollider2D>();
            if (col == null)
            {
                report.Add($"❌ LỖI [{name}]: Thiếu BoxCollider2D!");
                errorCount++;
            }
            else if (!col.isTrigger)
            {
                report.Add($"⚠️ CẢNH BÁO [{name}]: BoxCollider2D chưa bật 'Is Trigger'. Sẽ va chạm cứng thay vì kích hoạt dịch chuyển.");
                warningCount++;
            }

            // Cảnh báo 4: Destination quá gần Portal (có thể gây lặp vô tận)
            if (portal.destination != null)
            {
                float dist = Vector3.Distance(portal.transform.position, portal.destination.position);
                if (dist < 0.5f)
                {
                    report.Add($"⚠️ CẢNH BÁO [{name}]: Destination quá gần Portal (khoảng cách = {dist:F2}). Có thể gây teleport lặp.");
                    warningCount++;
                }
            }

            // Cảnh báo 5: Portal mode Interact + chồng SavePoint
            if (portal.requireInteract)
            {
                Collider2D portalCol = portal.GetComponent<Collider2D>();
                if (portalCol != null)
                {
                    foreach (var sp in savePoints)
                    {
                        Collider2D spCol = sp.GetComponent<Collider2D>();
                        if (spCol != null && spCol.bounds.Intersects(portalCol.bounds))
                        {
                            report.Add($"⚠️ CẢNH BÁO [{name}]: Vùng ColliderPortal CHỒNG với SavePoint '{sp.pointId}'. " +
                                       "Phím Interact sẽ ưu tiên SavePoint (đã có guard). Nên tách hai vùng ra hoặc đổi Portal sang auto.");
                            warningCount++;
                        }
                    }
                }
            }

            // Cảnh báo 6: Destination nằm trong vùng của Portal khác
            if (portal.destination != null)
            {
                foreach (var otherPortal in portals)
                {
                    if (otherPortal == portal) continue;
                    if (!otherPortal.requireInteract) // Chỉ cảnh báo portal auto-trigger
                    {
                        Collider2D otherCol = otherPortal.GetComponent<Collider2D>();
                        if (otherCol != null && otherCol.bounds.Contains(portal.destination.position))
                        {
                            report.Add($"⚠️ CẢNH BÁO [{name}]: Destination rơi vào vùng auto-trigger của '{otherPortal.gameObject.name}'. " +
                                       "Nhờ có cooldown 0.5s nên vẫn an toàn, nhưng nên dời SpawnPoint ra khỏi vùng trigger.");
                            warningCount++;
                        }
                    }
                }
            }

            // OK
            if (portal.destination != null && col != null && col.isTrigger)
            {
                report.Add($"✅ OK [{name}]: destination = '{portal.destination.name}'" +
                           (portal.targetMapData != null ? $", mapData = '{portal.targetMapData.mapName}'" : "") +
                           (portal.requireInteract ? ", mode = Interact" : ", mode = Auto"));
            }
        }

        // Tổng kết
        report.Add($"\n======= TỔNG KẾT: {errorCount} lỗi, {warningCount} cảnh báo =======");

        if (errorCount == 0 && warningCount == 0)
            report.Add("🎉 Tất cả portal đều hợp lệ!");

        // In toàn bộ báo cáo ra Console
        Debug.Log(string.Join("\n", report));
    }

    // ================================================================
    //  TOOL 4: TỰ ĐỘNG SỬA LỖI COLLIDER TRIGGER
    // ================================================================
    [MenuItem("Tools/Teleport Portal/4. Tự Động Sửa Tất Cả Collider → Is Trigger")]
    public static void FixAllPortalColliders()
    {
        TeleportPortal[] portals = Object.FindObjectsByType<TeleportPortal>(FindObjectsSortMode.None);
        int fixedCount = 0;

        foreach (var portal in portals)
        {
            BoxCollider2D col = portal.GetComponent<BoxCollider2D>();
            if (col != null && !col.isTrigger)
            {
                Undo.RecordObject(col, "Fix Portal Collider");
                col.isTrigger = true;
                fixedCount++;
            }
        }

        if (fixedCount > 0)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"[TeleportSetupTool] Đã sửa {fixedCount} Portal(s) → Is Trigger = true.");
        }
        else
        {
            Debug.Log("[TeleportSetupTool] ✅ Tất cả collider đã đúng, không cần sửa gì.");
        }
    }

    // ================================================================
    //  TOOL 5: HIỂN THỊ GIZ MOS (SCENE VIEW) CHO TẤT CẢ PORTAL
    // ================================================================
    // (Tự động hiện qua TeleportPortalGizmoDrawer bên dưới)

    // ================================================================
    //  HELPERS
    // ================================================================

    private static GameObject CreatePortalObject(string name, Vector3 position, Transform parent)
    {
        GameObject portalObj = new GameObject(name);
        portalObj.transform.position = position;
        portalObj.transform.SetParent(parent);

        // BoxCollider2D - trigger
        BoxCollider2D col = portalObj.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1f, 2f); // Cổng đứng mặc định

        // SpriteRenderer để nhìn thấy trong Scene
        SpriteRenderer sr = portalObj.AddComponent<SpriteRenderer>();
        sr.color = new Color(0.3f, 0.6f, 1f, 0.5f); // Xanh dương mờ
        sr.sortingOrder = 5;

        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        portalObj.transform.localScale = new Vector3(1f, 2f, 1f);

        // TeleportPortal script
        portalObj.AddComponent<TeleportPortal>();

        Undo.RegisterCreatedObjectUndo(portalObj, $"Create {name}");
        return portalObj;
    }

    private static GameObject CreateSpawnPoint(string name, Vector3 position, Transform parent)
    {
        GameObject spawnObj = new GameObject(name);
        spawnObj.transform.position = position;
        spawnObj.transform.SetParent(parent);

        // Icon nhỏ để dễ nhìn trong Scene View
        // (Sử dụng Gizmo icon tích hợp hoặc SpriteRenderer nhỏ)
        SpriteRenderer sr = spawnObj.AddComponent<SpriteRenderer>();
        sr.color = new Color(0f, 1f, 0.3f, 0.6f); // Xanh lá mờ
        sr.sortingOrder = 5;

        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        spawnObj.transform.localScale = new Vector3(0.3f, 0.3f, 1f);

        Undo.RegisterCreatedObjectUndo(spawnObj, $"Create {name}");
        return spawnObj;
    }
}

// ================================================================
//  GIZMO DRAWER: Vẽ đường kẻ từ Portal → Destination trong Scene View
// ================================================================
[InitializeOnLoad]
public static class TeleportPortalGizmoDrawer
{
    static TeleportPortalGizmoDrawer()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    static void OnSceneGUI(SceneView sceneView)
    {
        TeleportPortal[] portals = Object.FindObjectsByType<TeleportPortal>(FindObjectsSortMode.None);
        if (portals == null || portals.Length == 0) return;

        foreach (var portal in portals)
        {
            if (portal == null || portal.destination == null) continue;

            Vector3 from = portal.transform.position;
            Vector3 to = portal.destination.position;

            // Vẽ đường kẻ từ Portal → Destination
            Handles.color = new Color(0.2f, 0.8f, 1f, 0.8f); // Cyan
            Handles.DrawDottedLine(from, to, 4f);

            // Vẽ mũi tên ở giữa
            Vector3 mid = (from + to) * 0.5f;
            Vector3 dir = (to - from).normalized;
            float arrowSize = 0.3f;
            Handles.ConeHandleCap(0, mid, Quaternion.LookRotation(dir), arrowSize, EventType.Repaint);

            // Label nhỏ
            Handles.Label(from + Vector3.up * 0.5f, portal.gameObject.name,
                new GUIStyle(GUI.skin.label) { fontSize = 10, fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(0.2f, 0.8f, 1f) } });
        }
    }
}
