using UnityEngine;

/// <summary>
/// Holder cho sound effects và hiệu ứng battle.
/// Kéo component này vào một GameObject trong BattleScene,
/// sau đó gán AudioClip / AudioSource tương ứng khi có asset.
/// </summary>
public class BattleEffectsController : MonoBehaviour
{
    [Header("Parry Success SFX")]
    [SerializeField] private AudioSource parrySFXSource;
    [SerializeField] private AudioClip   parrySuccessClip;

    [Header("Attack Announce SFX")]
    [SerializeField] private AudioSource attackAnnounceSFXSource;
    [SerializeField] private AudioClip   attackAnnounceClip;

    private void OnEnable()
    {
        BattleEvents.OnParrySuccess         += HandleParrySuccess;
        BattleEvents.OnEnemyAttackAnnounced += HandleAttackAnnounced;
        BattleEvents.OnEnemyHitIncoming     += HandleHitIncoming;
    }

    private void OnDisable()
    {
        BattleEvents.OnParrySuccess         -= HandleParrySuccess;
        BattleEvents.OnEnemyAttackAnnounced -= HandleAttackAnnounced;
        BattleEvents.OnEnemyHitIncoming     -= HandleHitIncoming;
    }

    private void HandleParrySuccess(PlayerStatus player)
    {
        // TODO: thêm hiệu ứng parry thành công (particle, screen flash, v.v.)
        if (parrySFXSource != null && parrySuccessClip != null)
            parrySFXSource.PlayOneShot(parrySuccessClip);
    }

    private void HandleAttackAnnounced(EnemyAttackData attack, EnemyStatus enemy, PlayerStatus target)
    {
        // TODO: thêm hiệu ứng thông báo đòn đánh (animation panel, v.v.)
        if (attackAnnounceSFXSource != null && attackAnnounceClip != null)
            attackAnnounceSFXSource.PlayOneShot(attackAnnounceClip);
    }

    private void HandleHitIncoming(EnemyAttackHit hit, int hitIndex, EnemyStatus enemy)
    {
        // TODO: thêm hiệu ứng cảnh báo từng hit (screen shake, flash, v.v.)
    }
}
