using System;
using System.Collections.Generic;

namespace LittleKeep.Rules
{
    /// One Skill's growth during a Run: whether it is learned yet, the upgrades taken so far and, from the third,
    /// its Master Form.
    public sealed class SkillProgress
    {
        public SkillDef Base { get; }
        public IReadOnlyList<SkillUpgradeDef> Taken => taken;
        readonly List<SkillUpgradeDef> taken = new List<SkillUpgradeDef>();
        public const int UpgradesForMaster = 3;

        /// A Familiar knows its first Skill from the start and learns the others during the Run.
        public bool IsLearned { get; private set; }

        public SkillProgress(SkillDef baseDef, bool learned = true)
        {
            Base = baseDef;
            IsLearned = learned;
        }

        internal void Learn() => IsLearned = true;

        public bool IsMaster => taken.Count >= UpgradesForMaster && Base.MasterForm != null;
        public bool NextMakesMaster => taken.Count == UpgradesForMaster - 1 && Base.MasterForm != null;
        public IEnumerable<SkillUpgradeDef> Remaining
        {
            get
            {
                if (!IsLearned) yield break;
                foreach (var u in Base.Upgrades) if (!taken.Contains(u)) yield return u;
            }
        }

        internal void Take(SkillUpgradeDef u) => taken.Add(u);

        /// The Skill as it fights right now: Master Form once earned, with every upgrade applied.
        public SkillDef Build()
        {
            var s = IsMaster ? Base.MasterForm.Clone() : Base.Clone();
            foreach (var u in taken)
                foreach (var m in u.Mods) m.ApplyTo(s);
            s.Growth = IsMaster ? SkillGrowth.Master : taken.Count > 0 ? SkillGrowth.Upgraded : SkillGrowth.Base;
            return s;
        }
    }

    /// The Keeper or a Familiar as it stands in this Run.
    public sealed class SquadMember
    {
        public FighterDef Def { get; }
        public IReadOnlyList<SkillProgress> Skills => skills;
        readonly List<SkillProgress> skills = new List<SkillProgress>();

        public SquadMember(FighterDef def)
        {
            Def = def;
            for (int i = 0; i < def.Skills.Count; i++) skills.Add(new SkillProgress(def.Skills[i], learned: i == 0));
        }

        /// The Skill this member can learn next (Skills are learned in order), or null when it knows them all.
        public SkillProgress NextToLearn
        {
            get { foreach (var s in skills) if (!s.IsLearned) return s; return null; }
        }

        public FighterDef Build()
        {
            var f = new FighterDef
            {
                Id = Def.Id, Name = Def.Name, Hp = Def.Hp, AttackDamage = Def.AttackDamage,
                AttackInterval = Def.AttackInterval, ProjectileSpeed = Def.ProjectileSpeed,
            };
            foreach (var s in skills) if (s.IsLearned) f.Skills.Add(s.Build());
            return f;
        }
    }

    public sealed class UpgradeChoice
    {
        public SquadMember Member;
        public SkillProgress Skill;
        /// The upgrade to take; null when the choice is to learn Skill.
        public SkillUpgradeDef Upgrade;
        /// Taking it teaches the member Skill, which then fights from the next Battle.
        public bool Learns;
        /// Taking it turns the Skill into its Master Form.
        public bool MakesMaster;
    }

    public enum ShopItemKind { Upgrade, Repair }

    public sealed class ShopItem
    {
        public ShopItemKind Kind;
        public UpgradeChoice Upgrade;
        public int Price;
        public bool Sold;
    }

    public enum StopKind { Battle, ToughBattle, MidBoss, Boss, Rest, Shop, Event, Wild }

    /// One step's option on the Trail.
    public sealed class Stop
    {
        public StopKind Kind;
        public int Chapter;
        public int Step;

        public bool IsBattle => Kind == StopKind.Battle || Kind == StopKind.ToughBattle || Kind == StopKind.MidBoss || Kind == StopKind.Boss;
    }
}
