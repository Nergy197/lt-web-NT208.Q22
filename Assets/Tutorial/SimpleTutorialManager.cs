using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SimpleTutorialManager : MonoBehaviour
{
    [Header("Data Assets")]
    public PlayerData heroData;
    public EnemyData enemyData;

    [Header("Menus (Canvas Groups)")]
    public CanvasGroup mainMenuCG;
    public CanvasGroup skillMenuCG;
    public CanvasGroup itemMenuCG;

    [Header("UI & Characters")]
    public Image tuanHPBar;
    public TextMeshProUGUI tuanHPText;
    public GameObject[] tuanAPGlows;
    public Image linhHPBar;
    public TextMeshProUGUI linhHPText;
    public GameObject tuanObj;
    public GameObject linhObj;

    [Header("Movement Settings")]
    public float dashSpeed = 30f;
    public float returnSpeed = 20f;
    public float attackOffset = 1.3f;

    private @GameInput inputActions;
    private PlayerStatus tuanStatus;
    private EnemyStatus linhStatus;
    private UnitVisual tuanVisual;
    private UnitVisual linhVisual;
    private Vector3 tuanHomePos;
    private Vector3 linhHomePos;

    private bool canInput = true;
    private enum MenuState { Main, Skill, Item }
    private MenuState currentState = MenuState.Main;

    void Awake() { inputActions = new @GameInput(); }
    void OnEnable() { inputActions.Enable(); }
    void OnDisable() { inputActions.Disable(); }

    void Start()
    {
        if (heroData == null || enemyData == null)
        {
            Debug.LogError("Anh Chuẩn ơi! Quên kéo Hero/Enemy Data vào Controller rồi!");
            return;
        }

        // 1. Khởi tạo Status từ Data
        tuanStatus = new PlayerStatus(heroData.entityName, heroData.baseHP, heroData.baseAtk, heroData.baseDef, heroData.baseSpd, heroData.maxAP);
        tuanStatus.InitializeSkills(heroData.attacks);
        tuanStatus.currentAP = 20; // Bắt đầu với 1 cục mana

        linhStatus = new EnemyStatus(enemyData.entityName, enemyData.baseHP, enemyData.baseAtk, enemyData.baseDef, enemyData.baseSpd);

        tuanVisual = tuanObj.GetComponent<UnitVisual>();
        linhVisual = linhObj.GetComponent<UnitVisual>();
        tuanStatus.SpawnedModel = tuanObj;
        linhStatus.SpawnedModel = linhObj;
        tuanHomePos = tuanObj.transform.position;
        linhHomePos = linhObj.transform.position;

        UpdateUI();
        SetupInputs();
        SwitchMenu(MenuState.Main);
    }

    void SetupInputs()
    {
        // LOGIC PHÍM BÀN PHÍM (Sửa lỗi bấm Q trôi menu)

        // Phím E (Tương ứng Basic Attack / Skill 1 / Item 1)
        inputActions.Battle.BasicAttack.performed += ctx => {
            if (!canInput || !(ctx.control.device is Keyboard) || EventSystem.current.IsPointerOverGameObject()) return;

            if (currentState == MenuState.Main) ExecuteBasicAttack();
            else if (currentState == MenuState.Skill) ExecuteSkillByIndex(0);
            else if (currentState == MenuState.Item) ExecuteItemByIndex(0);
        };

        // Phím W (Tương ứng Open Skill / Skill 2 / Item 2)
        inputActions.Battle.OpenSkillMenu.performed += ctx => {
            if (!canInput || !(ctx.control.device is Keyboard) || EventSystem.current.IsPointerOverGameObject()) return;

            if (currentState == MenuState.Main) OpenSkillMenu();
            else if (currentState == MenuState.Skill) ExecuteSkillByIndex(1);
            else if (currentState == MenuState.Item) ExecuteItemByIndex(1);
        };

        // Phím Q (Tương ứng Open Item / Skill 3 / Item 3)
        inputActions.Battle.OpenItemMenu.performed += ctx => {
            if (!canInput || !(ctx.control.device is Keyboard) || EventSystem.current.IsPointerOverGameObject()) return;

            if (currentState == MenuState.Main) OpenItemMenu();
            else if (currentState == MenuState.Skill) ExecuteSkillByIndex(2);
            else if (currentState == MenuState.Item) ExecuteItemByIndex(2);
        };

        inputActions.Battle.Parry.performed += _ => { tuanStatus.RequestParry(); };
        inputActions.SkillMenu.Cancel.performed += _ => { if (canInput && currentState != MenuState.Main) BackToMainMenu(); };
    }

    // --- LƯỢT NGƯỜI CHƠI ---
    public void ExecuteBasicAttack()
    {
        var basicAtk = tuanStatus.BasicAttack;
        if (basicAtk != null)
        {
            tuanStatus.RestoreAP(20);
            int dmg = (basicAtk.hits.Count > 0) ? Mathf.RoundToInt(tuanStatus.Atk * basicAtk.hits[0].damageMultiplier) : tuanStatus.Atk;
            StartCoroutine(CombatSequence(dmg, 0));
        }
    }

    public void ExecuteSkillByIndex(int index)
    {
        var skill = tuanStatus.GetSkillByIndex(index);
        if (skill == null) return;

        int actualCost = skill.apCost * 20; // 1 cục mana = 20 điểm AP

        if (tuanStatus.currentAP >= actualCost)
        {
            int calculatedDmg = 0;
            if (skill.hits != null && skill.hits.Count > 0)
            {
                calculatedDmg = Mathf.RoundToInt(tuanStatus.Atk * skill.hits[0].damageMultiplier);
            }
            StartCoroutine(CombatSequence(calculatedDmg, actualCost));
        }
        else
        {
            Debug.Log("<color=yellow>Không đủ năng lượng! Cần " + skill.apCost + " cục mana.</color>");
        }
    }

    public void ExecuteItemByIndex(int index)
    {
        canInput = false;
        SwitchMenu(MenuState.Main);
        SetMenuGroup(mainMenuCG, false);

        switch (index)
        {
            case 0: // Bình Máu
                int heal = 30;
                tuanStatus.currentHP = Mathf.Min(tuanStatus.MaxHP, tuanStatus.currentHP + heal);
                Debug.Log("<color=green>Hồi " + heal + " HP</color>");
                break;
            case 1: // Bình Năng Lượng
                tuanStatus.RestoreAP(40);
                Debug.Log("<color=cyan>Hồi 2 cục AP</color>");
                break;
            case 2: // Giải Độc
                tuanStatus.ResetForBattle(0);
                Debug.Log("<color=white>Giải trừ trạng thái xấu</color>");
                break;
        }

        UpdateUI();
        StartCoroutine(EndItemTurn());
    }

    IEnumerator CombatSequence(int dmg, int apCost)
    {
        canInput = false;
        SwitchMenu(MenuState.Main);
        SetMenuGroup(mainMenuCG, false);
        if (apCost > 0) tuanStatus.UseAP(apCost);

        yield return MoveTo(tuanObj, linhHomePos - (linhHomePos - tuanHomePos).normalized * attackOffset, dashSpeed);
        tuanVisual.PlayAttack();
        yield return new WaitForSeconds(0.4f);
        if (dmg > 0) linhStatus.TakeDamage(tuanStatus, dmg);
        UpdateUI();
        yield return new WaitForSeconds(0.6f);
        yield return MoveTo(tuanObj, tuanHomePos, returnSpeed);

        if (linhStatus.IsAlive) StartCoroutine(EnemyComboTurn());
        else { canInput = true; SwitchMenu(MenuState.Main); }
    }

    IEnumerator EnemyComboTurn()
    {
        yield return MoveTo(linhObj, tuanHomePos - (tuanHomePos - linhHomePos).normalized * attackOffset, dashSpeed);

        var enemyAtkData = enemyData.attacks[0];

        foreach (var hit in enemyAtkData.hits)
        {
            if (hit.windUpTime > 0) yield return new WaitForSeconds(hit.windUpTime);

            tuanStatus.OpenParryWindow();
            linhVisual.PlayAttack();

            bool isParried = false;
            float timer = 0;
            while (timer < hit.parryWindowDuration)
            {
                if (tuanStatus.ConsumeParry()) { isParried = true; break; }
                timer += Time.deltaTime;
                yield return null;
            }
            tuanStatus.CloseParryWindow();

            if (isParried)
            {
                int counter = tuanStatus.Atk / 2;
                linhStatus.TakeDamage(tuanStatus, counter);
                tuanStatus.RestoreAP(10);
                Debug.Log("<color=green>PARRY THÀNH CÔNG!</color>");
                yield return new WaitForSeconds(0.6f); // Đợi lính phục hồi sau khi bị phản đòn
            }
            else
            {
                int dmg = Mathf.RoundToInt(linhStatus.Atk * hit.damageMultiplier);
                tuanStatus.TakeDamage(linhStatus, dmg);
            }
            UpdateUI();
            if (hit.delayBetweenHits > 0) yield return new WaitForSeconds(hit.delayBetweenHits);
        }

        yield return new WaitForSeconds(0.5f);
        yield return MoveTo(linhObj, linhHomePos, returnSpeed);
        canInput = true;
        SwitchMenu(MenuState.Main);
    }

    // --- HELPER METHODS ---
    IEnumerator EndItemTurn()
    {
        yield return new WaitForSeconds(0.8f);
        if (linhStatus.IsAlive) StartCoroutine(EnemyComboTurn());
        else { canInput = true; SwitchMenu(MenuState.Main); }
    }

    IEnumerator MoveTo(GameObject obj, Vector3 target, float speed)
    {
        while (Vector3.Distance(obj.transform.position, target) > 0.05f)
        {
            obj.transform.position = Vector3.MoveTowards(obj.transform.position, target, speed * Time.deltaTime);
            yield return null;
        }
    }

    public void OpenSkillMenu() { SwitchMenu(MenuState.Skill); }
    public void OpenItemMenu() { SwitchMenu(MenuState.Item); }
    public void BackToMainMenu() { SwitchMenu(MenuState.Main); }

    void SwitchMenu(MenuState newState)
    {
        currentState = newState;
        SetMenuGroup(mainMenuCG, newState == MenuState.Main);
        SetMenuGroup(skillMenuCG, newState == MenuState.Skill);
        SetMenuGroup(itemMenuCG, newState == MenuState.Item);
    }

    void SetMenuGroup(CanvasGroup cg, bool isShow)
    {
        if (cg == null) return;
        cg.alpha = isShow ? 1f : 0f;
        cg.interactable = isShow;
        cg.blocksRaycasts = isShow;
    }

    void UpdateUI()
    {
        if (tuanStatus == null) return;
        tuanHPBar.fillAmount = (float)tuanStatus.currentHP / tuanStatus.MaxHP;
        tuanHPText.text = tuanStatus.currentHP.ToString();
        linhHPBar.fillAmount = (float)linhStatus.currentHP / linhStatus.MaxHP;
        linhHPText.text = linhStatus.currentHP.ToString();
        for (int i = 0; i < tuanAPGlows.Length; i++)
        {
            if (tuanAPGlows[i] != null) tuanAPGlows[i].SetActive(tuanStatus.currentAP >= (i + 1) * 20);
        }
    }
}