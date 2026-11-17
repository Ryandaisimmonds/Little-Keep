using System;
using System.Collections.Generic;

namespace LittleKeep.Rules
{
    /// A fight: waves of Heroes and the gold it pays.
    public sealed class EncounterDef
    {
        public string Id = "encounter";
        public string Name = "";
        public List<WaveDef> Waves = new List<WaveDef>();
        public int Gold = 20;
    }

    /// One of a Run's three Chapters: the encounters its Stops draw from, its mid-boss and its boss.
    public sealed class ChapterDef
    {
        public string Name = "Chapter";
        public List<EncounterDef> Battles = new List<EncounterDef>();
        public List<EncounterDef> ToughBattles = new List<EncounterDef>();
        public EncounterDef MidBoss;
        public EncounterDef Boss;
    }

    public enum EventEffectKind
    {
        Gold,
        /// Repairs the Keep.
        Heal,
        /// Raises the Keep's maximum health (and its health by the same amount).
        MaxHp,
        /// Applies a random Skill Upgrade from the squad's pools.
        RandomUpgrade,
        /// Offers CreatureId (or a random creature if empty) for Befriending.
        Creature,
    }

    [Serializable]
    public sealed class EventEffect
    {
        public EventEffectKind Kind;
        public float Amount;
        public string CreatureId = "";
    }

    [Serializable]
    public sealed class EventOption
    {
        public string Text = "Continue";
        /// Text shown after choosing.
        public string Outcome = "";
        public int GoldCost;
        public List<EventEffect> Effects = new List<EventEffect>();
    }

    /// A story event Stop: some text and a few choices.
    [Serializable]
    public sealed class EventDef
    {
        public string Id = "event";
        public string Title = "Event";
        public string Text = "";
        public List<EventOption> Options = new List<EventOption>();
    }

    [Serializable]
    public sealed class RunTuning
    {
        public int StepsPerChapter = 7;
        /// The Trail step (0-based) where the mid-boss stands.
        public int MidBossStep = 3;
        public float KeepMaxHp = 100f;
        public float RestHeal = 30f;
        public int UpgradeOfferSize = 3;
        public int MaxFamiliars = 4;
        public int ShopUpgradePrice = 40;
        public int ShopRepairPrice = 25;
        public float ShopRepairAmount = 30f;
        public int ShopUpgrades = 3;
        /// Gold given instead when there is nothing left to offer.
        public int ConsolationGold = 15;
        /// Overall difficulty: multiplies every Hero's health.
        public float HeroHpScale = 1f;
        /// Overall difficulty: multiplies how many Heroes come in each wave.
        public float HeroCountScale = 1f;
        /// Extra Hero health per Chapter after the first (0.35 = +35%).
        public float HeroHpPerChapter = 0.35f;
        /// Extra Hero health per step along a Chapter's Trail (0.04 = +4% a step).
        public float HeroHpPerStep = 0.04f;
        public int WeightBattle = 40, WeightTough = 12, WeightRest = 10, WeightShop = 12, WeightEvent = 16, WeightWild = 10;
    }

    /// Everything a Run is built from.
    public sealed class RunContent
    {
        public FighterDef Keeper;
        /// Every creature that can become a Familiar: starters and those befriended along the way.
        public List<FighterDef> Familiars = new List<FighterDef>();
        public List<ChapterDef> Chapters = new List<ChapterDef>();
        public List<EventDef> Events = new List<EventDef>();
        public RunTuning Tuning = new RunTuning();
        public BattleTuning BattleTuning = new BattleTuning();
        /// Creatures wild creature Stops and events can offer (ids). Empty = any Familiar not in the squad.
        public List<string> WildCreatures = new List<string>();

        public FighterDef FindFamiliar(string id) => Familiars.Find(f => f.Id == id);

        /// A short code that changes whenever anything a saved Run depends on changes: ids, order, counts, and every
        /// number that steers its random draws or decides what a replayed choice can afford (Gold, prices, Keep health).
        /// Hero health and Battle numbers are left out: a Battle is never replayed, only its result.
        public string Fingerprint()
        {
            var sb = new System.Text.StringBuilder();
            // Numbers are written the same way whatever language the device is set to.
            string N(float v) => v.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
            void Fighter(FighterDef f)
            {
                sb.Append(f.Id).Append('[');
                foreach (var s in f.Skills)
                {
                    sb.Append(s.Id).Append('(');
                    foreach (var u in s.Upgrades) sb.Append(u.Id).Append(',');
                    sb.Append(s.MasterForm?.Id).Append(')');
                }
                sb.Append(']');
            }
            void Encounter(EncounterDef e) => sb.Append(e?.Id).Append('=').Append(N(e?.Gold ?? 0)).Append(',');
            Fighter(Keeper);
            foreach (var f in Familiars) Fighter(f);
            foreach (var c in Chapters)
            {
                sb.Append('|').Append(c.Battles.Count).Append(',').Append(c.ToughBattles.Count).Append(':');
                foreach (var e in c.Battles) Encounter(e);
                foreach (var e in c.ToughBattles) Encounter(e);
                Encounter(c.MidBoss);
                Encounter(c.Boss);
            }
            foreach (var e in Events)
            {
                sb.Append('|').Append(e.Id);
                foreach (var o in e.Options)
                {
                    sb.Append('{').Append(N(o.GoldCost));
                    foreach (var fx in o.Effects) sb.Append(';').Append((int)fx.Kind).Append(':').Append(N(fx.Amount)).Append(':').Append(fx.CreatureId);
                    sb.Append('}');
                }
            }
            sb.Append('|').Append(string.Join(",", WildCreatures));
            var t = Tuning;
            foreach (float v in new float[]
            {
                t.StepsPerChapter, t.MidBossStep, t.UpgradeOfferSize, t.ShopUpgrades, t.MaxFamiliars,
                t.WeightBattle, t.WeightTough, t.WeightRest, t.WeightShop, t.WeightEvent, t.WeightWild,
                t.KeepMaxHp, t.RestHeal, t.ShopUpgradePrice, t.ShopRepairPrice, t.ShopRepairAmount, t.ConsolationGold,
            }) sb.Append('|').Append(N(v));
            uint hash = 2166136261;
            foreach (char ch in sb.ToString()) hash = (hash ^ ch) * 16777619;
            return hash.ToString("x8");
        }
    }

    /// What a finished Battle tells the Run.
    public struct BattleReport
    {
        public bool Won;
        public float KeepHp;
        public List<string> FreedCreatures;

        public static BattleReport From(Battle battle) => new BattleReport
        {
            Won = battle.Outcome == BattleOutcome.Won,
            KeepHp = battle.KeepHp,
            FreedCreatures = new List<string>(battle.FreedCreatures),
        };
    }

    public sealed class RunResult
    {
        public bool Won;
        public int ChaptersCleared;
        public int BattlesWon;
        /// 1-based Chapter the Run ended in.
        public int ChapterReached;
        public int Gold;
        public int Tier;
        public int RunNumber;
        /// Every creature befriended this Run, in order (repeats included).
        public List<string> Befriended = new List<string>();
    }
}
