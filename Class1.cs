using System.Collections;
using Il2CppRUMBLE.Networking.MatchFlow;
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
        private static string? _currentSceneName;
        
        private static ModSetting<float> volume; // max 1f min 0.001f
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
            
            volume = mod.AddToList("Float Setting", 0.05f, "Is Float.", new Tags());
            
            mod.GetFromFile();

            UI.instance.AddMod(mod); // Use the instance-level field
            MelonLogger.Msg("Added Mod: " + BuildInfo.ModName);
        }
        
        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            _currentSceneName = sceneName;
        }
        
        private static void SceneLoaded() // Logic/CombatMusic
        {
            if (CurrentAudio != null)
            {
                CurrentAudio.Dispose();
            }

            if (_currentSceneName is "Map0" or "Map1")
            {
                MelonLogger.Msg("Map is battle map.");
                MelonCoroutines.Start(StartBattleMusic(6f)); // Run the coroutine properly
            }
        }

        [HarmonyLib.HarmonyPatch(typeof(MatchHandler), "ExecuteNextRound")]
        public static class RoundPatch
        {
            public static void Prefix()
            {
                try
                {
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

        private static IEnumerator StartBattleMusic(float delay)
        {
            yield return new WaitForSeconds(delay);

            MelonLogger.Msg("Playing new sound");
            PlaySoundIfFileExists(Path.Combine(folderPath, "jetpack_hellRide.mp3")); // Use Path.Combine for better path handling
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

