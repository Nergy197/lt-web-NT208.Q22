using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class QuestTrackerUnderMinimapUI : MonoBehaviour
{
    static readonly HashSet<string> HIDDEN_IN_SCENES = new()
        { "Chapter0_Login", "Chapter5a_Battle" };

    bool IsExcludedScene() =>
        HIDDEN_IN_SCENES.Contains(SceneManager.GetActiveScene().name);

    [Header("UI Refs")]
    public GameObject panel;
    public TMP_Text questTitleText;
    public TMP_Text objectivesText;

    [Header("Behavior")]
    public bool showWhenNoQuest = false;

    private QuestSO trackedQuest;

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        QuestEvents.OnQuestStarted += OnQuestChanged;
        QuestEvents.OnObjectiveCompleted += OnObjectiveCompleted;
        QuestEvents.OnQuestCompleted += OnQuestChanged;

        // Kiểm tra ngay scene hiện tại (kể cả khi object đã DontDestroyOnLoad)
        if (IsExcludedScene())
            SetVisible(false);
        else
            RefreshUI();
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        QuestEvents.OnQuestStarted -= OnQuestChanged;
        QuestEvents.OnObjectiveCompleted -= OnObjectiveCompleted;
        QuestEvents.OnQuestCompleted -= OnQuestChanged;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (HIDDEN_IN_SCENES.Contains(scene.name))
            SetVisible(false);
        else
            RefreshUI();
    }

    private void OnQuestChanged(QuestSO _) => RefreshUI();
    private void OnObjectiveCompleted(string _, string __) => RefreshUI();

    public void RefreshUI()
    {
        QuestManager qm = QuestManager.Instance;
        if (qm == null)
        {
            if (!showWhenNoQuest)
            {
                SetVisible(false);
                return;
            }

            SetVisible(true);
            if (questTitleText != null) questTitleText.text = "NHIEM VU";
            if (objectivesText != null) objectivesText.text = "Dang khoi tao du lieu quest...";
            return;
        }

        trackedQuest = PickQuestToTrack(qm);
        if (trackedQuest == null)
        {
            if (!showWhenNoQuest)
            {
                SetVisible(false);
                return;
            }

            SetVisible(true);
            if (questTitleText != null) questTitleText.text = "NHIEM VU";
            if (objectivesText != null) objectivesText.text = "Khong co quest dang active.";
            return;
        }

        SetVisible(true);

        if (questTitleText != null)
            questTitleText.text = "NHIEM VU: " + trackedQuest.Title;

        if (objectivesText == null) return;

        StringBuilder sb = new StringBuilder();
        foreach (var obj in trackedQuest.Objectives)
        {
            if (obj == null) continue;
            sb.Append(obj.IsCompleted ? "[x] " : "[ ] ");
            sb.AppendLine(obj.Description);
        }

        objectivesText.text = sb.ToString().TrimEnd();
    }

    private QuestSO PickQuestToTrack(QuestManager qm)
    {
        for (int i = 0; i < qm.ActiveQuests.Count; i++)
        {
            var q = qm.ActiveQuests[i];
            if (q != null && q.IsMainQuest) return q;
        }

        for (int i = 0; i < qm.ActiveQuests.Count; i++)
        {
            var q = qm.ActiveQuests[i];
            if (q != null) return q;
        }

        return null;
    }

    private void SetVisible(bool visible)
    {
        if (panel != null) panel.SetActive(visible);
        else gameObject.SetActive(visible);
    }
}
