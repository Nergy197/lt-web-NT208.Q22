using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Kết thúc Chapter1_Tutorial: đánh dấu tutorial xong, start quest chapter 1, load MapScene.
/// Gắn trên Tutorial_Controller hoặc tự thêm bởi SimpleTutorialManager.
/// </summary>
public class TutorialChapter1Exit : MonoBehaviour
{
    [Header("Chuyển scene")]
    public string nextScene = "Chapter1_CutScene";
    public float delayBeforeLoad = 0.5f;

    bool _completed;

    public void CompleteTutorial()
    {
        if (_completed) return;
        _completed = true;
        StartCoroutine(CompleteRoutine());
    }

    IEnumerator CompleteRoutine()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.MarkChapter1TutorialCompleted();

        var qm = QuestManager.Instance;
        if (qm != null)
            qm.TryStartChapter1Quests();

        yield return new WaitForSeconds(delayBeforeLoad);
        SceneManager.LoadScene(nextScene);
    }
}
