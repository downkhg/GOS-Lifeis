using System;
using System.Collections.Generic;
using UnityEngine;
using _Project.Scripts.Data;

/// DB의 데이터 엔트리를 복제(Clone)하여 
/// 오브젝트가 독립적으로 소유할 수 있게 해주는 컴포넌트입니다.
/// </summary>
public class EntryCloner : MonoBehaviour
{
    private Dictionary<string, object> _clonedData = new Dictionary<string, object>();

    [Header("Auto Initialization")]
    [SerializeField] private string targetTable;
    [SerializeField] private string targetId;
    [SerializeField] private bool autoInitOnStart = false;

    [Header("Debug Display")]
    [SerializeField] private bool showWorldGUI = true;

    // 에디터에서 딕셔너리를 확인할 수 있게 열어줌
    public IReadOnlyDictionary<string, object> ClonedData => _clonedData;

    private void Start()
    {
        if (autoInitOnStart) InitFromDB(targetTable, targetId);
    }

    public void InitFromDB(string tableName, string id)
    {
        if (GameManager.instance == null || GameManager.instance.DatabaseManager == null) return;

        var db = GameManager.instance.DatabaseManager;
        var table = db.GetTable(tableName);
        if (table == null) return;

        var row = table.GetRow(id);
        if (row == null) return;

        _clonedData.Clear();
        foreach (var key in row.GetColumns())
        {
            _clonedData[key] = row.Get<object>(key);
        }
    }

    // --- 최대한 단순화한 월드 좌표 GUI 출력 ---
    private void OnGUI()
    {
        if (!showWorldGUI || _clonedData.Count == 0) return;

        // GameManager를 통해 검증된 카메라 가져오기
        Camera targetCam = GameManager.instance.MainRenderCamera;
        if (targetCam == null) return;

        // 1. 월드 좌표를 스크린 좌표로 변환 (캐릭터 머리 위 2m 지점)
        Vector3 worldPos = transform.position + Vector3.up * 2.0f;
        Vector3 screenPos = targetCam.WorldToScreenPoint(worldPos);

        // 2. 카메라 뒤에 있는 경우 출력 안 함
        if (screenPos.z < 0) return;

        // 3. GUI 박스 출력 (Y좌표는 유니티 GUI 특성상 반전 필요)
        float rectWidth = 150f;
        float rectHeight = _clonedData.Count * 20f + 5f;
        Rect rect = new Rect(screenPos.x - (rectWidth / 2), Screen.height - screenPos.y, rectWidth, rectHeight);

        GUI.Box(rect, ""); // 배경 박스

        // 4. 데이터 내용 한 줄씩 출력
        GUILayout.BeginArea(rect);
        foreach (var kvp in _clonedData)
        {
            // 너무 긴 데이터는 제외하고 주요 수치 위주로 출력
            GUILayout.Label($"<color=white>{kvp.Key}:</color> <color=yellow>{kvp.Value}</color>",
                new GUIStyle(GUI.skin.label) { richText = true, fontSize = 11 });
        }
        GUILayout.EndArea();
    }

    public T Get<T>(string key)
    {
        if (_clonedData.TryGetValue(key, out object val))
            return (T)Convert.ChangeType(val, typeof(T));
        return default;
    }

    public void Set(string key, object val) => _clonedData[key] = val;
}