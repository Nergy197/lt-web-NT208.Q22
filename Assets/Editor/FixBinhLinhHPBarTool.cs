using UnityEditor;
using UnityEngine;

/// <summary>
/// Sửa 3 vấn đề của BinhLinh_Combat.prefab khiến thanh máu không hiển thị đúng:
/// 1. Gán bodyRenderer cho UnitVisual (trỏ đến child "Body")
/// 2. Xóa Enemy_HP_Canvas cũ (disabled, không còn dùng)
/// 3. Reset scale về (1,1,1) — scale 0.333 gây lệch vị trí HP bar so với enemy khác
///
/// Menu: Tools/Battle/Fix BinhLinh HP Bar
/// </summary>
public static class FixBinhLinhHPBarTool
{
    const string PREFAB_PATH  = "Assets/Prefabs/BinhLinh_Combat.prefab";
    const string BODY_NAME    = "Body";
    const string CANVAS_NAME  = "Enemy_HP_Canvas";

    [MenuItem("Tools/Battle/Fix BinhLinh HP Bar")]
    static void Run()
    {
        var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
        if (prefabAsset == null)
        {
            EditorUtility.DisplayDialog("Lỗi", $"Không tìm thấy prefab tại:\n{PREFAB_PATH}", "OK");
            return;
        }

        bool confirm = EditorUtility.DisplayDialog(
            "Fix BinhLinh HP Bar",
            "Tool sẽ chỉnh sửa BinhLinh_Combat.prefab:\n\n" +
            "• Gán bodyRenderer cho UnitVisual → child \"Body\"\n" +
            "• Xóa Enemy_HP_Canvas (canvas cũ, không dùng)\n" +
            "• Đặt localScale = (0.333, 0.333, 1) — để 0.333 × enemySpawnScale(3) = 1.0\n\n" +
            "Thao tác này không thể Undo qua Ctrl+Z.\nTiếp tục?",
            "Sửa ngay", "Hủy");

        if (!confirm) return;

        using var scope = new PrefabUtility.EditPrefabContentsScope(PREFAB_PATH);
        var root = scope.prefabContentsRoot;

        int fixCount = 0;

        // ── 1. Gán bodyRenderer ───────────────────────────────────────────────
        var visual = root.GetComponent<UnitVisual>();
        if (visual == null)
        {
            Debug.LogWarning("[FixBinhLinh] Không tìm thấy UnitVisual trên root — bỏ qua gán bodyRenderer.");
        }
        else if (visual.bodyRenderer != null)
        {
            Debug.Log("[FixBinhLinh] bodyRenderer đã được gán sẵn — bỏ qua.");
        }
        else
        {
            var bodyRenderer = FindBodyRenderer(root.transform);
            if (bodyRenderer != null)
            {
                visual.bodyRenderer = bodyRenderer;
                EditorUtility.SetDirty(visual);
                Debug.Log($"[FixBinhLinh] ✓ Gán bodyRenderer → \"{bodyRenderer.gameObject.name}\".");
                fixCount++;
            }
            else
            {
                Debug.LogWarning($"[FixBinhLinh] Không tìm thấy child tên \"{BODY_NAME}\" có SpriteRenderer.");
            }
        }

        // ── 2. Xóa Enemy_HP_Canvas ────────────────────────────────────────────
        var canvas = root.transform.Find(CANVAS_NAME);
        if (canvas != null)
        {
            Object.DestroyImmediate(canvas.gameObject);
            Debug.Log($"[FixBinhLinh] ✓ Xóa {CANVAS_NAME} (canvas cũ).");
            fixCount++;
        }
        else
        {
            Debug.Log($"[FixBinhLinh] {CANVAS_NAME} không có — bỏ qua.");
        }

        // ── 3. Đảm bảo scale đúng: 0.333 × enemySpawnScale(3) = 1.0 thực tế ──
        // KHÔNG reset về (1,1,1) vì đó sẽ làm BinhLinh to gấp 3 lần.
        var targetScale = new Vector3(1f / 3f, 1f / 3f, 1f);
        var localScale  = root.transform.localScale;
        if (Mathf.Abs(localScale.x - targetScale.x) > 0.001f)
        {
            Debug.Log($"[FixBinhLinh] ✓ Đặt scale {localScale} → (0.333, 0.333, 1).");
            root.transform.localScale = targetScale;
            fixCount++;
        }
        else
        {
            Debug.Log("[FixBinhLinh] Scale đã đúng (0.333) — bỏ qua.");
        }

        Debug.Log($"[FixBinhLinh] Hoàn tất — {fixCount} thay đổi được áp dụng.");

        EditorUtility.DisplayDialog(
            "Hoàn tất",
            fixCount > 0
                ? $"Đã sửa {fixCount} vấn đề trên BinhLinh_Combat.prefab.\n\nNhớ kiểm tra lại spawn scale trong BattleManager nếu BinhLinh trông quá to/nhỏ."
                : "Không có gì cần sửa — prefab đã đúng.",
            "OK");
    }

    static SpriteRenderer FindBodyRenderer(Transform parent)
    {
        foreach (Transform child in parent)
        {
            if (child.name == BODY_NAME)
            {
                var sr = child.GetComponent<SpriteRenderer>();
                if (sr != null) return sr;
            }
        }
        // Fallback: bất kỳ SpriteRenderer nào trong children
        return parent.GetComponentInChildren<SpriteRenderer>();
    }
}
