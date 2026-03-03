using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using _Project.Scripts.VisualScripting;

namespace _Project.Scripts.VisualScripting.Editor
{
    public class VisualScriptingPalette : EditorWindow
    {
        private Dictionary<string, List<Type>> _categorizedNodes = new Dictionary<string, List<Type>>();
        private Vector2 _scrollPosition;

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

                // 기본값
                string categoryName = "ETC (미분류)";

                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);

                    if (path.EndsWith($"/{type.Name}.cs"))
                    {
                        // [핵심 변경점] 하위 폴더 깊이에 상관없이, 경로에 포함된 대분류 키워드만 찾습니다.
                        if (path.Contains("/Input/")) categoryName = "Input";
                        else if (path.Contains("/Logic/")) categoryName = "Logic";
                        else if (path.Contains("/Output/")) categoryName = "Output";
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

        // 카테고리 정렬 순서를 지정하는 헬퍼 함수
        private int GetCategoryOrder(string category)
        {
            if (category == "Input") return 1;
            if (category == "Logic") return 2;
            if (category == "Output") return 3;
            return 4; // ETC 등 나머지는 맨 아래로
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            if (GUILayout.Button("🔄 리스트 새로고침 (Refresh)", GUILayout.Height(30)))
            {
                RefreshNodeList();
            }
            GUILayout.Space(10);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            // 알파벳 순서가 아닌, GetCategoryOrder에 정의된 논리적 흐름(Input -> Process -> Output) 순으로 정렬
            var orderedCategories = _categorizedNodes.OrderBy(k => GetCategoryOrder(k.Key));

            foreach (var category in orderedCategories)
            {
                EditorGUILayout.LabelField($"📂 {category.Key}", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;

                foreach (Type nodeType in category.Value.OrderBy(t => t.Name))
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

        private void CreateNode(Type nodeType, string category)
        {
            GameObject go = new GameObject(nodeType.Name);
            go.AddComponent(nodeType);

            // Input 그룹에 속해있거나 이름에 Trigger가 있다면 자동으로 설정
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