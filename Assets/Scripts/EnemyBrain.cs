using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TopDownShooter
{
    public enum EnemyKind { Chaser, Ranged, Boss }

    public sealed class EnemyBrain : MonoBehaviour
    {
        [SerializeField] private EnemyKind kind;
        [SerializeField] private Sprite shotSprite;
        [SerializeField] private float moveSpeed = 2f;

        private Rigidbody2D body;
        private Health health;
        private PlayerController player;
        private float nextShotTime;
        private float nextBurstTime;
        private float nextContactTime;

        public EnemyKind Kind => kind;

        public void Configure(EnemyKind enemyKind, Sprite projectileSprite, float speed)
        {
            kind = enemyKind;
            shotSprite = projectileSprite;
            moveSpeed = speed;
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            health = GetComponent<Health>();
            health.Died += OnDied;
        }

        private void Start()
        {
            player = FindAnyObjectByType<PlayerController>();
            nextShotTime = Time.time + (kind == EnemyKind.Boss ? 1f : 1.5f);
            nextBurstTime = Time.time + 3.5f;
        }

        private void OnDestroy() => health.Died -= OnDied;

        private void FixedUpdate()
        {
            if (!health.IsAlive || player == null || !player.Health.IsAlive || GameFlow.Instance.IsLocked)
            {
                body.linearVelocity = Vector2.zero;
                return;
            }

            var toPlayer = (Vector2)(player.transform.position - transform.position);
            float distance = toPlayer.magnitude;
            var direction = distance > 0.01f ? toPlayer / distance : Vector2.zero;
            Vector2 motion = Vector2.zero;
            if (kind == EnemyKind.Chaser && distance > 0.9f) motion = direction;
            if (kind == EnemyKind.Ranged || kind == EnemyKind.Boss)
            {
                if (distance > 4.4f) motion = direction;
                else if (distance < 3.2f) motion = -direction;
            }
            body.linearVelocity = motion * moveSpeed;
        }

        private void Update()
        {
            if (!health.IsAlive || player == null || !player.Health.IsAlive || GameFlow.Instance.IsLocked) return;
            if (kind == EnemyKind.Chaser) return;
            var toPlayer = (Vector2)(player.transform.position - transform.position);
            if (toPlayer.sqrMagnitude < 0.01f) return;
            var direction = toPlayer.normalized;

            if (Time.time >= nextShotTime)
            {
                nextShotTime = Time.time + (kind == EnemyKind.Boss ? 1.45f : 1.8f);
                if (kind == EnemyKind.Boss)
                {
                    for (int i = -1; i <= 1; i++) Fire(Rotate(direction, i * 14f), 7.2f, 0.28f);
                }
                else Fire(direction, 7.5f, 0.20f);
            }

            if (kind == EnemyKind.Boss && Time.time >= nextBurstTime)
            {
                nextBurstTime = Time.time + 4.2f;
                for (int i = 0; i < 12; i++)
                {
                    float angle = i * 30f * Mathf.Deg2Rad;
                    Fire(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)), 5f, 0.25f);
                }
            }
        }

        private void Fire(Vector2 direction, float speed, float size)
        {
            Projectile.Spawn(shotSprite, new Color(1f, 0.58f, 0.24f),
                (Vector2)transform.position + direction * (kind == EnemyKind.Boss ? 1.05f : 0.55f),
                direction, speed, 1, false, size);
        }

        private static Vector2 Rotate(Vector2 vector, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            return new Vector2(vector.x * Mathf.Cos(radians) - vector.y * Mathf.Sin(radians),
                vector.x * Mathf.Sin(radians) + vector.y * Mathf.Cos(radians));
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (!health.IsAlive || Time.time < nextContactTime) return;
            var target = collision.gameObject.GetComponent<PlayerController>();
            if (target == null) return;
            nextContactTime = Time.time + 1f;
            target.Health.TakeDamage(kind == EnemyKind.Boss ? 2 : 1);
        }

        private void OnDied(Health _)
        {
            body.linearVelocity = Vector2.zero;
            Destroy(gameObject);
        }
    }
}
