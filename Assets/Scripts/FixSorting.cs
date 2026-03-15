using UnityEngine;
using UnityEngine.Tilemaps;

[ExecuteInEditMode]
public class FixSorting : MonoBehaviour {
    void Update() {
        var tr = GetComponent<TilemapRenderer>();
        if (tr != null) {
            // Chỉ cần dòng này là quan trọng nhất để nấp được từng hòn đá
            tr.mode = TilemapRenderer.Mode.Individual; 
            
            // Nếu nó báo lỗi ở dòng spriteSortPoint thì cứ xóa nó đi, Unity 6 sẽ tự hiểu.
            Debug.Log("Đã ép Tilemap sang Individual thành công!");
        }
    }
}