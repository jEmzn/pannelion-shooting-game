using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TopDownShooter
{
    public sealed class Health : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 5;
        [SerializeField] private float invulnerabilitySeconds;

        public int Maximum => maxHealth;
        public int Current { get; private set; }
        public bool IsAlive => Current > 0;
        public event Action<Health> Died;

        private float nextDamageTime;

        private void Awake() => Current = maxHealth;

        public void Configure(int maximum, float invulnerability = 0f)
        {
            maxHealth = Mathf.Max(1, maximum);
            invulnerabilitySeconds = Mathf.Max(0f, invulnerability);
            Current = maxHealth;
        }

        public void TakeDamage(int amount)
        {
            if (!IsAlive || Time.time < nextDamageTime || amount <= 0) return;
            Current = Mathf.Max(0, Current - amount);
            nextDamageTime = Time.time + invulnerabilitySeconds;
            if (Current == 0) Died?.Invoke(this);
        }
    }
}
