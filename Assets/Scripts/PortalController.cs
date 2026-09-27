using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TopDownShooter
{
    public sealed class PortalController : MonoBehaviour
    {
        private SpriteRenderer visual;
        private PlayerController player;
        private bool active;
        private bool used;

        public bool IsActive => active;

        private void Awake()
        {
            visual = GetComponent<SpriteRenderer>();
            visual.enabled = false;
        }

        public void Activate()
        {
            active = true;
            used = false;
            visual.enabled = true;
        }

        private void Update()
        {
            if (!active || used || GameFlow.Instance == null) return;
            if (player == null) player = FindAnyObjectByType<PlayerController>();
            bool near = player != null && player.Health.IsAlive &&
                Vector2.Distance(player.transform.position, transform.position) <= 1.45f;
            GameUI.Instance?.SetInteractVisible(near && !GameFlow.Instance.IsLocked);
            if (near && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) TryUse();
        }

        public bool TryUse()
        {
            if (!active || used || player == null || GameFlow.Instance == null || GameFlow.Instance.IsLocked)
                return false;
            if (Vector2.Distance(player.transform.position, transform.position) > 1.45f) return false;
            used = true;
            GameUI.Instance?.SetInteractVisible(false);
            GameFlow.Instance.AdvanceStage();
            return true;
        }
    }
}
