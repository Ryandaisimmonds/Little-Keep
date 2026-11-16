using System;
using System.Collections.Generic;

namespace LittleKeep.Rules
{
    /// One real-time Battle. Advance it with time; read its state and take its events.
    public sealed class Battle
    {
        public const float TickSeconds = 1f / 60f;

        readonly BattleSetup setup;
        readonly BattleTuning t;
        readonly Rng rng;
        readonly List<BattleEvent> events = new List<BattleEvent>();
        readonly List<Hero> heroes = new List<Hero>();
        readonly List<Fighter> squad = new List<Fighter>();
        readonly List<Projectile> projectiles = new List<Projectile>();
        readonly Queue<HeroDef> spawnQueue = new Queue<HeroDef>();
        int nextId = 1;
        float accumulator;
        int waveIndex = -1;
        float waveTimer;
        float spawnTimer;
        Vec2? aimPoint;
        float rallyRemaining;

        public float Time { get; private set; }
        public float KeepHp { get; private set; }
        public float KeepMaxHp => setup.KeepMaxHp;
        public BattleOutcome Outcome { get; private set; }
        public IReadOnlyList<Hero> Heroes => heroes;
        /// The Keeper first, then Familiars.
        public IReadOnlyList<Fighter> Squad => squad;
        public Fighter Keeper => squad[0];
        public IReadOnlyList<Projectile> Projectiles => projectiles;
        /// 1-based number of the wave currently in play (0 before the first wave).
        public int WaveNumber => waveIndex + 1;
        public int WaveCount => setup.Waves.Count;
        public BattleTuning Tuning => t;
        /// 0 = just used, 1 = ready.
        public float RallyCharge => t.RallyCooldown <= 0f ? 1f : 1f - rallyRemaining / t.RallyCooldown;

        public Battle(BattleSetup setup)
        {
            this.setup = setup ?? throw new ArgumentNullException(nameof(setup));
            t = setup.Tuning ?? new BattleTuning();
            rng = new Rng(setup.Seed);
            KeepHp = Math.Min(setup.KeepHp, setup.KeepMaxHp);
            waveTimer = t.FirstWaveDelay;
            rallyRemaining = t.RallyCooldown * (1f - t.RallyStartCharge);

            squad.Add(new Fighter(nextId++, setup.Keeper, true, t.KeeperPosition, t.SkillStartCharge));
            for (int i = 0; i < setup.Familiars.Count && i < t.FamiliarPositions.Length; i++)
                squad.Add(new Fighter(nextId++, setup.Familiars[i], false, t.FamiliarPositions[i], t.SkillStartCharge));
        }

        /// The point the Player is holding on the lane, or null to let the Keeper aim by itself.
        public Vec2? AimPoint => aimPoint;

        public void SetAim(Vec2? point) => aimPoint = point;

        /// Rally: the Keeper and every Familiar who is not knocked out casts every Skill at once. Needs a Hero on the lane and a full charge.
        public bool TryRally()
        {
            if (Outcome != BattleOutcome.Ongoing || rallyRemaining > 0f || ClosestToKeep() == null) return false;
            rallyRemaining = t.RallyCooldown;
            Emit(new BattleEvent { Kind = BattleEventKind.Rally, SourceId = Keeper.Id, Position = Keeper.Position });
            foreach (var f in squad)
            {
                if (f.IsKnockedOut) continue;
                foreach (var skill in f.skills)
                {
                    Cast(f, skill.Def);
                    skill.CooldownRemaining = skill.Def.Cooldown;
                }
            }
            RemoveDefeated();
            return true;
        }

        /// Events since the last call, oldest first.
        public List<BattleEvent> TakeEvents()
        {
            var list = new List<BattleEvent>(events);
            events.Clear();
            return list;
        }

        /// Move the Battle forward. Runs whole fixed ticks; leftover time carries to the next call.
        public void Advance(float seconds)
        {
            accumulator += seconds;
            while (accumulator >= TickSeconds - 1e-6f && Outcome == BattleOutcome.Ongoing)
            {
                accumulator -= TickSeconds;
                Tick();
            }
        }

        void Tick()
        {
            const float dt = TickSeconds;
            Time += dt;
            rallyRemaining = Math.Max(0f, rallyRemaining - dt);
            UpdateWaves(dt);
            UpdateSquad(dt);
            UpdateProjectiles(dt);
            UpdateHeroes(dt);
            CheckOutcome();
        }

        void UpdateWaves(float dt)
        {
            if (spawnQueue.Count > 0)
            {
                spawnTimer -= dt;
                if (spawnTimer <= 0f)
                {
                    Spawn(spawnQueue.Dequeue());
                    spawnTimer = setup.Waves[waveIndex].SpawnInterval;
                }
                return;
            }
            if (heroes.Count > 0 || waveIndex + 1 >= setup.Waves.Count) return;
            waveTimer -= dt;
            if (waveTimer > 0f) return;
            StartWave(waveIndex + 1);
        }

