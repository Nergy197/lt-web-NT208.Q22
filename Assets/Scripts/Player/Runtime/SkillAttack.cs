/// <summary>
/// SkillAttack: Subclass của PlayerAttack dành cho skill thuần buff/debuff
/// (không có damage hits). Dùng khi muốn tạo skill như "Shield Stance",
/// "War Cry" v.v. chỉ apply effect mà không gây damage.
///
/// Cách dùng: Tạo PlayerAttackData trong Inspector,
///   - Để `hits` trống
///   - Điền vào `effects` các StatusEffect cần áp dụng
/// → PlayerAttackData.CreateInstance() sẽ tạo PlayerAttack xử lý đúng cách.
///
/// File này giữ lại như placeholder/documentation.
/// Logic thực tế nằm trong PlayerAttack.Execute().
/// </summary>
public static class SkillAttackInfo
{
    // Không cần code thêm — PlayerAttack đã xử lý cả damage lẫn effect.
    // Để tạo skill buff/debuff: tạo PlayerAttackData với danh sách hits rỗng
    // và điền effects[] trong Unity Inspector.
}
