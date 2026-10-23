#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using KingdomDefense.Map;

namespace KingdomDefense.Map.Editor
{
    [CustomEditor(typeof(MapGenerator))]
    public class MapGeneratorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();

            MapGenerator generator = (MapGenerator)target;

            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("--- CÔNG CỤ TẠO BẢN ĐỒ ---", EditorStyles.boldLabel);

            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.3f);
            if (GUILayout.Button("▶ Sinh Bản Đồ (Tường Thành, Đường Mòn & UI Xây Dựng)", GUILayout.Height(38)))
            {
                generator.GenerateMap();
                EditorUtility.SetDirty(generator);
            }

            GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
            if (GUILayout.Button("✕ Xóa Toàn Bộ Map", GUILayout.Height(28)))
            {
                if (EditorUtility.DisplayDialog("Xác nhận xóa map", "Bạn có chắc chắn muốn xóa toàn bộ các tile đã sinh?", "Đồng ý", "Hủy"))
                {
                    generator.ClearMap();
                    EditorUtility.SetDirty(generator);
                }
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox("Bản đồ bao gồm:\n- Doanh trại quái vật Cổng Bắc tại (-1, 10.4)\n- Đường mòn uốn lượn chữ S dẫn thẳng qua Cổng Tường Thành\n- Tường Thành Kiên Cố bảo vệ Vương quốc (Cổng vòm + 6 tường đá + 2 tháp canh + Lâu đài)\n- 12 ô cắm tháp (Node) xếp dọc 2 bên đường\n- Hệ thống Waypoints chuẩn dẫn đường cho quái vật\n- UI HUD Canvas (Top HUD Máu, Vàng, Wave, Mana + Bottom Dock 4 công trình Cung, Pháo, Phép, Tường Thành).", MessageType.Info);
        }

        [MenuItem("Tools/Kingdom Defense/▶ Generate Map & UI", false, 1)]
        public static void GenerateMapFromMenu()
        {
            MapGenerator generator = Object.FindFirstObjectByType<MapGenerator>();
            if (generator == null)
            {
                GameObject go = new GameObject("MapGenerator");
                generator = go.AddComponent<MapGenerator>();
            }
            generator.GenerateMap();
            EditorUtility.SetDirty(generator.gameObject);
            Debug.Log("[KingdomDefense] Đã sinh thành công Map 1 từ thanh công cụ Tools!");
        }

        [MenuItem("Tools/Kingdom Defense/✕ Clear Map", false, 2)]
        public static void ClearMapFromMenu()
        {
            MapGenerator generator = Object.FindFirstObjectByType<MapGenerator>();
            if (generator != null)
            {
                generator.ClearMap();
                EditorUtility.SetDirty(generator.gameObject);
                Debug.Log("[KingdomDefense] Đã xóa Map thành công!");
            }
        }
    }
}
#endif
