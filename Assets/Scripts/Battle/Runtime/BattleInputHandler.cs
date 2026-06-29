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

        Camera cam = Camera.main;
        if (cam == null) { Debug.Log("[MOBILE-TAP] Camera.main null"); return; }

        int targetIndex = -1;
        if (bm.IsTargetingAlly)
            targetIndex = bm.Targeting.FindAliveAllyIndexNearScreenPoint(cam, screenPos);
        else
            targetIndex = bm.Targeting.FindAliveEnemyIndexNearScreenPoint(cam, screenPos);

        int curIndex = bm.IsTargetingAlly ? bm.Targeting.AllyIndex : bm.Targeting.EnemyIndex;
        Debug.Log($"[MOBILE-TAP] tap={screenPos} → idx={targetIndex} | đãChọn={bm.IsMobileTargetSelected} curIdx={curIndex} ally={bm.IsTargetingAlly}");

        // Tap trúng một mục tiêu KHÁC mục tiêu hiện tại → đổi mục tiêu (chưa confirm).
        if (targetIndex >= 0 && (!bm.IsMobileTargetSelected || targetIndex != curIndex))
        {
            bm.SetMobileTargetSelected(true);
            Status t2 = null;
            if (bm.IsTargetingAlly)
            {
                bm.Targeting.AllyIndex = targetIndex;
                t2 = bm.GetAllyTarget();
                if (t2 != null) BattleUI.Instance?.HighlightActivePlayerHUD(t2.BattleSlotId);
            }
            else
            {
                bm.Targeting.EnemyIndex = targetIndex;
                t2 = bm.GetEnemyTargetPublic();
                if (t2 != null) BattleUI.Instance?.HighlightEnemyHUD(bm.Targeting.EnemyIndex);
            }
            if (t2 != null)
            {
                BattleUI.Instance?.SetTargetName(t2.entityName);
                var dlg = bm.GetBattleDialogPublic();
                if (dlg != null)
                {
                    if (bm.IsTargetingAlly) dlg.UpdateBuffDebuff(t2);
                    else { dlg.UpdateBuffDebuff(bm.CurrentUnitPublic); dlg.UpdateEnemyCombo((t2 as EnemyStatus)?.PlannedAttack); }
                }
            }
            return;
        }

        // Còn lại: đã có mục tiêu (auto-select hoặc vừa chọn) + tap trúng mục tiêu đó HOẶC
        // tap chỗ trống → CONFIRM. Nới lỏng để khỏi phải tap chính xác vào sprite enemy.
        if (bm.IsMobileTargetSelected)
        {
            Debug.Log("[MOBILE-TAP] → ConfirmAction");
            bm.ConfirmAction();
            return;
        }

        // Chưa có mục tiêu và tap trượt hết enemy → bỏ qua.
        if (targetIndex < 0) return;

        // (đường dự phòng) chọn mục tiêu vừa tap
        bm.SetMobileTargetSelected(true);
        Status target = null;
        if (bm.IsTargetingAlly)
        {
            bm.Targeting.AllyIndex = targetIndex;
            target = bm.GetAllyTarget();
            if (target != null) BattleUI.Instance?.HighlightActivePlayerHUD(target.BattleSlotId);
        }
        else
        {
            bm.Targeting.EnemyIndex = targetIndex;
            target = bm.GetEnemyTargetPublic();
            if (target != null) BattleUI.Instance?.HighlightEnemyHUD(bm.Targeting.EnemyIndex);
        }

        if (target == null) return;
        BattleUI.Instance?.SetTargetName(target.entityName);

        var dialog = bm.GetBattleDialogPublic();
        if (dialog != null)
        {
            if (bm.IsTargetingAlly)
            {
                dialog.UpdateBuffDebuff(target);
            }
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
