using UnityEditor;
using UnityEngine;

public class FixMapLayersTool : Editor
{
    [MenuItem("Tools/Fix Map & Ground Layer Order")]
    public static void FixMapLayers()
    {
        // Ưu tiên chọn GameObject đang click, nếu không thì lấy toàn bộ
        GameObject targetMap = Selection.activeGameObject;
        
        if (targetMap == null)
        {
            Debug.LogWarning("Vui lòng click chọn Map (hoặc Battlefield-Map) trong Hierarchy (Scene) trước khi chạy công cụ này.");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(targetMap, "Fix Map Layer Order");

        Renderer[] allRenderers = targetMap.GetComponentsInChildren<Renderer>(true);
        int changedCount = 0;

        foreach (Renderer rend in allRenderers)
        {
            string objName = rend.gameObject.name.ToLower();

            // Nếu là layer mặt đất nền (Ground) -> set index âm sâm nhất
            if (objName.Contains("ground") || objName.Contains("nền"))
            {
                rend.sortingOrder = -100;
                changedCount++;
            }
            // Nếu là layer cỏ, lối đi trên mặt đất
            else if (objName.Contains("path") || objName.Contains("grass"))
            {
                rend.sortingOrder = -90;
                changedCount++;
            }
            // Nếu là layer Water (nước)
            else if (objName.Contains("water") || objName.Contains("nước"))
            {
                rend.sortingOrder = -95;
                changedCount++;
            }
            // Nếu là layer Wall (tường)
            else if (objName.Contains("wall") || objName.Contains("tường"))
            {
                rend.sortingOrder = -50;
                changedCount++;
            }
            // Nếu là layer trang trí hoặc các object khác
            else if (objName.Contains("decor") || objName.Contains("prop") || objName.Contains("tree"))
            {
                rend.sortingOrder = -10;
                changedCount++;
            }
            else 
            {
                // Mặc định cho Map Object khác là 0 để không lộn với Player
                rend.sortingOrder = 0;
                changedCount++;
            }
            
            // Đảm bảo tất cả đều nằm ở layer chung là Environment
            rend.sortingLayerName = "Environment";
        }

        Debug.Log($"[FixMapLayersTool] Đã tự động sắp xếp Sorting Order (Ground xuống cuối) cho {changedCount} tilemaps/renderers trong {targetMap.name}!");
    }
}
