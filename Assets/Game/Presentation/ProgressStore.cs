using System;
using System.IO;
using LittleKeep.Rules;
using UnityEngine;

namespace LittleKeep.Game
{
    /// Reads and writes permanent progress as a versioned JSON file in the app's private storage.
    /// Writes go to a temporary file first, so a crash mid-save never leaves a broken file behind.
    public static class ProgressStore
    {
        static string FilePath => Path.Combine(Application.persistentDataPath, "progress.json");

        public static KeepProgress Load(ProgressContent content)
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var data = JsonUtility.FromJson<ProgressData>(File.ReadAllText(FilePath));
                    if (data == null) throw new FormatException("the file is empty.");
                    var keep = new KeepProgress(content, data);
                    if (keep.FromNewerVersion) Debug.LogWarning("Progress was saved by a newer version of the game; it will not be saved over.");
                    return keep;
                }
            }
            catch (Exception e)
            {
                // Keep the unreadable file for inspection rather than overwriting it.
                Debug.LogWarning("Could not read saved progress: " + e.Message);
                try { File.Copy(FilePath, FilePath + ".unreadable", true); } catch { }
            }
            return new KeepProgress(content);
        }

        public static void Save(KeepProgress progress)
        {
            if (progress.FromNewerVersion) return;
            try
            {
                string temp = FilePath + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(progress.ToData(), true));
                if (File.Exists(FilePath)) File.Replace(temp, FilePath, null);
                else File.Move(temp, FilePath);
            }
            catch (Exception e)
            {
                Debug.LogError("Could not save progress: " + e.Message);
            }
        }
    }
}
