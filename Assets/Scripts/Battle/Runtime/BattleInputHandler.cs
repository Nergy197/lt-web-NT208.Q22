using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// Quản lý việc bắt input trên thiết bị cảm ứng (Mobile/Touch) hoặc mô phỏng Editor.
/// Tách khỏi BattleManager để giữ BattleManager thuần logic lượt đánh.
/// </summary>
public class BattleInputHandler : MonoBehaviour
{
    private BattleManager bm;

    private bool _prevMousePressed = false;
    private Vector2 _pendingEditorTap = new Vector2(float.NaN, float.NaN);

    public void Init(BattleManager manager)
    {
        bm = manager;
#if UNITY_EDITOR
        SetupEditorClickCapture();
#endif
    }

    public void HandleMobileTapTargetSelection()
    {
        if (!bm.ShouldUseMobileTouchFlowPublic()) return;
        if (!bm.IsWaitingForPlayerAction || !bm.IsSelectingTarget) return;

        // Bỏ qua tap trong grace period ngay sau khi chọn action
        if (Time.unscaledTime < bm.MobileSkipTapsUntil) return;

        if (!TryGetTapScreenPosition(out Vector2 screenPos)) return;

        // Bỏ qua nếu tap đang TRÊN UI (nút Confirm/Back/Skill...) — để nút tự xử lý onClick,
        // không cho cycle/đổi target (tránh bấm Confirm lại đánh nhầm enemy).
        var es = UnityEngine.EventSystems.EventSystem.current;
        if (es != null && es.IsPointerOverGameObject()) { Debug.Log("[MOBILE-TAP] tap trên UI → bỏ qua"); return; }

        Camera cam = Camera.main;
        if (cam == null) { Debug.Log("[MOBILE-TAP] Camera.main null"); return; }

        // MỖI TAP = CHỌN/ĐỔI mục tiêu, KHÔNG BAO GIỜ confirm. Confirm & Back dùng NÚT
        // mobile (đang hiện sẵn). Tap trúng 1 enemy → chọn enemy đó; tap trượt → xoay
        // sang mục tiêu kế tiếp. Nhờ vậy người chơi đổi được mục tiêu và không bị đánh nhầm.
        Status target;
        if (bm.IsTargetingAlly)
        {
            int idx = bm.Targeting.FindAliveAllyIndexNearScreenPoint(cam, screenPos);
            if (idx >= 0) bm.Targeting.AllyIndex = idx;
            target = (idx >= 0) ? bm.GetAllyTarget() : bm.Targeting.CycleAlly(1);
            if (target != null) BattleUI.Instance?.HighlightActivePlayerHUD(target.BattleSlotId);
        }
        else
        {
            int idx = bm.Targeting.FindAliveEnemyIndexNearScreenPoint(cam, screenPos);
            if (idx >= 0) bm.Targeting.EnemyIndex = idx;
            target = (idx >= 0) ? bm.GetEnemyTargetPublic() : bm.Targeting.CycleEnemy(1);
            if (target != null) BattleUI.Instance?.HighlightEnemyHUD(bm.Targeting.EnemyIndex);
        }

        bm.SetMobileTargetSelected(true); // để nút Confirm hiện
        Debug.Log($"[MOBILE-TAP] tap={screenPos} → target={(target != null ? target.entityName : "null")} (enemyIdx={bm.Targeting.EnemyIndex})");

        if (target == null) return;
        BattleUI.Instance?.SetTargetName(target.entityName);

        var dialog = bm.GetBattleDialogPublic();
        if (dialog != null)
        {
            if (bm.IsTargetingAlly)
                dialog.UpdateBuffDebuff(target);
            else
            {
                dialog.UpdateBuffDebuff(bm.CurrentUnitPublic);
                dialog.UpdateEnemyCombo((target as EnemyStatus)?.PlannedAttack);
            }
        }
    }

    public bool TryGetTapScreenPosition(out Vector2 screenPos)
    {
        screenPos = Vector2.zero;

        // 1) New Input System: Touchscreen (device thật / simulator)
        var touchscreen = Touchscreen.current;
        if (touchscreen != null)
        {
            foreach (var touch in touchscreen.touches)
            {
                if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    screenPos = touch.position.ReadValue();
                    return true;
                }
            }
        }

#if UNITY_EDITOR
        // 2a) Editor: EventSystem click capture (reliable trong Play Focused)
        if (!float.IsNaN(_pendingEditorTap.x))
        {
            screenPos = _pendingEditorTap;
            _pendingEditorTap = new Vector2(float.NaN, float.NaN);
            _prevMousePressed = Pointer.current?.press.isPressed ?? false;
            return true;
        }
#endif

        // 2b) Pointer edge-detection — CHẠY CẢ TRONG BUILD vì WebGL mobile thường đưa
        //     touch qua Pointer/mouse chứ không phải Touchscreen → nếu bỏ thì không tap được.
        var pointer = Pointer.current;
        bool isNowPressed = pointer?.press.isPressed ?? false;
        bool tapped = !_prevMousePressed && isNowPressed;
        _prevMousePressed = isNowPressed;
        if (tapped)
        {
            screenPos = pointer != null ? pointer.position.ReadValue() : (Vector2)Input.mousePosition;
            return true;
        }

        // 3) Fallback Input Manager cũ (activeInputHandler = Both)
        if (Input.GetMouseButtonDown(0))
        {
            screenPos = Input.mousePosition;
            return true;
        }

        return false;
    }

#if UNITY_EDITOR
    void SetupEditorClickCapture()
    {
        var canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        var go = new GameObject("_EditorMobileClickCapture",
            typeof(RectTransform), typeof(CanvasRenderer),
            typeof(UnityEngine.UI.Image));
        go.transform.SetParent(canvas.transform, false);
        go.transform.SetAsFirstSibling();

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var img = go.GetComponent<UnityEngine.UI.Image>();
        img.color = new Color(0, 0, 0, 0); 
        img.raycastTarget = true;

        var cap = go.AddComponent<EditorMobileClickCapture>();
        cap.Init(pos => _pendingEditorTap = pos);
    }
#endif
}
