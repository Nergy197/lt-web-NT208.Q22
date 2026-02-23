using UnityEngine;
using UnityEngine.SceneManagement;
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public EnemyData currentEnemy;
    public int currentMapLevel;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    public void StartBattle(EnemyData enemy, int mapLevel)
    {
        currentEnemy = enemy;
        currentMapLevel = mapLevel;
        SceneManager.LoadScene("BattleScene");
    }
}
