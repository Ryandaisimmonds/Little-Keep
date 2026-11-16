using LittleKeep.Game;
using LittleKeep.Rules;
using UnityEditor;
using UnityEngine;

namespace LittleKeep.Editor
{
    /// Gives every Skill (and its Master Form) its Element. Safe to run again.
    public static class ElementSetup
    {
        const string Skills = "Assets/Game/Content/Skills";

        [MenuItem("Little Keep/Assign Elements")]
        public static void Apply()
        {
            Set("StarVolley", Element.Spark);
            Set("PopBurst", Element.Fire);
            Set("ThornFan", Element.Bloom);
            Set("SporePuff", Element.Bloom);
            Set("Stomp", Element.Spark);
            Set("FeatherFlurry", Element.Frost);
            Set("BubbleVolley", Element.Frost);
            AssetDatabase.SaveAssets();

            foreach (var guid in AssetDatabase.FindAssets("t:SkillAsset", new[] { Skills }))
            {
                var s = AssetDatabase.LoadAssetAtPath<SkillAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (s.def.Element == Element.None) Debug.LogWarning($"Skill {s.name} has no Element.");
            }
        }

        static void Set(string file, Element element)
        {
            var s = AssetDatabase.LoadAssetAtPath<SkillAsset>($"{Skills}/{file}.asset");
            if (s == null) { Debug.LogWarning($"No Skill asset {file}"); return; }
            s.def.Element = element;
            if (s.masterForm != null) s.masterForm.Element = element;
            EditorUtility.SetDirty(s);
        }
    }
}
