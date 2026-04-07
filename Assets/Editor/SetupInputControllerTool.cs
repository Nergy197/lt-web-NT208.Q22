using UnityEditor;
using UnityEngine;

public class SetupInputControllerTool : EditorWindow
{
    [MenuItem("Tools/Setup InputController for MapScene")]
    public static void AddInputControllerToScene()
    {
        // Kiểm tra xem đã có InputController trong scene chưa
        InputController existing = FindFirstObjectByType<InputController>();
        if (existing != null)
        {
            Debug.Log("InputController đã tồn tại trong Scene: " + existing.gameObject.name);
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        // Tạo mới GameObject
        GameObject icObj = new GameObject("InputController");
        
        // Thêm script InputController
        icObj.AddComponent<InputController>();

        // Thêm MapSceneBootstrap nếu chưa có
        if (FindFirstObjectByType<MapSceneBootstrap>() == null)
        {
            GameObject bootstrapObj = new GameObject("Bootstrap");
            bootstrapObj.AddComponent<MapSceneBootstrap>();
            Undo.RegisterCreatedObjectUndo(bootstrapObj, "Create Bootstrap");
        }

        // Lưu hành động để có thể Undo (Ctrl+Z)
        Undo.RegisterCreatedObjectUndo(icObj, "Create InputController");
        
        Selection.activeGameObject = icObj;
        Debug.Log("Đã tạo thành công InputController và Bootstrap vào Scene hiện tại!");
    }
}
