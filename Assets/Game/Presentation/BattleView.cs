using System.Collections.Generic;
using LittleKeep.Rules;
using UnityEngine;

namespace LittleKeep.Game
{
    /// Draws a Battle in grey-box shapes and turns its events into effects. Reads rule state; never changes it.
    public sealed class BattleView : MonoBehaviour
    {
        sealed class UnitView
        {
            public Transform root;
            public SpriteRenderer body;
            public SpriteRenderer barBack, barFill;
            public Color color;
            public float baseScale;
            public float aspect = 1f;
            public float flash, squash;
        }

        Battle battle;
        CompiledContent content;
        Fx fx;
        readonly Dictionary<int, UnitView> fighters = new Dictionary<int, UnitView>();
        readonly Dictionary<int, UnitView> heroes = new Dictionary<int, UnitView>();
        readonly Dictionary<int, SpriteRenderer> shots = new Dictionary<int, SpriteRenderer>();
        readonly Stack<SpriteRenderer> shotPool = new Stack<SpriteRenderer>();
        readonly HashSet<int> seen = new HashSet<int>();
        Transform world;
        SpriteRenderer aimMarker;
        SpriteRenderer keepBody;
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
                var color = asset != null ? asset.color : Color.white;
                float size = f.IsKeeper ? 0.95f : 0.75f;
                fighters[f.Id] = MakeUnit(f.IsKeeper ? "Keeper" : f.Def.Name, Shapes.Circle, color, size, new Vector2(f.Position.X, f.Position.Y), 10);
            }
            aimMarker = MakeSprite("Aim", Shapes.Ring, new Color(1f, 1f, 1f, 0.8f), 0.6f, 15, world);
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
            var bg = MakeSprite("Ground", Shapes.Square, Palette.LaneEdge, 1f, -10, world);
            bg.transform.position = new Vector3(t.LaneLength * 0.5f, 0f, 0f);
            bg.transform.localScale = new Vector3(t.LaneLength + 40f, t.LaneHalfHeight * 2f + 0.6f, 1f);
            var lane = MakeSprite("Lane", Shapes.Square, Palette.Lane, 1f, -9, world);
            lane.transform.position = new Vector3(t.LaneLength * 0.5f, 0f, 0f);
            lane.transform.localScale = new Vector3(t.LaneLength + 40f, t.LaneHalfHeight * 2f, 1f);

