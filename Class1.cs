﻿using System.Collections;
using Il2CppRUMBLE.Networking.MatchFlow;
using Il2CppRUMBLE.Players;
using MelonLoader;
using MelonLoader.Utils;
using NAudio.Wave;
using RumbleModdingAPI;
using RumbleModUI;
using UnityEngine;
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
        
        public static WaveOutEvent? CurrentAudio;
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
            isModEnabled = mod.AddToList("Is mod enabled", true,1, "Enable or disable the mod", new Tags());
            
            mod.GetFromFile();
            
            mod.ModSaved += Save;

            UI.instance.AddMod(mod); // Use the instance-level field
            MelonLogger.Msg("Added Mod: " + BuildInfo.ModName);
        }

        private static void Save()
        {
            CurrentAudio.Volume = (float)volume.Value;

            if ((bool)isModEnabled.Value == false)
            {
                CurrentAudio.Dispose();
            }
        }
        
        private static void SceneLoaded() // Logic/CombatMusic
        {
            
            if (CurrentAudio != null)
            {
                CurrentAudio.Dispose();
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
                        CurrentAudio.Play();
                    }
                    
                    if (CurrentAudio != null)
                    {
                        CurrentAudio.Dispose();
                    }

                    var combatMusic = GameObject.Find("CombatMusic");
                    combatMusic.SetActive(false);
                    MelonCoroutines.Start(StartBattleMusic(1f)); // Run the coroutine properly
                }
                catch (Exception e)
                {
                    MelonLogger.Error("Whoops, the music crashed.");
                    MelonLogger.Error(e.Message);
                }
            }
        }

        [HarmonyLib.HarmonyPatch(typeof(MatchHandler), "OnPlayerDefeated")]
        public static class PlayerKill
        {
            public static void Prefix( ref Player p, ref string killDescription)
            {
                if (!(Calls.Scene.GetSceneName() is "Map0" or "Map1")) return;
                
                MelonLogger.Warning("PlayerKill trigerred");

                if (CurrentAudio != null)
                {
                    CurrentAudio.Pause();
                }
            }
        }

        

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
            
            PlaySoundIfFileExists(mp3Files[randomIndex]);
        }
        
        //Everything bellow was stolen from Ulvak's mod 
        // https://thunderstore.io/c/rumble/p/UlvakSkillz/Rumble_Additional_Sounds/
        private static IEnumerator PlaySound(string FilePath)
        {
            Mp3FileReader reader = new Mp3FileReader(FilePath);
            WaveOutEvent waveOut = new WaveOutEvent();
            CurrentAudio = waveOut;
            waveOut.Init((IWaveProvider)(object)reader);
            waveOut.Play();
            
            waveOut.Volume = (float)volume.Value; 
            while ((int)waveOut.PlaybackState == 1)
            {
                yield return (object)new WaitForFixedUpdate();
            }
            ((Stream)(object)reader).Dispose();
            waveOut.Dispose();
        }

        public static void PlaySoundIfFileExists(string soundFilePath)
        {
            try
            {
                if (File.Exists(soundFilePath))
                {
                    MelonCoroutines.Start(PlaySound(soundFilePath)); // Pass the correct path
                }
                else
                {
                    MelonLogger.Error($"Sound file does not exist: {soundFilePath}");
                }
            }
            catch (Exception e)
            {
                MelonLogger.Error($"Failed to play sound: {e.Message}");
            }
        }
    }
}

