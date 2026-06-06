using UnityEngine;
using System.Collections.Generic;

namespace _Project.Scripts.VisualScripting
{
    public class InputEvent : ProcessBase
    {
        [Header("Input Settings")]
        [SerializeField] private KeyCode triggerKey = KeyCode.Mouse0; // 마우스 왼쪽 클릭 디폴트

        [Header("Outputs")]
        [SerializeField] private List<ProcessData> outputs;

        private void Update()
        {
            if (Input.GetKeyDown(triggerKey))
            {
                IsOn = true;
                Execute();
            }
            else if (Input.GetKeyUp(triggerKey))
            {
                IsOn = false;
            }
        }

        public override void Execute()
        {
            if (outputs == null) return;
            foreach (var output in outputs)
            {
                if (output.process != null)
                {
                    output.process.Execute();
                }
            }
        }
    }
}