            keepBody = MakeSprite("Keep", Shapes.Square, Palette.Keep, 1f, -5, world);
            keepBody.transform.position = new Vector3(t.KeepX - 0.9f, 0f, 0f);
            keepBody.transform.localScale = new Vector3(1.8f, t.LaneHalfHeight * 2f + 0.5f, 1f);
            var roof = MakeSprite("Roof", Shapes.Diamond, Palette.KeepRoof, 1f, -4, world);
            roof.transform.position = new Vector3(t.KeepX - 0.9f, t.LaneHalfHeight + 0.35f, 0f);
            roof.transform.localScale = new Vector3(2.4f, 1.0f, 1f);
        }

        UnitView MakeUnit(string name, Sprite sprite, Color color, float size, Vector2 pos, int order)
        {
            var root = new GameObject(name).transform;
            root.SetParent(world, false);
            root.position = pos;
            var body = MakeSprite("Body", sprite, color, 1f, order, root);
            body.transform.localScale = Vector3.one * size;
            var back = MakeSprite("HpBack", Shapes.Square, new Color(0f, 0f, 0f, 0.35f), 1f, order + 1, root);
            back.transform.localPosition = new Vector3(0f, size * 0.5f + 0.18f, 0f);
            back.transform.localScale = new Vector3(0.8f, 0.09f, 1f);
            var fill = MakeSprite("HpFill", Shapes.Square, Palette.Health, 1f, order + 2, root);
            fill.transform.localPosition = back.transform.localPosition;
            fill.transform.localScale = back.transform.localScale;
            return new UnitView { root = root, body = body, barBack = back, barFill = fill, color = color, baseScale = size };
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
                    case BattleEventKind.Hit:
                        if (heroes.TryGetValue(e.TargetId, out var hv)) { hv.flash = 0.08f; hv.squash = 0.12f; }
                        else if (fighters.TryGetValue(e.TargetId, out var fv)) { fv.flash = 0.1f; fv.squash = 0.15f; }
                        if (e.FromSkill) fx.Blob(pos, 0.5f, SkillColorOf(e.SourceId), 0.2f);
                        break;
                    case BattleEventKind.SkillCast:
                        var col = SkillColorOf(e.SourceId);
                        int tier = SkillTier(e.SourceId, e.Tag);
                        float loud = 1f + tier * 0.35f;
                        if (fighters.TryGetValue(e.SourceId, out var caster))
                        {
                            caster.squash = 0.25f;
                            if (tier > 0) fx.Ring(caster.root.position, 0.6f * loud, col, 0.3f);
                        }
                        if (e.Amount > 0f)
                        {
                            fx.Ring(pos, e.Amount, col, 0.45f * loud);
                            fx.Blob(pos, e.Amount * 1.2f, col.WithAlpha(0.3f + tier * 0.15f), 0.3f * loud);
                            if (tier == 2) fx.Ring(pos, e.Amount * 1.4f, Color.white, 0.6f, 0.5f);
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

        int SkillTier(int fighterId, string skillId)
        {
            foreach (var f in battle.Squad)
                if (f.Id == fighterId)
                    foreach (var s in f.Skills)
                        if (s.Def.Id == skillId) return s.Def.Tier;
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
                float ko = f.IsKnockedOut ? 1f : 0f;
                var c = Color.Lerp(v.color, new Color(0.55f, 0.55f, 0.6f, 0.55f), ko);
                Animate(v, c, dt, f.IsKnockedOut ? 0.75f : 1f);
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
                    var sprite = look == HeroLook.Diamond ? Shapes.Diamond : Shapes.Square;
                    v = MakeUnit(h.Def.Name, sprite, asset != null ? asset.color : Color.yellow, h.Def.Size * 2f, Vector2.zero, 5);
                    if (look == HeroLook.Tall) { v.baseScale = h.Def.Size * 2.6f; v.aspect = 1.8f / 2.6f; }
                    if (h.Def.IsBoss)
                    {
                        MakeSprite("Crown", Shapes.Ring, new Color(1f, 0.85f, 0.35f), h.Def.Size * 2.6f, 4, v.root);
                        v.barBack.transform.localScale = new Vector3(1.6f, 0.14f, 1f);
                    }
                    heroes[h.Id] = v;
                }
                v.root.position = new Vector3(h.Position.X, h.Position.Y, 0f);
                Animate(v, v.color, dt, 1f);
                SetBar(v, h.Hp / h.MaxHp, Palette.HeroHealth, h.Hp < h.MaxHp);
                if (!h.IsEngaged) v.body.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 10f + h.Id) * 4f);
            }
            RemoveMissing(heroes);

            seen.Clear();
            foreach (var p in battle.Projectiles)
            {
                seen.Add(p.Id);
                if (!shots.TryGetValue(p.Id, out var sr))
                {
                    sr = shotPool.Count > 0 ? shotPool.Pop() : MakeSprite("Shot", Shapes.Circle, Color.white, 1f, 12, world);
                    sr.gameObject.SetActive(true);
                    sr.color = p.FromSquad ? (p.FromSkill ? SkillColorOf(p.SourceId) : new Color(1f, 0.98f, 0.85f)) : Palette.HeroShot;
                    sr.transform.localScale = Vector3.one * (p.FromSkill ? 0.32f + p.Tier * 0.1f : 0.2f);
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

        void RemoveMissing(Dictionary<int, UnitView> map)
        {
            var gone = new List<int>();
            foreach (var kv in map) if (!seen.Contains(kv.Key)) gone.Add(kv.Key);
            foreach (var id in gone) { Destroy(map[id].root.gameObject); map.Remove(id); }
        }

        static void Animate(UnitView v, Color color, float dt, float sizeMul)
        {
            v.flash = Mathf.Max(0f, v.flash - dt);
            v.squash = Mathf.Max(0f, v.squash - dt);
            v.body.color = v.flash > 0f ? Color.white : color;
            float s = v.squash * 2.2f;
            float height = v.baseScale * sizeMul * (1f - s * 0.3f);
            float width = v.baseScale * v.aspect * sizeMul * (1f + s * 0.3f);
            v.body.transform.localScale = new Vector3(width, height, 1f);
        }

        static void SetBar(UnitView v, float fraction, Color color, bool visible)
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
