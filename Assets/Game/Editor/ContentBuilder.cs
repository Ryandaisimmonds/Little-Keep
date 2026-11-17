using System.Collections.Generic;
using LittleKeep.Game;
using LittleKeep.Rules;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LittleKeep.Editor
{
    /// Creates the game's starting content and the Game scene. Safe to re-run: anything that already exists is kept
    /// as it is, so Inspector edits survive; only missing assets are made.
    public static class ContentBuilder
    {
        const string Root = "Assets/Game/Content";
        const string ScenePath = "Assets/Game/Scenes/Game.unity";

        [MenuItem("Little Keep/Create Missing Content and Scene")]
        public static void Build()
        {
            foreach (var dir in new[] { "Skills", "Fighters", "Heroes", "Encounters", "Chapters", "Events" })
                System.IO.Directory.CreateDirectory($"{Root}/{dir}");
            System.IO.Directory.CreateDirectory("Assets/Game/Scenes");
            AssetDatabase.Refresh();

            // --- Skills: each with three upgrades and a Master Form ---------------------------------
            var starVolley = Skill("StarVolley", "Fires a fan of little stars at the Hero nearest the Keep.",
                new SkillDef { Id = "star_volley", Name = "Star Volley", Shape = SkillShape.Volley, Cooldown = 7f, Damage = 8f, Count = 5, Spread = 24f, ProjectileSpeed = 20f },
                new[] { Up("star_volley_bright", "Bright Stars", "Stars hit 35% harder.", SkillStat.Damage, ModOp.Multiply, 1.35f),
                        Up("star_volley_quick", "Shooting Stars", "Recharges 20% faster.", SkillStat.Cooldown, ModOp.Multiply, 0.8f),
                        Up("star_volley_more", "Star Shower", "Two more stars per volley.", SkillStat.Count, ModOp.Add, 2f) },
                new SkillDef { Id = "star_volley_master", Name = "Starfall", Shape = SkillShape.Volley, Cooldown = 6f, Damage = 12f, Count = 9, Spread = 50f, ProjectileSpeed = 22f },
                "A wide rain of big, bright stars.");
            var popBurst = Skill("PopBurst", "A bright burst that hits every Hero around the target.",
                new SkillDef { Id = "pop_burst", Name = "Pop Burst", Shape = SkillShape.Burst, Cooldown = 6f, Damage = 18f, Radius = 1.6f },
                new[] { Up("pop_burst_hot", "Hot Pop", "The burst hits 35% harder.", SkillStat.Damage, ModOp.Multiply, 1.35f),
                        Up("pop_burst_quick", "Quick Pop", "Recharges 20% faster.", SkillStat.Cooldown, ModOp.Multiply, 0.8f),
                        Up("pop_burst_big", "Big Pop", "The burst is 30% wider.", SkillStat.Radius, ModOp.Multiply, 1.3f) },
                new SkillDef { Id = "pop_burst_master", Name = "Bonfire Bloom", Shape = SkillShape.Burst, Cooldown = 5f, Damage = 32f, Radius = 2.4f },
                "A huge, blooming bonfire burst.");
            var thornFan = Skill("ThornFan", "Sprays a wide fan of thorns across the lane.",
                new SkillDef { Id = "thorn_fan", Name = "Thorn Fan", Shape = SkillShape.Volley, Cooldown = 8f, Damage = 10f, Count = 7, Spread = 60f, ProjectileSpeed = 16f },
                new[] { Up("thorn_fan_sharp", "Sharp Thorns", "Thorns hit 35% harder.", SkillStat.Damage, ModOp.Multiply, 1.35f),
                        Up("thorn_fan_quick", "Quick Growth", "Recharges 20% faster.", SkillStat.Cooldown, ModOp.Multiply, 0.8f),
                        Up("thorn_fan_thicket", "Thicket", "Three more thorns per fan.", SkillStat.Count, ModOp.Add, 3f) },
                new SkillDef { Id = "thorn_fan_master", Name = "Bramble Storm", Shape = SkillShape.Volley, Cooldown = 7f, Damage = 12f, Count = 14, Spread = 110f, ProjectileSpeed = 17f },
                "A storm of thorns that covers the whole lane.");
            var stomp = Skill("Stomp", "Stamps down hard, shaking every Hero nearby.",
                new SkillDef { Id = "stomp", Name = "Stomp", Shape = SkillShape.Burst, Cooldown = 7f, Damage = 16f, Radius = 1.4f },
                new[] { Up("stomp_heavy", "Heavy Hooves", "Stomps hit 35% harder.", SkillStat.Damage, ModOp.Multiply, 1.35f),
                        Up("stomp_trot", "Trot", "Recharges 20% faster.", SkillStat.Cooldown, ModOp.Multiply, 0.8f),
                        Up("stomp_big", "Big Stomp", "The stomp reaches 30% wider.", SkillStat.Radius, ModOp.Multiply, 1.3f) },
                new SkillDef { Id = "stomp_master", Name = "Thunder Hooves", Shape = SkillShape.Burst, Cooldown = 6f, Damage = 30f, Radius = 2.3f },
                "A thundering stomp that rattles half the lane.");
            var flurry = Skill("FeatherFlurry", "A quick flurry of sharp feathers.",
                new SkillDef { Id = "feather_flurry", Name = "Feather Flurry", Shape = SkillShape.Volley, Cooldown = 6f, Damage = 5f, Count = 6, Spread = 40f, ProjectileSpeed = 22f },
                new[] { Up("feather_flurry_sharp", "Sharp Quills", "Feathers hit 40% harder.", SkillStat.Damage, ModOp.Multiply, 1.4f),
                        Up("feather_flurry_swift", "Swift Wings", "Recharges 20% faster.", SkillStat.Cooldown, ModOp.Multiply, 0.8f),
                        Up("feather_flurry_more", "Flurry", "Three more feathers per flurry.", SkillStat.Count, ModOp.Add, 3f) },
                new SkillDef { Id = "feather_flurry_master", Name = "Night Gale", Shape = SkillShape.Volley, Cooldown = 5f, Damage = 7f, Count = 12, Spread = 90f, ProjectileSpeed = 24f },
                "A gale of midnight feathers.");

            // --- Keeper and Familiars ------------------------------------------------------------
            var keeper = Fighter("Keeper", "Your own character on the battlefield. Hold a finger on the lane to aim.",
                new Color(1f, 0.93f, 0.75f), new Color(1f, 0.86f, 0.35f),
                new FighterDef { Id = "keeper", Name = "Keeper", Hp = 80f, AttackDamage = 6f, AttackInterval = 0.3f, ProjectileSpeed = 18f }, starVolley);
            var pip = Fighter("Pip", "A little ember sprite. Pops Heroes in a bright burst.",
                new Color(1f, 0.62f, 0.45f), new Color(1f, 0.5f, 0.2f),
                new FighterDef { Id = "pip", Name = "Pip", Hp = 50f, AttackDamage = 4f, AttackInterval = 0.45f, ProjectileSpeed = 16f }, popBurst);
            var bramble = Fighter("Bramble", "A mossy hedgehog. Sprays a wide fan of thorns.",
                new Color(0.55f, 0.75f, 0.45f), new Color(0.45f, 0.95f, 0.5f),
                new FighterDef { Id = "bramble", Name = "Bramble", Hp = 70f, AttackDamage = 3f, AttackInterval = 0.6f, ProjectileSpeed = 15f }, thornFan);
            var pony = Fighter("Pony", "A knight's overworked pony, glad of a quieter job. Sturdy and stompy.",
                new Color(0.85f, 0.72f, 0.55f), new Color(1f, 0.92f, 0.45f),
                new FighterDef { Id = "pony", Name = "Pony", Hp = 90f, AttackDamage = 5f, AttackInterval = 0.7f, ProjectileSpeed = 14f }, stomp);
            var owl = Fighter("Owl", "A falconer's owl who never liked the falconer. Quick and sharp-eyed.",
                new Color(0.75f, 0.68f, 0.85f), new Color(0.8f, 0.7f, 1f),
                new FighterDef { Id = "owl", Name = "Owl", Hp = 40f, AttackDamage = 3f, AttackInterval = 0.3f, ProjectileSpeed = 18f }, flurry);

            // --- Heroes ---------------------------------------------------------------------------
            var squire = Hero("Squire", "Eager and underpaid. Walks straight at the Keep and swings at anyone in the way.", new Color(0.93f, 0.80f, 0.55f), HeroLook.Square,
                new HeroDef { Id = "squire", Name = "Squire", Hp = 38f, Speed = 1.4f, KeepDamage = 6f, Size = 0.38f, MeleeDamage = 6f, MeleeInterval = 1f, MeleeRange = 0.9f });
            var shieldbearer = Hero("Shieldbearer", "Hides behind an enormous shield. Ordinary attacks barely scratch it; Skills hit in full.", new Color(0.55f, 0.65f, 0.85f), HeroLook.Tall,
                new HeroDef { Id = "shieldbearer", Name = "Shieldbearer", Hp = 130f, Speed = 0.9f, KeepDamage = 12f, Size = 0.45f, MeleeDamage = 10f, MeleeInterval = 1.4f, MeleeRange = 0.95f, AttackBlock = 0.7f });
            var archer = Hero("Archer", "Stops at a distance and shoots at the squad.", new Color(0.60f, 0.80f, 0.55f), HeroLook.Diamond,
                new HeroDef { Id = "archer", Name = "Archer", Hp = 26f, Speed = 1.5f, KeepDamage = 5f, Size = 0.35f, ShotDamage = 5f, ShotInterval = 1.4f, ShotRange = 7f, ShotSpeed = 9f });
            var knight = Hero("Knight", "Rides in on a tired pony. Beat the knight and the pony is free.", new Color(0.85f, 0.60f, 0.55f), HeroLook.Tall,
                new HeroDef { Id = "knight", Name = "Knight", Hp = 75f, Speed = 1.1f, KeepDamage = 10f, Size = 0.45f, MeleeDamage = 9f, MeleeInterval = 1.1f, MeleeRange = 0.95f, AttackBlock = 0.3f }, creature: pony);
            var falconer = Hero("Falconer", "Shoots from afar with an owl on one arm. Beat the falconer and the owl is free.", new Color(0.55f, 0.70f, 0.85f), HeroLook.Diamond,
                new HeroDef { Id = "falconer", Name = "Falconer", Hp = 32f, Speed = 1.4f, KeepDamage = 5f, Size = 0.36f, ShotDamage = 5f, ShotInterval = 1.3f, ShotRange = 7f, ShotSpeed = 10f }, creature: owl);
            var brash = Hero("CaptainBrash", "Mid-boss. A loud captain who swings first and thinks never.", new Color(0.95f, 0.70f, 0.40f), HeroLook.Square,
                new HeroDef { Id = "captain_brash", Name = "Captain Brash", Hp = 420f, Speed = 0.7f, KeepDamage = 25f, Size = 0.65f, MeleeDamage = 14f, MeleeInterval = 1f, MeleeRange = 1.2f, IsBoss = true });
            var shieldwall = Hero("SergeantShieldwall", "Mid-boss. An unstoppable wall of shield.", new Color(0.45f, 0.55f, 0.85f), HeroLook.Tall,
                new HeroDef { Id = "sergeant_shieldwall", Name = "Sergeant Shieldwall", Hp = 520f, Speed = 0.6f, KeepDamage = 30f, Size = 0.7f, MeleeDamage = 16f, MeleeInterval = 1.4f, MeleeRange = 1.2f, AttackBlock = 0.7f, IsBoss = true });
            var vale = Hero("MasterArcherVale", "Mid-boss. Never misses, never stops talking about it.", new Color(0.45f, 0.75f, 0.50f), HeroLook.Diamond,
                new HeroDef { Id = "master_archer_vale", Name = "Master Archer Vale", Hp = 380f, Speed = 0.9f, KeepDamage = 25f, Size = 0.6f, ShotDamage = 9f, ShotInterval = 1f, ShotRange = 8.5f, ShotSpeed = 11f, IsBoss = true });
            var pompous = Hero("SirPompous", "Boss. Brings his squires and his speeches.", new Color(1f, 0.82f, 0.45f), HeroLook.Square,
                new HeroDef { Id = "sir_pompous", Name = "Sir Pompous", Hp = 950f, Speed = 0.45f, KeepDamage = 40f, Size = 0.8f, MeleeDamage = 16f, MeleeInterval = 1.2f, MeleeRange = 1.3f, IsBoss = true, SummonInterval = 7f, SummonCount = 2 }, summon: squire);
            var gleam = Hero("LadyGleam", "Boss. Polished armour, polished knights.", new Color(0.85f, 0.85f, 0.95f), HeroLook.Tall,
                new HeroDef { Id = "lady_gleam", Name = "Lady Gleam", Hp = 1050f, Speed = 0.45f, KeepDamage = 45f, Size = 0.8f, MeleeDamage = 18f, MeleeInterval = 1.2f, MeleeRange = 1.3f, AttackBlock = 0.4f, IsBoss = true, SummonInterval = 8f, SummonCount = 1 }, summon: knight);
            var paragon = Hero("TheParagon", "Boss. The most heroic Hero there ever was, apparently.", new Color(1f, 0.95f, 0.70f), HeroLook.Diamond,
                new HeroDef { Id = "the_paragon", Name = "The Paragon", Hp = 1150f, Speed = 0.4f, KeepDamage = 50f, Size = 0.85f, ShotDamage = 12f, ShotInterval = 1.2f, ShotRange = 8f, ShotSpeed = 11f, IsBoss = true, SummonInterval = 9f, SummonCount = 2 }, summon: shieldbearer);

            // --- Encounters and Chapters -------------------------------------------------------------
            var meadow = Chapter("1_MeadowRoad", "Meadow Road",
                battles: new[] {
                    Encounter("meadow_1", "", 15, W(0.9f, (squire, 4)), W(0.8f, (squire, 4), (archer, 1))),
                    Encounter("meadow_2", "", 15, W(0.9f, (squire, 3), (archer, 2)), W(0.8f, (squire, 3), (knight, 1))),
                    Encounter("meadow_3", "", 15, W(0.8f, (squire, 5)), W(0.8f, (falconer, 1), (squire, 3)), W(0.7f, (squire, 4))) },
                tough: new[] {
                    Encounter("meadow_tough_1", "", 30, W(0.8f, (squire, 5), (archer, 2)), W(0.7f, (shieldbearer, 1), (knight, 1), (squire, 4))),
                    Encounter("meadow_tough_2", "", 30, W(0.8f, (knight, 2), (squire, 3)), W(0.7f, (falconer, 2), (squire, 4))) },
                mid: Encounter("meadow_mid", "Captain Brash", 35, W(0.8f, (squire, 4)), W(1f, (brash, 1), (squire, 3))),
                boss: Encounter("meadow_boss", "Sir Pompous", 50, W(0.8f, (squire, 4), (archer, 2)), W(1f, (pompous, 1))));
            var woods = Chapter("2_WhisperingWoods", "Whispering Woods",
                battles: new[] {
                    Encounter("woods_1", "", 18, W(0.8f, (squire, 4), (shieldbearer, 1)), W(0.7f, (archer, 2), (squire, 4))),
                    Encounter("woods_2", "", 18, W(0.8f, (knight, 2), (squire, 3)), W(0.7f, (squire, 5), (falconer, 1))),
                    Encounter("woods_3", "", 18, W(0.8f, (archer, 3), (squire, 3)), W(0.7f, (shieldbearer, 2), (squire, 3))) },
                tough: new[] {
                    Encounter("woods_tough_1", "", 35, W(0.7f, (shieldbearer, 2), (squire, 4)), W(0.6f, (knight, 2), (archer, 3), (squire, 3))),
                    Encounter("woods_tough_2", "", 35, W(0.7f, (falconer, 2), (archer, 2), (squire, 3)), W(0.6f, (shieldbearer, 2), (knight, 1), (squire, 4))) },
                mid: Encounter("woods_mid", "Sergeant Shieldwall", 40, W(0.8f, (squire, 4), (archer, 2)), W(1f, (shieldwall, 1), (squire, 3))),
                boss: Encounter("woods_boss", "Lady Gleam", 55, W(0.7f, (knight, 2), (squire, 4)), W(1f, (gleam, 1), (archer, 2))));
            var hill = Chapter("3_CobbleHill", "Cobble Hill",
                battles: new[] {
                    Encounter("hill_1", "", 20, W(0.7f, (squire, 5), (archer, 2)), W(0.6f, (shieldbearer, 2), (falconer, 1), (squire, 3))),
                    Encounter("hill_2", "", 20, W(0.7f, (knight, 2), (archer, 2)), W(0.6f, (squire, 6), (shieldbearer, 1))),
                    Encounter("hill_3", "", 20, W(0.7f, (archer, 3), (falconer, 1)), W(0.6f, (knight, 2), (squire, 5))) },
                tough: new[] {
                    Encounter("hill_tough_1", "", 40, W(0.6f, (shieldbearer, 2), (archer, 3), (squire, 3)), W(0.5f, (knight, 3), (squire, 5))),
                    Encounter("hill_tough_2", "", 40, W(0.6f, (falconer, 2), (knight, 2), (squire, 3)), W(0.5f, (shieldbearer, 3), (archer, 3))) },
                mid: Encounter("hill_mid", "Master Archer Vale", 45, W(0.7f, (squire, 4), (shieldbearer, 1)), W(1f, (vale, 1), (squire, 4))),
                boss: Encounter("hill_boss", "The Paragon", 60, W(0.7f, (knight, 2), (archer, 3)), W(1f, (paragon, 1), (squire, 2))));

            // --- Events ------------------------------------------------------------------------------
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
            };

            // --- Game content and scene ----------------------------------------------------------------
            var game = Load<GameContent>("GameContent");
            if (game == null)
            {
                game = ScriptableObject.CreateInstance<GameContent>();
                game.keeper = keeper;
                game.familiars = new List<FighterAsset> { pip, bramble, pony, owl };
                game.heroes = new List<HeroAsset> { squire, shieldbearer, archer, knight, falconer, brash, shieldwall, vale, pompous, gleam, paragon };
                game.chapters = new List<ChapterAsset> { meadow, woods, hill };
                game.events = events;
                AssetDatabase.CreateAsset(game, $"{Root}/GameContent.asset");
            }
            AssetDatabase.SaveAssets();
            BuildScene(Load<GameContent>("GameContent"));
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

        // --- Asset helpers (each returns the existing asset when there is one) ---------------------

        static SkillUpgradeDef Up(string id, string name, string description, SkillStat stat, ModOp op, float value) =>
            new SkillUpgradeDef { Id = id, Name = name, Description = description, Mods = { new SkillMod(stat, op, value) } };

        static SkillAsset Skill(string file, string description, SkillDef def, SkillUpgradeDef[] upgrades, SkillDef master, string masterDescription)
        {
            var a = Load<SkillAsset>("Skills/" + file);
            if (a != null) return a;
            a = ScriptableObject.CreateInstance<SkillAsset>();
            a.def = def;
            a.description = description;
            a.upgrades = new List<SkillUpgradeDef>(upgrades);
            a.hasMasterForm = master != null;
            a.masterForm = master;
            a.masterDescription = masterDescription;
            return Create(a, "Skills/" + file);
        }

        static FighterAsset Fighter(string file, string description, Color color, Color skillColor, FighterDef def, params SkillAsset[] skills)
        {
            var a = Load<FighterAsset>("Fighters/" + file) ?? Load<FighterAsset>(file);
            if (a != null)
            {
                // Fighters from milestone 1 carried their Skills inline; move them onto Skill assets.
                if (a.skills.Count == 0) { a.skills = new List<SkillAsset>(skills); a.def.Skills.Clear(); EditorUtility.SetDirty(a); }
                if (AssetDatabase.GetAssetPath(a) != $"{Root}/Fighters/{file}.asset") AssetDatabase.MoveAsset(AssetDatabase.GetAssetPath(a), $"{Root}/Fighters/{file}.asset");
                return a;
            }
            a = ScriptableObject.CreateInstance<FighterAsset>();
            a.def = def;
            a.description = description;
            a.color = color;
            a.skillColor = skillColor;
            a.skills = new List<SkillAsset>(skills);
            return Create(a, "Fighters/" + file);
        }

        static HeroAsset Hero(string file, string description, Color color, HeroLook look, HeroDef def, FighterAsset creature = null, HeroAsset summon = null)
        {
            var a = Load<HeroAsset>("Heroes/" + file) ?? Load<HeroAsset>(file);
            if (a != null)
            {
                if (AssetDatabase.GetAssetPath(a) != $"{Root}/Heroes/{file}.asset") AssetDatabase.MoveAsset(AssetDatabase.GetAssetPath(a), $"{Root}/Heroes/{file}.asset");
                return a;
            }
            a = ScriptableObject.CreateInstance<HeroAsset>();
            a.def = def;
            a.description = description;
            a.color = color;
            a.look = look;
            a.creature = creature;
            a.summon = summon;
            return Create(a, "Heroes/" + file);
        }

        static WaveAsset W(float interval, params (HeroAsset hero, int count)[] entries)
        {
            var w = new WaveAsset { spawnInterval = interval };
            foreach (var (hero, count) in entries) w.entries.Add(new WaveEntryAsset { hero = hero, count = count });
            return w;
        }

        static EncounterAsset Encounter(string id, string name, int gold, params WaveAsset[] waves)
        {
            var a = Load<EncounterAsset>("Encounters/" + id);
            if (a != null) return a;
            a = ScriptableObject.CreateInstance<EncounterAsset>();
            a.id = id;
            a.displayName = name;
            a.gold = gold;
            a.waves = new List<WaveAsset>(waves);
            return Create(a, "Encounters/" + id);
        }

        static ChapterAsset Chapter(string file, string name, EncounterAsset[] battles, EncounterAsset[] tough, EncounterAsset mid, EncounterAsset boss)
        {
            var a = Load<ChapterAsset>("Chapters/" + file);
            if (a != null) return a;
            a = ScriptableObject.CreateInstance<ChapterAsset>();
            a.displayName = name;
            a.battles = new List<EncounterAsset>(battles);
            a.toughBattles = new List<EncounterAsset>(tough);
            a.midBoss = mid;
            a.boss = boss;
            return Create(a, "Chapters/" + file);
        }

        static EventEffect Fx(EventEffectKind kind, float amount) => new EventEffect { Kind = kind, Amount = amount };

        static EventOption Opt(string text, string outcome, int cost, params EventEffect[] effects) =>
            new EventOption { Text = text, Outcome = outcome, GoldCost = cost, Effects = new List<EventEffect>(effects) };

        static EventAsset Event(string file, string title, string text, params EventOption[] options)
        {
            var a = Load<EventAsset>("Events/" + file);
            if (a != null) return a;
            a = ScriptableObject.CreateInstance<EventAsset>();
            a.def = new EventDef { Id = file, Title = title, Text = text, Options = new List<EventOption>(options) };
            return Create(a, "Events/" + file);
        }

        static T Create<T>(T asset, string path) where T : Object
        {
            AssetDatabase.CreateAsset(asset, $"{Root}/{path}.asset");
            return asset;
        }

        static T Load<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>($"{Root}/{path}.asset");
    }
}
