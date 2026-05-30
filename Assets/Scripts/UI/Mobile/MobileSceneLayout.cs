using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject lưu cấu hình layout mobile per-scene.
/// Tạo qua menu: Assets → Create → Mobile → Scene Layout
/// </summary>
[CreateAssetMenu(fileName = "MobileLayout_", menuName = "Mobile/Scene Layout")]
public class MobileSceneLayout : ScriptableObject
{
    [System.Serializable]
    public class ElementLayout
    {
        public string path;
        public Vector2 anchorMin   = new Vector2(0, 0);
        public Vector2 anchorMax   = new Vector2(0, 0);
        public Vector2 pivot       = new Vector2(0.5f, 0.5f);
        public Vector2 position;   // anchoredPosition
        public Vector2 size;       // sizeDelta
        public bool    active = true;
    }

    [Header("Canvas Scaler")]
    public Vector2Int referenceResolution = new Vector2Int(1920, 1080);
    [Range(0f, 1f)]
    public float matchWidthOrHeight = 0.5f;

    [Header("Elements")]
    public List<ElementLayout> elements = new();

    /// <summary>Tìm layout theo đường dẫn prefab.</summary>
    public ElementLayout Get(string path)
    {
        return elements.Find(e => e.path == path);
    }
}
