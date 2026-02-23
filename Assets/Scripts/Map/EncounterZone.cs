using UnityEngine;

public class EncounterZone : MonoBehaviour
{
    [Header("Zone Settings")]
    public Mapdata mapData;
    public float zoneEncounterRate = 0.1f; // Override if needed

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && MapManager.Instance != null)
        {
            MapManager.Instance.SetMap(mapData);
            // Optionally set encounter rate
            MapManager.Instance.encounterRate = zoneEncounterRate;
        }
    }
}