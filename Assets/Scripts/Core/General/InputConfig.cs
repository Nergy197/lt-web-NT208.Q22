using UnityEngine;

[CreateAssetMenu(fileName = "DefaultKeymap", menuName = "Game/InputConfig")]
public class InputConfig : ScriptableObject
{
    [Header("=== COMBAT KEYS ===")]
    public KeyCode parryKey = KeyCode.Space;
    public KeyCode basicAttackKey = KeyCode.E; 
    public KeyCode skill1Key = KeyCode.Q;

    [Header("=== NAVIGATION KEYS ===")]
    public KeyCode nextTargetKey = KeyCode.RightArrow;
    public KeyCode prevTargetKey = KeyCode.LeftArrow;

    [Header("=== SYSTEM KEYS ===")]
    public KeyCode confirmKey = KeyCode.Return;
    public KeyCode cancelKey = KeyCode.Escape;
}