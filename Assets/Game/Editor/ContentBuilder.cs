using System.Collections.Generic;
using LittleKeep.Game;
using LittleKeep.Rules;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LittleKeep.Editor
{
    /// Creates the milestone 1 content and Battle scene. Safe to re-run: existing assets and the scene are kept, so edits survive.
    public static class ContentBuilder
    {
        const string Dir = "Assets/Game/Content";
        const string ScenePath = "Assets/Game/Scenes/Battle.unity";

        [MenuItem("Little Keep/Create Missing Content and Scene")]
        public static void Build()
        {
            System.IO.Directory.CreateDirectory(Dir);
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(ScenePath));
            AssetDatabase.Refresh();
            var keeper = Fighter("Keeper", "Your own character on the battlefield. Hold a finger on the lane to aim.",
                new Color(1f, 0.93f, 0.75f), new Color(1f, 0.86f, 0.35f),
                new FighterDef { Id = "keeper", Name = "Keeper", Hp = 80f, AttackDamage = 6f, AttackInterval = 0.3f, ProjectileSpeed = 18f,
                    Skills = { new SkillDef { Id = "star_volley", Name = "Star Volley", Shape = SkillShape.Volley, Cooldown = 7f, Damage = 8f, Count = 5, Spread = 24f, ProjectileSpeed = 20f } } });
            var pip = Fighter("Pip", "A little ember sprite. Pops Heroes in a bright burst.",
                new Color(1f, 0.62f, 0.45f), new Color(1f, 0.5f, 0.2f),
                new FighterDef { Id = "pip", Name = "Pip", Hp = 50f, AttackDamage = 4f, AttackInterval = 0.45f, ProjectileSpeed = 16f,
                    Skills = { new SkillDef { Id = "pop_burst", Name = "Pop Burst", Shape = SkillShape.Burst, Cooldown = 6f, Damage = 18f, Radius = 1.6f } } });
            var bramble = Fighter("Bramble", "A mossy hedgehog. Sprays a wide fan of thorns.",
                new Color(0.55f, 0.75f, 0.45f), new Color(0.45f, 0.95f, 0.5f),
                new FighterDef { Id = "bramble", Name = "Bramble", Hp = 70f, AttackDamage = 3f, AttackInterval = 0.6f, ProjectileSpeed = 15f,
                    Skills = { new SkillDef { Id = "thorn_fan", Name = "Thorn Fan", Shape = SkillShape.Volley, Cooldown = 8f, Damage = 10f, Count = 7, Spread = 60f, ProjectileSpeed = 16f } } });

            var squire = Hero("Squire", "Eager and underpaid. Walks straight at the Keep and swings at anyone in the way.",
                new Color(0.93f, 0.80f, 0.55f), HeroLook.Square,
                new HeroDef { Id = "squire", Name = "Squire", Hp = 22f, Speed = 1.3f, KeepDamage = 6f, Size = 0.38f, MeleeDamage = 5f, MeleeInterval = 1f, MeleeRange = 0.9f });
            var shieldbearer = Hero("Shieldbearer", "Hides behind an enormous shield. Ordinary attacks barely scratch it; Skills hit in full.",
                new Color(0.55f, 0.65f, 0.85f), HeroLook.Tall,
                new HeroDef { Id = "shieldbearer", Name = "Shieldbearer", Hp = 70f, Speed = 0.8f, KeepDamage = 12f, Size = 0.45f, MeleeDamage = 8f, MeleeInterval = 1.4f, MeleeRange = 0.95f, AttackBlock = 0.7f });
            var archer = Hero("Archer", "Stops at a distance and shoots at the squad.",
                new Color(0.60f, 0.80f, 0.55f), HeroLook.Diamond,
                new HeroDef { Id = "archer", Name = "Archer", Hp = 16f, Speed = 1.5f, KeepDamage = 5f, Size = 0.35f, ShotDamage = 4f, ShotInterval = 1.6f, ShotRange = 7f, ShotSpeed = 9f });

            var battle = Load<BattleAsset>("Battle");
            if (battle == null)
            {
                battle = ScriptableObject.CreateInstance<BattleAsset>();
                battle.keeper = keeper;
                battle.familiars = new List<FighterAsset> { pip, bramble };
                battle.keepMaxHp = 100f;
                battle.waves = new List<WaveAsset>
                {
                    Wave(1.0f, (squire, 5)),
                    Wave(0.9f, (squire, 6), (archer, 2)),
                    Wave(0.8f, (squire, 4), (shieldbearer, 2), (archer, 2)),
                    Wave(0.7f, (squire, 6), (archer, 3), (shieldbearer, 2)),
                    Wave(0.6f, (squire, 8), (archer, 4), (shieldbearer, 3)),
                };
                AssetDatabase.CreateAsset(battle, $"{Dir}/Battle.asset");
            }
            AssetDatabase.SaveAssets();
            BuildScene(Load<BattleAsset>("Battle"));
        }

        static void BuildScene(BattleAsset battle)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)) { tag = "MainCamera" };
            cam.transform.position = new Vector3(7f, 0f, -10f);
            var c = cam.GetComponent<Camera>();
            c.orthographic = true;
            c.orthographicSize = 5f;
            var runner = new GameObject("Battle").AddComponent<BattleRunner>();
            var so = new SerializedObject(runner);
            so.FindProperty("content").objectReferenceValue = battle;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        static WaveAsset Wave(float interval, params (HeroAsset hero, int count)[] entries)
        {
            var w = new WaveAsset { spawnInterval = interval };
            foreach (var (hero, count) in entries) w.entries.Add(new WaveEntryAsset { hero = hero, count = count });
            return w;
        }

        static FighterAsset Fighter(string name, string description, Color color, Color skillColor, FighterDef def)
        {
            var a = Load<FighterAsset>(name);
            if (a != null) return a;
            a = ScriptableObject.CreateInstance<FighterAsset>();
            a.def = def;
            a.description = description;
            a.color = color;
            a.skillColor = skillColor;
            AssetDatabase.CreateAsset(a, $"{Dir}/{name}.asset");
            return a;
        }

        static HeroAsset Hero(string name, string description, Color color, HeroLook look, HeroDef def)
        {
            var a = Load<HeroAsset>(name);
            if (a != null) return a;
            a = ScriptableObject.CreateInstance<HeroAsset>();
            a.def = def;
            a.description = description;
            a.color = color;
            a.look = look;
            AssetDatabase.CreateAsset(a, $"{Dir}/{name}.asset");
            return a;
        }

        static T Load<T>(string name) where T : Object => AssetDatabase.LoadAssetAtPath<T>($"{Dir}/{name}.asset");
    }
}
