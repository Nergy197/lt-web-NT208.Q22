#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class CreateEndingSceneMenu
{
    [MenuItem("Tools/Tạo Scene KẾT THÚC HỒI 1")]
    public static void CreateScene()
    {
        // Tạo một Scene trống mới
        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        
        // 1. Tạo Camera
        GameObject camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        Camera cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;

        // 2. Tạo Canvas
        GameObject canvasGO = new GameObject("Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.AddComponent<GraphicRaycaster>();

        // 3. Tạo Background (Nền đen)
        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(canvasGO.transform, false);
        Image bgImg = bgGO.AddComponent<Image>();
        bgImg.color = Color.black;
        RectTransform bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.sizeDelta = Vector2.zero;
        bgRT.anchoredPosition = Vector2.zero;

        // 4. Tạo Text (Chữ KẾT THÚC HỒI 1)
        GameObject textGO = new GameObject("Text_KetThuc");
        textGO.transform.SetParent(canvasGO.transform, false);
        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = "KẾT THÚC HỒI 1";
        tmp.color = Color.white;
        tmp.fontSize = 80;
        tmp.alignment = TextAlignmentOptions.Center;
        RectTransform textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0.5f, 0.5f);
        textRT.anchorMax = new Vector2(0.5f, 0.5f);
        textRT.sizeDelta = new Vector2(1000, 200);
        textRT.anchoredPosition = Vector2.zero;

        // 5. Thêm Script điều khiển tự động chuyển màn
        GameObject scriptGO = new GameObject("EndSceneController");
        EndAct1Screen script = scriptGO.AddComponent<EndAct1Screen>();
        script.waitTime = 4f;
        script.nextScene = "Chapter0_Login"; // Tự về lại Menu

        // 6. Lưu Scene
        string scenePath = "Assets/Scenes/Chapter6_Ending.unity";
        EditorSceneManager.SaveScene(newScene, scenePath);

        // 7. Tự động Add vào Build Settings
        EditorBuildSettingsScene[] original = EditorBuildSettings.scenes;
        bool alreadyExists = false;
        foreach (var s in original)
        {
            if (s.path == scenePath) alreadyExists = true;
        }

        if (!alreadyExists)
        {
            EditorBuildSettingsScene[] newSettings = new EditorBuildSettingsScene[original.Length + 1];
            System.Array.Copy(original, newSettings, original.Length);
            newSettings[newSettings.Length - 1] = new EditorBuildSettingsScene(scenePath, true);
            EditorBuildSettings.scenes = newSettings;
        }

        Debug.Log("<color=green>Đã tạo thành công Scene Chapter6_Ending và thêm vào Build Settings!</color>");
    }
}
#endif
