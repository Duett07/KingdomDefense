namespace KingdomDefense.Core
{
    /// <summary>
    /// Các dạng sát thương trong Kingdom Defense:
    /// - Physical: Sát thương vật lý (từ Tháp Cung, Đạn thường), bị giảm trừ bởi Giáp (Armor%).
    /// - Magic: Sát thương phép thuật (từ Tháp Pháp Sư, Tháp Băng), bỏ qua 100% Giáp vật lý, bị giảm bởi Kháng phép (MagicResistance%).
    /// - AoEExplosion: Sát thương nổ lan diện rộng (từ Tháp Đại Bác), bị giảm bởi Giáp và thuộc tính Kháng nổ (Stone Golem giảm thêm 50%).
    /// - Pure: Sát thương chuẩn (Kỹ năng Thiên Thạch / Tuyệt chiêu), bỏ qua toàn bộ Giáp và Kháng phép.
    /// </summary>
    public enum DamageType
    {
        Physical,
        Magic,
        AoEExplosion,
        Pure
    }

    /// <summary>
    /// Giao diện chuẩn cho mọi thực thể có thể nhận sát thương trong Kingdom Defense.
    /// Cho phép các loại Tháp, Đạn bay, Hiệu ứng nổ và Phép thuật tương tác đồng nhất.
    /// </summary>
    public interface IDamageable
    {
        float CurrentHealth { get; }
        float MaxHealth { get; }
        bool IsDead { get; }

        /// <summary>
        /// Gây sát thương lên thực thể kèm phân loại loại sát thương.
        /// </summary>
        /// <param name="amount">Lượng sát thương gốc (Raw Damage)</param>
        /// <param name="damageType">Loại sát thương (Mặc định: Physical)</param>
        void TakeDamage(float amount, DamageType damageType = DamageType.Physical);
    }
}
