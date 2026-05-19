using System.Collections;
using UnityEngine;

public class TutorialController : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private TutorialPromptUI promptUI;

    private int currentStep = 1; // Step 1: Basic Attack, Step 2: Parry, Step 3: Skill, Step 4: Done
    private bool parrySuccess = false;

    void Start()
    {
        if (promptUI == null)
        {
            promptUI = Object.FindFirstObjectByType<TutorialPromptUI>();
        }

        StartCoroutine(StartTutorialWithDelay());
    }

    private IEnumerator StartTutorialWithDelay()
    {
        yield return null;
        UpdatePromptForStep();
    }

    void OnEnable()
    {
        BattleEvents.OnAttackFinished       += HandleAttackFinished;
        BattleEvents.OnEnemyAttackAnnounced += HandleEnemyAttackAnnounced;
        BattleEvents.OnEnemyHitIncoming     += HandleEnemyHitIncoming;
        BattleEvents.OnParryWindowOpened    += HandleParryWindowOpened;
        BattleEvents.OnParrySuccess         += HandleParrySuccess;
    }

    void OnDisable()
    {
        BattleEvents.OnAttackFinished       -= HandleAttackFinished;
        BattleEvents.OnEnemyAttackAnnounced -= HandleEnemyAttackAnnounced;
        BattleEvents.OnEnemyHitIncoming     -= HandleEnemyHitIncoming;
        BattleEvents.OnParryWindowOpened    -= HandleParryWindowOpened;
        BattleEvents.OnParrySuccess         -= HandleParrySuccess;
    }

    private void HandleAttackFinished()
    {
        if (currentStep == 1)
        {
            AdvanceStep();
        }
        else if (currentStep == 2)
        {
            if (!parrySuccess)
            {
                AdvanceStep();
            }
        }
        else if (currentStep == 3)
        {
            AdvanceStep();
        }
    }

    private void HandleEnemyAttackAnnounced(EnemyAttackData attack, EnemyStatus enemy, PlayerStatus target)
    {
        if (currentStep == 2)
        {
            promptUI.Show("Ke dich chuan bi tan cong! Hay chu y!");
        }
    }

    private void HandleEnemyHitIncoming(EnemyAttackHit hit, int hitIndex, EnemyStatus enemy)
    {
        if (currentStep == 2)
        {
            promptUI.Show("Sap toi roi...");
        }
    }

    private void HandleParryWindowOpened(PlayerStatus player)
    {
        if (currentStep == 2)
        {
            promptUI.FlashParry();
        }
    }

    private void HandleParrySuccess(PlayerStatus player)
    {
        if (currentStep == 2)
        {
            parrySuccess = true;
            StartCoroutine(ShowParrySuccessPrompt());
        }
    }

    private IEnumerator ShowParrySuccessPrompt()
    {
        promptUI.Show("Do don thanh cong! Ban da duoc tang AP!");
        yield return new WaitForSeconds(1.5f);
        AdvanceStep();
    }

    private void AdvanceStep()
    {
        currentStep++;
        UpdatePromptForStep();

        if (currentStep == 4)
        {
            SetTutorialComplete();
        }
    }

    private void UpdatePromptForStep()
    {
        if (promptUI == null) return;

        switch (currentStep)
        {
            case 1:
                promptUI.Show("Nhan key Q (hoac click nut Tan Cong) de thuc hien tan cong thuong!");
                break;
            case 2:
                promptUI.Show("Cho luot cua ke dich de thuc hien do don (Parry).");
                break;
            case 3:
                promptUI.Show("Dung chieu thuc: Mo Menu chieu thuc [W] va chon mot ky nang de tan cong!");
                break;
            case 4:
                promptUI.Show("Hoan thanh huong dan! Hay tieu diet ke dich de ket thuc tran dau!");
                StartCoroutine(HidePromptAfterDelay(4.0f));
                break;
        }
    }

    private IEnumerator HidePromptAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (promptUI != null)
        {
            promptUI.Hide();
        }
    }

    private void SetTutorialComplete()
    {
        PlayerPrefs.SetInt("tutorialCompleted", 1);
        PlayerPrefs.Save();
        Debug.Log("[TUTORIAL] Completed! PlayerPrefs updated.");
    }
}
