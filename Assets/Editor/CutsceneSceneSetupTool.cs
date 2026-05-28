using UnityEditor;
using UnityEngine;

public class CutsceneSceneSetupTool
{
    [MenuItem("Tools/Cutscene/Setup Cutscene Scene")]
    public static void Setup()
    {
        bool changed = false;

        // ── Bootstrap ──────────────────────────────────────────────────────────
        CutsceneBootstrap existing = Object.FindFirstObjectByType<CutsceneBootstrap>();
        if (existing != null)
        {
            Debug.Log("[CutsceneTool] CutsceneBootstrap đã có: " + existing.gameObject.name);
            Selection.activeGameObject = existing.gameObject;
        }
        else
        {
            GameObject bootstrapGo = new GameObject("Bootstrap");
            bootstrapGo.AddComponent<CutsceneBootstrap>();
            Undo.RegisterCreatedObjectUndo(bootstrapGo, "Create CutsceneBootstrap");
            Selection.activeGameObject = bootstrapGo;
            Debug.Log("[CutsceneTool] Đã tạo Bootstrap + CutsceneBootstrap.");
            changed = true;
        }

        // ── MobileInputCanvas ──────────────────────────────────────────────────
        MobileInputUI mobileUI = Object.FindFirstObjectByType<MobileInputUI>();
        if (mobileUI == null)
        {
            Debug.LogWarning("[CutsceneTool] Không tìm thấy MobileInputCanvas trong scene.\n" +
                             "→ Kéo prefab MobileInputCanvas vào Hierarchy để mobile overlay hoạt động.");
        }
        else
        {
            Debug.Log("[CutsceneTool] MobileInputCanvas: " + mobileUI.gameObject.name + " ✓");
        }

        if (changed)
            EditorUtility.DisplayDialog(
                "Cutscene Setup",
                "Đã tạo Bootstrap với CutsceneBootstrap.\n\nChạy scene từ đây sẽ tự đảm bảo InputController và InputMode = Cutscene.",
                "OK");
        else
            EditorUtility.DisplayDialog(
                "Cutscene Setup",
                "CutsceneBootstrap đã tồn tại trong scene, không cần tạo thêm.",
                "OK");
    }

    [MenuItem("Tools/Cutscene/Validate Cutscene Scene")]
    public static void Validate()
    {
        bool ok = true;
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("=== Cutscene Scene Validation ===\n");

        // Bootstrap
        CutsceneBootstrap bootstrap = Object.FindFirstObjectByType<CutsceneBootstrap>();
        if (bootstrap != null)
            sb.AppendLine("✓ CutsceneBootstrap: " + bootstrap.gameObject.name);
        else
        {
            sb.AppendLine("✗ CutsceneBootstrap: CHƯA CÓ → chạy Tools/Cutscene/Setup Cutscene Scene");
            ok = false;
        }

        // MobileInputCanvas
        MobileInputUI mobileUI = Object.FindFirstObjectByType<MobileInputUI>(FindObjectsInactive.Include);
        if (mobileUI != null)
            sb.AppendLine("✓ MobileInputCanvas: " + mobileUI.gameObject.name);
        else
            sb.AppendLine("⚠ MobileInputCanvas: không có (mobile overlay sẽ không hiện)");

        // PlayerMovement_Cutscene
        PlayerMovement_Cutscene pm = Object.FindFirstObjectByType<PlayerMovement_Cutscene>(FindObjectsInactive.Include);
        if (pm != null)
            sb.AppendLine("✓ PlayerMovement_Cutscene: " + pm.gameObject.name);
        else
            sb.AppendLine("⚠ PlayerMovement_Cutscene: không có trong scene này");

        // QuanLyHoiThoai
        QuanLyHoiThoai[] dialogues = Object.FindObjectsByType<QuanLyHoiThoai>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        sb.AppendLine($"✓ QuanLyHoiThoai: {dialogues.Length} instance(s)");

        sb.AppendLine("\n" + (ok ? "✓ Scene sẵn sàng." : "✗ Scene cần fix trước khi test."));

        Debug.Log(sb.ToString());
        EditorUtility.DisplayDialog("Cutscene Validation", sb.ToString(), "OK");
    }
}