        void StartWave(int index)
        {
            waveIndex = index;
            waveTimer = t.TimeBetweenWaves;
            spawnTimer = 0f;
            var wave = setup.Waves[index];
            var order = new List<HeroDef>();
            foreach (var e in wave.Entries)
                for (int i = 0; i < e.Count; i++) order.Add(e.Hero);
            for (int i = order.Count - 1; i > 0; i--)
            {
                int j = rng.Range(0, i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }
            foreach (var h in order) spawnQueue.Enqueue(h);
            Emit(new BattleEvent { Kind = BattleEventKind.WaveStarted, Amount = WaveNumber });
        }

        void Spawn(HeroDef def)
        {
            float margin = def.Size;
            var pos = new Vec2(t.LaneLength, rng.Range(-t.LaneHalfHeight + margin, t.LaneHalfHeight - margin));
            var hero = new Hero(nextId++, def, pos);
            heroes.Add(hero);
            Emit(new BattleEvent { Kind = BattleEventKind.HeroSpawned, TargetId = hero.Id, Position = pos, Tag = def.Id });
        }

        void UpdateHeroes(float dt)
        {
            for (int i = heroes.Count - 1; i >= 0; i--)
            {
                var h = heroes[i];
                var target = EngageTarget(h);
                if (target != null)
                {
                    if (!h.IsEngaged) h.attackTimer = AttackInterval(h) * 0.5f;
                    h.IsEngaged = true;
                    h.attackTimer -= dt;
                    if (h.attackTimer <= 0f)
                    {
                        h.attackTimer = AttackInterval(h);
                        HeroAttack(h, target);
                    }
                    continue;
                }
                h.IsEngaged = false;
                h.Position = new Vec2(h.Position.X - h.Def.Speed * dt, h.Position.Y);
                if (h.Position.X <= t.KeepX)
                {
                    heroes.RemoveAt(i);
                    KeepHp = Math.Max(0f, KeepHp - h.Def.KeepDamage);
                    Emit(new BattleEvent { Kind = BattleEventKind.HeroReachedKeep, SourceId = h.Id, Position = h.Position, Amount = h.Def.KeepDamage });
                }
            }
        }

        static float AttackInterval(Hero h) => h.Def.MeleeDamage > 0f ? h.Def.MeleeInterval : h.Def.ShotInterval;

        /// The Keeper or Familiar this Hero stops to fight, if any is in reach.
        Fighter EngageTarget(Hero h)
        {
            float reach = h.Def.MeleeDamage > 0f ? h.Def.MeleeRange : h.Def.ShotDamage > 0f ? h.Def.ShotRange : 0f;
            if (reach <= 0f) return null;
            Fighter best = null;
            float bestDist = reach;
            foreach (var f in squad)
            {
                if (f.IsKnockedOut) continue;
                float d = Vec2.Distance(f.Position, h.Position);
                if (d <= bestDist) { best = f; bestDist = d; }
            }
            return best;
        }

        void HeroAttack(Hero h, Fighter target)
        {
            Emit(new BattleEvent { Kind = BattleEventKind.HeroAttack, SourceId = h.Id, TargetId = target.Id, Position = target.Position });
            if (h.Def.MeleeDamage > 0f)
            {
                DamageFighter(target, h.Def.MeleeDamage, h.Id);
                return;
            }
            var dir = (target.Position - h.Position).Normalized;
            projectiles.Add(new Projectile(nextId++, h.Id, h.Position, dir * h.Def.ShotSpeed, h.Def.ShotDamage, false, false));
        }

        void DamageFighter(Fighter f, float amount, int sourceId)
        {
            if (f.IsKnockedOut) return;
            f.Hp = Math.Max(0f, f.Hp - amount);
            Emit(new BattleEvent { Kind = BattleEventKind.Hit, SourceId = sourceId, TargetId = f.Id, Position = f.Position, Amount = amount });
            if (f.Hp > 0f) return;
            f.KnockoutRemaining = t.KnockoutSeconds;
            Emit(new BattleEvent { Kind = BattleEventKind.KnockedOut, SourceId = sourceId, TargetId = f.Id, Position = f.Position });
        }

        void UpdateSquad(float dt)
        {
            foreach (var f in squad)
            {
                if (f.IsKnockedOut)
                {
                    f.KnockoutRemaining -= dt;
                    if (f.KnockoutRemaining > 0f) continue;
                    f.KnockoutRemaining = 0f;
                    f.Hp = f.MaxHp;
                    f.attackTimer = f.Def.AttackInterval;
                    Emit(new BattleEvent { Kind = BattleEventKind.Recovered, TargetId = f.Id, Position = f.Position });
                    continue;
                }
                foreach (var skill in f.skills)
                {
                    skill.CooldownRemaining = Math.Max(0f, skill.CooldownRemaining - dt);
                    if (skill.CooldownRemaining > 0f || ClosestToKeep() == null) continue;
                    Cast(f, skill.Def);
                    skill.CooldownRemaining = skill.Def.Cooldown;
                }
                f.attackTimer -= dt;
                if (f.attackTimer > 0f) continue;
                if (!TryGetAim(f, f.Def.ProjectileSpeed, out var aim)) continue;
                f.attackTimer = f.Def.AttackInterval;
                var dir = (aim - f.Position).Normalized;
                projectiles.Add(new Projectile(nextId++, f.Id, f.Position, dir * f.Def.ProjectileSpeed, f.Def.AttackDamage, true, false));
                Emit(new BattleEvent { Kind = BattleEventKind.Attack, SourceId = f.Id, Position = aim });
            }
        }

        void Cast(Fighter f, SkillDef skill)
        {
            if (!TryGetAim(f, skill.Shape == SkillShape.Volley ? skill.ProjectileSpeed : 0f, out var aim)) return;
            if (skill.Shape == SkillShape.Burst)
            {
                Emit(new BattleEvent { Kind = BattleEventKind.SkillCast, SourceId = f.Id, Position = aim, Amount = skill.Radius, Tag = skill.Id });
                foreach (var h in heroes)
                    if (Vec2.Distance(h.Position, aim) <= skill.Radius + h.Def.Size) DamageHero(h, skill.Damage, f.Id, true);
                return;
            }
            Emit(new BattleEvent { Kind = BattleEventKind.SkillCast, SourceId = f.Id, Position = aim, Tag = skill.Id });
            var dir = (aim - f.Position).Normalized;
            int count = Math.Max(1, skill.Count);
            for (int i = 0; i < count; i++)
            {
                float angle = count == 1 ? 0f : -skill.Spread * 0.5f + skill.Spread * i / (count - 1);
                projectiles.Add(new Projectile(nextId++, f.Id, f.Position, dir.Rotated(angle) * skill.ProjectileSpeed, skill.Damage, true, true));
            }
        }

        void RemoveDefeated() => heroes.RemoveAll(h => h.Hp <= 0f);

        /// Where a fighter should aim: the Hero closest to the Keep, led by its movement.
        bool TryGetAim(Fighter f, float projectileSpeed, out Vec2 aim)
        {
            if (f.IsKeeper && aimPoint.HasValue)
            {
                aim = aimPoint.Value;
                return true;
            }
            var target = ClosestToKeep();
            aim = default;
            if (target == null) return false;
            aim = Lead(f.Position, target, projectileSpeed);
            return true;
        }

        Hero ClosestToKeep()
        {
            Hero best = null;
            foreach (var h in heroes)
                if (h.Hp > 0f && h.Position.X <= t.LaneLength && (best == null || h.Position.X < best.Position.X)) best = h;
            return best;
        }

        static Vec2 Lead(Vec2 from, Hero h, float projectileSpeed)
        {
            if (h.IsEngaged || projectileSpeed <= 0f) return h.Position;
            float time = Vec2.Distance(from, h.Position) / projectileSpeed;
            return new Vec2(h.Position.X - h.Def.Speed * time, h.Position.Y);
        }

        void UpdateProjectiles(float dt)
        {
            for (int i = projectiles.Count - 1; i >= 0; i--)
            {
                var p = projectiles[i];
                p.Position = p.Position + p.Velocity * dt;
                bool remove = p.Position.X < -1f || p.Position.X > t.LaneLength + 2f || Math.Abs(p.Position.Y) > t.LaneHalfHeight + 2f;
                if (!remove && p.FromSquad)
                {
                    foreach (var h in heroes)
                    {
                        if (Vec2.Distance(h.Position, p.Position) > h.Def.Size + 0.15f) continue;
                        DamageHero(h, p.damage, p.SourceId, p.FromSkill);
                        remove = true;
                        break;
                    }
                }
                else if (!remove)
                {
                    foreach (var f in squad)
                    {
                        if (f.IsKnockedOut || Vec2.Distance(f.Position, p.Position) > 0.5f) continue;
                        DamageFighter(f, p.damage, p.SourceId);
                        remove = true;
                        break;
                    }
                }
                if (remove) projectiles.RemoveAt(i);
            }
            RemoveDefeated();
        }

        void DamageHero(Hero h, float amount, int sourceId, bool fromSkill)
        {
            if (h.Hp <= 0f) return;
            if (!fromSkill) amount *= 1f - h.Def.AttackBlock;
            h.Hp -= amount;
            Emit(new BattleEvent { Kind = BattleEventKind.Hit, SourceId = sourceId, TargetId = h.Id, Position = h.Position, Amount = amount, FromSkill = fromSkill });
            if (h.Hp <= 0f)
                Emit(new BattleEvent { Kind = BattleEventKind.HeroDefeated, SourceId = sourceId, TargetId = h.Id, Position = h.Position, Tag = h.Def.Id });
        }

        void CheckOutcome()
        {
            if (KeepHp <= 0f)
            {
                Outcome = BattleOutcome.Lost;
                Emit(new BattleEvent { Kind = BattleEventKind.Lost });
            }
            else if (setup.Waves.Count > 0 && waveIndex == setup.Waves.Count - 1 && spawnQueue.Count == 0 && heroes.Count == 0)
            {
                Outcome = BattleOutcome.Won;
                Emit(new BattleEvent { Kind = BattleEventKind.Won });
            }
        }

        void Emit(BattleEvent e) => events.Add(e);
    }
}
