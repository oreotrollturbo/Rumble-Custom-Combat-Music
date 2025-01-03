﻿using System.Collections;
using Il2CppRUMBLE.Networking.MatchFlow;
using Il2CppRUMBLE.Players;
using MelonLoader;
using MelonLoader.Utils;
using RumbleModdingAPI;
using RumbleModUI;
using UnityEngine;
using UnityEngine.Playables;
using BuildInfo = CustomBattleMusic.BuildInfo;

[assembly: MelonInfo(typeof(CustomBattleMusic.Main), BuildInfo.ModName, BuildInfo.ModVersion, BuildInfo.Author)]
[assembly: MelonGame("Buckethead Entertainment", "RUMBLE")]

namespace CustomBattleMusic
{
    public static class BuildInfo
    {
        public const string ModName = "Custom battle music";
        public const string ModVersion = "1.0.0";
        public const string Description = "Replace the rumble battle music with your own!";
        public const string Author = "oreotrollturbo";
    }

    public class Main : MelonMod
    {
        private Mod mod = new Mod();

        public static AudioManager.ClipData? CurrentAudio;
        public static string folderPath = MelonEnvironment.UserDataDirectory + @"\CustomBattleMusic";

        private static ModSetting<float> volume; // max 1f min 0.001f
        private static ModSetting<bool> isModEnabled;
        public override void OnLateInitializeMelon()
        {
            Calls.onMapInitialized += SceneLoaded;

            if (!Directory.Exists(folderPath))
            {
                MelonLogger.Error("File at " + folderPath + " does not exist");
            }

            UI.instance.UI_Initialized += OnUIInit;
        }

        public void OnUIInit()
        {
            mod.ModName = BuildInfo.ModName; // Use the instance-level field
            mod.ModVersion = BuildInfo.ModVersion;
            mod.SetFolder("CustomBattleMusic");
            mod.AddDescription("Description", "", BuildInfo.Description, new Tags { IsSummary = true });

            volume = mod.AddToList("Volume", 0.05f, "The volume at which every song will play", new Tags());
            isModEnabled = mod.AddToList("Is mod enabled", true, 1, "Enable or disable the mod", new Tags());

            mod.GetFromFile();

            mod.ModSaved += Save;

            UI.instance.AddMod(mod); // Use the instance-level field
            MelonLogger.Msg("Added Mod: " + BuildInfo.ModName);
        }

        private static void Save()
        {
            if (CurrentAudio == null) return;

            AudioManager.ChangeVolume(CurrentAudio ,(float)volume.Value);

            if ((bool)isModEnabled.Value == false)
            {
                CurrentAudio.Pause();
                CurrentAudio = null; // Always reset
                MelonLogger.Msg("Mod disabled stopping all music");
            }
        }

        private static void SceneLoaded() // Logic/CombatMusic
        {
            if (CurrentAudio != null)
            {
                MelonLogger.Msg("New scene loaded disposing of old music object");
                CurrentAudio.Pause();
                CurrentAudio = null;
            }
        }

        [HarmonyLib.HarmonyPatch(typeof(MatchHandler), "ExecuteRound")]
        public static class RoundPatch
        {
            public static void Prefix(ref int roundNo)
            {
                try
                {
                    if (roundNo > 0 && CurrentAudio != null)
                    {
                        MelonLogger.Msg("New round started resuming music");
                        PlayBattleMusic(2f);
                    }
                    else if (roundNo == 0)
                    {
                        MelonCoroutines.Start(StartBattleMusic(1f)); // Run the coroutine properly
                    }

                    var combatMusic = GameObject.Find("CombatMusic");
                    if (combatMusic != null)
                    {
                        combatMusic.SetActive(false);
                    }
                }
                catch (Exception e)
                {
                    MelonLogger.Error("Whoops, the music crashed.");
                    MelonLogger.Error(e.Message);
                }
            }
        }

        [HarmonyLib.HarmonyPatch(typeof(MatchHandler), "OnPlayerDefeated")]
        public static class PlayerDefeat
        {
            public static void Prefix(Player p, string killDescription)
            {
                if (!(Calls.Scene.GetSceneName() is "Map0" or "Map1")) return;

                if (CurrentAudio != null)
                {
                    MelonLogger.Msg("Round ended pausing music");
                    CurrentAudio.Pause();
                }
            }
        }

        private static IEnumerator PlayBattleMusic(float delay)
        {
            yield return new WaitForSeconds(delay);
            CurrentAudio?.Play();
        }

        //Thank you to MadLike for showing me this <3
        private static IEnumerator StartBattleMusic(float delay)
        {
            if (!(bool)isModEnabled.Value) yield break;

            yield return new WaitForSeconds(delay);

            MelonLogger.Msg("Playing new song");

            string[] mp3Files = Directory.GetFiles(folderPath, "*.mp3");

            if (mp3Files.Length == 0)
            {
                MelonLogger.Warning("No MP3 files found in the specified folder.");
            }

            System.Random random = new System.Random();
            int randomIndex = random.Next(mp3Files.Length);

            CurrentAudio = AudioManager.PlaySoundIfFileExists(mp3Files[randomIndex]);
        }
    }
}
