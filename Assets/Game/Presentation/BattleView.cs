using System.Collections.Generic;
using LittleKeep.Rules;
using UnityEngine;

namespace LittleKeep.Game
{
    /// Draws a Battle and turns its events into effects. Reads rule state; never changes it.
    /// Characters with CharacterArt are animated sprites (the squad faces right, Heroes face left);
    /// anything without art falls back to a grey-box shape.
    public sealed class BattleView : MonoBehaviour
    {
        sealed class CharacterView
        {
            public Transform root;
            public SpriteRenderer body;
            public SpriteRenderer barBack, barFill, statusIcon;
            public Color color;
            public float baseScale, aspect = 1f;
            public float flash, squash;
            public CharacterArt art;
            public bool faceRight;
            public float animTime, attackTime;
        }

        const float AttackPoseSeconds = 0.4f;
        /// Standard on-screen height of a character with art, in lane units.
        const float CharacterHeight = 1.05f;
        static readonly Color HitTint = new Color(1f, 0.55f, 0.55f);

        Battle battle;
        CompiledContent content;
        Fx fx;
        readonly Dictionary<int, CharacterView> fighters = new Dictionary<int, CharacterView>();
        readonly Dictionary<int, CharacterView> heroes = new Dictionary<int, CharacterView>();
        readonly Dictionary<int, SpriteRenderer> shots = new Dictionary<int, SpriteRenderer>();
        readonly Stack<SpriteRenderer> shotPool = new Stack<SpriteRenderer>();
        readonly HashSet<int> seen = new HashSet<int>();
        Transform world;
        SpriteRenderer aimMarker, keepBody;
        float keepFlash;

        public void Show(Battle battle, CompiledContent content, Fx fx)
        {
            Clear();
            this.battle = battle;
            this.content = content;
            this.fx = fx;
            world = new GameObject("World").transform;
            world.SetParent(transform, false);
            BuildLane(battle.Tuning);
            foreach (var f in battle.Squad)
            {
                var asset = content.FighterAsset(f.Def.Id);
                float size = f.IsKeeper ? 0.95f : 0.75f;
                var v = MakeUnit(f.Def.Name, Shapes.Circle, asset != null ? asset.color : Color.white, size, asset?.art, true, 1f);
                v.root.position = new Vector3(f.Position.X, f.Position.Y, 0f);
                fighters[f.Id] = v;
            }
            aimMarker = MakeSprite("Aim", Shapes.Ring, new Color(1f, 1f, 1f, 0.8f), 0.6f, 900, world);
            aimMarker.gameObject.SetActive(false);
        }

        void Clear()
        {
            if (world != null) Destroy(world.gameObject);
            fighters.Clear();
            heroes.Clear();
            shots.Clear();
            shotPool.Clear();
        }

        void BuildLane(BattleTuning t)
        {
            var bg = MakeSprite("Ground", Shapes.Square, Palette.LaneEdge, 1f, -1000, world);
            bg.transform.position = new Vector3(t.LaneLength * 0.5f, 0f, 0f);
            bg.transform.localScale = new Vector3(t.LaneLength + 40f, t.LaneHalfHeight * 2f + 0.6f, 1f);
            var lane = MakeSprite("Lane", Shapes.Square, Palette.Lane, 1f, -999, world);
            lane.transform.position = new Vector3(t.LaneLength * 0.5f, 0f, 0f);
            lane.transform.localScale = new Vector3(t.LaneLength + 40f, t.LaneHalfHeight * 2f, 1f);

            keepBody = MakeSprite("Keep", Shapes.Square, Palette.Keep, 1f, -900, world);
            keepBody.transform.position = new Vector3(t.KeepX - 0.9f, 0f, 0f);
            keepBody.transform.localScale = new Vector3(1.8f, t.LaneHalfHeight * 2f + 0.5f, 1f);
            var roof = MakeSprite("Roof", Shapes.Diamond, Palette.KeepRoof, 1f, -899, world);
            roof.transform.position = new Vector3(t.KeepX - 0.9f, t.LaneHalfHeight + 0.35f, 0f);
            roof.transform.localScale = new Vector3(2.4f, 1.0f, 1f);
        }

