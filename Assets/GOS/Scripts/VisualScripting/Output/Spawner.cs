using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class Spawner : ProcessBase
    {
        [Tooltip("생성할 특정 프리팹")]
        [SerializeField] private GameObject prefabToSpawn;

        [Tooltip("생성할 위치 (미지정 시 본 오브젝트 위치)")]
        [SerializeField] private Transform spawnPoint;

        [Tooltip("생성될 오브젝트의 부모 Transform")]
        [SerializeField] private Transform parentTransform;

        public override void Execute()
        {
            if (prefabToSpawn == null)
            {
                Debug.LogError($"[{gameObject.name}] SpawnerOutput: 생성할 프리팹이 지정되지 않았습니다.");
                return;
            }

            Vector3 position = spawnPoint != null ? spawnPoint.position : transform.position;
            Quaternion rotation = spawnPoint != null ? spawnPoint.rotation : transform.rotation;

            GameObject spawnedObj = Instantiate(prefabToSpawn, position, rotation, parentTransform);
            
            IsOn = true;
            Debug.Log($"[{gameObject.name}] SpawnerOutput: 프리팹 '{prefabToSpawn.name}'을(를) 위치 {position}에 생성했습니다.");
        }
    }
}
