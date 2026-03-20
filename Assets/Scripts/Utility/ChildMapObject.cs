using UnityEngine;

[ExecuteInEditMode] // Chạy ngay trong Editor, không cần nhấn Play
public class GroupSettingsManager : MonoBehaviour {
    [Header("Thiết lập chung")]
    public string targetLayer = "Map";           // Layer vật lý (để va chạm)
    public string targetSortingLayer = "Objects"; // Sorting Layer hiển thị (để che khuất)
    public string targetTag = "Wall";        // Tag nếu cần

    void Update() {
        // Duyệt qua tất cả các con trực tiếp
        foreach (Transform child in transform) {
            // 1. Ép Layer vật lý
            child.gameObject.layer = LayerMask.NameToLayer(targetLayer);
            
            // 2. Ép Tag
            child.gameObject.tag = targetTag;

            SpriteRenderer sr = child.GetComponent<SpriteRenderer>();
            if (sr != null) {
                // 3. Ép Sorting Layer hiển thị
                sr.sortingLayerName = targetSortingLayer;

                // 4. Ép Sprite Sort Point về Pivot để tính nấp theo chân
                sr.spriteSortPoint = SpriteSortPoint.Pivot;
                // 5. Thêm Polygon Collider 2D nếu chưa có (để va chạm)
                PolygonCollider2D poly = child.GetComponent<PolygonCollider2D>();
                if (poly == null) {
                    // Nếu chưa có thì add mới
                    poly = child.gameObject.AddComponent<PolygonCollider2D>();
                    Debug.Log($"Đã tự add PolygonCollider cho: {child.name}");
                }
                // 2. Tự add Script làm mờ
                if (child.GetComponent<ObjectFader>() == null)
                    child.gameObject.AddComponent<ObjectFader>();

                // 3. TỰ ĐỘNG TẠO VÙNG MỜ THEO KÍCH THƯỚC ẢNH
                BoxCollider2D box = child.GetComponent<BoxCollider2D>();
                if (box == null) {
                    box = child.gameObject.AddComponent<BoxCollider2D>();
                    box.isTrigger = true; // Để đi xuyên qua được
                    
                    // Lấy kích thước thực tế của cái ảnh đá
                    Vector2 spriteSize = sr.sprite.bounds.size;
                    box.size = spriteSize;
                    
                    // Đẩy cái tâm Box lên trên một chút để nó bao trọn phần thân đá
                    box.offset = sr.sprite.bounds.center;
                    Debug.Log($"Đã tự căn chỉnh vùng mờ cho {child.name}");
                }
            }
        }
    }
}