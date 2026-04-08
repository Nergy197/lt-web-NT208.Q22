using UnityEngine;

/// <summary>
/// Tạo icon hiển thị trên minimap cho bất kỳ đối tượng nào.
/// Gắn vào GameObject cha (vd: TeleportPillar, SavePoint, NPC, Chest...)
/// Script sẽ tự tạo một child sprite trên layer "MinimapIcon".
/// Layer này được minimap camera render nhưng main camera ẩn đi.
///
/// Cách dùng:
///   1. Gắn MinimapIcon component vào bất kỳ GameObject nào.
///   2. Chỉnh iconColor, iconSize trong Inspector.
///   3. Done — icon tự xuất hiện trên minimap.
/// </summary>
public class MinimapIcon : MonoBehaviour
{
    [Header("Icon Settings")]
    [Tooltip("Màu icon trên minimap.")]
    public Color iconColor = new Color(0.4f, 0.3f, 0.9f, 1f); // Tím (mặc định cho teleport)

    [Tooltip("Kích thước icon trên minimap (world unit).")]
    public float iconSize = 1.5f;

    [Tooltip("Sorting Order cho icon (cao hơn = hiển thị trên cùng).")]
    public int sortingOrder = 100;

    [Header("Label (Tùy chọn)")]
    [Tooltip("Nếu bật, hiện tên đối tượng phía trên icon trên minimap.")]
    public bool showLabel = false;

    [Tooltip("Text hiển thị. Để trống sẽ dùng tên GameObject.")]
    public string labelText = "";

    private GameObject iconObj;
    private const string MINIMAP_LAYER = "MinimapIcon";
    private const int MINIMAP_LAYER_INDEX = 10; // Layer 10 = MinimapIcon (tránh xung đột layer 6 của prefab)

    void Start()
    {
        CreateIcon();
    }

    void CreateIcon()
    {
        // Kiểm tra layer tồn tại
        if (LayerMask.NameToLayer(MINIMAP_LAYER) == -1)
        {
            Debug.LogWarning($"[MinimapIcon] Layer '{MINIMAP_LAYER}' chưa tồn tại! " +
                             "Icon minimap sẽ không hiển thị đúng. Layer đã được thêm vào TagManager, hãy restart Unity.");
        }

        // Tạo child object cho icon
        iconObj = new GameObject("_MinimapIcon");
        iconObj.transform.SetParent(transform, false);
        iconObj.transform.localPosition = Vector3.zero;
        iconObj.transform.localScale = new Vector3(iconSize, iconSize, 1f);
        iconObj.layer = MINIMAP_LAYER_INDEX;

        // Tạo sprite hình tròn (diamond shape qua rotation hoặc đơn giản là hình vuông)
        SpriteRenderer sr = iconObj.AddComponent<SpriteRenderer>();
        sr.sprite = CreateCircleSprite();
        sr.color = iconColor;
        sr.sortingOrder = sortingOrder;

        // Label
        if (showLabel)
        {
            CreateLabel();
        }
    }

    /// <summary>Tạo sprite hình tròn bằng code (32x32 pixel).</summary>
    Sprite CreateCircleSprite()
    {
        int size = 32;
        float radius = size / 2f;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        Color transparent = new Color(0, 0, 0, 0);

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(radius, radius));
                if (dist < radius - 1f)
                {
                    tex.SetPixel(x, y, Color.white);
                }
                else if (dist < radius)
                {
                    // Anti-alias edge
                    float alpha = 1f - (dist - (radius - 1f));
                    tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
                else
                {
                    tex.SetPixel(x, y, transparent);
                }
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    void CreateLabel()
    {
        GameObject labelObj = new GameObject("_MinimapLabel");
        labelObj.transform.SetParent(iconObj.transform, false);
        labelObj.transform.localPosition = new Vector3(0, 1.2f, 0);
        labelObj.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
        labelObj.layer = MINIMAP_LAYER_INDEX;

        // Dùng TextMesh (world space) vì TextMeshPro cần Canvas
        TextMesh tm = labelObj.AddComponent<TextMesh>();
        tm.text = string.IsNullOrEmpty(labelText) ? gameObject.name : labelText;
        tm.fontSize = 36;
        tm.characterSize = 0.15f;
        tm.anchor = TextAnchor.LowerCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = iconColor;

        // MeshRenderer sorting
        MeshRenderer mr = labelObj.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.sortingOrder = sortingOrder + 1;
        }
    }

    void OnDestroy()
    {
        if (iconObj != null)
        {
            Destroy(iconObj);
        }
    }

    /// <summary>Đổi màu icon lúc runtime.</summary>
    public void SetColor(Color color)
    {
        iconColor = color;
        if (iconObj != null)
        {
            SpriteRenderer sr = iconObj.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = color;
        }
    }
}
