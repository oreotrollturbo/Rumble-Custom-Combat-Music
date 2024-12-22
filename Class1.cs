using System.Collections;
using Il2CppRUMBLE.Networking.MatchFlow;
using MelonLoader;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using RumbleModdingAPI;
using UnityEngine;
using BuildInfo = CustomBattleMusic.BuildInfo;



[assembly: MelonInfo(typeof(CustomBattleMusic.Main), BuildInfo.ModName, BuildInfo.ModVersion, BuildInfo.Author)]
[assembly: MelonGame("Buckethead Entertainment", "RUMBLE")]

namespace CustomBattleMusic
{
    public static class BuildInfo
    {
        public const string ModName = "Custom battle music";
        public const string ModVersion = "1.0.1";
        public const string Author = "oreotrollturbo";
    }
    public class Main : MelonMod
    {
        public static WasapiOut currentAudio;
        public override void OnLateInitializeMelon()
        {
            string filePath = @"\AdditionalSounds\CustomBattleMusic";
            Calls.onMapInitialized += SceneLoaded;

            if (!File.Exists(filePath))
            {
                using (File.Create(filePath))
                {
                    MelonLogger.Msg($"File '{filePath}' created successfully.");
                }
            }
        }


        private static void SceneLoaded() //  Logic/CombatMusic
        {
            currentAudio.Dispose();
            if (Calls.Scene.GetSceneName() is "Map0" or "Map1")
            {
                StartBattleMusic(6f);
            }
        }
        
        
        [HarmonyLib.HarmonyPatch(typeof(MatchHandler), "ExecuteNextRound")]
        public static class RoundPatch //Thanks to Elmish for showing me how to do this
        {
            public static void Prefix()
            {
                currentAudio.Dispose();
                var combatMusic = GameObject.Find("CombatMusic");
                combatMusic.SetActive(false);
                StartBattleMusic(1f);
            }
        }
        
        
        private static IEnumerator StartBattleMusic(float delay)
        {
            
            yield return new WaitForSeconds(delay);

            currentAudio = PlayAudio(@"\AdditionalSounds\jetpack_hellRide.mp3");
        }
        
        public static WasapiOut? PlayAudio(string filePath)
        {
            try
            {
                // Create a WaveStream to read the audio file
                using var audioFileReader = new AudioFileReader(filePath);

                // Initialize WASAPI output device
                var wasapiOut = new WasapiOut(AudioClientShareMode.Shared, true, 200);

                // Set the input source for playback
                wasapiOut.Init(audioFileReader);

                // Play the audio
                wasapiOut.Play();

                Console.WriteLine("Playing audio at " + filePath);
                
                wasapiOut.Volume /= 2;

                return wasapiOut;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while playing the file '{filePath}': {ex.Message}");
                return null;
            }
        }
    }
}

