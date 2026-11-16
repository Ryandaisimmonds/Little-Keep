using System.Collections.Generic;
using System.Linq;

namespace LittleKeep.Rules.Tests
{
    /// A small, complete Run's worth of content for tests, and a driver that plays a Run by a simple policy.
    static class RunTestContent
    {
        public static SkillDef Skill(string id, int upgrades = 3, bool master = true)
        {
            var s = new SkillDef { Id = id, Name = id, Shape = SkillShape.Volley, Cooldown = 5f, Damage = 10f, Count = 2, Spread = 10f, ProjectileSpeed = 20f };
            for (int i = 1; i <= upgrades; i++)
                s.Upgrades.Add(new SkillUpgradeDef { Id = $"{id}_up{i}", Name = $"{id} +{i}", SkillId = id, Mods = { new SkillMod(SkillStat.Damage, ModOp.Add, 5f) } });
            if (master) s.MasterForm = new SkillDef { Id = id + "_master", Name = id + " Master", Shape = SkillShape.Burst, Cooldown = 4f, Damage = 50f, Radius = 3f };
            return s;
        }

        public static FighterDef Fighter(string id, params SkillDef[] skills) =>
            new FighterDef { Id = id, Name = id, Hp = 50f, AttackDamage = 5f, AttackInterval = 0.5f, Skills = skills.ToList() };

        public static EncounterDef Encounter(string id, int gold = 10, string creature = "") =>
            new EncounterDef { Id = id, Gold = gold, Waves = { new WaveDef { Entries = { new WaveEntry { Hero = new HeroDef { Id = id + "_hero", Hp = 10f, CreatureId = creature }, Count = 1 } } } } };

        public static RunContent Make()
        {
            var content = new RunContent
            {
                Keeper = Fighter("keeper", Skill("keeper_skill")),
                Familiars =
                {
                    Fighter("pip", Skill("pip_skill")),
                    Fighter("bramble", Skill("bramble_skill")),
                    Fighter("pony", Skill("pony_skill")),
                    Fighter("owl", Skill("owl_skill")),
                    Fighter("moth", Skill("moth_skill")),
                    Fighter("newt", Skill("newt_skill")),
                },
                Events =
                {
                    new EventDef { Id = "gift", Title = "A gift", Options = { new EventOption { Text = "Take it", Effects = { new EventEffect { Kind = EventEffectKind.Gold, Amount = 15f } } } } },
                    new EventDef { Id = "well", Title = "A wishing well", Options =
                    {
                        new EventOption { Text = "Toss a coin", GoldCost = 10, Effects = { new EventEffect { Kind = EventEffectKind.Heal, Amount = 20f } } },
                        new EventOption { Text = "Walk on" },
                    } },
                },
            };
            for (int c = 0; c < 3; c++)
                content.Chapters.Add(new ChapterDef
                {
                    Name = $"Chapter {c + 1}",
                    Battles = { Encounter($"c{c}_battle_a"), Encounter($"c{c}_battle_b", creature: "pony") },
                    ToughBattles = { Encounter($"c{c}_tough", gold: 25, creature: "owl") },
                    MidBoss = Encounter($"c{c}_midboss", gold: 30),
                    Boss = Encounter($"c{c}_boss", gold: 50),
                });
            return content;
        }

        public static BattleReport Win(Run run, float keepHp = -1f, params string[] freed) =>
            new BattleReport { Won = true, KeepHp = keepHp < 0f ? run.KeepHp : keepHp, FreedCreatures = freed.ToList() };

        /// Plays a whole Run: first Stop each time, wins every Battle untouched, first upgrade, passes on creatures,
        /// leaves shops, first affordable event option. Returns a trace of everything that happened.
        public static List<string> Autoplay(Run run, int maxActions = 500)
        {
            var trace = new List<string>();
            for (int i = 0; i < maxActions && run.Phase != RunPhase.Ended; i++)
            {
                switch (run.Phase)
                {
                    case RunPhase.Trail:
                        trace.Add($"c{run.Chapter} next=[{string.Join(",", run.NextChoices.Select(s => s.Kind))}] beyond=[{string.Join(",", run.Beyond.Select(s => s.Kind))}] toBoss={run.StepsToBoss}");
                        run.ChooseStop(0);
                        break;
                    case RunPhase.Battle:
                        var setup = run.CreateBattle();
                        trace.Add($"battle {run.CurrentStop.Kind} seed={setup.Seed} heroes={setup.Waves.Sum(w => w.Entries.Sum(e => e.Count))}");
                        run.FinishBattle(Win(run));
                        break;
                    case RunPhase.Upgrade:
                        trace.Add("offer " + string.Join(",", run.UpgradeOffer.Select(o => o.Upgrade.Id)));
                        if (run.UpgradeOffer.Count > 0) run.PickUpgrade(0); else run.SkipUpgrade();
                        break;
                    case RunPhase.Befriend:
                        trace.Add("creature " + run.CreatureOffer.Id);
                        run.PassCreature();
                        break;
                    case RunPhase.Shop:
                        trace.Add("shop " + string.Join(",", run.Shop.Select(s => s.Kind + ":" + s.Price)));
                        run.LeaveShop();
                        break;
                    case RunPhase.Event:
                        trace.Add("event " + run.CurrentEvent.Id);
                        int option = Enumerable.Range(0, run.CurrentEvent.Options.Count).First(run.CanChoose);
                        run.ChooseEventOption(option);
                        break;
                }
            }
            return trace;
        }
    }
}
