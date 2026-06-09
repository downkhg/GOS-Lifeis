using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace _Project.Scripts.VisualScripting.Editor
{
    public class VisualScriptingPalette : EditorWindow
    {
        private Dictionary<string, List<Type>> _categorizedNodes = new Dictionary<string, List<Type>>();
        private Vector2 _scrollPosition;

        // 검색 기능을 위한 변수 추가
        private string _searchQuery = "";

        [MenuItem("Visual Scripting/GameObject/Pallete Window")]
        public static void ShowWindow()
        {
            var window = GetWindow<VisualScriptingPalette>("Visual Scripting Pallete Window");
            window.Show();
        }

        private void OnEnable()
        {
            RefreshNodeList();
        }

        private void RefreshNodeList()
        {
            _categorizedNodes.Clear();

            var nodeTypes = TypeCache.GetTypesDerivedFrom<ProcessBase>()
                .Where(t => !t.IsAbstract && !t.IsInterface);

            foreach (Type type in nodeTypes)
            {
                string[] guids = AssetDatabase.FindAssets($"{type.Name} t:MonoScript");
                string categoryName = "ETC (미분류)";

                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);

                    if (path.EndsWith($"/{type.Name}.cs"))
                    {
                        // 폴더 구조를 기반으로 카테고리 파싱 (예: VisualScripting/Output_Camera/.. 이면 Output_Camera)
                        string[] parts = path.Split('/');
                        if (parts.Length > 2)
                        {
                            categoryName = parts[parts.Length - 2];
                        }
                        break;
                    }
                }

                if (!_categorizedNodes.ContainsKey(categoryName))
                {
                    _categorizedNodes[categoryName] = new List<Type>();
                }
                _categorizedNodes[categoryName].Add(type);
            }
        }

        private void OnGUI()
        {
            GUILayout.Label("Visual Scripting Node Palette", EditorStyles.boldLabel);
            GUILayout.Space(5);

            // --- 🔍 검색창 UI 추가 ---
            EditorGUILayout.BeginHorizontal(GUI.skin.box);
            GUILayout.Label("🔍 검색:", GUILayout.Width(50));

            // 텍스트가 바뀔 때마다 OnGUI가 실시간으로 갱신되며 리스트를 필터링합니다.
            string prevSearch = _searchQuery;
            _searchQuery = EditorGUILayout.TextField(_searchQuery);

            // 검색어를 지우는 'X' 버튼
            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                _searchQuery = "";
                GUI.FocusControl(null); // 검색창 포커스 해제
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(10);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            // 카테고리 순서 정렬
            var orderedCategories = _categorizedNodes.OrderBy(k => GetCategoryOrder(k.Key));

            foreach (var category in orderedCategories)
            {
                // 현재 검색어 조건에 맞는 노드들만 필터링
                var filteredNodes = category.Value
                    .Where(t => string.IsNullOrEmpty(_searchQuery) || t.Name.IndexOf(_searchQuery, StringComparison.OrdinalIgnoreCase) >= 0)
                    .OrderBy(t => t.Name)
                    .ToList();

                // 이 카테고리 안에 검색 조건에 맞는 노드가 하나도 없다면 카테고리 타이틀 자체를 그리지 않음
                if (filteredNodes.Count == 0) continue;

                EditorGUILayout.LabelField($"📂 {category.Key}", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;

                foreach (Type nodeType in filteredNodes)
                {
                    if (GUILayout.Button($"➕ {nodeType.Name}", GUILayout.Height(25)))
                    {
                        CreateNode(nodeType, category.Key);
                    }
                }

                EditorGUI.indentLevel--;
                GUILayout.Space(10);
            }

            EditorGUILayout.EndScrollView();
        }

        // 기존 카테고리 정렬 순서 보장용 헬퍼 함수 (구조에 맞게 순서 수치 커스텀 가능)
        private int GetCategoryOrder(string categoryName)
        {
            if (categoryName.StartsWith("Input")) return 0;
            if (categoryName.StartsWith("Logic")) return 1;
            if (categoryName.StartsWith("Output")) return 2;
            return 99;
        }

        private void CreateNode(Type nodeType, string category)
        {
            GameObject go = new GameObject(nodeType.Name);
            go.AddComponent(nodeType);

            if (nodeType.Name.Contains("Trigger"))
            {
                BoxCollider col = go.AddComponent<BoxCollider>();
                col.isTrigger = true;
            }

            GameObject activeObj = Selection.activeGameObject;
            if (activeObj != null)
            {
                GameObjectUtility.SetParentAndAlign(go, activeObj);
            }

            Undo.RegisterCreatedObjectUndo(go, $"Create {nodeType.Name}");
            Selection.activeObject = go;
        }
    }
}