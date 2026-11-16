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
        /// Extra Hero health per Chapter after the first (0.35 = +35%).
        public float HeroHpPerChapter = 0.35f;
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

        public FighterDef FindFamiliar(string id) => Familiars.Find(f => f.Id == id);
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
    }
}
