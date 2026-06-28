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
        if (cam == null) return;

        int targetIndex = -1;
        if (bm.IsTargetingAlly)
            targetIndex = bm.Targeting.FindAliveAllyIndexNearScreenPoint(cam, screenPos);
        else
            targetIndex = bm.Targeting.FindAliveEnemyIndexNearScreenPoint(cam, screenPos);

        if (targetIndex < 0) return;

        if (bm.IsMobileTargetSelected)
        {
            // Tap 2: cùng mục tiêu → confirm
            bool sameTarget = bm.IsTargetingAlly 
                ? (targetIndex == bm.Targeting.AllyIndex)
                : (targetIndex == bm.Targeting.EnemyIndex);

            if (sameTarget)
            {
                bm.ConfirmAction();
                return;
            }
        }

        // Tap 1: chọn mục tiêu, chờ tap 2 để confirm
        bm.SetMobileTargetSelected(true);
        Status target = null;

        if (bm.IsTargetingAlly)
        {
            bm.Targeting.AllyIndex = targetIndex;
            target = bm.GetAllyTarget();
            if (target != null)
                BattleUI.Instance?.HighlightActivePlayerHUD(target.BattleSlotId);
        }
        else
        {
            bm.Targeting.EnemyIndex = targetIndex;
            target = bm.GetEnemyTargetPublic();
            if (target != null)
                BattleUI.Instance?.HighlightEnemyHUD(bm.Targeting.EnemyIndex);
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

        // New Input System: touchscreen
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
        var pointer = Pointer.current;
        bool isNowPressed = pointer?.press.isPressed ?? false;

        // Primary: EventSystem click capture
        if (!float.IsNaN(_pendingEditorTap.x))
        {
            screenPos = _pendingEditorTap;
            _pendingEditorTap = new Vector2(float.NaN, float.NaN);
            _prevMousePressed = isNowPressed; 
            return true;
        }

        // Fallback: Pointer.current edge detection
        bool tapped = !_prevMousePressed && isNowPressed;
        _prevMousePressed = isNowPressed;
        if (tapped)
        {
            screenPos = Input.mousePosition;
            return true;
        }
#endif

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
