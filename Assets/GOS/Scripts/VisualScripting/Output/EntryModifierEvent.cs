using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class EntryModifierEvent : ProcessBase
    {
        [Header("Attacker Settings")]
        [SerializeField] private EntryCloner attackerCloner;

        [Header("Target Settings")]
        [SerializeField] private ProcessBase targetSource;

        [Header("Stat Settings")]
        [SerializeField] private string statKey = "HP";
        [SerializeField] private string valueSourceKey = "Atk";
        [SerializeField] private int constantValue = 0;
        [SerializeField] private bool isSubtraction = true;

        [Header("Death Settings")]
        [SerializeField] private bool destroyOnZeroHP = true;

        public override void Execute()
        {
            if (targetSource == null) return;

            GameObject target = null;
            if (targetSource is RaycastTrigger raycastTrigger) target = raycastTrigger.GetTarget();
            else if (targetSource is Trigger trigger) target = trigger.GetTarget();

            if (target == null) return;

            EntryCloner targetCloner = target.GetComponent<EntryCloner>();
            if (targetCloner == null)
            {
                Debug.LogWarning($"[{gameObject.name}] Target {target.name} has no EntryCloner!");
                return;
            }

            // Calculate modifier value
            int modifierValue = constantValue;
            if (attackerCloner == null) attackerCloner = GetComponent<EntryCloner>();
            if (attackerCloner != null && !string.IsNullOrEmpty(valueSourceKey))
            {
                modifierValue = attackerCloner.GetStat<int>(valueSourceKey);
            }

            // Modify stat
            int currentValue = targetCloner.GetStat<int>(statKey);
            int nextValue = isSubtraction ? (currentValue - modifierValue) : (currentValue + modifierValue);

            // HP shouldn't go below 0
            if (statKey == "HP")
            {
                nextValue = Mathf.Max(0, nextValue);
            }

            targetCloner.SetStat(statKey, nextValue);
            Debug.Log($"[{gameObject.name}] Modified target {target.name}'s {statKey}: {currentValue} -> {nextValue} (Modifier: {(isSubtraction ? "-" : "+")}{modifierValue})");

            // Destroy if HP is 0
            if (statKey == "HP" && nextValue <= 0 && destroyOnZeroHP)
            {
                Debug.Log($"[{gameObject.name}] Target {target.name} HP reached 0. Destroying.");
                Destroy(target);
            }

            IsOn = true;
        }
    }
}
