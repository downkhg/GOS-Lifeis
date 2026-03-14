using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.Data
{
    public class DatabaseManager : MonoBehaviour
    {
        // 모든 테이블 데이터를 테이블 이름을 키로 저장
        private Dictionary<string, TableData> _tables = new Dictionary<string, TableData>();

        [Header("Debug Settings")]
        [SerializeField] private bool showDebugGUI = true;
        private bool _isMinimized = false;
        private Vector2 _scrollPos;
        private Rect _windowRect = new Rect(20, 20, 500, 600);

        /// <summary>
        /// 특정 경로의 CSV를 로드하여 테이블로 등록합니다.
        /// </summary>
        public void LoadTableFromPath(string tableName, string path)
        {
            TextAsset csvFile = Resources.Load<TextAsset>(path);
            if (csvFile == null)
            {
                Debug.LogError($"[DatabaseManager] 파일을 찾을 수 없습니다: {path}");
                return;
            }

            // 줄바꿈 기호 통합 처리
            string[] lines = csvFile.text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 3) return;

            string[] headers = lines[0].Trim().Split(',');
            string[] types = lines[1].Trim().Split(',');

            TableData newTable = new TableData();
            for (int i = 2; i < lines.Length; i++)
            {
                string[] values = lines[i].Trim().Split(',');
                if (values.Length < headers.Length) continue;

                RowEntity row = new RowEntity();
                for (int j = 0; j < headers.Length; j++)
                {
                    object parsedValue = CSVParser.ParseValue(types[j], values[j]);
                    row.SetField(headers[j], parsedValue);
                }
                // 첫 번째 컬럼(보통 ID)을 Key로 사용
                newTable.AddRow(values[0], row);
            }

            _tables[tableName] = newTable;
            Debug.Log($"<color=green>[DatabaseManager]</color> 테이블 로드 완료: {tableName}");
        }

        public TableData GetTable(string tableName) => _tables.GetValueOrDefault(tableName);

        public void ClearDatabase() => _tables.Clear();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void OnGUI()
        {
            if (!showDebugGUI) return;

            // 창 크기를 화면 높이에 어느 정도 맞춤
            _windowRect.height = Mathf.Min(Screen.height - 40, 800);
            _windowRect = GUILayout.Window(1, _windowRect, DrawWindow, "<b>[ Database Runtime Debugger ]</b>");
        }

        private void DrawWindow(int id)
        {
            // 상단 바: 최소화 및 닫기
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(_isMinimized ? "Maximize" : "Minimize", GUILayout.Width(100)))
                _isMinimized = !_isMinimized;

            GUILayout.FlexibleSpace();

            GUI.color = Color.red;
            if (GUILayout.Button("X", GUILayout.Width(30))) showDebugGUI = false;
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            if (_isMinimized)
            {
                GUI.DragWindow();
                return;
            }

            // 재로드 버튼
            if (GUILayout.Button("Reload From Game Manager", GUILayout.Height(30)))
            {
                // ManagerBase를 사용하는 GameManager 인스턴스에 접근
                if (GameManager.instance != null)
                    GameManager.instance.ReloadAllData();
                else
                    Debug.LogWarning("GameManager 인스턴스를 찾을 수 없습니다.");
            }

            GUILayout.Space(10);

            // 메인 스크롤 영역
            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUI.skin.box);
            {
                if (_tables.Count == 0)
                {
                    GUILayout.Label("<color=yellow>로드된 테이블이 없습니다.</color>");
                }
                else
                {
                    foreach (var table in _tables)
                    {
                        DrawTableGUI(table.Key, table.Value);
                        GUILayout.Space(15);
                    }
                }
            }
            GUILayout.EndScrollView();

            GUI.DragWindow();
        }

        private void DrawTableGUI(string name, TableData table)
        {
            // 테이블별 섹션
            GUILayout.BeginVertical(GUI.skin.window);
            {
                GUI.contentColor = Color.cyan;
                GUILayout.Label($"<b>Table: {name}</b>");
                GUI.contentColor = Color.white;

                foreach (var row in table.AllRows)
                {
                    // 행별 구분 박스
                    GUILayout.BeginVertical(GUI.skin.box);
                    {
                        foreach (var col in row.GetColumns())
                        {
                            object val = row.Get<object>(col);
                            string displayVal = (val != null) ? val.ToString() : "NULL";

                            GUILayout.BeginHorizontal();
                            // 컬럼명은 회색으로 고정폭 지정 (정렬 목적)
                            GUILayout.Label($"<color=silver>{col}:</color>", GUILayout.Width(100));
                            // 데이터 내용 출력
                            GUILayout.Label(displayVal, GUILayout.ExpandWidth(true));
                            GUILayout.EndHorizontal();
                        }
                    }
                    GUILayout.EndVertical();
                    GUILayout.Space(2);
                }
            }
            GUILayout.EndVertical();
        }
#endif
    }
}