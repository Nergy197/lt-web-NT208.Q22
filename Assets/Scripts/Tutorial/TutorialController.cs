using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Đồng bộ hướng dẫn với trạng thái thực tế của trận đấu.
///
/// Bước 1 — Tấn công thường  : chỉ hiện "LƯỢT CỦA BẠN" khi OnPlayerTurnStart bắn.
/// Bước 2 — Đỡ đòn Parry    : chỉ advance khi địch đánh xong (OnAttackFinished sau OnEnemyTurnStart).
/// Bước 3 — Dùng Kỹ Năng    : chỉ advance khi người chơi đánh (không phải địch).
/// Bước 4 — Hoàn thành       : ẩn sau delay.
/// </summary>
public class TutorialController : MonoBehaviour
{
    [SerializeField] private TutorialPromptUI promptUI;
    [SerializeField] private string nextScene = "Chapter1_CutScene";
    [SerializeField] private float delayAfterComplete = 2f;

    // ── State ─────────────────────────────────────────────────────────────────
    int  step             = 0;     // 0 = chờ khởi động
    bool isEnemyTurn      = false; // true khi đang trong lượt địch
    bool parryDone        = false;
    bool cutscenePending  = false; // true khi đang chờ load cutscene sau BattleWin

    // ── Nội dung hướng dẫn ───────────────────────────────────────────────────

    // Khi đến lượt NGƯỜI CHƠI
    const string PLAYER_TURN_1 =
        "LƯỢT CỦA BẠN!\n" +
        "Nhấn [E] hoặc click 'Đánh Thường' để tấn công.";

    const string PLAYER_TURN_3 =
        "LƯỢT CỦA BẠN!\n" +
        "Nhấn [W] hoặc click 'Kỹ Năng' → chọn kỹ năng bằng [Q] / [W] / [E].";

    // Khi đến lượt ĐỊCH
    const string ENEMY_TURN_GENERIC =
        "Lượt địch đang hành động...\n" +
        "Nhấn [SPACE] khi thấy nhấp nháy để đỡ đòn!";

    const string ENEMY_TURN_STEP2 =
        "Lượt địch! Hãy sẵn sàng đỡ đòn.\n" +
        "Nhấn [SPACE] ngay khi cửa sổ đỡ đòn xuất hiện!";

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    void Start()
    {
        if (promptUI == null)
            promptUI = Object.FindFirstObjectByType<TutorialPromptUI>();

        // step = 0, chờ BattleManager bắn OnPlayerTurnStart lần đầu tiên.
        promptUI?.Hide();
    }

    void OnEnable()
    {
        BattleEvents.OnPlayerTurnStart      += HandlePlayerTurnStart;
        BattleEvents.OnEnemyTurnStart       += HandleEnemyTurnStart;
        BattleEvents.OnEnemyAttackAnnounced += HandleEnemyAttackAnnounced;
        BattleEvents.OnEnemyHitIncoming     += HandleEnemyHitIncoming;
        BattleEvents.OnParryWindowOpened    += HandleParryWindowOpened;
        BattleEvents.OnParrySuccess         += HandleParrySuccess;
        BattleEvents.OnAttackFinished       += HandleAttackFinished;
        EventManager.Subscribe(GameEvent.BattleWin, HandleBattleWin);
    }

    void OnDisable()
    {
        BattleEvents.OnPlayerTurnStart      -= HandlePlayerTurnStart;
        BattleEvents.OnEnemyTurnStart       -= HandleEnemyTurnStart;
        BattleEvents.OnEnemyAttackAnnounced -= HandleEnemyAttackAnnounced;
        BattleEvents.OnEnemyHitIncoming     -= HandleEnemyHitIncoming;
        BattleEvents.OnParryWindowOpened    -= HandleParryWindowOpened;
        BattleEvents.OnParrySuccess         -= HandleParrySuccess;
        BattleEvents.OnAttackFinished       -= HandleAttackFinished;
        EventManager.Unsubscribe(GameEvent.BattleWin, HandleBattleWin);
    }

    // ── Turn events (đồng bộ với BattleManager) ───────────────────────────────

    void HandlePlayerTurnStart(PlayerStatus player)
    {
        isEnemyTurn = false;

        if (step == 0)
        {
            // Lần đầu tiên đến lượt người chơi → bắt đầu bước 1
            step = 1;
        }

        switch (step)
        {
            case 1: promptUI.Show(PLAYER_TURN_1);  break;
            case 2:
                // Nếu địch đã đánh ít nhất 1 lần → advance sang bước 3
                // (OnAttackFinished sẽ xử lý; ở đây chỉ nhắc chờ địch)
                promptUI.Show(
                    "Tốt! Bây giờ hãy chờ lượt địch để luyện đỡ đòn.\n" +
                    "Khi thấy nhấp nháy → nhấn ngay [SPACE]!");
                break;
            case 3: promptUI.Show(PLAYER_TURN_3);  break;
        }
    }

