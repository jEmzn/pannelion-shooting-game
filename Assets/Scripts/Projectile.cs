using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TopDownShooter
{
    public sealed class Projectile : MonoBehaviour
    {
        private Vector2 direction;
        private float speed;
        private int damage;
        private bool firedByPlayer;
        private float expiry;
        private Rigidbody2D body;

        public static void Spawn(Sprite sprite, Color color, Vector2 position, Vector2 travelDirection,
            float travelSpeed, int hitDamage, bool fromPlayer, float size = 0.22f)
        {
            var projectile = new GameObject(fromPlayer ? "Player Shot" : "Enemy Shot");
            projectile.transform.position = position;
            projectile.transform.localScale = Vector3.one * size;
            var renderer = projectile.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = 8;
            var collider = projectile.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            var rigidbody = projectile.AddComponent<Rigidbody2D>();
            rigidbody.bodyType = RigidbodyType2D.Kinematic;
            rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var shot = projectile.AddComponent<Projectile>();
            shot.direction = travelDirection.normalized;
            shot.speed = travelSpeed;
            shot.damage = hitDamage;
            shot.firedByPlayer = fromPlayer;
            shot.expiry = Time.time + 3.5f;
            shot.body = rigidbody;
        }

        private void FixedUpdate()
        {
            if (Time.time >= expiry) { Destroy(gameObject); return; }
            body.MovePosition(body.position + direction * (speed * Time.fixedDeltaTime));
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<WallMarker>() != null) { Destroy(gameObject); return; }
            var health = other.GetComponent<Health>();
            if (health == null || !health.IsAlive) return;
            if (firedByPlayer && other.GetComponent<EnemyBrain>() != null ||
                !firedByPlayer && other.GetComponent<PlayerController>() != null)
            {
                health.TakeDamage(damage);
                Destroy(gameObject);
            }
        }
    }
}
