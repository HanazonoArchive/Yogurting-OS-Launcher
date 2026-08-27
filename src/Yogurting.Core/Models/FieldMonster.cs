using System;

namespace Yogurting.Core.Models
{
    /// <summary>
    /// Represents an active field monster instance spawned from a generator.
    /// </summary>
    public class FieldMonster
    {
        public int EntityId { get; set; }
        public int MonsterType { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Level { get; set; }
        public int CurrentHp { get; set; }
        public int MaxHp { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float SpawnX { get; set; }
        public float SpawnY { get; set; }
        public int DirX { get; set; } = 0;
        public int DirY { get; set; } = 1000;
        public int ExpReward { get; set; }
        public int DropItemType { get; set; }
        public int DropCount { get; set; }
        public int DropRate { get; set; }
        public bool IsDead { get; set; }
        public DateTime DeathTime { get; set; }
        public int RespawnSeconds { get; set; } = 5;
        public int TargetPlayerId { get; set; }
        public DateTime LastAttackTime { get; set; }

        public void TakeDamage(int damage)
        {
            CurrentHp = Math.Max(0, CurrentHp - damage);
            if (CurrentHp <= 0)
            {
                IsDead = true;
                DeathTime = DateTime.UtcNow;
            }
        }

        public void Respawn()
        {
            IsDead = false;
            CurrentHp = MaxHp;
            X = SpawnX;
            Y = SpawnY;
            TargetPlayerId = 0;
        }
    }
}
