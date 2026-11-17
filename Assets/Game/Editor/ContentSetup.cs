using System;
using System.Collections.Generic;
using System.Linq;
using LittleKeep.Game;
using LittleKeep.Rules;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LittleKeep.Editor
{
    /// The first complete version's content, written down in one place: Skills, Keeper, Familiars, Heroes, Encounters,
    /// Chapters, events, Deeds, Keeper upgrades, Difficulty tiers and tuning, plus the Game scene.
    ///
    /// "Set Up Content" only creates what is missing, so Inspector edits are kept. "Rebuild Content From Script"
    /// writes every asset back to what this file says (asset files and their references are kept).
    public static class ContentSetup
    {
        const string Root = "Assets/Game/Content";
        const string ScenePath = "Assets/Game/Scenes/Game.unity";
        static bool overwrite;
        /// Assets created by this run. When keeping edits, only these join lists, so anything removed in the
        /// Inspector stays removed.
        static readonly HashSet<UnityEngine.Object> created = new HashSet<UnityEngine.Object>();

        [MenuItem("Little Keep/Set Up Content (keeps your edits)")]
        static void AddMissing() => Build(false);

        [MenuItem("Little Keep/Rebuild Content From Script (overwrites edits)")]
        static void Rebuild()
        {
            if (EditorUtility.DisplayDialog("Rebuild content", "Write every content asset back to what ContentSetup.cs says? Inspector edits to content will be lost.", "Rebuild", "Cancel"))
                Build(true);
        }

        public static void Build(bool overwriteExisting)
        {
            overwrite = overwriteExisting;
            created.Clear();
            foreach (var dir in new[] { "Skills", "Fighters", "Heroes", "Encounters", "Chapters", "Events", "Art" })
                System.IO.Directory.CreateDirectory($"{Root}/{dir}");
            System.IO.Directory.CreateDirectory("Assets/Game/Scenes");
            AssetDatabase.Refresh();
            RenameOldFiles();

            // --- Keeper ---------------------------------------------------------------------------------
            var starVolley = Skill("StarVolley", "Fires a fan of little stars at the Hero nearest the Keep.",
                Volley("star_volley", "Star Volley", Element.Spark, cooldown: 7f, damage: 8f, count: 5, spread: 24f, speed: 20f),
                "A wide rain of big, bright stars.", Volley("star_volley_master", "Starfall", Element.Spark, 6f, 12f, 9, 50f, 22f),
                Dmg("Bright Stars", "Stars"), Cd("Shooting Stars"), More("Star Shower", "Two more stars per volley.", 2));
            var keeper = Fighter("Keeper", "keeper", "Keeper", "You: a little dragon who keeps the Keep. Hold a finger on the lane to aim.",
                hp: 80f, attack: 6f, interval: 0.3f, shot: 18f, color: new Color(1f, 0.93f, 0.75f), skillColor: new Color(1f, 0.86f, 0.35f),
                art: "Dragon Hatchling", artScale: 1.2f, starVolley);

            // --- Familiars: four Skills each. The first is known from the start; the rest are learned in a Run. ---
            var pip = Fighter("Pip", "pip", "Pip", "A little fire spirit. Pops Heroes in a bright burst.",
                50f, 4f, 0.45f, 16f, new Color(1f, 0.62f, 0.45f), new Color(1f, 0.5f, 0.2f), "Elemental Fire 1", 1f,
                Skill("PopBurst", "A bright burst that hits every Hero around the target.",
                    Burst("pop_burst", "Pop Burst", Element.Fire, 6f, 18f, 1.6f),
                    "A huge, blooming bonfire burst.", Burst("pop_burst_master", "Bonfire Bloom", Element.Fire, 5f, 32f, 2.4f),
                    Dmg("Hot Pop", "The burst"), Cd("Quick Pop"), Wide("Big Pop", "The burst")),
                Skill("EmberDarts", "Flicks three quick embers at the Hero nearest the Keep.",
                    Volley("ember_darts", "Ember Darts", Element.Fire, 4f, 7f, 3, 12f, 24f),
                    "A crackling storm of embers.", Volley("ember_darts_master", "Ember Storm", Element.Fire, 3.5f, 10f, 7, 30f, 26f),
                    Dmg("Hotter Embers", "Embers"), Cd("Flicker"), More("Ember Spray", "Two more embers per flick.", 2)),
                Skill("SparkHop", "A spark that hops from Hero to Hero.",
                    Chain("spark_hop", "Spark Hop", Element.Spark, 6f, 11f, 3, 2.5f),
                    "A sparkler that zips through half the wave.", Chain("spark_hop_master", "Sparkler Chain", Element.Spark, 5f, 16f, 7, 3.2f),
                    Dmg("Bright Spark", "Each hop"), Cd("Quick Hop"), More("Long Hop", "Hops to two more Heroes.", 2)),
                Skill("AshBloom", "Pip glows and ash-flowers bloom around it, holding close Heroes in place.",
                    Nova("ash_bloom", "Ash Bloom", Element.Bloom, 6f, 14f, 1.9f),
                    "A whole garden of glowing flowers.", Nova("ash_bloom_master", "Cinder Garden", Element.Bloom, 5f, 24f, 2.8f),
                    Dmg("Thick Ash", "The bloom"), Cd("Quick Bloom"), Wide("Wide Bloom", "The bloom")));

            var bramble = Fighter("Bramble", "bramble", "Bramble", "A prickly little plant. Sprays a wide fan of thorns.",
                70f, 3f, 0.6f, 15f, new Color(0.55f, 0.75f, 0.45f), new Color(0.45f, 0.95f, 0.5f), "Plant Seeder", 1f,
                Skill("ThornFan", "Sprays a wide fan of thorns across the lane.",
                    Volley("thorn_fan", "Thorn Fan", Element.Bloom, 8f, 10f, 7, 60f, 16f),
                    "A storm of thorns that covers the whole lane.", Volley("thorn_fan_master", "Bramble Storm", Element.Bloom, 7f, 12f, 14, 110f, 17f),
                    Dmg("Sharp Thorns", "Thorns"), Cd("Quick Growth"), More("Thicket", "Three more thorns per fan.", 3)),
                Skill("RootSnare", "Roots burst from the ground, holding every Hero around the target.",
                    Burst("root_snare", "Root Snare", Element.Bloom, 7f, 7f, 2.4f),
                    "An old forest springs up and holds half the lane.", Burst("root_snare_master", "Old Forest", Element.Bloom, 6f, 14f, 3.4f),
                    Dmg("Gnarled Roots", "Roots"), Cd("Quick Roots"), Wide("Wide Snare", "The snare")),
                Skill("NettleSting", "Stinging nettles that jump from Hero to Hero.",
                    Chain("nettle_sting", "Nettle Sting", Element.Spark, 6f, 9f, 4, 2.2f),
                    "A crackling hedge of nettles.", Chain("nettle_sting_master", "Nettle Storm", Element.Spark, 5f, 13f, 8, 3f),
                    Dmg("Stingier", "Each sting"), Cd("Quick Sting"), More("Nettle Patch", "Jumps to two more Heroes.", 2)),
                Skill("DewDrops", "Shakes off a few cold dewdrops.",
                    Volley("dew_drops", "Dew Drops", Element.Frost, 5f, 9f, 3, 20f, 18f),
                    "A sudden hailstorm across the lane.", Volley("dew_drops_master", "Hailstorm", Element.Frost, 4.5f, 12f, 8, 45f, 20f),
                    Dmg("Heavy Dew", "Drops"), Cd("Morning Dew"), More("Downpour", "Two more drops each time.", 2)));

            var puff = Fighter("Puff", "puff", "Puff", "A grumpy mushroom. Puffs clouds of spores.",
                65f, 4f, 0.5f, 15f, new Color(0.85f, 0.55f, 0.55f), new Color(0.85f, 0.7f, 1f), "Mushroom", 1f,
                Skill("SporePuff", "A soft cloud of spores that hits every Hero around the target.",
                    Burst("spore_puff", "Spore Puff", Element.Bloom, 6.5f, 14f, 1.9f),
                    "A great drifting cloud that blankets the lane.", Burst("spore_puff_master", "Spore Cloud", Element.Bloom, 5.5f, 26f, 2.8f),
                    Dmg("Thick Spores", "Spores"), Cd("Quick Puff"), Wide("Big Puff", "The cloud")),
                Skill("ChillSpores", "A cold puff of spores that chills every Hero around the target.",
                    Burst("chill_spores", "Chill Spores", Element.Frost, 7f, 12f, 1.8f),
                    "A freezing fog of frost-capped spores.", Burst("chill_spores_master", "Frostcap Fog", Element.Frost, 6f, 22f, 2.7f),
                    Dmg("Frosty Spores", "The puff"), Cd("Quick Chill"), Wide("Wide Chill", "The puff")),
                Skill("Grump", "Puff puffs up crossly, zapping any Hero too close.",
                    Nova("grump", "Grump", Element.Spark, 5f, 15f, 1.8f),
                    "A thunderous huff that rattles everything nearby.", Nova("grump_master", "Thunder Grump", Element.Spark, 4f, 26f, 2.6f),
                    Dmg("Very Cross", "The zap"), Cd("Short Temper"), Wide("Big Huff", "The huff")),
                Skill("FireCap", "Flings one hot mushroom cap, hard.",
                    Volley("fire_cap", "Fire Cap", Element.Fire, 5f, 20f, 1, 0f, 15f),
                    "A volley of blazing caps.", Volley("fire_cap_master", "Fire Fungus", Element.Fire, 4.5f, 26f, 5, 24f, 17f),
                    Dmg("Hotter Cap", "The cap"), Cd("Quick Fling"), More("Cap Pile", "Flings two more caps.", 2)));

            var biscuit = Fighter("Biscuit", "biscuit", "Biscuit", "A knight's hound, glad of a quieter job. Bounds in with a big bark.",
                90f, 5f, 0.7f, 14f, new Color(0.85f, 0.72f, 0.55f), new Color(1f, 0.92f, 0.45f), "Canine Beagle", 1f,
                Skill("Stomp", "A huge bark that knocks every Hero nearby.",
                    Burst("bark_blast", "Bark Blast", Element.Spark, 7f, 16f, 1.4f),
                    "A howl that rattles half the lane.", Burst("bark_blast_master", "Howling Blast", Element.Spark, 6f, 30f, 2.3f),
                    Dmg("Big Bark", "Barks"), Cd("Quick Bark"), Wide("Echoing Bark", "The bark")),
                Skill("Fetch", "Throws a big stick at the Hero nearest the Keep.",
                    Volley("fetch", "Fetch!", Element.Spark, 5f, 26f, 1, 0f, 22f),
                    "Tosses whole logs.", Volley("fetch_master", "Log Toss", Element.Spark, 4.5f, 40f, 3, 16f, 22f),
                    Dmg("Heavy Stick", "The stick"), Cd("Good Dog"), More("Two Sticks", "Throws one more stick.", 1)),
                Skill("DigUp", "Digs furiously: roots and earth burst up around the target.",
                    Burst("dig_up", "Dig Up", Element.Bloom, 7f, 12f, 2f),
                    "Digs so hard the lane shakes.", Burst("dig_up_master", "Earthquake Dig", Element.Bloom, 6f, 22f, 3f),
                    Dmg("Deep Dig", "The dig"), Cd("Busy Paws"), Wide("Wide Dig", "The dig")),
                Skill("SnowShake", "Shakes snow from its coat, chilling every Hero close by.",
                    Nova("snow_shake", "Snow Shake", Element.Frost, 5.5f, 14f, 2f),
                    "A blizzard bursts from one fluffy coat.", Nova("snow_shake_master", "Blizzard Shake", Element.Frost, 4.5f, 24f, 2.9f),
                    Dmg("Cold Coat", "The shake"), Cd("Quick Shake"), Wide("Big Shake", "The shake")));

            var bun = Fighter("Bun", "bun", "Bun", "A huntsman's horned rabbit who never liked the huntsman. Quick and fierce.",
                40f, 3f, 0.3f, 18f, new Color(0.75f, 0.68f, 0.85f), new Color(0.8f, 0.7f, 1f), "Rabbit Horned", 1f,
                Skill("FeatherFlurry", "A quick flurry of sharp, lucky clovers.",
                    Volley("clover_flurry", "Clover Flurry", Element.Frost, 6f, 5f, 6, 40f, 22f),
                    "A gale of four-leaf clovers.", Volley("clover_flurry_master", "Lucky Gale", Element.Frost, 5f, 7f, 12, 90f, 24f),
                    Dmg("Four-Leaf", "Clovers"), Cd("Quick Paws"), More("Clover Patch", "Three more clovers per flurry.", 3)),
                Skill("HopStrike", "Bun bounds from Hero to Hero.",
                    Chain("hop_strike", "Hop Strike", Element.Spark, 5f, 10f, 3, 2.4f),
                    "Bun zigzags through the whole wave.", Chain("hop_strike_master", "Lightning Hops", Element.Spark, 4f, 15f, 7, 3.2f),
                    Dmg("Sharp Horn", "Each bound"), Cd("Springy Legs"), More("Big Hops", "Bounds to two more Heroes.", 2)),
                Skill("BurrowBloom", "Pops out of a burrow with flowers springing up all around.",
                    Nova("burrow_bloom", "Burrow Bloom", Element.Bloom, 6f, 13f, 1.8f),
                    "A whole meadow bursts into bloom.", Nova("burrow_bloom_master", "Meadow Burst", Element.Bloom, 5f, 22f, 2.7f),
                    Dmg("Thick Clover", "The bloom"), Cd("Quick Burrow"), Wide("Wide Meadow", "The bloom")),
                Skill("CarrotRocket", "Launches one fiery carrot.",
                    Volley("carrot_rocket", "Carrot Rocket", Element.Fire, 6f, 30f, 1, 0f, 20f),
                    "A whole barrage of fiery carrots.", Volley("carrot_rocket_master", "Carrot Barrage", Element.Fire, 5f, 34f, 5, 24f, 22f),
                    Dmg("Spicy Carrot", "The carrot"), Cd("Quick Launch"), More("Carrot Bunch", "Launches two more carrots.", 2)));

            var tide = Fighter("Tide", "tide", "Tide", "A playful water spirit who only follows those who reach the end of Meadow Road.",
                55f, 5f, 0.4f, 17f, new Color(0.45f, 0.65f, 0.95f), new Color(0.5f, 0.85f, 1f), "Elemental Water 1", 1f,
                Skill("BubbleVolley", "Blows a spray of heavy bubbles at the Hero nearest the Keep.",
                    Volley("bubble_volley", "Bubble Volley", Element.Frost, 6f, 9f, 4, 30f, 17f),
                    "A rolling wave of bubbles across half the lane.", Volley("bubble_volley_master", "Tidal Wave", Element.Frost, 5f, 11f, 10, 70f, 19f),
                    Dmg("Big Bubbles", "Bubbles"), Cd("Quick Tide"), More("Bubble Stream", "Two more bubbles per volley.", 2)),
                Skill("Ripple", "A cold ripple spreads out around Tide.",
                    Nova("ripple", "Ripple", Element.Frost, 5f, 13f, 2f),
                    "A swirling whirlpool around Tide.", Nova("ripple_master", "Whirlpool", Element.Frost, 4f, 22f, 2.9f),
                    Dmg("Strong Ripple", "The ripple"), Cd("Quick Ripple"), Wide("Wide Ripple", "The ripple")),
                Skill("StormSurge", "Lightning crackles through the spray, Hero to Hero.",
                    Chain("storm_surge", "Storm Surge", Element.Spark, 6f, 11f, 4, 2.6f),
                    "A thunderhead that strikes through the whole wave.", Chain("storm_surge_master", "Thunderhead", Element.Spark, 5f, 16f, 8, 3.4f),
                    Dmg("Charged Spray", "Each strike"), Cd("Quick Storm"), More("Wide Storm", "Strikes two more Heroes.", 2)),
                Skill("SteamGeyser", "A scalding geyser bursts up under the target.",
                    Burst("steam_geyser", "Steam Geyser", Element.Fire, 7f, 17f, 1.7f),
                    "A whole hot spring erupts.", Burst("steam_geyser_master", "Hot Spring", Element.Fire, 6f, 30f, 2.6f),
                    Dmg("Hotter Steam", "The geyser"), Cd("Quick Geyser"), Wide("Wide Geyser", "The geyser")));

            Unlock(pip, UnlockKind.Start, "");
            Unlock(bramble, UnlockKind.Start, "");
            Unlock(puff, UnlockKind.Start, "Clerics carry one in their basket, and they turn up in the wild.");
            Unlock(biscuit, UnlockKind.Bond, "Knights bring him into Battle, and he turns up in the wild.");
            Unlock(bun, UnlockKind.Bond, "Huntsmen bring her into Battle, and she turns up in the wild.");
            Unlock(tide, UnlockKind.Deed, "Deed: beat the Meadow Road boss.", "clear_meadow");

            // --- Heroes -----------------------------------------------------------------------------------
            var squire = Hero("Squire", "Eager and underpaid. Walks straight at the Keep and swings at anyone in the way.", "Knight Squire", HeroLook.Square,
                new HeroDef { Id = "squire", Name = "Squire", Hp = 38f, Speed = 1.4f, KeepDamage = 6f, Size = 0.38f, MeleeDamage = 6f, MeleeInterval = 1f, MeleeRange = 0.9f });
            var militia = Hero("Militia", "Farmhands with pitchforks, pressed into service. Weak, but they come in crowds.", "Peasant Male Black", HeroLook.Square,
                new HeroDef { Id = "militia", Name = "Militia", Hp = 24f, Speed = 1.2f, KeepDamage = 4f, Size = 0.36f, MeleeDamage = 4f, MeleeInterval = 1.2f, MeleeRange = 0.9f });
            var page = Hero("Page", "Sprints for the Keep and never stops to scrap with the squad. Catch it early.", "Peasant Male Brown", HeroLook.Square,
                new HeroDef { Id = "page", Name = "Page", Hp = 18f, Speed = 2.6f, KeepDamage = 5f, Size = 0.33f });
            var archer = Hero("Archer", "Stops at a distance and shoots at the squad.", "Knight Archer", HeroLook.Diamond,
                new HeroDef { Id = "archer", Name = "Archer", Hp = 26f, Speed = 1.5f, KeepDamage = 5f, Size = 0.35f, ShotDamage = 5f, ShotInterval = 1.4f, ShotRange = 7f, ShotSpeed = 9f });
            var shieldbearer = Hero("Shieldbearer", "Hides behind an enormous shield. Ordinary attacks barely scratch it; Skills hit in full.", "Knight Guardsman", HeroLook.Tall,
                new HeroDef { Id = "shieldbearer", Name = "Shieldbearer", Hp = 130f, Speed = 0.9f, KeepDamage = 12f, Size = 0.45f, MeleeDamage = 10f, MeleeInterval = 1.4f, MeleeRange = 0.95f, AttackBlock = 0.7f });
            var knight = Hero("Knight", "Rides in with a hound at heel. Beat the knight and the hound is free.", "Knight", HeroLook.Tall,
                new HeroDef { Id = "knight", Name = "Knight", Hp = 75f, Speed = 1.1f, KeepDamage = 10f, Size = 0.45f, MeleeDamage = 9f, MeleeInterval = 1.1f, MeleeRange = 0.95f, AttackBlock = 0.3f }, creature: biscuit);
            var huntsman = Hero("Huntsman", "Shoots from afar with a horned rabbit in tow. Beat the huntsman and the rabbit is free.", "Knight Marksman", HeroLook.Diamond,
                new HeroDef { Id = "huntsman", Name = "Huntsman", Hp = 32f, Speed = 1.4f, KeepDamage = 5f, Size = 0.36f, ShotDamage = 5f, ShotInterval = 1.3f, ShotRange = 7f, ShotSpeed = 10f }, creature: bun);
            var spearman = Hero("Spearman", "Pokes the squad from well out of reach of a hug.", "Knight Spearman", HeroLook.Tall,
                new HeroDef { Id = "spearman", Name = "Spearman", Hp = 55f, Speed = 1.2f, KeepDamage = 8f, Size = 0.4f, MeleeDamage = 8f, MeleeInterval = 1.3f, MeleeRange = 1.7f });
            var cleric = Hero("Cleric", "Mends every Hero around her, so beat her first. Carries a grumpy mushroom in her basket.", "Mage Priest", HeroLook.Diamond,
                new HeroDef { Id = "cleric", Name = "Cleric", Hp = 40f, Speed = 1.1f, KeepDamage = 5f, Size = 0.37f, HealAmount = 6f, HealInterval = 2f, HealRadius = 2.5f }, creature: puff);
            var hedgeWizard = Hero("HedgeWizard", "Lobs slow, heavy bolts from the very back.", "Mage Witch Doctor", HeroLook.Diamond,
                new HeroDef { Id = "hedge_wizard", Name = "Hedge Wizard", Hp = 34f, Speed = 1.2f, KeepDamage = 6f, Size = 0.37f, ShotDamage = 9f, ShotInterval = 2.2f, ShotRange = 8.5f, ShotSpeed = 8f });
            var monk = Hero("Monk", "So calm that burns, chills, shocks and roots slide right off. No Combos on this one.", "Mage Monk", HeroLook.Square,
                new HeroDef { Id = "monk", Name = "Monk", Hp = 70f, Speed = 1.2f, KeepDamage = 8f, Size = 0.4f, MeleeDamage = 8f, MeleeInterval = 1f, MeleeRange = 0.9f, Steadfast = true });
            var blackKnight = Hero("BlackKnight", "Heavy armour, heavier attitude. Statuses don't stick and ordinary attacks barely dent it.", "Knight Dark", HeroLook.Tall,
                new HeroDef { Id = "black_knight", Name = "Black Knight", Hp = 180f, Speed = 0.8f, KeepDamage = 16f, Size = 0.5f, MeleeDamage = 14f, MeleeInterval = 1.4f, MeleeRange = 1f, AttackBlock = 0.5f, Steadfast = true });

            var brash = Hero("CaptainBrash", "Mid-boss. A loud captain who swings first, thinks never, and keeps shouting for more militia.", "Knight Bodyguard", HeroLook.Square,
                new HeroDef { Id = "captain_brash", Name = "Captain Brash", Hp = 420f, Speed = 0.7f, KeepDamage = 25f, Size = 0.65f, MeleeDamage = 14f, MeleeInterval = 1f, MeleeRange = 1.2f, IsBoss = true, SummonInterval = 8f, SummonCount = 2 }, summon: militia);
            var shieldwall = Hero("SergeantShieldwall", "Mid-boss. An unstoppable wall of shield. Statuses don't stick.", "Knight Royal Guard", HeroLook.Tall,
                new HeroDef { Id = "sergeant_shieldwall", Name = "Sergeant Shieldwall", Hp = 520f, Speed = 0.6f, KeepDamage = 30f, Size = 0.7f, MeleeDamage = 16f, MeleeInterval = 1.4f, MeleeRange = 1.2f, AttackBlock = 0.7f, Steadfast = true, IsBoss = true });
            var vale = Hero("ArchmageVale", "Mid-boss. Never misses, never stops talking about it, and summons apprentices to listen.", "Mage Wizard", HeroLook.Diamond,
                new HeroDef { Id = "archmage_vale", Name = "Archmage Vale", Hp = 380f, Speed = 0.9f, KeepDamage = 25f, Size = 0.6f, ShotDamage = 9f, ShotInterval = 1f, ShotRange = 8.5f, ShotSpeed = 11f, IsBoss = true, SummonInterval = 9f, SummonCount = 1 }, summon: hedgeWizard);
            var pompous = Hero("SirPompous", "Boss. Brings his squires and his speeches.", "Knight General", HeroLook.Square,
                new HeroDef { Id = "sir_pompous", Name = "Sir Pompous", Hp = 950f, Speed = 0.45f, KeepDamage = 40f, Size = 0.8f, MeleeDamage = 16f, MeleeInterval = 1.2f, MeleeRange = 1.3f, IsBoss = true, SummonInterval = 7f, SummonCount = 2 }, summon: squire);
            var gleam = Hero("LadyGleam", "Boss. Polished armour, polished knights, and a glow that mends them.", "Knight Templar", HeroLook.Tall,
                new HeroDef { Id = "lady_gleam", Name = "Lady Gleam", Hp = 1050f, Speed = 0.45f, KeepDamage = 45f, Size = 0.8f, MeleeDamage = 18f, MeleeInterval = 1.2f, MeleeRange = 1.3f, AttackBlock = 0.4f, IsBoss = true, SummonInterval = 8f, SummonCount = 1, HealAmount = 12f, HealInterval = 3f, HealRadius = 3f }, summon: knight);
            var paragon = Hero("TheParagon", "Boss. The most heroic Hero there ever was, apparently.", "Knight Highborn", HeroLook.Diamond,
                new HeroDef { Id = "the_paragon", Name = "The Paragon", Hp = 1150f, Speed = 0.4f, KeepDamage = 50f, Size = 0.85f, ShotDamage = 12f, ShotInterval = 1.2f, ShotRange = 8f, ShotSpeed = 11f, IsBoss = true, SummonInterval = 9f, SummonCount = 2 }, summon: shieldbearer);

            // --- Encounters and Chapters: four Battles, two tough Battles, a mid-boss and a boss each ---------
            var meadow = Chapter("1_MeadowRoad", "Meadow Road",
                battles: new[] {
                    Encounter("meadow_1", "", 15, W(0.9f, (squire, 4)), W(0.8f, (squire, 3), (militia, 3)), W(0.7f, (squire, 4), (archer, 1))),
                    Encounter("meadow_2", "", 15, W(0.9f, (squire, 3), (archer, 2)), W(0.8f, (page, 3), (squire, 3)), W(0.7f, (militia, 4), (archer, 2))),
                    Encounter("meadow_3", "", 15, W(0.8f, (militia, 5)), W(0.8f, (knight, 1), (squire, 3), (archer, 1)), W(0.7f, (page, 3), (squire, 3))),
                    Encounter("meadow_4", "", 15, W(0.8f, (squire, 3), (page, 2)), W(0.7f, (huntsman, 1), (militia, 4)), W(0.7f, (squire, 4), (militia, 3))) },
                tough: new[] {
                    Encounter("meadow_tough_1", "", 30, W(0.8f, (squire, 5), (archer, 2)), W(0.7f, (shieldbearer, 1), (knight, 1), (squire, 4)), W(0.6f, (knight, 1), (page, 3), (militia, 3))),
                    Encounter("meadow_tough_2", "", 30, W(0.8f, (knight, 2), (militia, 4)), W(0.7f, (huntsman, 2), (page, 3), (squire, 2)), W(0.6f, (shieldbearer, 1), (archer, 2), (squire, 3))) },
                mid: Encounter("meadow_mid", "Captain Brash", 35, W(0.8f, (squire, 4)), W(1f, (brash, 1), (militia, 4))),
                boss: Encounter("meadow_boss", "Sir Pompous", 50, W(0.8f, (squire, 4), (archer, 2)), W(1f, (pompous, 1), (page, 2))));
            var woods = Chapter("2_WhisperingWoods", "Whispering Woods",
                battles: new[] {
                    Encounter("woods_1", "", 18, W(0.8f, (squire, 4), (spearman, 2)), W(0.7f, (archer, 2), (militia, 4)), W(0.7f, (cleric, 1), (squire, 4), (page, 2))),
                    Encounter("woods_2", "", 18, W(0.8f, (knight, 2), (squire, 3)), W(0.7f, (cleric, 1), (squire, 4), (page, 2)), W(0.7f, (spearman, 2), (archer, 2), (militia, 3))),
                    Encounter("woods_3", "", 18, W(0.8f, (spearman, 3), (archer, 2)), W(0.7f, (shieldbearer, 2), (cleric, 1), (squire, 2)), W(0.7f, (huntsman, 1), (militia, 5))),
                    Encounter("woods_4", "", 18, W(0.8f, (hedgeWizard, 2), (militia, 5)), W(0.7f, (huntsman, 1), (spearman, 2), (page, 3)), W(0.7f, (knight, 1), (squire, 4), (cleric, 1))) },
                tough: new[] {
                    Encounter("woods_tough_1", "", 35, W(0.7f, (shieldbearer, 2), (cleric, 1), (squire, 4)), W(0.6f, (knight, 2), (hedgeWizard, 2), (spearman, 2)), W(0.6f, (huntsman, 2), (page, 4), (militia, 3))),
                    Encounter("woods_tough_2", "", 35, W(0.7f, (huntsman, 2), (archer, 2), (militia, 4)), W(0.6f, (spearman, 3), (cleric, 2), (page, 3)), W(0.6f, (shieldbearer, 2), (hedgeWizard, 2), (squire, 3))) },
                mid: Encounter("woods_mid", "Sergeant Shieldwall", 40, W(0.8f, (squire, 4), (archer, 2)), W(1f, (shieldwall, 1), (cleric, 1), (spearman, 2))),
                boss: Encounter("woods_boss", "Lady Gleam", 55, W(0.7f, (knight, 2), (spearman, 3)), W(1f, (gleam, 1), (archer, 2))));
            var hill = Chapter("3_CobbleHill", "Cobble Hill",
                battles: new[] {
                    Encounter("hill_1", "", 20, W(0.7f, (squire, 5), (hedgeWizard, 2)), W(0.6f, (monk, 2), (huntsman, 1), (militia, 4)), W(0.6f, (knight, 2), (cleric, 1), (page, 3))),
                    Encounter("hill_2", "", 20, W(0.7f, (knight, 2), (spearman, 2)), W(0.6f, (page, 4), (shieldbearer, 1), (cleric, 1)), W(0.6f, (monk, 2), (hedgeWizard, 2), (militia, 4))),
                    Encounter("hill_3", "", 20, W(0.7f, (archer, 3), (monk, 2)), W(0.6f, (blackKnight, 1), (squire, 5)), W(0.6f, (spearman, 3), (huntsman, 1), (cleric, 1))),
                    Encounter("hill_4", "", 20, W(0.7f, (militia, 6), (hedgeWizard, 1)), W(0.6f, (spearman, 3), (cleric, 1), (knight, 1)), W(0.6f, (blackKnight, 1), (archer, 3), (page, 3))) },
                tough: new[] {
                    Encounter("hill_tough_1", "", 40, W(0.6f, (shieldbearer, 2), (hedgeWizard, 2), (squire, 4)), W(0.5f, (blackKnight, 1), (monk, 2), (cleric, 1), (squire, 3)), W(0.5f, (knight, 2), (spearman, 3), (cleric, 1))),
                    Encounter("hill_tough_2", "", 40, W(0.6f, (huntsman, 2), (knight, 2), (page, 3)), W(0.5f, (blackKnight, 2), (archer, 3)), W(0.5f, (monk, 3), (hedgeWizard, 2), (militia, 5))) },
                mid: Encounter("hill_mid", "Archmage Vale", 45, W(0.7f, (squire, 4), (monk, 1)), W(1f, (vale, 1), (spearman, 2), (cleric, 1))),
                boss: Encounter("hill_boss", "The Paragon", 60, W(0.7f, (knight, 2), (hedgeWizard, 3)), W(1f, (paragon, 1), (monk, 2))));

            // --- Events: every one has a free choice --------------------------------------------------------
            var events = new List<EventAsset>
            {
                Event("LostLamb", "A lost lamb", "A small, muddy lamb bleats at the edge of the Trail. It looks very lost.",
                    Opt("Walk it home", "The farmer is so grateful she patches up the Keep's gate.", 0, Fx(EventEffectKind.Heal, 15f)),
                    Opt("Wave goodbye and keep going", "", 0)),
                Event("WishingWell", "A wishing well", "An old stone well, its bottom glittering with Gold.",
                    Opt("Toss in some Gold and make a wish", "Something about the squad feels a little stronger.", 15, Fx(EventEffectKind.RandomUpgrade, 1f)),
                    Opt("Walk on", "", 0)),
                Event("HerosPicnic", "A Hero's picnic", "A picnic blanket, abandoned mid-sandwich. A purse of Gold sits by the basket.",
                    Opt("Help yourselves", "Crumbs and Gold. Not bad.", 0, Fx(EventEffectKind.Gold, 25f)),
                    Opt("Leave it be", "", 0)),
                Event("MossyShrine", "A mossy shrine", "A tiny shrine, half swallowed by moss. It hums warmly.",
                    Opt("Leave an offering", "The Keep's walls feel sturdier.", 20, Fx(EventEffectKind.MaxHp, 15f)),
                    Opt("Bow politely and move on", "", 0)),
                Event("Thicket", "Rustling in a thicket", "Two bright eyes peer out of a thicket.",
                    Opt("Coax it out", "", 0, Fx(EventEffectKind.Creature, 0f)),
                    Opt("Leave it in peace", "", 0)),
                Event("OldForge", "An abandoned forge", "A forge with the coals still warm. Tinkering might help, or singe something.",
                    Opt("Tinker", "Singed whiskers, but something got better.", 0, Fx(EventEffectKind.RandomUpgrade, 1f), Fx(EventEffectKind.Heal, -10f)),
                    Opt("Better not", "", 0)),
                Event("Tinker", "A travelling tinker", "A tinker with a cart full of clanking gadgets offers to tune up the squad.",
                    Opt("Pay for a tune-up", "Whirr, clank, ping. Much better.", 25, Fx(EventEffectKind.RandomUpgrade, 1f)),
                    Opt("No thank you", "", 0)),
                Event("LostBanner", "A Hero's lost banner", "A grand banner lies in the mud, embroidered with someone's very long name.",
                    Opt("Return it for the reward", "A squire hands over a small purse, red in the face.", 0, Fx(EventEffectKind.Gold, 20f)),
                    Opt("Use it to patch the Keep", "It makes a very fancy curtain.", 0, Fx(EventEffectKind.Heal, 12f))),
                Event("BridgeTroll", "A sleepy bridge troll", "A troll snoozes across the only bridge. A sign reads: TOLL, 10 GOLD.",
                    Opt("Pay the toll", "The troll waves you over without opening its eyes.", 10),
                    Opt("Tiptoe past", "It snores, rolls over and squashes the cart. The Keep takes a knock.", 0, Fx(EventEffectKind.Heal, -8f))),
                Event("MushroomRing", "A mushroom ring", "A perfect ring of mushrooms. Puff looks very interested.",
                    Opt("Dance in the ring", "Dizzy, but the Keep feels oddly sturdier.", 0, Fx(EventEffectKind.MaxHp, 10f), Fx(EventEffectKind.Heal, -5f)),
                    Opt("Step around it", "", 0)),
                Event("SupplyCart", "A tipped-over supply cart", "A Hero supply cart lies on its side, wheels still spinning.",
                    Opt("Take the Gold", "Heavy, but worth it. Somebody's armour gets in the way, though.", 0, Fx(EventEffectKind.Gold, 30f), Fx(EventEffectKind.Heal, -5f)),
                    Opt("Take the bandages", "Clean bandages: the Keep's dents get patched.", 0, Fx(EventEffectKind.Heal, 15f))),
                Event("TrainingDummy", "A training dummy", "A straw dummy wearing a tin hat, set up by Heroes for practice.",
                    Opt("Practise on it", "The squad learns a trick or two.", 0, Fx(EventEffectKind.RandomUpgrade, 1f), Fx(EventEffectKind.Heal, -6f)),
                    Opt("Leave it standing", "", 0)),
                Event("BerryBush", "A berry bush", "A bush heavy with fat, shiny berries.",
                    Opt("Eat your fill", "Full tummies all round. The squad patches the Keep happily.", 0, Fx(EventEffectKind.Heal, 20f)),
                    Opt("Pick some to sell", "A basket of berries fetches a few Gold.", 0, Fx(EventEffectKind.Gold, 12f))),
                Event("AbandonedNest", "An abandoned nest", "A nest in the roots of an old oak. Something small is still curled up inside.",
                    Opt("Peek inside", "", 0, Fx(EventEffectKind.Creature, 0f)),
                    Opt("Let it sleep", "", 0)),
            };

            // --- Game content -------------------------------------------------------------------------------
            var game = AssetDatabase.LoadAssetAtPath<GameContent>($"{Root}/GameContent.asset");
            bool newGame = game == null;
            if (newGame) game = ScriptableObject.CreateInstance<GameContent>();
            game.keeper = keeper;
            SetList(game.familiars, pip, bramble, puff, biscuit, bun, tide);
            SetList(game.heroes, squire, militia, page, archer, shieldbearer, knight, huntsman, spearman, cleric, hedgeWizard, monk, blackKnight,
                brash, shieldwall, vale, pompous, gleam, paragon);
            SetList(game.chapters, meadow, woods, hill);
            SetList(game.events, events.ToArray());
            if (newGame || overwrite)
            {
                game.deeds = new List<DeedDef> { new DeedDef { Id = "clear_meadow", Name = "Clear Meadow Road", Description = "Beat the Meadow Road boss in any Run.", Kind = DeedKind.ClearChapters, Target = 1 } };
                game.keeperUpgrades = new List<KeeperUpgradeDef>
                {
                    new KeeperUpgradeDef { Id = "walls", Name = "Sturdy Walls", Description = "+10 Keep health per level.", Stat = KeeperStat.KeepMaxHp, PerLevel = 10f, Costs = { 30, 60, 100, 150, 220 } },
                    new KeeperUpgradeDef { Id = "claws", Name = "Sharp Claws", Description = "The Keeper hits 8% harder per level.", Stat = KeeperStat.KeeperDamage, PerLevel = 0.08f, Costs = { 40, 80, 130, 190 } },
                    new KeeperUpgradeDef { Id = "purse", Name = "Gold Purse", Description = "Start each Run with 15 more Gold per level.", Stat = KeeperStat.StartGold, PerLevel = 15f, Costs = { 25, 50, 90 } },
                    new KeeperUpgradeDef { Id = "horn", Name = "Rally Horn", Description = "Rally recharges 8% faster per level.", Stat = KeeperStat.RallyCooldown, PerLevel = -0.08f, Costs = { 50, 100, 160 } },
                };
                game.tiers = new List<DifficultyDef>
                {
                    new DifficultyDef { Name = "Cozy", Description = "A gentle march of Heroes.", HeroHp = 0.95f, HeroCount = 1f, Reward = 1f },
                    new DifficultyDef { Name = "Brisk", Description = "Tougher, more numerous Heroes. 50% more Gems.", HeroHp = 1.15f, HeroCount = 1.1f, Reward = 1.5f },
                    new DifficultyDef { Name = "Fierce", Description = "The Heroes mean it. Double Gems.", HeroHp = 1.4f, HeroCount = 1.2f, Reward = 2f },
                };
                game.rewards = new RewardTuning { PerBattle = 3, PerChapter = 10, ForWin = 25, Minimum = 5 };
                game.runTuning = new RunTuning
                {
                    StepsPerChapter = 8, MidBossStep = 3, KeepMaxHp = 100f, RestHeal = 30f, UpgradeOfferSize = 3, MaxFamiliars = 4,
                    ShopUpgradePrice = 40, ShopRepairPrice = 25, ShopRepairAmount = 30f, ShopUpgrades = 3, ConsolationGold = 15,
                    HeroHpScale = 2.05f, HeroCountScale = 1.8f, HeroHpPerChapter = 0.25f, HeroHpPerStep = 0.06f, // see docs/balance.md
                    WeightBattle = 40, WeightTough = 12, WeightRest = 10, WeightShop = 12, WeightEvent = 16, WeightWild = 10,
                };
                game.battleTuning = new BattleTuning
                {
                    StatusSeconds = 3f, RootSeconds = 1.5f, BossRootFactor = 0.5f, BurnFraction = 0.5f, BurnTickSeconds = 0.5f,
                    ChillSlow = 0.4f, ShockBonus = 0.2f, ComboMultiplier = 1.25f, ComboRadius = 1.5f,
                };
            }
            if (newGame) AssetDatabase.CreateAsset(game, $"{Root}/GameContent.asset");
            EditorUtility.SetDirty(game);
            AssetDatabase.SaveAssets();

            ArtSetup.Apply();
            BuildScene(game);
            Validate(game);
            Debug.Log($"Little Keep: content {(overwrite ? "rebuilt from script" : "set up (missing pieces only)")}.");
        }

        /// Files whose names no longer match who they are (milestone 2 names), renamed in place so references hold.
        /// Their ids change too (pony to biscuit, owl to bun, falconer to huntsman) once rebuilt: progress saved
        /// under the old ids is dropped when read, which is fine before release.
        static void RenameOldFiles()
        {
            Move("Fighters/Pony", "Fighters/Biscuit");
            Move("Fighters/Owl", "Fighters/Bun");
            Move("Heroes/Falconer", "Heroes/Huntsman");
            Move("Heroes/MasterArcherVale", "Heroes/ArchmageVale");
        }

        static void Move(string from, string to)
        {
            string a = $"{Root}/{from}.asset", b = $"{Root}/{to}.asset";
            if (AssetDatabase.LoadMainAssetAtPath(a) != null && AssetDatabase.LoadMainAssetAtPath(b) == null)
            {
                string error = AssetDatabase.MoveAsset(a, b);
                if (!string.IsNullOrEmpty(error)) Debug.LogWarning(error);
            }
        }

        static void Validate(GameContent game)
        {
            var compiled = new CompiledContent(game);
            foreach (var f in game.familiars)
            {
                if (f.skills.Count < 4) Debug.LogWarning($"{f.def.Name} has only {f.skills.Count} Skills.");
                foreach (var s in f.skills)
                {
                    if (s.def.Element == Element.None) Debug.LogWarning($"Skill {s.def.Name} has no Element.");
                    if (!s.hasMasterForm || s.masterForm == null) Debug.LogWarning($"Skill {s.def.Name} has no Master Form.");
                }
            }
            foreach (var h in game.heroes)
                if (h.art == null) Debug.LogWarning($"Hero {h.def.Name} has no art.");
            Debug.Log($"Little Keep content: {game.familiars.Count} Familiars, {game.familiars.Sum(f => f.skills.Count)} Familiar Skills, " +
                      $"{game.heroes.Count(h => !h.def.IsBoss)} Heroes, {game.heroes.Count(h => h.def.IsBoss)} bosses, {game.events.Count} events. Fingerprint {compiled.Run.Fingerprint()}.");
        }

        static void BuildScene(GameContent game)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)) { tag = "MainCamera" };
                cam.transform.position = new Vector3(7f, 0f, -10f);
                cam.GetComponent<Camera>().orthographic = true;
                var runner = new GameObject("Game").AddComponent<GameRunner>();
                var so = new SerializedObject(runner);
                so.FindProperty("content").objectReferenceValue = game;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        // --- Skill shapes -----------------------------------------------------------------------------------

        static SkillDef Volley(string id, string name, Element element, float cooldown, float damage, int count, float spread, float speed) =>
            new SkillDef { Id = id, Name = name, Shape = SkillShape.Volley, Element = element, Cooldown = cooldown, Damage = damage, Count = count, Spread = spread, ProjectileSpeed = speed };

        static SkillDef Burst(string id, string name, Element element, float cooldown, float damage, float radius) =>
            new SkillDef { Id = id, Name = name, Shape = SkillShape.Burst, Element = element, Cooldown = cooldown, Damage = damage, Radius = radius };

        static SkillDef Chain(string id, string name, Element element, float cooldown, float damage, int count, float reach) =>
            new SkillDef { Id = id, Name = name, Shape = SkillShape.Chain, Element = element, Cooldown = cooldown, Damage = damage, Count = count, Radius = reach };

        static SkillDef Nova(string id, string name, Element element, float cooldown, float damage, float radius) =>
            new SkillDef { Id = id, Name = name, Shape = SkillShape.Nova, Element = element, Cooldown = cooldown, Damage = damage, Radius = radius };

        // --- Skill Upgrades: the same three kinds of growth, named for each Skill -----------------------------

        static SkillUpgradeDef Dmg(string name, string what) =>
            new SkillUpgradeDef { Name = name, Description = $"{what} hit 35% harder.", Mods = { new SkillMod(SkillStat.Damage, ModOp.Multiply, 1.35f) } };

        static SkillUpgradeDef Cd(string name) =>
            new SkillUpgradeDef { Name = name, Description = "Recharges 20% faster.", Mods = { new SkillMod(SkillStat.Cooldown, ModOp.Multiply, 0.8f) } };

        static SkillUpgradeDef More(string name, string description, int extra) =>
            new SkillUpgradeDef { Name = name, Description = description, Mods = { new SkillMod(SkillStat.Count, ModOp.Add, extra) } };

        static SkillUpgradeDef Wide(string name, string what) =>
            new SkillUpgradeDef { Name = name, Description = $"{what} reaches 30% wider.", Mods = { new SkillMod(SkillStat.Radius, ModOp.Multiply, 1.3f) } };

        // --- Assets ---------------------------------------------------------------------------------------

        /// Loads the asset; creates it when missing; fills it when new or when rebuilding.
        static T Asset<T>(string path, Action<T> fill) where T : ScriptableObject
        {
            string full = $"{Root}/{path}.asset";
            var a = AssetDatabase.LoadAssetAtPath<T>(full);
            bool isNew = a == null;
            if (isNew) a = ScriptableObject.CreateInstance<T>();
            if (isNew || overwrite)
            {
                fill(a);
                EditorUtility.SetDirty(a);
            }
            if (isNew)
            {
                AssetDatabase.CreateAsset(a, full);
                created.Add(a);
            }
            return a;
        }

        static SkillAsset Skill(string file, string description, SkillDef def, string masterDescription, SkillDef master, params SkillUpgradeDef[] upgrades) =>
            Asset<SkillAsset>("Skills/" + file, a =>
            {
                foreach (var u in upgrades) u.Id = def.Id + "_" + Slug(u.Name);
                a.def = def;
                a.description = description;
                a.upgrades = new List<SkillUpgradeDef>(upgrades);
                a.hasMasterForm = master != null;
                a.masterForm = master;
                a.masterDescription = masterDescription;
            });

        static string Slug(string s) => new string(s.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray()).Trim('_');

        static FighterAsset Fighter(string file, string id, string name, string description, float hp, float attack, float interval, float shot,
            Color color, Color skillColor, string art, float artScale, params SkillAsset[] skills)
        {
            var a = Asset<FighterAsset>("Fighters/" + file, f =>
            {
                f.def = new FighterDef { Id = id, Name = name, Hp = hp, AttackDamage = attack, AttackInterval = interval, ProjectileSpeed = shot };
                f.description = description;
                f.color = color;
                f.skillColor = skillColor;
                f.skills = new List<SkillAsset>(skills);
                f.art = ArtSetup.Art(art, artScale);
            });
            // Skills added since a Familiar was first made join its list (in order), even when keeping edits.
            bool changed = false;
            foreach (var s in skills)
                if (Joins(s) && !a.skills.Contains(s)) { a.skills.Add(s); changed = true; }
            if (a.art == null) { a.art = ArtSetup.Art(art, artScale); changed = true; }
            if (changed) EditorUtility.SetDirty(a);
            return a;
        }

        static void Unlock(FighterAsset a, UnlockKind kind, string hint, string deed = "")
        {
            // When keeping edits, only fill in a Familiar whose unlock was never set.
            bool untouched = a.unlock == UnlockKind.Start && string.IsNullOrEmpty(a.unlockHint) && string.IsNullOrEmpty(a.deedId);
            if (!overwrite && !untouched) return;
            a.unlock = kind;
            a.bondSize = 3;
            a.deedId = deed;
            a.unlockHint = hint;
            EditorUtility.SetDirty(a);
        }

        static HeroAsset Hero(string file, string description, string art, HeroLook look, HeroDef def, FighterAsset creature = null, HeroAsset summon = null)
        {
            var a = Asset<HeroAsset>("Heroes/" + file, h =>
            {
                h.def = def;
                h.description = description;
                h.look = look;
                h.creature = creature;
                h.summon = summon;
                h.art = ArtSetup.Art(art, 1f);
            });
            if (a.art == null) { a.art = ArtSetup.Art(art, 1f); EditorUtility.SetDirty(a); }
            return a;
        }

        static WaveAsset W(float interval, params (HeroAsset hero, int count)[] entries)
        {
            var w = new WaveAsset { spawnInterval = interval };
            foreach (var (hero, count) in entries) w.entries.Add(new WaveEntryAsset { hero = hero, count = count });
            return w;
        }

        static EncounterAsset Encounter(string id, string name, int gold, params WaveAsset[] waves) =>
            Asset<EncounterAsset>("Encounters/" + id, a =>
            {
                a.id = id;
                a.displayName = name;
                a.gold = gold;
                a.waves = new List<WaveAsset>(waves);
            });

        static ChapterAsset Chapter(string file, string name, EncounterAsset[] battles, EncounterAsset[] tough, EncounterAsset mid, EncounterAsset boss)
        {
            var a = Asset<ChapterAsset>("Chapters/" + file, c =>
            {
                c.displayName = name;
                c.battles = new List<EncounterAsset>(battles);
                c.toughBattles = new List<EncounterAsset>(tough);
                c.midBoss = mid;
                c.boss = boss;
            });
            bool changed = false;
            foreach (var e in battles) if (Joins(e) && !a.battles.Contains(e)) { a.battles.Add(e); changed = true; }
            foreach (var e in tough) if (Joins(e) && !a.toughBattles.Contains(e)) { a.toughBattles.Add(e); changed = true; }
            if (changed) EditorUtility.SetDirty(a);
            return a;
        }

        static EventEffect Fx(EventEffectKind kind, float amount) => new EventEffect { Kind = kind, Amount = amount };

        static EventOption Opt(string text, string outcome, int cost, params EventEffect[] effects) =>
            new EventOption { Text = text, Outcome = outcome, GoldCost = cost, Effects = new List<EventEffect>(effects) };

        static EventAsset Event(string file, string title, string text, params EventOption[] options) =>
            Asset<EventAsset>("Events/" + file, a => a.def = new EventDef { Id = file, Title = title, Text = text, Options = new List<EventOption>(options) });

        /// Rebuilding sets the list to exactly these; otherwise only newly created assets are added.
        static void SetList<T>(List<T> list, params T[] items) where T : UnityEngine.Object
        {
            if (overwrite) list.Clear();
            foreach (var item in items) if (Joins(item) && !list.Contains(item)) list.Add(item);
        }

        static bool Joins(UnityEngine.Object item) => overwrite || created.Contains(item);
    }
}
