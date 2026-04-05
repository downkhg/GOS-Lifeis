using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class MoveTo : ProcessBase
    {
        [Header("Target Settings")]
        [Tooltip("이동시킬 오브젝트 (비워두면 이 스크립트가 달린 객체 이동)")]
        [SerializeField] private GameObject objToMove;

        [Tooltip("목표 위치 (빈 오브젝트 등을 배치해서 할당)")]
        [SerializeField] private Transform destination;

        [Header("Movement Settings")]
        [Tooltip("이동에 걸리는 시간 (초)")]
        [SerializeField] private float duration = 1.0f;

        [Tooltip("이동 움직임 그래프 (Ease In/Out 등 표현)")]
        [SerializeField] private AnimationCurve movementCurve = AnimationCurve.Linear(0, 0, 1, 1);

        private Coroutine _moveCoroutine;

        private void Awake()
        {
            // 타겟이 없으면 자기 자신을 대상으로 설정
            if (!objToMove) objToMove = this.gameObject;
        }

        public override void Execute()
        {
            // 실행 신호가 들어오면 이동 시작
            IsOn = true;

            if (objToMove != null && destination != null)
            {
                StartMove();
            }
            else
            {
                Debug.LogWarning($"{name}: 이동할 오브젝트나 목적지가 설정되지 않았습니다.");
            }
        }

        private void StartMove()
        {
            // 이미 이동 중이라면 멈추고 새로 시작 (중복 실행 방지)
            if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);

            _moveCoroutine = StartCoroutine(MoveRoutine());
        }

        private IEnumerator MoveRoutine()
        {
            Vector3 startPosition = objToMove.transform.position;
            Vector3 targetPosition = destination.position;
            float elapsedTime = 0f;

            Debug.Log($"{name}: {objToMove.name} 이동 시작 -> {targetPosition}");

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / duration;

                // 커브를 적용하여 부드러운 움직임 구현 (0~1 사이 값)
                float curveValue = movementCurve.Evaluate(t);

                // 위치 보간 (Lerp)
                objToMove.transform.position = Vector3.Lerp(startPosition, targetPosition, curveValue);

                yield return null;
            }

            // 정확한 목표 위치로 강제 설정 (오차 방지)
            objToMove.transform.position = targetPosition;
            _moveCoroutine = null;

            Debug.Log($"{name}: 이동 완료");
        }

        // 에디터에서 이동 경로를 미리 보여주는 기능
        private void OnDrawGizmos()
        {
            if (destination != null)
            {
                // 출발지 결정 (objToMove가 할당 안되었으면 자기 자신 기준)
                Vector3 startPos = (objToMove != null) ? objToMove.transform.position : transform.position;
                Vector3 endPos = destination.position;

                Gizmos.color = Color.green;
                // 경로 선 그리기
                Gizmos.DrawLine(startPos, endPos);
                // 도착 지점 구체 그리기
                Gizmos.DrawWireSphere(endPos, 0.5f);
            }
        }
    }
}