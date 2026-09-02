using System;

namespace Yogurting.Core.Models
{
    public enum MonsterState : byte
    {
        Wait = 0,   // stWait: Idle / Leash Anchor Wait
        Walk = 1,   // stWalk: Peaceful Wandering (Motion 1, Speed 0.8x)
        Chase = 2,  // stChase: Aggro Target Chasing (Motion 2, Speed 0.8x)
        Attack = 3, // stAttack: In Melee Combat Range
        Dead = 4    // stDead: Awaiting Respawn
    }

    /// <summary>
    /// Represents an active field monster instance spawned from a generator.
    /// Matches Delphi Quartet TMonster (_Unit49.pas:1011-1065) layout & physics.
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
        public int DirY { get; set; } = 1;
        public int ExpReward { get; set; }
        public int DropItemType { get; set; }
        public int DropCount { get; set; }
        public int DropRate { get; set; }
        public bool IsDead { get; set; }
        public DateTime DeathTime { get; set; }
        public int RespawnSeconds { get; set; } = 15;
        public int TargetPlayerId { get; set; }
        public int AttackPower { get; set; } = 8;
        public int MotionType { get; set; } = 300011;
        public DateTime LastAttackTime { get; set; }

        // Authentic Delphi AI State Machine Fields (_Unit49.pas:1026-1060)
        public MonsterState State { get; set; } = MonsterState.Wait;
        public uint Frame { get; set; } = 0;
        public float StartX { get; set; }
        public float StartY { get; set; }
        public float DestX { get; set; }
        public float DestY { get; set; }
        public float DeltaX { get; set; }
        public float DeltaY { get; set; }
        public float WalkLength { get; set; }
        public byte MoveSpeedRate { get; set; } = 80; // 0x50 = 80 decimal (0.8x base speed in Delphi)
        public int MoveMotion { get; set; } = 1;     // 1 = Walk, 2 = Run
        public int NextWanderInterval { get; set; } = 5;

        private readonly object _syncLock = new();

        public bool TakeDamage(int damage)
        {
            lock (_syncLock)
            {
                if (IsDead) return false;

                CurrentHp = Math.Max(0, CurrentHp - damage);
                if (CurrentHp <= 0)
                {
                    IsDead = true;
                    State = MonsterState.Dead;
                    DeathTime = DateTime.UtcNow;
                    return true;
                }
                return false;
            }
        }

        public void Respawn()
        {
            lock (_syncLock)
            {
                IsDead = false;
                CurrentHp = MaxHp;
                X = SpawnX;
                Y = SpawnY;
                DestX = SpawnX;
                DestY = SpawnY;
                StartX = SpawnX;
                StartY = SpawnY;
                TargetPlayerId = 0;
                State = MonsterState.Wait;
                Frame = 0;
            }
        }
    }
}
