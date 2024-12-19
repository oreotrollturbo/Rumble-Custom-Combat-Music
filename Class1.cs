using Il2CppRUMBLE.Networking.MatchFlow;
using MelonLoader;
using RumbleModdingAPI;
using RumbleSoundsOnSceneChange;
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
            
        }
        
        
        [HarmonyLib.HarmonyPatch(typeof(MatchHandler), "ExecuteNextRound")]
        public static class RoundPatch //Thanks to Elmish for showing me how to do this
        {
            public static void Prefix() //TODO check if this works
            {
                var combatMusic = GameObject.Find("CombatMusic");
                combatMusic.SetActive(false);
                AdditionalSounds.PlaySoundIfFileExists(@"\AdditionalSounds\jetpack_hellRide.mp3",0);
                MelonLogger.Warning("New round triggered");
                
            }
        }
    }
}

