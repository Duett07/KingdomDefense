using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace KingdomDefense.Towers
{
    /// <summary>
    /// Tháp Xạ Thủ (Archer Tower) - Tháp phòng thủ tầm xa cơ bản trong Kingdom Defense.
    /// Có tầm bắn xa (7.0m), tốc độ bắn cao (1.4 tên/s), đặt một cung thủ thiện xạ trên nóc tháp xoay người ngắm bắn.
    /// </summary>
    public class ArcherTower : Tower
    {
        [Header("--- THIẾT LẬP THÁP CUNG THỦ ---")]
        [SerializeField] private GameObject archerFigurePrefab;
        [SerializeField] private GameObject crossbowPrefab;
        [SerializeField] private GameObject quiverPrefab;
        [SerializeField] private Material rogueMaterial;
        [SerializeField] private Material weaponMaterial;
        [SerializeField] private Vector3 archerOffset = new Vector3(0f, 2.2f, 0f);

        private Transform _archerFigure;

        protected override void Start()
        {
            // Thiết lập chỉ số chiến đấu chuyên biệt cho Tháp Cung
            range = 7.0f;
            fireRate = 1.4f;
            damage = 30f;
            turnSpeed = 12f;

            SetupArcherFigure();
            base.Start();
        }

        /// <summary>
        /// Khởi tạo mô hình chiến binh cung thủ trên đỉnh tháp và trang bị vũ khí.
        /// </summary>
        private void SetupArcherFigure()
        {
            if (_archerFigure != null) return;

#if UNITY_EDITOR
            string charPath = "Assets/_KingdomDefense/Models/Characters/Adventures/";
            string weaponPath = "Assets/_KingdomDefense/Models/Characters/Weapons/";
            string matPath = "Assets/_KingdomDefense/Materials/";

            if (archerFigurePrefab == null)
                archerFigurePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(charPath + "Rogue.fbx");
            if (crossbowPrefab == null)
                crossbowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(weaponPath + "crossbow_2handed.fbx");
            if (quiverPrefab == null)
                quiverPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(weaponPath + "quiver.fbx");
            if (rogueMaterial == null)
                rogueMaterial = AssetDatabase.LoadAssetAtPath<Material>(matPath + "M_Rogue.mat");
            if (weaponMaterial == null)
                weaponMaterial = AssetDatabase.LoadAssetAtPath<Material>(matPath + "M_KayKit_Medieval.mat");
#endif

            if (archerFigurePrefab != null)
            {
                GameObject archerObj = Instantiate(archerFigurePrefab, transform);
                archerObj.name = "Archer_Figure";
                archerObj.transform.localPosition = archerOffset;
                archerObj.transform.localScale = Vector3.one * 0.48f; // Tỉ lệ chuẩn vừa nóc tháp

                // Áp dụng vật liệu Rogue
                if (rogueMaterial != null)
                {
                    foreach (Renderer r in archerObj.GetComponentsInChildren<Renderer>())
                    {
                        r.sharedMaterial = rogueMaterial;
                    }
                }

                // Trang bị Nỏ chiến (Crossbow) phía trước / tay
                if (crossbowPrefab != null)
                {
                    GameObject crossbow = Instantiate(crossbowPrefab, archerObj.transform);
                    crossbow.name = "Equipped_Crossbow";
                    crossbow.transform.localPosition = new Vector3(0f, 0.42f, 0.32f);
                    crossbow.transform.localRotation = Quaternion.Euler(-10f, 0f, 0f);
                    crossbow.transform.localScale = Vector3.one * 0.85f;

                    if (weaponMaterial != null)
                    {
                        foreach (Renderer r in crossbow.GetComponentsInChildren<Renderer>())
                        {
                            r.sharedMaterial = weaponMaterial;
                        }
                    }
                }

                // Trang bị Ống đựng tên (Quiver) sau lưng
                if (quiverPrefab != null)
                {
                    GameObject quiver = Instantiate(quiverPrefab, archerObj.transform);
                    quiver.name = "Equipped_Quiver";
                    quiver.transform.localPosition = new Vector3(0.08f, 0.48f, -0.16f);
                    quiver.transform.localRotation = Quaternion.Euler(15f, 25f, -15f);
                    quiver.transform.localScale = Vector3.one * 0.75f;

                    if (weaponMaterial != null)
                    {
                        foreach (Renderer r in quiver.GetComponentsInChildren<Renderer>())
                        {
                            r.sharedMaterial = weaponMaterial;
                        }
                    }
                }

                // Cung thủ chính là bộ phận xoay ngắm mục tiêu (partToRotate)
                partToRotate = archerObj.transform;
                _archerFigure = archerObj.transform;

                // FirePoint gắn làm con của cung thủ để tự xoay hướng theo mục tiêu
                GameObject fp = new GameObject("FirePoint");
                fp.transform.SetParent(archerObj.transform);
                fp.transform.localPosition = new Vector3(0f, 0.42f, 0.45f);
                firePoint = fp.transform;
            }
        }
    }
}
