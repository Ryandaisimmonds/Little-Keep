using System.Collections.Generic;
using LittleKeep.Game;
using LittleKeep.Rules;
using UnityEditor;
using UnityEngine;

namespace LittleKeep.Editor
{
    /// Adds the milestone 3 content to an existing project: Puff and Tide, how each Familiar is earned,
    /// the Deed, the Keeper's upgrade tracks and the difficulty tiers. Safe to re-run: existing entries are kept.
    public static class KeepSetup
    {
        const string Content = "Assets/Game/Content";

        [MenuItem("Little Keep/Add Keep Content")]
        public static void Apply()
        {
            var game = AssetDatabase.LoadAssetAtPath<GameContent>($"{Content}/GameContent.asset");
            if (game == null) { Debug.LogError("Run 'Create Missing Content and Scene' first."); return; }

            var sporePuff = Skill("SporePuff", "A soft cloud of spores that hits every Hero around the target.",
                new SkillDef { Id = "spore_puff", Name = "Spore Puff", Shape = SkillShape.Burst, Cooldown = 6.5f, Damage = 14f, Radius = 1.9f },
                new[] { Up("spore_puff_thick", "Thick Spores", "Spores hit 35% harder.", SkillStat.Damage, ModOp.Multiply, 1.35f),
                        Up("spore_puff_quick", "Quick Puff", "Recharges 20% faster.", SkillStat.Cooldown, ModOp.Multiply, 0.8f),
                        Up("spore_puff_big", "Big Puff", "The cloud spreads 30% wider.", SkillStat.Radius, ModOp.Multiply, 1.3f) },
                new SkillDef { Id = "spore_puff_master", Name = "Spore Cloud", Shape = SkillShape.Burst, Cooldown = 5.5f, Damage = 26f, Radius = 2.8f },
                "A great drifting cloud that blankets the lane.");
            var bubbles = Skill("BubbleVolley", "Blows a spray of heavy bubbles at the Hero nearest the Keep.",
                new SkillDef { Id = "bubble_volley", Name = "Bubble Volley", Shape = SkillShape.Volley, Cooldown = 6f, Damage = 9f, Count = 4, Spread = 30f, ProjectileSpeed = 17f },
                new[] { Up("bubble_volley_big", "Big Bubbles", "Bubbles hit 35% harder.", SkillStat.Damage, ModOp.Multiply, 1.35f),
                        Up("bubble_volley_quick", "Quick Tide", "Recharges 20% faster.", SkillStat.Cooldown, ModOp.Multiply, 0.8f),
                        Up("bubble_volley_stream", "Bubble Stream", "Two more bubbles per volley.", SkillStat.Count, ModOp.Add, 2f) },
                new SkillDef { Id = "bubble_volley_master", Name = "Tidal Wave", Shape = SkillShape.Volley, Cooldown = 5f, Damage = 11f, Count = 10, Spread = 70f, ProjectileSpeed = 19f },
                "A rolling wave of bubbles across half the lane.");

            var puff = Fighter("Puff", "A grumpy mushroom. Puffs clouds of spores.",
                new Color(0.85f, 0.55f, 0.55f), new Color(0.85f, 0.7f, 1f),
                new FighterDef { Id = "puff", Name = "Puff", Hp = 65f, AttackDamage = 4f, AttackInterval = 0.5f, ProjectileSpeed = 15f }, sporePuff);
            var tide = Fighter("Tide", "A playful water spirit who only follows those who reach the end of Meadow Road.",
                new Color(0.45f, 0.65f, 0.95f), new Color(0.5f, 0.85f, 1f),
                new FighterDef { Id = "tide", Name = "Tide", Hp = 55f, AttackDamage = 5f, AttackInterval = 0.4f, ProjectileSpeed = 17f }, bubbles);
            puff.art = ArtFor("Mushroom");
            tide.art = ArtFor("Elemental Water 1");

            Unlock("Pip", UnlockKind.Start, "");
            Unlock("Bramble", UnlockKind.Start, "");
            Unlock("Puff", UnlockKind.Start, "");
            Unlock("Pony", UnlockKind.Bond, "Befriend Biscuit 3 times. Knights bring him into Battle, and he turns up in the wild.");
            Unlock("Owl", UnlockKind.Bond, "Befriend Bun 3 times. Huntsmen bring her into Battle, and she turns up in the wild.");
            Unlock("Tide", UnlockKind.Deed, "Deed: beat the Meadow Road boss.", "clear_meadow");

            foreach (var f in new[] { "Pip", "Bramble", "Puff", "Pony", "Owl", "Tide" })
            {
                var a = AssetDatabase.LoadAssetAtPath<FighterAsset>($"{Content}/Fighters/{f}.asset");
                if (a != null && !game.familiars.Contains(a)) game.familiars.Add(a);
            }

            if (game.deeds.Count == 0)
                game.deeds.Add(new DeedDef { Id = "clear_meadow", Name = "Clear Meadow Road", Description = "Beat the Meadow Road boss in any Run.", Kind = DeedKind.ClearChapters, Target = 1 });

            if (game.keeperUpgrades.Count == 0)
            {
                game.keeperUpgrades.Add(new KeeperUpgradeDef { Id = "walls", Name = "Sturdy Walls", Description = "+10 Keep health per level.", Stat = KeeperStat.KeepMaxHp, PerLevel = 10f, Costs = { 30, 60, 100, 150, 220 } });
                game.keeperUpgrades.Add(new KeeperUpgradeDef { Id = "claws", Name = "Sharp Claws", Description = "The Keeper hits 8% harder per level.", Stat = KeeperStat.KeeperDamage, PerLevel = 0.08f, Costs = { 40, 80, 130, 190 } });
                game.keeperUpgrades.Add(new KeeperUpgradeDef { Id = "purse", Name = "Pocket Money", Description = "Start each Run with 15 more Gold per level.", Stat = KeeperStat.StartGold, PerLevel = 15f, Costs = { 25, 50, 90 } });
                game.keeperUpgrades.Add(new KeeperUpgradeDef { Id = "horn", Name = "Rally Horn", Description = "Rally recharges 8% faster per level.", Stat = KeeperStat.RallyCooldown, PerLevel = -0.08f, Costs = { 50, 100, 160 } });
            }

            if (game.tiers.Count == 0)
            {
                game.tiers.Add(new DifficultyDef { Name = "Cozy", Description = "A gentle march of Heroes.", HeroHp = 0.95f, HeroCount = 1f, Reward = 1f });
                game.tiers.Add(new DifficultyDef { Name = "Brisk", Description = "Tougher, more numerous Heroes. 50% more Gems.", HeroHp = 1.15f, HeroCount = 1.1f, Reward = 1.5f });
                game.tiers.Add(new DifficultyDef { Name = "Fierce", Description = "The Heroes mean it. Double Gems.", HeroHp = 1.3f, HeroCount = 1.2f, Reward = 2f });
            }

            EditorUtility.SetDirty(game);
            AssetDatabase.SaveAssets();
            Debug.Log("Little Keep: Keep content added.");
        }

