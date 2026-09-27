using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TopDownShooter
{
    public sealed class StageController : MonoBehaviour
    {
        [SerializeField] private int stageNumber;
        [SerializeField] private Health[] objectives;
        [SerializeField] private PortalController portal;

        private int remaining;

        public int StageNumber => stageNumber;
        public int Total => objectives == null ? 0 : objectives.Length;
        public int Remaining => remaining;
        public PortalController Portal => portal;
        public Health BossHealth => stageNumber == 3 && objectives != null && objectives.Length > 0 ? objectives[0] : null;

        public void Configure(int number, Health[] targets, PortalController exit)
        {
            stageNumber = number;
            objectives = targets;
            portal = exit;
        }

        private void Start()
        {
            remaining = 0;
            foreach (var objective in objectives)
            {
                if (objective == null || !objective.IsAlive) continue;
                remaining++;
                objective.Died += OnObjectiveDied;
            }
            GameFlow.Instance?.BindStage(this);
            if (remaining == 0) Complete();
        }

        private void OnDestroy()
        {
            if (objectives == null) return;
            foreach (var objective in objectives)
                if (objective != null) objective.Died -= OnObjectiveDied;
        }

        private void OnObjectiveDied(Health objective)
        {
            objective.Died -= OnObjectiveDied;
            remaining = Mathf.Max(0, remaining - 1);
            if (remaining == 0) Complete();
        }

        private void Complete() => portal.Activate();
    }
}
