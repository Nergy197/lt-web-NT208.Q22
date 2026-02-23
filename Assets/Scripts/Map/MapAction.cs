using UnityEngine;

public abstract class MapAction : ScriptableObject
{
    public string actionName;

    public abstract void Execute();
}
