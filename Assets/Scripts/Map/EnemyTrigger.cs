using UnityEngine;
using UnityEngine.SceneManagement;

public class EnemyTrigger : MonoBehaviour
{
    public EnemyData enemyData;   // enemy sẽ đánh
    public int mapLevel = 1;      // level map

    private bool triggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("TRIGGER WORKED");
    }

}
