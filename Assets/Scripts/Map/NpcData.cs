using UnityEngine;

/// <summary>
/// ScriptableObject chứa dữ liệu của một NPC trên map.
/// Tạo asset: chuột phải trong Project → Create → NPC → NPC Data
/// </summary>
[CreateAssetMenu(menuName = "NPC/NPC Data", fileName = "NewNpc")]
public class NpcData : ScriptableObject
{
    [Header("Identity")]
    public string npcName = "NPC";

    [Tooltip("Sprite hiển thị trên map (tùy chọn, dùng nếu không gắn SpriteRenderer tay).")]
    public Sprite portrait;

    [Header("Dialogue")]
    [Tooltip("Các dòng hội thoại theo thứ tự. Mỗi phần tử là một dòng thoại.")]
    [TextArea(2, 5)]
    public string[] dialogueLines;

    [Header("Quest Integration")]
    [Tooltip("Id của quest cần complete objective (để trống nếu NPC không liên quan quest).")]
    public string questId;

    [Tooltip("Id của objective sẽ được đánh dấu hoàn thành khi nói chuyện xong.")]
    public string objectiveId;

    [Header("Repeat")]
    [Tooltip("Dialogue lặp lại mỗi lần tương tác sau lần đầu (nếu triggerOnce = false trên NpcTrigger).")]
    [TextArea(2, 4)]
    public string repeatLine = "...";
}
