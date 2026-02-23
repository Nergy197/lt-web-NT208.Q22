using UnityEngine;

public class MapTrigger : MonoBehaviour
{
    public MapAction action;
    public bool triggerOnce = true;

    private bool triggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        action?.Execute();

        if (triggerOnce)
            triggered = true;
    }
}
