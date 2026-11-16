using System.Collections.Generic;
using System.IO;
using System.Linq;
using LittleKeep.Game;
using LittleKeep.Rules;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace LittleKeep.Editor
{
    /// Wires the imported art packs into the game: builds CharacterArt for each Keeper, Familiar and Hero from the
    /// monster pack's own animation clips, builds the UiTheme from the UI pack, and renames the creatures whose
    /// art changed who they are. Safe to re-run.
    public static class ArtSetup
    {
        const string Content = "Assets/Game/Content";
        const string Monsters = "Assets/Imported/100 Top Down Monsters - Pixel Art - Vol 1/Monsters";
        const string Ui = "Assets/Imported/Layer Lab/GUI-TheStone/ResourcesData";

        [MenuItem("Little Keep/Apply Art Packs")]
        public static void Apply()
        {
            Directory.CreateDirectory($"{Content}/Art");
            AssetDatabase.Refresh();

            Fighter("Keeper", "Dragon Hatchling", 1.2f, "Keeper", "You: a little dragon who keeps the Keep. Hold a finger on the lane to aim.");
            Fighter("Pip", "Elemental Fire 1", 1f, "Pip", "A little fire spirit. Pops Heroes in a bright burst.");
            Fighter("Bramble", "Plant Seeder", 1f, "Bramble", "A prickly little plant. Sprays a wide fan of thorns.");
            Fighter("Pony", "Canine Beagle", 1f, "Biscuit", "A knight's hound, glad of a quieter job. Bounds in with a big bark.");
            Fighter("Owl", "Rabbit Horned", 1f, "Bun", "A huntsman's horned rabbit who never liked the huntsman. Quick and fierce.");

            Rename("Stomp", "Bark Blast", "A huge bark that knocks every Hero nearby.", "Howling Blast", "A howl that rattles half the lane.",
                ("Big Bark", "Barks hit 35% harder."), ("Quick Bark", "Recharges 20% faster."), ("Echoing Bark", "The bark reaches 30% wider."));
            Rename("FeatherFlurry", "Clover Flurry", "A quick flurry of sharp, lucky clovers.", "Lucky Gale", "A gale of four-leaf clovers.",
                ("Four-Leaf", "Clovers hit 40% harder."), ("Quick Paws", "Recharges 20% faster."), ("Clover Patch", "Three more clovers per flurry."));

            Hero("Squire", "Knight Squire", 1f);
            Hero("Shieldbearer", "Knight Guardsman", 1f);
            Hero("Archer", "Knight Archer", 1f);
            Hero("Knight", "Knight", 1f, description: "Rides in with a hound at heel. Beat the knight and the hound is free.");
            Hero("Falconer", "Knight Marksman", 1f, "Huntsman", "Shoots from afar with a horned rabbit in tow. Beat the huntsman and the rabbit is free.");
            Hero("CaptainBrash", "Knight Bodyguard", 1f);
            Hero("SergeantShieldwall", "Knight Royal Guard", 1f);
            Hero("MasterArcherVale", "Mage Wizard", 1f, "Archmage Vale", "Mid-boss. Never misses, never stops talking about it.");
            Hero("SirPompous", "Knight General", 1f);
            Hero("LadyGleam", "Knight Templar", 1f);
            Hero("TheParagon", "Knight Highborn", 1f);
            var vale = AssetDatabase.LoadAssetAtPath<EncounterAsset>($"{Content}/Encounters/hill_mid.asset");
            if (vale != null) { vale.displayName = "Archmage Vale"; EditorUtility.SetDirty(vale); }

            var theme = BuildTheme();
            var game = AssetDatabase.LoadAssetAtPath<GameContent>($"{Content}/GameContent.asset");
            if (game != null) { game.theme = theme; EditorUtility.SetDirty(game); }
            AssetDatabase.SaveAssets();
            Debug.Log("Little Keep: art packs applied.");
        }

        // --- Characters -------------------------------------------------------------------------

        static void Fighter(string file, string monster, float scale, string name, string description)
        {
            var a = AssetDatabase.LoadAssetAtPath<FighterAsset>($"{Content}/Fighters/{file}.asset");
            if (a == null) { Debug.LogWarning($"No fighter asset {file}"); return; }
            a.art = Art(monster, scale);
            a.def.Name = name;
            a.description = description;
            EditorUtility.SetDirty(a);
        }

        static void Hero(string file, string monster, float scale, string name = null, string description = null)
        {
            var a = AssetDatabase.LoadAssetAtPath<HeroAsset>($"{Content}/Heroes/{file}.asset");
            if (a == null) { Debug.LogWarning($"No hero asset {file}"); return; }
            a.art = Art(monster, scale);
            if (name != null) a.def.Name = name;
            if (description != null) a.description = description;
            EditorUtility.SetDirty(a);
        }

        static void Rename(string file, string name, string description, string masterName, string masterDescription, params (string name, string description)[] upgrades)
        {
            var s = AssetDatabase.LoadAssetAtPath<SkillAsset>($"{Content}/Skills/{file}.asset");
            if (s == null) return;
            s.def.Name = name;
            s.description = description;
            if (s.masterForm != null) s.masterForm.Name = masterName;
            s.masterDescription = masterDescription;
            for (int i = 0; i < upgrades.Length && i < s.upgrades.Count; i++)
            {
                s.upgrades[i].Name = upgrades[i].name;
                s.upgrades[i].Description = upgrades[i].description;
            }
            EditorUtility.SetDirty(s);
        }

        /// One CharacterArt per monster, read from the pack's own Left/Right clips so frame order matches the pack.
        static CharacterArt Art(string monster, float scale)
        {
            string folder = $"{Monsters}/{monster}";
            if (!AssetDatabase.IsValidFolder(folder)) { Debug.LogWarning($"Monster '{monster}' not found in the pack"); return null; }
            string path = $"{Content}/Art/{monster}.asset";
            var art = AssetDatabase.LoadAssetAtPath<CharacterArt>(path);
            if (art == null)
            {
                art = ScriptableObject.CreateInstance<CharacterArt>();
                art.scale = scale;
                art.footOffset = 0f;
                AssetDatabase.CreateAsset(art, path);
            }
            art.idleLeft = Clip(folder, "Idle", "Left");
            art.idleRight = Clip(folder, "Idle", "Right");
            art.moveLeft = Clip(folder, "Move", "Left", "Run", "Fly");
            art.moveRight = Clip(folder, "Move", "Right", "Run", "Fly");
            art.attackLeft = Clip(folder, "Attack", "Left");
            art.attackRight = Clip(folder, "Attack", "Right");
            art.scale = scale;
            Measure(art);
            EditorUtility.SetDirty(art);
            return art;
        }

        /// Measures the visible character across its idle frames from the sprites' tight outlines.
        static void Measure(CharacterArt art)
        {
            var frames = art.idleRight.Concat(art.idleLeft).Where(f => f != null).ToArray();
            if (frames.Length == 0) return;
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
            foreach (var f in frames)
                foreach (var v in f.vertices) { min = Vector2.Min(min, v); max = Vector2.Max(max, v); }
            art.visibleSize = max - min;
            art.visibleCenter = new Vector2(0f, (min.y + max.y) * 0.5f);
        }

        static Sprite[] Clip(string folder, string action, string side, params string[] alternatives)
        {
            var names = new[] { action }.Concat(alternatives);
            var files = Directory.GetFiles(folder, "*.anim");
            // The pack names sides both ways: "_Idle_Left" and "_Idle_L".
            foreach (var n in names)
            {
                var file = files.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).EndsWith($"_{n}_{side}"))
                        ?? files.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).EndsWith($"_{n}_{side[0]}"));
                if (file == null) continue;
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(file.Replace('\\', '/'));
                if (clip == null) continue;
                foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                {
                    var frames = AnimationUtility.GetObjectReferenceCurve(clip, binding).Select(k => k.value as Sprite).Where(s => s != null).ToArray();
                    if (frames.Length > 0) return frames;
                }
            }
            return new Sprite[0];
        }

        // --- UI theme -----------------------------------------------------------------------------

        static UiTheme BuildTheme()
        {
            string path = $"{Content}/UiTheme.asset";
            var t = AssetDatabase.LoadAssetAtPath<UiTheme>(path);
            if (t == null)
            {
                t = ScriptableObject.CreateInstance<UiTheme>();
                AssetDatabase.CreateAsset(t, path);
            }
            string c = $"{Ui}/Sprites/Components", picto = $"{c}/Icon_PictoIcons/PictoIcon_128", item = $"{c}/Icon_ItemIcons/ItemIcon_128";
            t.font = Find<TMP_FontAsset>($"{Ui}/Fonts", "Changa-Medium SDF.asset");
            t.fontOutlined = Find<TMP_FontAsset>($"{Ui}/Fonts", "Changa-Bold_Underlay SDF.asset");
            t.background = Find<Sprite>($"{Ui}/Sprites/Demo/Demo_Image", "Background.png");
            t.panel = Find<Sprite>($"{c}/Popup", "Popup_Bg.png");
            t.card = Find<Sprite>($"{c}/Frame", "StageFrame_Demo_n.png");
            t.cardDim = Find<Sprite>($"{c}/Frame", "StageFrame_Demo_d.png");
            t.topBar = Find<Sprite>($"{c}/Frame", "PanelFrame_Topbar_Demo.png");
            t.ribbon = Find<Sprite>($"{c}/Label-Title", "Title_Ribbon01_Green.png");
            t.tag = Find<Sprite>($"{c}/Label-Title", "Label_Oval_Brown.png");
            t.buttonBrown = Find<Sprite>($"{c}/Button", "Button01_Brown.png");
            t.buttonGreen = Find<Sprite>($"{c}/Button", "Button01_Green.png");
            t.buttonBlue = Find<Sprite>($"{c}/Button", "Button01_Blue.png");
            t.buttonRed = Find<Sprite>($"{c}/Button", "Button01_Red.png");
            t.buttonPurple = Find<Sprite>($"{c}/Button", "Button01_Purple.png");
            t.buttonTeal = Find<Sprite>($"{c}/Button", "Button01_Bluegreen.png");
            t.buttonRound = Find<Sprite>($"{c}/Button", "Button_Circle02.png");
            t.buttonSquare = Find<Sprite>($"{c}/Button", "Button_Square03_n.png");
            t.barBack = Find<Sprite>($"{c}/Slider", "Slider_Basic02_Bg_Demo.png");
            t.barFill = Find<Sprite>($"{c}/Slider", "Slider_Basic02_Fill_White.png");
            t.iconPause = Find<Sprite>(picto, "Icon_PictoIcon_Control_Pause.png");
            t.iconPlay = Find<Sprite>(picto, "Icon_PictoIcon_Control_Play.png");
            t.iconHome = Find<Sprite>(picto, "Icon_PictoIcon_Home.png");
            t.iconSpeed = Find<Sprite>(picto, "Icon_PictoIcon_Speedmeter.png");
            t.iconHeart = Find<Sprite>(picto, "Icon_PictoIcon_Heart.png");
            t.iconStar = Find<Sprite>(picto, "Icon_PictoIcon_Star.png");
            t.iconSquad = Find<Sprite>(picto, "Icon_PictoIcon_User.png");
            t.iconRest = Find<Sprite>(picto, "Icon_PictoIcon_Tent.png");
            t.iconSparkle = Find<Sprite>(picto, "Icon_PictoIcon_Sparkle.png");
            t.iconGold = Find<Sprite>(item, "Icon_ItemIcon_Gold.png");
            t.iconCrown = Find<Sprite>(item, "Icon_ItemIcon_Crown.png");
            t.iconSkull = Find<Sprite>(item, "Icon_ItemIcon_Skull.png");
            t.iconBattle = Find<Sprite>(item, "Icon_ItemIcon_Sword_A.png");
            t.iconShop = Find<Sprite>(item, "Icon_ItemIcon_Shop.png");
            t.iconEvent = Find<Sprite>(item, "Icon_ItemIcon_Scroll.png");
            t.iconWild = Find<Sprite>(item, "Icon_ItemIcon_Clover.png");
            t.iconLock = Find<Sprite>(item, "Icon_ItemIcon_Lock.png");
            EditorUtility.SetDirty(t);
            return t;
        }

        /// Loads an asset by file name, ignoring the pack's mixed-case extensions (.Png / .png).
        static T Find<T>(string folder, string file) where T : Object
        {
            if (!Directory.Exists(folder)) { Debug.LogWarning($"Missing folder {folder}"); return null; }
            var match = Directory.GetFiles(folder).FirstOrDefault(f => string.Equals(Path.GetFileName(f), file, System.StringComparison.OrdinalIgnoreCase));
            if (match == null) { Debug.LogWarning($"Missing {file} in {folder}"); return null; }
            var asset = AssetDatabase.LoadAllAssetsAtPath(match.Replace('\\', '/')).OfType<T>().FirstOrDefault();
            if (asset == null) Debug.LogWarning($"{file} has no {typeof(T).Name}");
            return asset;
        }
    }
}
