using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class RaycastTrigger : ProcessBase
    {
        [Header("Target settings")]
        [SerializeField] private GameObject objTarget;

        [Header("Raycast Settings")]
        [SerializeField] private Transform muzzleTransform;
        [SerializeField] private float maxDistance = 100f;
        [SerializeField] private LayerMask layerMask = ~0;
        [SerializeField] private Color debugColor = Color.red;

        public GameObject GetTarget() { return objTarget; }

        public void Fire()
        {
            // If muzzle transform is null, use the player transform itself
            Transform originTransform = muzzleTransform != null ? muzzleTransform : transform;

            // Offset the origin forward to prevent hitting own collider
            Vector3 origin = originTransform.position + originTransform.forward * 0.8f;
            Vector3 direction = originTransform.forward;

            if (Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance, layerMask))
            {
                objTarget = hit.collider.gameObject;
                IsOn = true;
                Debug.DrawRay(origin, direction * maxDistance, Color.green, 0.5f);
                Debug.Log($"[{gameObject.name}] Raycast Hit: {objTarget.name} at {hit.point}");
            }
            else
            {
                objTarget = null;
                IsOn = false;
                Debug.DrawRay(origin, direction * maxDistance, debugColor, 0.5f);
            }
        }

        public override void Execute()
        {
            Fire();
        }

        private void OnDrawGizmosSelected()
        {
            Transform originTransform = muzzleTransform != null ? muzzleTransform : transform;
            Gizmos.color = debugColor;
            Gizmos.DrawRay(originTransform.position, originTransform.forward * 5f);
        }
    }
}
