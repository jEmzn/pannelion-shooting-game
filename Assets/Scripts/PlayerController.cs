using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TopDownShooter
{
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private Sprite shotSprite;
        [SerializeField] private Transform barrel;
        [SerializeField] private float moveSpeed = 5.5f;
        [SerializeField] private float shotInterval = 0.17f;

        private Rigidbody2D body;
        private Health health;
        private Vector2 movement;
        private Vector2 aim = Vector2.up;
        private float nextShotTime;

        public Health Health => health;

        public void Configure(Sprite projectileSprite, Transform weaponBarrel)
        {
            shotSprite = projectileSprite;
            barrel = weaponBarrel;
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            health = GetComponent<Health>();
            health.Died += OnDied;
        }

        private void OnDestroy() => health.Died -= OnDied;

        private void Update()
        {
            var flow = GameFlow.Instance;
            if (!health.IsAlive || flow == null || flow.IsLocked)
            {
                movement = Vector2.zero;
                return;
            }

            movement = GameUI.Instance != null ? GameUI.Instance.MoveVector : Vector2.zero;
            bool firing = false;
            if (GameUI.Instance != null && GameUI.Instance.AimActive)
            {
                aim = GameUI.Instance.AimVector;
                firing = true;
            }

            var keyboard = Application.isMobilePlatform ? null : Keyboard.current;
            if (keyboard != null)
            {
                var keys = new Vector2(
                    (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1 : 0) -
                    (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1 : 0),
                    (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1 : 0) -
                    (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1 : 0));
                if (keys.sqrMagnitude > 0) movement = keys.normalized;
            }

            var mouse = Application.isMobilePlatform ? null : Mouse.current;
            if (mouse != null)
            {
                var world = Camera.main.ScreenToWorldPoint(mouse.position.ReadValue());
                var mouseAim = (Vector2)(world - transform.position);
                if (mouseAim.sqrMagnitude > 0.01f) aim = mouseAim.normalized;
                firing |= mouse.leftButton.isPressed;
            }

            if (barrel != null) barrel.up = aim;
            if (firing && Time.time >= nextShotTime)
            {
                nextShotTime = Time.time + shotInterval;
                Projectile.Spawn(shotSprite, new Color(0.40f, 1f, 1f),
                    (Vector2)transform.position + aim * 0.65f, aim, 13f, 1, true);
            }
        }

        private void FixedUpdate() => body.linearVelocity = movement * moveSpeed;

        private void OnDied(Health _) => GameFlow.Instance?.ShowDeath();
    }
}