        /// A character: an animated sprite when it has art, otherwise a grey-box shape; plus a small health bar.
        CharacterView MakeUnit(string name, Sprite shape, Color color, float size, CharacterArt art, bool faceRight, float artScale)
        {
            var root = new GameObject(name).transform;
            root.SetParent(world, false);
            var v = new CharacterView { root = root, color = color, art = art, faceRight = faceRight };
            if (art != null)
            {
                // Pack characters sit small and off-centre in big frames: scale by the measured visible height and
                // shift so the visible character is centred on its lane position.
                float height = CharacterHeight * artScale;
                v.body = MakeSprite("Body", art.Frames(CharacterPose.Idle, faceRight)[0], Color.white, 1f, 0, root);
                v.baseScale = art.ScaleFor(height);
                v.body.transform.localPosition = new Vector3(0f, -art.visibleCenter.y * v.baseScale + art.footOffset * height, 0f);
                v.color = Color.white;
            }
            else
            {
                v.body = MakeSprite("Body", shape, color, 1f, 0, root);
                v.baseScale = size;
            }
            v.body.transform.localScale = Vector3.one * v.baseScale;
            float barY = art != null ? CharacterHeight * artScale * 0.5f + 0.2f : size * 0.5f + 0.18f;
            v.barBack = MakeSprite("HpBack", Shapes.Square, new Color(0f, 0f, 0f, 0.45f), 1f, 0, root);
            v.barBack.transform.localPosition = new Vector3(0f, barY, 0f);
            v.barBack.transform.localScale = new Vector3(0.8f, 0.09f, 1f);
            v.barFill = MakeSprite("HpFill", Shapes.Square, Palette.Health, 1f, 0, root);
            v.barFill.transform.localPosition = v.barBack.transform.localPosition;
            v.barFill.transform.localScale = v.barBack.transform.localScale;
            v.statusIcon = MakeSprite("Status", null, Color.white, 0.42f, 0, root);
            v.statusIcon.transform.localPosition = new Vector3(0f, barY + 0.32f, 0f);
            v.statusIcon.enabled = false;
            return v;
        }

        static SpriteRenderer MakeSprite(string name, Sprite sprite, Color color, float scale, int order, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one * scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            return sr;
        }