        static void Unlock(string file, UnlockKind kind, string hint, string deed = "")
        {
            var a = AssetDatabase.LoadAssetAtPath<FighterAsset>($"{Content}/Fighters/{file}.asset");
            if (a == null) return;
            a.unlock = kind;
            a.bondSize = 3;
            a.deedId = deed;
            a.unlockHint = hint;
            EditorUtility.SetDirty(a);
        }

        static CharacterArt ArtFor(string monster)
        {
            var art = AssetDatabase.LoadAssetAtPath<CharacterArt>($"{Content}/Art/{monster}.asset");
            if (art != null) return art;
            // Built by Apply Art Packs; run it after this to measure the new characters.
            ArtSetup.Apply();
            return AssetDatabase.LoadAssetAtPath<CharacterArt>($"{Content}/Art/{monster}.asset");
        }

        static SkillUpgradeDef Up(string id, string name, string description, SkillStat stat, ModOp op, float value) =>
            new SkillUpgradeDef { Id = id, Name = name, Description = description, Mods = { new SkillMod(stat, op, value) } };

        static SkillAsset Skill(string file, string description, SkillDef def, SkillUpgradeDef[] upgrades, SkillDef master, string masterDescription)
        {
            string path = $"{Content}/Skills/{file}.asset";
            var a = AssetDatabase.LoadAssetAtPath<SkillAsset>(path);
            if (a != null) return a;
            a = ScriptableObject.CreateInstance<SkillAsset>();
            a.def = def;
            a.description = description;
            a.upgrades = new List<SkillUpgradeDef>(upgrades);
            a.masterForm = master;
            a.masterDescription = masterDescription;
            AssetDatabase.CreateAsset(a, path);
            return a;
        }

        static FighterAsset Fighter(string file, string description, Color color, Color skillColor, FighterDef def, params SkillAsset[] skills)
        {
            string path = $"{Content}/Fighters/{file}.asset";
            var a = AssetDatabase.LoadAssetAtPath<FighterAsset>(path);
            if (a != null) return a;
            a = ScriptableObject.CreateInstance<FighterAsset>();
            a.def = def;
            a.description = description;
            a.color = color;
            a.skillColor = skillColor;
            a.skills = new List<SkillAsset>(skills);
            AssetDatabase.CreateAsset(a, path);
            return a;
        }
    }
}
