using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace LittleKeep.Rules
{
    public enum RunPhase
    {
        /// Choosing the next Stop on the Trail.
        Trail,
        /// A Battle Stop is waiting to be fought: CreateBattle, then FinishBattle.
        Battle,
        /// Picking 1 of the offered Skill Upgrades.
        Upgrade,
        /// A creature wants to join: Befriend or pass.
        Befriend,
        Shop,
        Event,
        Ended,
    }

    /// One attempt from the first Battle to the final boss or defeat. Every choice goes through this class,
    /// all randomness comes from the seed, and the choices are recorded, so Save/Load replays a Run exactly.
    public sealed class Run
    {
        const string SaveHeader = "littlekeep-run/1";

        readonly RunContent content;
        readonly RunTuning t;
        readonly Rng rng;
        readonly int seed;
        readonly string starterId;
        readonly List<string> log = new List<string>();
        readonly List<SquadMember> familiars = new List<SquadMember>();
        readonly Queue<FighterDef> pendingCreatures = new Queue<FighterDef>();
        readonly Dictionary<HeroDef, HeroDef> scaledHeroes = new Dictionary<HeroDef, HeroDef>();
        List<List<Stop>> steps;
        int position = -1;
        int pendingUpgrades;
        int battleSeed;
        EncounterDef encounter;
        List<UpgradeChoice> offer = new List<UpgradeChoice>();
        List<ShopItem> shop = new List<ShopItem>();

        public RunTuning Tuning => t;
        public RunPhase Phase { get; private set; }
        /// 0-based.
        public int Chapter { get; private set; }
        public int ChapterCount => content.Chapters.Count;
        public string ChapterName => content.Chapters[Chapter].Name;
        public Stop CurrentStop { get; private set; }
        public float KeepHp { get; private set; }
        public float KeepMaxHp { get; private set; }
        public int Gold { get; private set; }
        public int BattlesWon { get; private set; }
        public SquadMember Keeper { get; }
        public IReadOnlyList<SquadMember> Familiars => familiars;
        public bool SquadFull => familiars.Count >= t.MaxFamiliars;
        public IReadOnlyList<UpgradeChoice> UpgradeOffer => offer;
        public FighterDef CreatureOffer { get; private set; }
        public IReadOnlyList<ShopItem> Shop => shop;
        public EventDef CurrentEvent { get; private set; }
        /// A line to show the Player about what just happened ("" = nothing to say).
        public string Message { get; private set; } = "";
        public RunResult Result { get; private set; }
        public string EncounterName => encounter?.Name ?? "";

        /// Choices for the next step of the Trail.
        public IReadOnlyList<Stop> NextChoices => position + 1 < steps.Count ? steps[position + 1] : new List<Stop>();
        /// The step after that, shown faintly. Empty past the boss: the next Chapter stays in fog.
        public IReadOnlyList<Stop> Beyond => position + 2 < steps.Count ? steps[position + 2] : new List<Stop>();
        /// Steps left including the boss.
        public int StepsToBoss => steps.Count - 1 - position;

        public Run(RunContent content, int seed, string starterId)
        {
            this.content = content ?? throw new ArgumentNullException(nameof(content));
            t = content.Tuning ?? new RunTuning();
            this.seed = seed;
            this.starterId = starterId;
            rng = new Rng(seed);
            KeepMaxHp = KeepHp = t.KeepMaxHp;
            Keeper = new SquadMember(content.Keeper);
            var starter = content.FindFamiliar(starterId) ?? content.Familiars.FirstOrDefault();
            if (starter != null) familiars.Add(new SquadMember(starter));
            StartChapter(0);
        }

        // --- Trail -------------------------------------------------------------

        void StartChapter(int chapter)
        {
            Chapter = chapter;
            position = -1;
            steps = GenerateTrail(chapter);
            Phase = RunPhase.Trail;
        }

        List<List<Stop>> GenerateTrail(int chapter)
        {
            int n = Math.Max(3, t.StepsPerChapter);
            var trail = new List<List<Stop>>();
            for (int step = 0; step < n; step++)
            {
                var kinds = new List<StopKind>();
                if (step == n - 1) kinds.Add(StopKind.Boss);
                else if (step == t.MidBossStep) kinds.Add(StopKind.MidBoss);
                else
                {
                    int count = rng.Range(2, 4);
                    if (step == 0) kinds.Add(StopKind.Battle);
                    if (step == n - 2) kinds.Add(StopKind.Rest);
                    while (kinds.Count < count)
                    {
                        var k = PickKind(step == 0, kinds);
                        if (k == null) break;
                        kinds.Add(k.Value);
                    }
                }
                trail.Add(kinds.Select(k => new Stop { Kind = k, Chapter = chapter, Step = step }).ToList());
            }
            return trail;
        }

        /// A Stop kind not already in this step, by weight, or null when none is left.
        StopKind? PickKind(bool firstStep, List<StopKind> taken)
        {
            var options = new List<(StopKind kind, int weight)>
            {
                (StopKind.Battle, t.WeightBattle), (StopKind.ToughBattle, firstStep ? 0 : t.WeightTough),
                (StopKind.Rest, firstStep ? 0 : t.WeightRest), (StopKind.Shop, firstStep ? 0 : t.WeightShop),
                (StopKind.Event, t.WeightEvent), (StopKind.Wild, firstStep ? 0 : t.WeightWild),
            };
            options.RemoveAll(o => o.weight <= 0 || taken.Contains(o.kind));
            int total = options.Sum(o => o.weight);
            if (total <= 0) return null;
            int roll = rng.Range(0, total);
            foreach (var o in options)
            {
                if (roll < o.weight) return o.kind;
                roll -= o.weight;
            }
            return options[options.Count - 1].kind;
        }

        public void ChooseStop(int index)
        {
            Require(RunPhase.Trail);
            var choices = NextChoices;
            if (index < 0 || index >= choices.Count) throw new ArgumentOutOfRangeException(nameof(index));
            log.Add("S:" + index);
            position++;
            CurrentStop = choices[index];
            Message = "";
            var chapter = content.Chapters[Chapter];
            switch (CurrentStop.Kind)
            {
                case StopKind.Battle: BeginBattle(Pick(chapter.Battles)); break;
                case StopKind.ToughBattle: BeginBattle(Pick(chapter.ToughBattles.Count > 0 ? chapter.ToughBattles : chapter.Battles)); break;
                case StopKind.MidBoss: BeginBattle(chapter.MidBoss ?? Pick(chapter.ToughBattles)); break;
                case StopKind.Boss: BeginBattle(chapter.Boss); break;
                case StopKind.Rest:
                    float healed = Heal(t.RestHeal);
                    Message = $"The squad patches up the Keep (+{healed:0}).";
                    Advance();
                    break;
                case StopKind.Shop: OpenShop(); break;
                case StopKind.Event:
                    CurrentEvent = content.Events.Count > 0 ? Pick(content.Events) : null;
                    if (CurrentEvent == null) Advance();
                    else Phase = RunPhase.Event;
                    break;
                case StopKind.Wild:
                    var creature = RandomCreature();
                    if (creature != null) pendingCreatures.Enqueue(creature);
                    else { Gold += t.ConsolationGold; Message = $"The clearing is empty, but you find {t.ConsolationGold} gold."; }
                    Advance();
                    break;
            }
        }

        T Pick<T>(List<T> list) => list.Count == 0 ? default : list[rng.Range(0, list.Count)];

        // --- Battles -----------------------------------------------------------

        void BeginBattle(EncounterDef e)
        {
            encounter = e;
            scaledHeroes.Clear();
            battleSeed = (int)(rng.NextUInt() & 0x7fffffff);
            Phase = RunPhase.Battle;
        }

        /// The Battle for the current Stop. Calling it again gives the same Battle.
        public BattleSetup CreateBattle()
        {
            Require(RunPhase.Battle);
            var setup = new BattleSetup
            {
                Keeper = Keeper.Build(),
                KeepHp = KeepHp,
                KeepMaxHp = KeepMaxHp,
                Seed = battleSeed,
                Tuning = content.BattleTuning,
            };
            foreach (var f in familiars) setup.Familiars.Add(f.Build());
            foreach (var w in encounter.Waves)
            {
                var wave = new WaveDef { SpawnInterval = w.SpawnInterval };
                foreach (var e in w.Entries)
                {
                    bool boss = e.Hero != null && e.Hero.IsBoss;
                    int count = boss ? e.Count : Math.Max(1, (int)Math.Round(e.Count * t.HeroCountScale));
                    wave.Entries.Add(new WaveEntry { Hero = Scaled(e.Hero), Count = count });
                }
                setup.Waves.Add(wave);
            }
            return setup;
        }

        HeroDef Scaled(HeroDef h)
        {
            if (h == null) return null;
            if (scaledHeroes.TryGetValue(h, out var s)) return s;
            s = h.Clone();
            int step = CurrentStop != null ? CurrentStop.Step : 0;
            s.Hp = h.Hp * t.HeroHpScale * (1f + t.HeroHpPerChapter * Chapter) * (1f + t.HeroHpPerStep * step);
            scaledHeroes[h] = s;
            s.Summon = Scaled(h.Summon);
            return s;
        }

        public void FinishBattle(BattleReport report)
        {
            Require(RunPhase.Battle);
            var freed = report.FreedCreatures ?? new List<string>();
            log.Add(string.Format(CultureInfo.InvariantCulture, "B:{0}:{1:R}:{2}", report.Won ? 1 : 0, report.KeepHp, string.Join(",", freed)));
            KeepHp = Math.Max(0f, Math.Min(KeepMaxHp, report.KeepHp));
            if (!report.Won || KeepHp <= 0f)
            {
                End(false);
                return;
            }
            BattlesWon++;
            Gold += encounter.Gold;
            pendingUpgrades = CurrentStop.Kind == StopKind.Battle ? 1 : 2;
            foreach (var id in freed)
            {
                var def = content.FindFamiliar(id);
                if (def == null || InSquad(def) || pendingCreatures.Contains(def)) continue;
                pendingCreatures.Enqueue(def);
                break;
            }
            Advance();
        }

        // --- After a Stop --------------------------------------------------------

        /// Moves to whatever the finished Stop still owes (upgrade picks, a creature), then back to the Trail.
        void Advance()
        {
            while (pendingUpgrades > 0)
            {
                offer = BuildOffer(t.UpgradeOfferSize);
                if (offer.Count > 0)
                {
                    Phase = RunPhase.Upgrade;
                    return;
                }
                pendingUpgrades--;
                Gold += t.ConsolationGold;
                Message = $"Every Skill is fully grown, so you take {t.ConsolationGold} gold instead.";
            }
            while (pendingCreatures.Count > 0)
            {
                var c = pendingCreatures.Dequeue();
                if (InSquad(c)) continue;
                CreatureOffer = c;
                Phase = RunPhase.Befriend;
                return;
            }
            CreatureOffer = null;
            CurrentEvent = null;
            if (CurrentStop != null && CurrentStop.Kind == StopKind.Boss && position >= steps.Count - 1)
            {
                if (Chapter + 1 >= content.Chapters.Count) End(true);
                else StartChapter(Chapter + 1);
                return;
            }
            Phase = RunPhase.Trail;
        }

        List<UpgradeChoice> BuildOffer(int size)
        {
            var all = new List<UpgradeChoice>();
            foreach (var m in AllMembers())
                foreach (var s in m.Skills)
                    foreach (var u in s.Remaining)
                        all.Add(new UpgradeChoice { Member = m, Skill = s, Upgrade = u, MakesMaster = s.NextMakesMaster });
            for (int i = all.Count - 1; i > 0; i--)
            {
                int j = rng.Range(0, i + 1);
                (all[i], all[j]) = (all[j], all[i]);
            }
            // Prefer one offer per Skill so the choice is between Skills, not three tweaks of one.
            var picked = new List<UpgradeChoice>();
            foreach (var c in all)
                if (picked.Count < size && !picked.Exists(p => p.Skill == c.Skill)) picked.Add(c);
            foreach (var c in all)
                if (picked.Count < size && !picked.Contains(c)) picked.Add(c);
            return picked;
        }

        IEnumerable<SquadMember> AllMembers()
        {
            yield return Keeper;
            foreach (var f in familiars) yield return f;
        }

        public void PickUpgrade(int index)
        {
            Require(RunPhase.Upgrade);
            if (index < 0 || index >= offer.Count) throw new ArgumentOutOfRangeException(nameof(index));
            log.Add("U:" + index);
            Apply(offer[index]);
            pendingUpgrades--;
            Advance();
        }

        public void SkipUpgrade()
        {
            Require(RunPhase.Upgrade);
            log.Add("U:-1");
            pendingUpgrades--;
            Advance();
        }

        void Apply(UpgradeChoice c)
        {
            bool master = c.Skill.NextMakesMaster;
            c.Skill.Take(c.Upgrade);
            Message = master ? $"{c.Skill.Base.Name} became {c.Skill.Base.MasterForm.Name}!" : $"{c.Skill.Base.Name}: {c.Upgrade.Name}.";
        }

        // --- Befriending ---------------------------------------------------------

        bool InSquad(FighterDef def) => familiars.Exists(f => f.Def == def);

        FighterDef RandomCreature()
        {
            var candidates = content.Familiars.Where(f => !InSquad(f)).ToList();
            return candidates.Count == 0 ? null : candidates[rng.Range(0, candidates.Count)];
        }

        /// The offered creature joins. If the squad is full, it takes the place of the Familiar at replaceIndex.
        public void Befriend(int replaceIndex = -1)
        {
            Require(RunPhase.Befriend);
            if (SquadFull && (replaceIndex < 0 || replaceIndex >= familiars.Count))
                throw new ArgumentOutOfRangeException(nameof(replaceIndex), "The squad is full: choose a Familiar to swap out.");
            log.Add("F:" + replaceIndex);
            var member = new SquadMember(CreatureOffer);
            if (SquadFull)
            {
                Message = $"{familiars[replaceIndex].Def.Name} heads home and {CreatureOffer.Name} joins the squad.";
                familiars[replaceIndex] = member;
            }
            else
            {
                Message = $"{CreatureOffer.Name} joins the squad!";
                familiars.Add(member);
            }
            Advance();
        }

        public void PassCreature()
        {
            Require(RunPhase.Befriend);
            log.Add("P");
            Advance();
        }

        // --- Shop, rest, events --------------------------------------------------

        void OpenShop()
        {
            shop = new List<ShopItem>();
            foreach (var c in BuildOffer(t.ShopUpgrades))
                shop.Add(new ShopItem { Kind = ShopItemKind.Upgrade, Upgrade = c, Price = t.ShopUpgradePrice });
            shop.Add(new ShopItem { Kind = ShopItemKind.Repair, Price = t.ShopRepairPrice });
            Phase = RunPhase.Shop;
        }

        public bool CanBuy(int index)
        {
            if (Phase != RunPhase.Shop || index < 0 || index >= shop.Count) return false;
            var item = shop[index];
            if (item.Sold || Gold < item.Price) return false;
            if (item.Kind == ShopItemKind.Repair) return KeepHp < KeepMaxHp;
            return !item.Upgrade.Skill.Taken.Contains(item.Upgrade.Upgrade);
        }

        public bool Buy(int index)
        {
            Require(RunPhase.Shop);
            if (!CanBuy(index)) return false;
            log.Add("$:" + index);
            var item = shop[index];
            Gold -= item.Price;
            item.Sold = true;
            if (item.Kind == ShopItemKind.Repair)
            {
                float healed = Heal(t.ShopRepairAmount);
                Message = $"The Keep is repaired (+{healed:0}).";
            }
            else Apply(item.Upgrade);
            return true;
        }

        public void LeaveShop()
        {
            Require(RunPhase.Shop);
            log.Add("L");
            shop = new List<ShopItem>();
            Advance();
        }

        public bool CanChoose(int option) =>
            Phase == RunPhase.Event && option >= 0 && option < CurrentEvent.Options.Count && Gold >= CurrentEvent.Options[option].GoldCost;

        public void ChooseEventOption(int index)
        {
            Require(RunPhase.Event);
            if (!CanChoose(index)) throw new ArgumentOutOfRangeException(nameof(index));
            log.Add("E:" + index);
            var option = CurrentEvent.Options[index];
            Gold -= option.GoldCost;
            Message = option.Outcome ?? "";
            foreach (var e in option.Effects)
            {
                switch (e.Kind)
                {
                    case EventEffectKind.Gold: Gold = Math.Max(0, Gold + (int)Math.Round(e.Amount)); break;
                    case EventEffectKind.Heal: Heal(e.Amount); break;
                    case EventEffectKind.MaxHp:
                        KeepMaxHp = Math.Max(1f, KeepMaxHp + e.Amount);
                        KeepHp = Math.Max(1f, Math.Min(KeepMaxHp, KeepHp + e.Amount));
                        break;
                    case EventEffectKind.RandomUpgrade:
                        var pick = BuildOffer(1);
                        if (pick.Count > 0) Apply(pick[0]);
                        break;
                    case EventEffectKind.Creature:
                        var c = string.IsNullOrEmpty(e.CreatureId) ? RandomCreature() : content.FindFamiliar(e.CreatureId);
                        if (c != null && !InSquad(c)) pendingCreatures.Enqueue(c);
                        break;
                }
            }
            Advance();
        }

        float Heal(float amount)
        {
            float before = KeepHp;
            // Events can hurt the Keep, but never end the Run by themselves.
            KeepHp = amount < 0f ? Math.Max(Math.Min(1f, before), before + amount) : Math.Min(KeepMaxHp, before + amount);
            return KeepHp - before;
        }

        // --- End -------------------------------------------------------------------

        void End(bool won)
        {
            Phase = RunPhase.Ended;
            Result = new RunResult
            {
                Won = won,
                ChaptersCleared = won ? content.Chapters.Count : Chapter,
                BattlesWon = BattlesWon,
                ChapterReached = Chapter + 1,
                Gold = Gold,
            };
        }

        void Require(RunPhase phase)
        {
            if (Phase != phase) throw new InvalidOperationException($"The Run is at {Phase}, not {phase}.");
        }

        // --- Save and resume ---------------------------------------------------------

        /// The whole Run as text: its seed, starter and every choice so far.
        public string Save() => $"{SaveHeader}|{seed}|{starterId}\n{string.Join(";", log)}";

        public static Run Load(RunContent content, string save)
        {
            var lines = save.Split('\n');
            var head = lines[0].Split('|');
            if (head.Length < 3 || head[0] != SaveHeader) throw new FormatException("Not a Little Keep Run save.");
            var run = new Run(content, int.Parse(head[1], CultureInfo.InvariantCulture), head[2]);
            if (lines.Length < 2 || lines[1].Length == 0) return run;
            foreach (var action in lines[1].Split(';')) run.Replay(action);
            return run;
        }

        void Replay(string action)
        {
            var parts = action.Split(':');
            int Arg(int i) => int.Parse(parts[i], CultureInfo.InvariantCulture);
            switch (parts[0])
            {
                case "S": ChooseStop(Arg(1)); break;
                case "B":
                    FinishBattle(new BattleReport
                    {
                        Won = parts[1] == "1",
                        KeepHp = float.Parse(parts[2], CultureInfo.InvariantCulture),
                        FreedCreatures = parts.Length > 3 && parts[3].Length > 0 ? parts[3].Split(',').ToList() : new List<string>(),
                    });
                    break;
                case "U": if (Arg(1) < 0) SkipUpgrade(); else PickUpgrade(Arg(1)); break;
                case "F": Befriend(Arg(1)); break;
                case "P": PassCreature(); break;
                case "$": Buy(Arg(1)); break;
                case "L": LeaveShop(); break;
                case "E": ChooseEventOption(Arg(1)); break;
                default: throw new FormatException("Unknown Run action: " + action);
            }
        }
    }
}