        public void Handle(List<BattleEvent> events)
        {
            foreach (var e in events)
            {
                var pos = new Vector2(e.Position.X, e.Position.Y);
                switch (e.Kind)
                {
                    case BattleEventKind.Attack:
                        if (fighters.TryGetValue(e.SourceId, out var shooter)) shooter.attackTime = AttackPoseSeconds;
                        break;
                    case BattleEventKind.HeroAttack:
                        if (heroes.TryGetValue(e.SourceId, out var striker)) striker.attackTime = AttackPoseSeconds;
                        break;
                    case BattleEventKind.Hit:
                        if (heroes.TryGetValue(e.TargetId, out var hv)) { hv.flash = 0.08f; hv.squash = 0.12f; }
                        else if (fighters.TryGetValue(e.TargetId, out var fv)) { fv.flash = 0.1f; fv.squash = 0.15f; }
                        if (e.Combo) break;
                        if (e.Element != Element.None) fx.Blob(pos, 0.5f, ElementLook.Color(e.Element).WithAlpha(0.8f), 0.2f);
                        else if (e.FromSkill) fx.Blob(pos, 0.5f, SkillColorOf(e.SourceId), 0.2f);
                        break;
                    case BattleEventKind.Zap:
                        {
                            var c = ElementLook.Color(e.Element);
                            fx.Line(new Vector2(e.From.X, e.From.Y), pos, c, 0.25f);
                            fx.Blob(pos, 0.6f, c.WithAlpha(0.8f), 0.2f);
                        }
                        break;
                    case BattleEventKind.HeroHealed:
                        fx.Blob(pos, 0.7f, new Color(0.5f, 1f, 0.55f, 0.6f), 0.35f);
                        fx.Text(pos + new Vector2(0f, 0.6f), $"+{e.Amount:0}", new Color(0.55f, 1f, 0.6f), 0.5f, 0.7f);
                        break;
                    case BattleEventKind.StatusApplied:
                        fx.Ring(pos, 0.7f, ElementLook.Color(e.Element), 0.3f, 0.4f);
                        break;
                    case BattleEventKind.Combo:
                        {
                            var a = ElementLook.Color(e.Element);
                            var b = ElementLook.Color(e.Other);
                            float r = battle.Tuning.ComboRadius;
                            fx.Blob(pos, r * 1.3f, Color.Lerp(a, b, 0.5f).WithAlpha(0.55f), 0.45f);
                            fx.Ring(pos, r * 1.2f, a, 0.5f, 0.1f);
                            fx.Ring(pos, r * 1.6f, b, 0.7f, 0.15f);
                            fx.Ring(pos, r * 2f, Color.white.WithAlpha(0.8f), 0.8f, 0.3f);
                            fx.Text(pos + new Vector2(0f, 0.9f), ElementLook.ComboName(e.Element, e.Other), Color.Lerp(a, Color.white, 0.25f), 1.1f);
                            fx.Shake(0.22f);
                        }
                        break;
                    case BattleEventKind.SkillCast:
                        var col = SkillColorOf(e.SourceId);
                        int growth = SkillGrowthOf(e.SourceId, e.Tag);
                        float loud = 1f + growth * 0.35f;
                        if (fighters.TryGetValue(e.SourceId, out var caster))
                        {
                            caster.squash = 0.25f;
                            caster.attackTime = AttackPoseSeconds;
                            if (growth > 0) fx.Ring(caster.root.position, 0.6f * loud, col, 0.3f);
                        }
                        if (e.Amount > 0f)
                        {
                            fx.Ring(pos, e.Amount, col, 0.45f * loud);
                            fx.Blob(pos, e.Amount * 1.2f, col.WithAlpha(0.3f + growth * 0.15f), 0.3f * loud);
                            if (growth == (int)SkillGrowth.Master) fx.Ring(pos, e.Amount * 1.4f, Color.white, 0.6f, 0.5f);
                            fx.Shake(0.05f * loud);
                        }
                        break;
                    case BattleEventKind.Rally:
                        var keeper = battle.Keeper.Position;
                        fx.Ring(new Vector2(keeper.X, keeper.Y), 6f, Palette.Rally, 0.6f, 0.05f);
                        fx.Shake(0.15f);
                        break;
                    case BattleEventKind.HeroDefeated:
                        fx.Ring(pos, 0.6f, Color.white, 0.3f);
                        break;
                    case BattleEventKind.CreatureFreed:
                        fx.Ring(pos, 1.2f, new Color(1f, 0.9f, 0.5f), 0.8f, 0.1f);
                        fx.Blob(pos, 0.9f, new Color(1f, 0.95f, 0.7f, 0.6f), 0.6f);
                        break;
                    case BattleEventKind.HeroReachedKeep:
                        keepFlash = 0.25f;
                        fx.Shake(0.2f);
                        fx.Blob(pos, 1.2f, Palette.HealthLow, 0.35f);
                        break;
                    case BattleEventKind.KnockedOut:
                        fx.Ring(pos, 0.8f, new Color(0.6f, 0.6f, 0.7f), 0.4f);
                        break;
                    case BattleEventKind.Recovered:
                        fx.Ring(pos, 0.9f, Palette.Health, 0.4f);
                        break;
                }
            }
        }

        int SkillGrowthOf(int fighterId, string skillId)
        {
            foreach (var f in battle.Squad)
                if (f.Id == fighterId)
                    foreach (var s in f.Skills)
                        if (s.Def.Id == skillId) return (int)s.Def.Growth;
            return 0;
        }

