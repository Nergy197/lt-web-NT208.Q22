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

    [Header("Visual Effects (VFX)")]
    public GameObject longTramVFXPrefab;
    public GameObject congKichVFXPrefab;
    public GameObject debuffIconPrefab;

    [Space(10)]
    [Header("Hộ Thể VFX (Sprite Sheet)")]
    public GameObject hoTheVFXPrefab;      // Prefab Cột Sáng Vàng
    public GameObject buffHoTheIconPrefab;   // Icon kiếm xanh dưới chân
    public float debuffYOffset = -1.2f;      // Offset icon dưới chân

    private GameObject activeDebuffIcon;
    private int debuffTurnsLeft = 0;
    private GameObject activeBuffIcon;
    private int buffTurnsLeft = 0;

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
            Debug.LogError("Lỗi: Quên kéo Hero/Enemy Data rồi anh Chuẩn ơi!");
            return;
        }

        tuanStatus = new PlayerStatus(heroData.entityName, heroData.baseHP, heroData.baseAtk, heroData.baseDef, heroData.baseSpd, heroData.maxAP);
        tuanStatus.InitializeSkills(heroData.attacks);
        tuanStatus.currentAP = 20;

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
        inputActions.Battle.BasicAttack.performed += ctx => {
            if (!canInput || !(ctx.control.device is Keyboard) || EventSystem.current.IsPointerOverGameObject()) return;
            if (currentState == MenuState.Main) ExecuteBasicAttack();
            else if (currentState == MenuState.Skill) ExecuteSkillByIndex(0);
            else if (currentState == MenuState.Item) ExecuteItemByIndex(0);
        };

        inputActions.Battle.OpenSkillMenu.performed += ctx => {
            if (!canInput || !(ctx.control.device is Keyboard) || EventSystem.current.IsPointerOverGameObject()) return;
            if (currentState == MenuState.Main) OpenSkillMenu();
            else if (currentState == MenuState.Skill) ExecuteSkillByIndex(1);
            else if (currentState == MenuState.Item) ExecuteItemByIndex(1);
        };

        inputActions.Battle.OpenItemMenu.performed += ctx => {
            if (!canInput || !(ctx.control.device is Keyboard) || EventSystem.current.IsPointerOverGameObject()) return;
            if (currentState == MenuState.Main) OpenItemMenu();
            else if (currentState == MenuState.Skill) ExecuteSkillByIndex(2);
            else if (currentState == MenuState.Item) ExecuteItemByIndex(2);
        };

        inputActions.Battle.Parry.performed += _ => {
            if (!canInput) tuanStatus.RequestParry();
        };

        inputActions.SkillMenu.Cancel.performed += _ => { if (canInput && currentState != MenuState.Main) BackToMainMenu(); };
    }

    public void ExecuteSkillByIndex(int index)
    {
        var skill = tuanStatus.GetSkillByIndex(index);
        if (skill == null) return;

        int actualCost = skill.apCost * 20;
        if (tuanStatus.currentAP >= actualCost)
        {
            if (index == 2) StartCoroutine(BuffSequence(actualCost, skill));
            else
            {
                int calculatedDmg = Mathf.RoundToInt(tuanStatus.Atk * skill.hits[0].damageMultiplier);
                GameObject slashVfx = (index == 0) ? longTramVFXPrefab : congKichVFXPrefab;
                StartCoroutine(CombatSequence(calculatedDmg, actualCost, slashVfx, skill));
            }
        }
        else Debug.Log("<color=yellow>Không đủ Mana!</color>");
    }

    // --- LOGIC HỘ THỂ ĐÃ CHỈNH SỬA ---
    IEnumerator BuffSequence(int apCost, PlayerAttackData skillData)
    {
        canInput = false;
        SwitchMenu(MenuState.Main);
        SetMenuGroup(mainMenuCG, false);
        if (apCost > 0) tuanStatus.UseAP(apCost);

        // 1. NHÂN VẬT ĐỨNG IM (Đã bỏ dòng chạy Animation doParry)

        // 2. Hiện Cột Sáng Vàng và tự hủy sau 1.5 giây
        if (hoTheVFXPrefab != null)
        {
            GameObject vfxInstance = Instantiate(hoTheVFXPrefab, tuanObj.transform.position, Quaternion.identity);
            Destroy(vfxInstance, 0.5f); // Tự động xóa khỏi scene sau 1.5s
        }

        yield return new WaitForSeconds(1.0f);

        // 3. Hiện Icon Kiếm Xanh dưới chân (Buff chỉ số)
        if (skillData != null && skillData.effects != null)
        {
            foreach (var entry in skillData.effects)
            {
                if (entry.target == SkillEffectTarget.Self)
                {
                    tuanStatus.ApplyStatusEffect(entry.effect);
                    if (activeBuffIcon != null) Destroy(activeBuffIcon);

                    Vector3 feetPos = tuanObj.transform.position + new Vector3(0, debuffYOffset, 0);
                    activeBuffIcon = Instantiate(buffHoTheIconPrefab, feetPos, Quaternion.identity);
                    activeBuffIcon.transform.SetParent(tuanObj.transform);
                    buffTurnsLeft = entry.effect.duration;
                }
            }
        }

        UpdateUI();
        yield return new WaitForSeconds(0.5f);
        if (linhStatus.IsAlive) StartCoroutine(EnemyComboTurn());
        else { canInput = true; UpdateUI(); }
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
                linhStatus.TakeDamage(tuanStatus, tuanStatus.Atk / 2);
                tuanStatus.RestoreAP(10);
                yield return new WaitForSeconds(0.6f);
            }
            else tuanStatus.TakeDamage(linhStatus, Mathf.RoundToInt(linhStatus.Atk * hit.damageMultiplier));

            UpdateUI();
            if (hit.delayBetweenHits > 0) yield return new WaitForSeconds(hit.delayBetweenHits);
        }

        yield return new WaitForSeconds(0.5f);
        yield return MoveTo(linhObj, linhHomePos, returnSpeed);

        UpdateStatusDurations();

        canInput = true;
        SwitchMenu(MenuState.Main);
    }

    void UpdateStatusDurations()
    {
        if (debuffTurnsLeft > 0)
        {
            debuffTurnsLeft--;
            linhStatus.UpdateEffectDurations();
            if (debuffTurnsLeft <= 0 && activeDebuffIcon != null) Destroy(activeDebuffIcon);
        }

        if (buffTurnsLeft > 0)
        {
            buffTurnsLeft--;
            tuanStatus.UpdateEffectDurations();
            if (buffTurnsLeft <= 0 && activeBuffIcon != null)
            {
                var handler = activeBuffIcon.GetComponent<VFXBuffHandler>();
                if (handler != null) handler.DestroyIcon();
                else Destroy(activeBuffIcon);
            }
        }
    }

    public void ExecuteBasicAttack()
    {
        var basicAtk = tuanStatus.BasicAttack;
        if (basicAtk != null)
        {
            tuanStatus.RestoreAP(20);
            int dmg = (basicAtk.hits.Count > 0) ? Mathf.RoundToInt(tuanStatus.Atk * basicAtk.hits[0].damageMultiplier) : tuanStatus.Atk;
            StartCoroutine(CombatSequence(dmg, 0, null, null));
        }
    }

    IEnumerator CombatSequence(int dmg, int apCost, GameObject vfx, PlayerAttackData skillData)
    {
        canInput = false;
        SwitchMenu(MenuState.Main);
        SetMenuGroup(mainMenuCG, false);
        if (apCost > 0) tuanStatus.UseAP(apCost);
        yield return MoveTo(tuanObj, linhHomePos - (linhHomePos - tuanHomePos).normalized * attackOffset, dashSpeed);
        tuanVisual.PlayAttack();
        yield return new WaitForSeconds(0.4f);
        if (dmg > 0)
        {
            linhStatus.TakeDamage(tuanStatus, dmg);
            if (vfx != null) Instantiate(vfx, linhObj.transform.position, Quaternion.identity);
            if (skillData != null && skillData.effects != null)
            {
                foreach (var entry in skillData.effects)
                {
                    if (entry.target == SkillEffectTarget.Enemy)
                    {
                        linhStatus.ApplyStatusEffect(entry.effect);
                        if (entry.effect.effectType == StatusEffectType.DebuffDef) SpawnDebuffVisual(entry.effect.duration);
                    }
                }
            }
        }
        UpdateUI();
        yield return new WaitForSeconds(0.6f);
        yield return MoveTo(tuanObj, tuanHomePos, returnSpeed);
        if (linhStatus.IsAlive) StartCoroutine(EnemyComboTurn());
        else { canInput = true; UpdateUI(); }
    }

    void SpawnDebuffVisual(int turns)
    {
        if (activeDebuffIcon != null) Destroy(activeDebuffIcon);
        Vector3 headPos = linhObj.transform.position + new Vector3(0, 1.5f, 0);
        activeDebuffIcon = Instantiate(debuffIconPrefab, headPos, Quaternion.identity);
        activeDebuffIcon.transform.SetParent(linhObj.transform);
        debuffTurnsLeft = turns;
    }

    public void ExecuteItemByIndex(int index)
    {
        canInput = false;
        SwitchMenu(MenuState.Main);
        SetMenuGroup(mainMenuCG, false);
        switch (index)
        {
            case 0: tuanStatus.currentHP = Mathf.Min(tuanStatus.MaxHP, tuanStatus.currentHP + 30); break;
            case 1: tuanStatus.RestoreAP(40); break;
            case 2: tuanStatus.ResetForBattle(0); break;
        }
        UpdateUI();
        StartCoroutine(EndItemTurn());
    }

    IEnumerator EndItemTurn() { yield return new WaitForSeconds(0.8f); if (linhStatus.IsAlive) StartCoroutine(EnemyComboTurn()); else { canInput = true; SwitchMenu(MenuState.Main); } }

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