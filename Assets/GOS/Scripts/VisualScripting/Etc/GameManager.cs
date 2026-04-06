using UnityEngine;
using _Project.Scripts.Data;
using System.Diagnostics; // 실행 시간 측정을 위해 추가

public class GameManager : ManagerBase<GameManager>
{
    [Header("Camera Settings")]
    [SerializeField] private Camera mainRenderCamera; // 인스펙터에서 직접 등록 가능

    // 외부에서 안전하게 가져갈 수 있는 프로퍼티
    public Camera MainRenderCamera
    {
        get
        {
            // 등록이 안 되어 있다면 런타임에라도 찾아보고 캐싱
            if (mainRenderCamera == null) mainRenderCamera = Camera.main;
            return mainRenderCamera;
        }
    }

    [Header("Database Load Settings")]
    [SerializeField] private string masterTableName = "DataTable";
    [SerializeField] private string masterTablePath = "data/TestData - Schema";

    private DatabaseManager _databaseManager;
    public DatabaseManager DatabaseManager => _databaseManager;

    protected override void Awake()
    {
        base.Awake();
        InitModules();
        ReloadAllData();
    }

    private void Start()
    {
        
    }

    public void ReloadAllData()
    {
        if (_databaseManager == null)
        {
            UnityEngine.Debug.LogError("<color=red>[GameManager]</color> DatabaseManager를 찾을 수 없습니다.");
            return;
        }

        Stopwatch sw = new Stopwatch();
        sw.Start();

        UnityEngine.Debug.Log($"<color=white><b>[GameManager]</b> 데이터베이스 초기화 시작...</color>");
        _databaseManager.ClearDatabase();

        // 1. 마스터 테이블 로드 시도
        _databaseManager.LoadTableFromPath(masterTableName, masterTablePath);
        var master = _databaseManager.GetTable(masterTableName);

        if (master == null)
        {
            UnityEngine.Debug.LogError($"<color=red><b>[GameManager]</b> 마스터 테이블('{masterTableName}') 로드 실패!</color>\n경로를 확인하세요: {masterTablePath}");
            return;
        }

        UnityEngine.Debug.Log($"<color=green><b>[GameManager]</b> 마스터 테이블 로드 완료.</color> 등록된 테이블들을 로드합니다...");

        // 2. 연쇄 로딩 및 결과 추적
        int successCount = 0;
        int totalTables = 0;

        foreach (var row in master.AllRows)
        {
            totalTables++;
            string name = row.Get<string>("TableName");
            string path = row.Get<string>("Path");

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(path))
            {
                UnityEngine.Debug.LogWarning($"<color=yellow>[GameManager]</color> {totalTables}번째 행의 데이터가 비어있습니다.");
                continue;
            }

            _databaseManager.LoadTableFromPath(name, path);

            if (_databaseManager.GetTable(name) != null)
            {
                successCount++;
                UnityEngine.Debug.Log($"   └ <color=white>테이블 로드 성공:</color> <b>{name}</b> ({path})");
            }
            else
            {
                UnityEngine.Debug.LogError($"   └ <color=red>테이블 로드 실패:</color> <b>{name}</b> (경로: {path})");
            }
        }

        sw.Stop();
        UnityEngine.Debug.Log($"<color=cyan><b>[GameManager]</b> 로드 프로세스 종료.</color> (성공: {successCount}/{totalTables}, 소요시간: {sw.ElapsedMilliseconds}ms)");
    }

    private void InitModules()
    {
        _databaseManager = GetComponentInChildren<DatabaseManager>() ?? gameObject.AddComponent<DatabaseManager>();
        if (_databaseManager != null) UnityEngine.Debug.Log("<color=white>[GameManager]</color> DatabaseManager 모듈 연결됨.");
    }
}