        Color SkillColorOf(int fighterId)
        {
            foreach (var f in battle.Squad)
                if (f.Id == fighterId)
                {
                    var a = content.FighterAsset(f.Def.Id);
                    return a != null ? a.skillColor : Color.white;
                }
            return Color.white;
        }

        void Update()
        {
            if (battle == null) return;
            float dt = Time.deltaTime;

            foreach (var f in battle.Squad)
            {
                var v = fighters[f.Id];
                var tint = f.IsKnockedOut ? new Color(0.5f, 0.5f, 0.6f, 0.6f) : v.color;
                Animate(v, tint, dt, f.IsKnockedOut ? 0.8f : 1f, CharacterPose.Idle, f.Position.Y);
                SetBar(v, f.IsKnockedOut ? 1f - f.KnockoutRemaining / battle.Tuning.KnockoutSeconds : f.Hp / f.MaxHp,
                    f.IsKnockedOut ? new Color(0.7f, 0.7f, 0.8f) : Palette.Health, f.IsKnockedOut || f.Hp < f.MaxHp);
            }

            seen.Clear();
            foreach (var h in battle.Heroes)
            {
                seen.Add(h.Id);
                if (!heroes.TryGetValue(h.Id, out var v))
                {
                    var asset = content.HeroAsset(h.Def.Id);
                    var look = asset != null ? asset.look : HeroLook.Square;
                    var shape = look == HeroLook.Diamond ? Shapes.Diamond : Shapes.Square;
                    float bigness = Mathf.Clamp(h.Def.Size / 0.4f, 0.8f, 2.2f);
                    v = MakeUnit(h.Def.Name, shape, asset != null ? asset.color : Color.yellow, h.Def.Size * 2f, asset?.art, false, bigness);
                    if (asset?.art == null && look == HeroLook.Tall) { v.baseScale = h.Def.Size * 2.6f; v.aspect = 1.8f / 2.6f; }
                    if (h.Def.IsBoss)
                    {
                        MakeSprite("Crown", Shapes.Ring, new Color(1f, 0.85f, 0.35f, 0.7f), h.Def.Size * 2.6f, 0, v.root).sortingOrder = -1;
                        v.barBack.transform.localScale = new Vector3(1.6f, 0.14f, 1f);
                    }
                    heroes[h.Id] = v;
                }
                v.root.position = new Vector3(h.Position.X, h.Position.Y, 0f);
                bool held = h.IsEngaged || h.Status == Element.Bloom;
                if (h.Status == Element.Bloom) v.animTime -= dt; // rooted: frozen mid-step
                Animate(v, StatusTint(v.color, h.Status), dt, 1f, held ? CharacterPose.Idle : CharacterPose.Move, h.Position.Y);
                SetBar(v, h.Hp / h.MaxHp, Palette.HeroHealth, h.Hp < h.MaxHp);
                v.statusIcon.enabled = h.Status != Element.None;
                if (v.statusIcon.enabled)
                {
                    v.statusIcon.sprite = ElementLook.Icon(h.Status);
                    v.statusIcon.color = ElementLook.IconTint(h.Status);
                    v.statusIcon.sortingOrder = v.barFill.sortingOrder + 1;
                    float fade = Mathf.Clamp01(h.StatusRemaining / 0.6f);
                    v.statusIcon.transform.localScale = Vector3.one * (0.55f + Mathf.Sin(Time.time * 9f) * 0.04f) * (0.6f + 0.4f * fade);
                }
                if (v.art == null && !held) v.body.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 10f + h.Id) * 4f);
            }
            RemoveMissing(heroes);