    void HandleEnemyTurnStart(EnemyStatus enemy)
    {
        isEnemyTurn = true;

        switch (step)
        {
            case 1:
                promptUI.Show(
                    "Lượt địch!\n" +
                    "Nhấn [SPACE] khi thấy nhấp nháy để đỡ đòn — sau đó nhớ tấn công.");
                break;
            case 2:
                promptUI.Show(ENEMY_TURN_STEP2);
                break;
            case 3:
                promptUI.Show(
                    "Lượt địch — hãy đỡ đòn [SPACE] nếu kịp!\n" +
                    "Sau đó dùng Kỹ Năng khi đến lượt bạn.");
                break;
        }
    }

    // ── Attack events ─────────────────────────────────────────────────────────

    void HandleEnemyAttackAnnounced(EnemyAttackData attack, EnemyStatus enemy, PlayerStatus target)
    {
        if (step == 1 || step == 2)
        {
            promptUI.Show(
                "⚔ Địch đang tấn công!\n" +
                "Sẵn sàng nhấn [SPACE] khi cửa sổ đỡ đòn mở!");
        }
    }

    void HandleEnemyHitIncoming(EnemyAttackHit hit, int hitIndex, EnemyStatus enemy)
    {
        if ((step == 1 || step == 2) && hit.canBeParried)
            promptUI.Show("Đòn đánh sắp tới! Nhấn [SPACE] ngay!");
    }

    void HandleParryWindowOpened(PlayerStatus player)
    {
        if (step == 1 || step == 2 || step == 3)
            promptUI.FlashParry();
    }

    void HandleParrySuccess(PlayerStatus player)
    {
        parryDone = true;
        StopAllCoroutines();
        StartCoroutine(ShowParrySuccessAndRestore());
    }

    IEnumerator ShowParrySuccessAndRestore()
    {
        string next = step == 1
            ? "Đỡ đòn thành công! (+AP)\nBây giờ hãy tấn công: nhấn [E]."
            : "Đỡ đòn thành công! Xuất sắc!";
        promptUI.Show(next);
        yield return new WaitForSeconds(2f);

        // Khôi phục nội dung phù hợp với trạng thái hiện tại
        if (!isEnemyTurn) HandlePlayerTurnStart(null);
        else              HandleEnemyTurnStart(null);
    }

    void HandleAttackFinished()
    {
        bool wasEnemy = isEnemyTurn;
        // isEnemyTurn sẽ được reset bởi HandlePlayerTurnStart ở lượt tiếp

        switch (step)
        {
            case 1:
                if (!wasEnemy)
                {
                    // Người chơi vừa tấn công → sang bước 2
                    step = 2;
                    parryDone = false;
                    promptUI.Show(
                        "Tốt! Đòn đánh đầu tiên thành công.\n" +
                        "Bây giờ chờ địch tấn công để luyện đỡ đòn [SPACE].");
                }
                break;

            case 2:
                if (wasEnemy)
                {
                    // Địch đánh xong → sang bước 3
                    step = 3;
                    promptUI.Show(
                        "Hoàn thành bước đỡ đòn!\n" +
                        "Khi đến lượt bạn: nhấn [W] → chọn Kỹ Năng → Q/W/E.");
                }
                break;

            case 3:
                if (!wasEnemy)
                {
                    // Người chơi hành động (skill hoặc đánh thường) → hoàn thành
                    step = 4;
                    promptUI.Show(
                        "Xuất sắc! Bạn đã nắm được các thao tác cơ bản!\n" +
                        "Chuẩn bị chuyển sang màn tiếp theo...");
                    StartCoroutine(FinishTutorial());
                }
                break;
        }
    }

    // Gọi khi player hoàn thành bước 3 (dùng skill) — chỉ hiện thông báo,
    // việc chuyển scene sẽ do HandleBattleWin xử lý sau khi thắng.
    IEnumerator FinishTutorial()
    {
        promptUI?.Show(
            "Xuất sắc! Bạn đã nắm được các thao tác cơ bản!\n" +
            "Hãy tiêu diệt kẻ địch để tiếp tục!");
        yield break;
    }

    void HandleBattleWin(object payload)
    {
        if (cutscenePending) return;
        cutscenePending = true;

        if (GameManager.Instance != null)
            GameManager.Instance.MarkChapter1TutorialCompleted();
        QuestManager.Instance?.TryStartChapter1Quests();

        promptUI?.Show("Chiến thắng! Chuẩn bị chuyển sang màn tiếp theo...");
        StartCoroutine(LoadCutscene());
    }

    IEnumerator LoadCutscene()
    {
        yield return new WaitForSeconds(delayAfterComplete);
        promptUI?.Hide();
        SceneManager.LoadScene(nextScene);
    }

    IEnumerator HideAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        promptUI?.Hide();
    }
}
