namespace KingdomDefense.Enemy
{
    /// <summary>
    /// 5 Chủng quái vật chính thức của Kingdom Defense theo Bảng 2.1:
    /// 1. GoblinRunner: Rất nhanh (4.5 m/s), máu giấy (80 HP), 0% giáp.
    /// 2. OrcWarrior: Máu vừa (240 HP), tốc độ trung bình (2.6 m/s), 25% giáp vật lý.
    /// 3. StoneGolem: Máu cực lớn (950 HP), tốc độ chậm (1.2 m/s), 60% giáp vật lý, kháng nổ AoE 50%.
    /// 4. DarkBat: Quái bay ở độ cao 2.8m, tốc độ 3.8 m/s, 120 HP, bỏ qua tháp pháo mặt đất.
    /// 5. BossOrcus: Trùm cuối Wave 10, 3800 HP, 40% giáp, 30% kháng phép, kỹ năng dậm đất (Stun Quake) 6.0m làm choáng tháp trong 3s.
    /// </summary>
    public enum EnemyType
    {
        GoblinRunner,
        OrcWarrior,
        StoneGolem,
        DarkBat,
        BossOrcus
    }
}
