using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Editor Tool sửa tất cả Sorting Layer "Unknown" trong Battlefield-Map.
/// Quy tắc:
///   - Ground (Tilemap) → Default sorting layer
///   - Object có tên bắt đầu bằng "m" (mountain) → Environment
///   - Tất cả còn lại → Player
///
/// Truy cập: Tools/Fix Sorting/...
/// </summary>
public class FixSortingLayerTool
{
    [MenuItem("Tools/Fix Sorting/1. Sửa Tất Cả Sorting Layer (Auto)")]
    public static void FixAllSortingLayers()
    {
        // Lấy tất cả SpriteRenderer trong scene (bao gồm children của prefab)
        SpriteRenderer[] allRenderers = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
        // Lấy tất cả TilemapRenderer
        UnityEngine.Tilemaps.TilemapRenderer[] tilemapRenderers = 
            Object.FindObjectsByType<UnityEngine.Tilemaps.TilemapRenderer>(FindObjectsSortMode.None);

        int fixedSprites = 0;
        int fixedTilemaps = 0;
        int skipped = 0;

        Debug.Log("========================================");
        Debug.Log($"[FixSortingLayer] Bắt đầu sửa... ({allRenderers.Length} SpriteRenderer, {tilemapRenderers.Length} TilemapRenderer)");

        // ---- Fix TilemapRenderer (Ground) → Default ----
        foreach (var tmr in tilemapRenderers)
        {
            string objName = tmr.gameObject.name.ToLower();

            if (objName.Contains("ground"))
            {
                if (tmr.sortingLayerName != "Default")
                {
                    Undo.RecordObject(tmr, "Fix Tilemap Sorting Layer");
                    tmr.sortingLayerName = "Default";
                    tmr.sortingOrder = 0;
                    fixedTilemaps++;
                    Debug.Log($"  [Tilemap] {GetPath(tmr.gameObject)} → Default (order 0)");
                }
            }
            else
            {
                // Tilemap khác (nếu có) → Environment
                if (tmr.sortingLayerName != "Environment")
                {
                    Undo.RecordObject(tmr, "Fix Tilemap Sorting Layer");
                    tmr.sortingLayerName = "Environment";
                    fixedTilemaps++;
                    Debug.Log($"  [Tilemap] {GetPath(tmr.gameObject)} → Environment");
                }
            }
        }

        // ---- Fix SpriteRenderer ----
        foreach (var sr in allRenderers)
        {
            string objName = sr.gameObject.name;
            string objNameLower = objName.ToLower();

            // Bỏ qua object nằm trong MinimapIcon system
            if (sr.gameObject.name.StartsWith("_Minimap"))
            {
                skipped++;
                continue;
            }

            string targetLayer;

            // Quy tắc: Mountain (tên bắt đầu bằng "m" + số, hoặc chứa "mountain"/"moutain") → Environment
            if (IsMountainObject(objName, objNameLower))
            {
                targetLayer = "Environment";
            }
            // Ground-related → Default
            else if (objNameLower.Contains("ground"))
            {
                targetLayer = "Default";
            }
            // Tất cả còn lại → Player
            else
            {
                targetLayer = "Player";
            }

            // Chỉ sửa nếu khác
            if (sr.sortingLayerName != targetLayer)
            {
                Undo.RecordObject(sr, "Fix Sprite Sorting Layer");
                string oldLayer = sr.sortingLayerName;
                sr.sortingLayerName = targetLayer;
                fixedSprites++;
                Debug.Log($"  [Sprite] {GetPath(sr.gameObject)} : {oldLayer} → {targetLayer} (order {sr.sortingOrder})");
            }
        }

        if (fixedSprites > 0 || fixedTilemaps > 0)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        Debug.Log("========================================");
        Debug.Log($"[FixSortingLayer] ✅ Hoàn tất!");
        Debug.Log($"  Đã sửa: {fixedSprites} SpriteRenderer, {fixedTilemaps} TilemapRenderer");
        Debug.Log($"  Bỏ qua: {skipped}");
        Debug.Log("========================================");
    }

    // ================================================================
    //  TOOL 2: BÁO CÁO SORTING LAYER HIỆN TẠI (KHÔNG SỬA)
    // ================================================================
    [MenuItem("Tools/Fix Sorting/2. Báo Cáo Sorting Layer (Chỉ Xem)")]
    public static void ReportSortingLayers()
    {
        SpriteRenderer[] allRenderers = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
        UnityEngine.Tilemaps.TilemapRenderer[] tilemapRenderers =
            Object.FindObjectsByType<UnityEngine.Tilemaps.TilemapRenderer>(FindObjectsSortMode.None);

        int unknown = 0, defaultCount = 0, envCount = 0, playerCount = 0, otherCount = 0;

        Debug.Log("======= BÁO CÁO SORTING LAYER =======");

        foreach (var sr in allRenderers)
        {
            string layer = sr.sortingLayerName;
            if (string.IsNullOrEmpty(layer) || layer == "Unknown" || 
                (layer != "Default" && layer != "Environment" && layer != "Player"))
            {
                Debug.Log($"  ⚠️ [{sr.gameObject.name}] sortingLayer='{layer}' order={sr.sortingOrder} — {GetPath(sr.gameObject)}");
                unknown++;
            }
            else if (layer == "Default") defaultCount++;
            else if (layer == "Environment") envCount++;
            else if (layer == "Player") playerCount++;
            else otherCount++;
        }

        foreach (var tmr in tilemapRenderers)
        {
            string layer = tmr.sortingLayerName;
            Debug.Log($"  [Tilemap] {tmr.gameObject.name}: sortingLayer='{layer}' order={tmr.sortingOrder}");
        }

        Debug.Log($"\n  Default: {defaultCount} | Environment: {envCount} | Player: {playerCount} | Unknown/Other: {unknown + otherCount}");
        Debug.Log("========================================");
    }

    // ================================================================
    //  HELPERS
    // ================================================================

    /// <summary>
    /// Kiểm tra xem object có phải Mountain không.
    /// Tên bắt đầu bằng "m" theo sau là số (m1, m2, m2.1, m3.1...)
    /// hoặc chứa "mountain"/"moutain" (typo trong prefab).
    /// Hoặc nằm trong parent có tên "Mountain"/"Moutain".
    /// </summary>
    static bool IsMountainObject(string name, string nameLower)
    {
        // Chứa "mountain" hoặc "moutain" (typo)
        if (nameLower.Contains("mountain") || nameLower.Contains("moutain"))
            return true;

        // Tên bắt đầu bằng "m" + số: m1, m2, m2.1, m3, m4, m5, m6, m1.1, etc.
        if (name.Length >= 2 && name[0] == 'm' && char.IsDigit(name[1]))
            return true;

        // rev_m pattern (reversed mountain): rev_m2.1
        if (nameLower.StartsWith("rev_m") && name.Length >= 6 && char.IsDigit(name[5]))
            return true;

        // Parent check: nếu parent là "Mountain" hoặc "Moutain"
        // (Không check quá sâu để tránh false positive)

        return false;
    }

    /// <summary>Trả về đường dẫn hierarchy của GameObject.</summary>
    static string GetPath(GameObject obj)
    {
        string path = obj.name;
        Transform parent = obj.transform.parent;
        int depth = 0;
        while (parent != null && depth < 3)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
            depth++;
        }
        return path;
    }
}