            seen.Clear();
            foreach (var p in battle.Projectiles)
            {
                seen.Add(p.Id);
                if (!shots.TryGetValue(p.Id, out var sr))
                {
                    sr = shotPool.Count > 0 ? shotPool.Pop() : MakeSprite("Shot", Shapes.Circle, Color.white, 1f, 800, world);
                    sr.gameObject.SetActive(true);
                    sr.color = !p.FromSquad ? Palette.HeroShot
                        : p.Element != Element.None ? ElementLook.Color(p.Element)
                        : p.FromSkill ? SkillColorOf(p.SourceId) : new Color(1f, 0.98f, 0.85f);
                    sr.transform.localScale = Vector3.one * (p.FromSkill ? 0.32f + (int)p.Growth * 0.1f : 0.2f);
                    shots[p.Id] = sr;
                }
                sr.transform.position = new Vector3(p.Position.X, p.Position.Y, 0f);
            }
            var gone = new List<int>();
            foreach (var kv in shots) if (!seen.Contains(kv.Key)) gone.Add(kv.Key);
            foreach (var id in gone) { shots[id].gameObject.SetActive(false); shotPool.Push(shots[id]); shots.Remove(id); }

            if (battle.AimPoint.HasValue)
            {
                aimMarker.gameObject.SetActive(true);
                aimMarker.transform.position = new Vector3(battle.AimPoint.Value.X, battle.AimPoint.Value.Y, 0f);
                aimMarker.transform.localScale = Vector3.one * (0.55f + Mathf.Sin(Time.time * 8f) * 0.05f);
            }
            else aimMarker.gameObject.SetActive(false);

            keepFlash = Mathf.Max(0f, keepFlash - dt);
            keepBody.color = Color.Lerp(Palette.Keep, Palette.HealthLow, keepFlash * 4f);
        }

        void RemoveMissing(Dictionary<int, CharacterView> map)
        {
            var gone = new List<int>();
            foreach (var kv in map) if (!seen.Contains(kv.Key)) gone.Add(kv.Key);
            foreach (var id in gone) { Destroy(map[id].root.gameObject); map.Remove(id); }
        }

        static void Animate(CharacterView v, Color tint, float dt, float sizeMul, CharacterPose pose, float laneY)
        {
            v.flash = Mathf.Max(0f, v.flash - dt);
            v.squash = Mathf.Max(0f, v.squash - dt);
            v.attackTime = Mathf.Max(0f, v.attackTime - dt);
            v.animTime += dt;
            if (v.art != null)
            {
                var frames = v.art.Frames(v.attackTime > 0f ? CharacterPose.Attack : pose, v.faceRight);
                if (frames.Length > 0) v.body.sprite = frames[(int)(v.animTime * v.art.framesPerSecond) % frames.Length];
                v.body.color = v.flash > 0f ? HitTint : tint;
            }
            else v.body.color = v.flash > 0f ? Color.white : tint;

            // Lower on the lane draws in front.
            int order = Mathf.RoundToInt(-laneY * 100f);
            v.body.sortingOrder = order;
            v.barBack.sortingOrder = order + 1;
            v.barFill.sortingOrder = order + 2;

            float s = v.squash * 2.2f;
            float height = v.baseScale * sizeMul * (1f - s * 0.3f);
            float width = v.baseScale * v.aspect * sizeMul * (1f + s * 0.3f);
            v.body.transform.localScale = new Vector3(width, height, 1f);
        }

        /// A Hero's body colour while it carries a status: burn and shock flicker, chill and root hold a cool or green cast.
        static Color StatusTint(Color baseColor, Element status)
        {
            if (status == Element.None) return baseColor;
            float k = status == Element.Frost || status == Element.Bloom ? 0.45f : 0.3f + Mathf.Sin(Time.time * 18f) * 0.15f;
            return Color.Lerp(baseColor, ElementLook.Color(status), k);
        }

        static void SetBar(CharacterView v, float fraction, Color color, bool visible)
        {
            v.barBack.enabled = v.barFill.enabled = visible;
            if (!visible) return;
            fraction = Mathf.Clamp01(fraction);
            var back = v.barBack.transform;
            v.barFill.color = color;
            v.barFill.transform.localScale = new Vector3(back.localScale.x * fraction, back.localScale.y, 1f);
            v.barFill.transform.localPosition = back.localPosition + new Vector3(-back.localScale.x * (1f - fraction) * 0.5f, 0f, 0f);
        }
    }
}
