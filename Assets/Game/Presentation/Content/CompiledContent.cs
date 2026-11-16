using System.Collections.Generic;
using LittleKeep.Rules;

namespace LittleKeep.Game
{
    /// Turns the Inspector-editable assets into the plain definitions the rules use. Each asset becomes exactly one
    /// definition object, so the rules can compare them by reference. Also answers "which asset is this?" for drawing.
    public sealed class CompiledContent
    {
        public RunContent Run { get; } = new RunContent();
        public ProgressContent Progress { get; } = new ProgressContent();
        public GameContent Source { get; }
        readonly Dictionary<FighterAsset, FighterDef> fighters = new Dictionary<FighterAsset, FighterDef>();
        readonly Dictionary<HeroAsset, HeroDef> heroes = new Dictionary<HeroAsset, HeroDef>();
        readonly Dictionary<SkillAsset, SkillDef> skills = new Dictionary<SkillAsset, SkillDef>();
        readonly Dictionary<EncounterAsset, EncounterDef> encounters = new Dictionary<EncounterAsset, EncounterDef>();
        readonly Dictionary<string, FighterAsset> fighterById = new Dictionary<string, FighterAsset>();
        readonly Dictionary<string, HeroAsset> heroById = new Dictionary<string, HeroAsset>();
        readonly Dictionary<string, SkillAsset> skillById = new Dictionary<string, SkillAsset>();

        public CompiledContent(GameContent source)
        {
            Source = source;
            Run.Keeper = Fighter(source.keeper);
            foreach (var f in source.familiars)
            {
                if (f == null) continue;
                Run.Familiars.Add(Fighter(f));
                Progress.Familiars.Add(new FamiliarUnlock { Id = f.def.Id, Kind = f.unlock, BondSize = f.bondSize, DeedId = f.deedId });
            }
            Progress.Deeds.AddRange(source.deeds);
            Progress.Upgrades.AddRange(source.keeperUpgrades);
            Progress.Tiers.AddRange(source.tiers);
            Progress.Rewards = source.rewards;
            Run.WildCreatures.AddRange(Progress.WildCreatures);
            foreach (var c in source.chapters)
            {
                if (c == null) continue;
                var chapter = new ChapterDef { Name = c.displayName, MidBoss = Encounter(c.midBoss), Boss = Encounter(c.boss) };
                foreach (var e in c.battles) if (e != null) chapter.Battles.Add(Encounter(e));
                foreach (var e in c.toughBattles) if (e != null) chapter.ToughBattles.Add(Encounter(e));
                Run.Chapters.Add(chapter);
            }
            foreach (var e in source.events) if (e != null) Run.Events.Add(e.def);
            foreach (var h in source.heroes) if (h != null) Hero(h);
            Run.Tuning = source.runTuning;
            Run.BattleTuning = source.battleTuning;
        }

        public FighterAsset FighterAsset(string id) => id != null && fighterById.TryGetValue(id, out var a) ? a : null;
        public HeroAsset HeroAsset(string id) => id != null && heroById.TryGetValue(id, out var a) ? a : null;
        /// The Skill asset for a Skill id, base or Master Form.
        public SkillAsset SkillAsset(string id) => id != null && skillById.TryGetValue(id, out var a) ? a : null;

        FighterDef Fighter(FighterAsset a)
        {
            if (a == null) return null;
            if (fighters.TryGetValue(a, out var d)) return d;
            d = new FighterDef
            {
                Id = a.def.Id, Name = a.def.Name, Hp = a.def.Hp, AttackDamage = a.def.AttackDamage,
                AttackInterval = a.def.AttackInterval, ProjectileSpeed = a.def.ProjectileSpeed,
            };
            foreach (var s in a.skills) if (s != null) d.Skills.Add(Skill(s));
            fighters[a] = d;
            fighterById[d.Id] = a;
            return d;
        }

        SkillDef Skill(SkillAsset a)
        {
            if (skills.TryGetValue(a, out var d)) return d;
            d = a.def.Clone();
            d.Upgrades = new List<SkillUpgradeDef>();
            foreach (var u in a.upgrades)
            {
                u.SkillId = d.Id;
                d.Upgrades.Add(u);
            }
            if (a.hasMasterForm && a.masterForm != null)
            {
                d.MasterForm = a.masterForm.Clone();
                skillById[d.MasterForm.Id] = a;
            }
            skills[a] = d;
            skillById[d.Id] = a;
            return d;
        }

        HeroDef Hero(HeroAsset a)
        {
            if (a == null) return null;
            if (heroes.TryGetValue(a, out var d)) return d;
            d = a.def.Clone();
            d.CreatureId = a.creature != null ? a.creature.def.Id : "";
            heroes[a] = d;
            heroById[d.Id] = a;
            d.Summon = Hero(a.summon);
            return d;
        }

        EncounterDef Encounter(EncounterAsset a)
        {
            if (a == null) return null;
            if (encounters.TryGetValue(a, out var d)) return d;
            d = new EncounterDef { Id = a.id, Name = a.displayName, Gold = a.gold };
            foreach (var w in a.waves)
            {
                var wave = new WaveDef { SpawnInterval = w.spawnInterval };
                foreach (var e in w.entries) if (e.hero != null) wave.Entries.Add(new WaveEntry { Hero = Hero(e.hero), Count = e.count });
                d.Waves.Add(wave);
            }
            encounters[a] = d;
            return d;
        }
    }
}
