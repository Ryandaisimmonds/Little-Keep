#if UNITY_EDITOR || DEVELOPMENT_BUILD
using LittleKeep.Rules;

namespace LittleKeep.Game
{
    /// Development builds only (not compiled into release builds): Battles for checking how the game looks and
    /// runs, outside any Run, so nothing is saved.
    public static class DevBattles
    {
        /// The busiest Battle the game can make: the last Chapter's boss twice over with double the Heroes, all
        /// sturdy enough to pile up, against the Keeper and four Familiars with every Skill in Master Form.
        public static BattleSetup Stress(CompiledContent content)
        {
            var run = content.Run;
            var setup = new BattleSetup
            {
                Keeper = AllMastered(run.Keeper),
                KeepHp = 100000f,
                KeepMaxHp = 100000f,
                Seed = 7,
                Tuning = run.BattleTuning.Clone(),
            };
            for (int i = 0; i < run.Familiars.Count && i < 4; i++) setup.Familiars.Add(AllMastered(run.Familiars[i]));
            var boss = run.Chapters[run.Chapters.Count - 1].Boss;
            for (int repeat = 0; repeat < 2; repeat++)
                foreach (var w in boss.Waves)
                {
                    var wave = new WaveDef { SpawnInterval = w.SpawnInterval };
                    foreach (var e in w.Entries)
                    {
                        if (e.Hero == null) continue;
                        var h = e.Hero.Clone();
                        h.Hp *= 20f;
                        wave.Entries.Add(new WaveEntry { Hero = h, Count = h.IsBoss ? e.Count : e.Count * 2 });
                    }
                    setup.Waves.Add(wave);
                }
            return setup;
        }

        /// A sturdier copy with every Skill swapped for its Master Form, marked as such so it draws its biggest effects.
        static FighterDef AllMastered(FighterDef d)
        {
            var c = d.Clone();
            c.Hp *= 4f;
            c.Skills.Clear();
            foreach (var s in d.Skills)
            {
                var master = (s.MasterForm ?? s).Clone();
                master.Growth = SkillGrowth.Master;
                c.Skills.Add(master);
            }
            return c;
        }
    }
}
#endif
