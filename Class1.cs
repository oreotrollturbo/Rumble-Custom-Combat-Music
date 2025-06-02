using System.Collections;
using Il2CppRUMBLE.Interactions.InteractionBase;
using Il2CppRUMBLE.Networking.MatchFlow;
using Il2CppRUMBLE.Players;
using Il2CppTMPro;
using MelonLoader;
using MelonLoader.Utils;
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

        public static GameObject mp3Text;
        public static AudioManager.ClipData? CurrentAudio;
        public static string folderPath = MelonEnvironment.UserDataDirectory + @"\CustomBattleMusic";
        public static string[] mp3Files = Directory.GetFiles(folderPath, "*.mp3");
        public static string[] playList = mp3Files;
        public static string nextSong;
        
        private static ModSetting<float> volume; // max 1f min 0.001f
        private static ModSetting<bool> isModEnabled;
        private static ModSetting<bool> shouldPauseOnRoundEnd;
        private static ModSetting<bool> shouldShuffle;
        public override void OnLateInitializeMelon()
        {
            Calls.onMapInitialized += SceneLoaded;

            if (!Directory.Exists(folderPath))
            {
                MelonLogger.Error("File at " + folderPath + " does not exist");
            }

            UI.instance.UI_Initialized += OnUIInit;
            
            Calls.onMatchEnded += MatchEnded;
        }

        public void OnUIInit()
        {
            mod.ModName = BuildInfo.ModName; // Use the instance-level field
            mod.ModVersion = BuildInfo.ModVersion;
            mod.SetFolder("CustomBattleMusic");
            mod.AddDescription("Description", "", BuildInfo.Description, new Tags { IsSummary = true });

            volume = mod.AddToList("Volume", 0.05f, "The volume at which every song will play", new Tags());
            
            isModEnabled = mod.AddToList("Is mod enabled", true, 1, "Enable or disable the mod", new Tags());
            
            shouldPauseOnRoundEnd = mod.AddToList("Should pause on round end", true, 1, 
                "Make sure the mod does/doesn't pause the music on round end", new Tags());
            
            shouldShuffle = mod.AddToList("Should shuffle playlist", true, 1, 
                "Make a shuffled playlist of leave the default one", new Tags());

            mod.GetFromFile();

            mod.ModSaved += Save;

            UI.instance.AddMod(mod); // Use the instance-level field
            MelonLogger.Msg("Added Mod: " + BuildInfo.ModName);

            if ((bool)shouldShuffle.Value)
            {
                playList = Shuffle(playList);
            }
            
            nextSong = playList[0];
        }

        private static void Save()
        {
            if (CurrentAudio != null)
            {
                AudioManager.ChangeVolume(CurrentAudio, (float)volume.Value);

                if (!(bool)isModEnabled.Value)
                {
                    AudioManager.StopPlayback(CurrentAudio);
                    CurrentAudio = null; // Always reset
                    MelonLogger.Msg("Mod disabled stopping all music");
                }
            }

            AudioManager.ChangeVolume(CurrentAudio ,(float)volume.Value);

            if ((bool)isModEnabled.Value == false)
            {
                AudioManager.StopPlayback(CurrentAudio);
                CurrentAudio = null; // Always reset
                MelonLogger.Msg("Mod disabled stopping all music");
            }

            if ((bool)shouldShuffle.Value)
            {
                playList = Shuffle(playList);
            }
            else
            {
                playList = mp3Files;
            }
        }
        
        private bool isOnCooldown;
        private static float cooldownTime = 0.5f;
        
        private static DateTime lastPressedRight;
        private static DateTime lastPressedLeft;
        private static float maxDoublePressTime = 0.5f + cooldownTime;

        public override void OnUpdate()
        {
            // Handle right joystick
            if ((double)Calls.ControllerMap.RightController.GetJoystickClick() == 1.0 && !isOnCooldown)
            {
                if ((DateTime.Now - lastPressedRight).TotalSeconds < maxDoublePressTime)
                {
                    // Double tap detected - skip forward
                    MelonLogger.Msg("Right joystick double-tap - skipping forward");
                    SkipSongsBy(1);
                    isOnCooldown = true;
                    MelonCoroutines.Start(StartCooldown());
                }
                else
                {
                    // Single tap - pause/resume
                    if (CurrentAudio != null)
                    {
                        if (CurrentAudio.IsPaused)
                        {
                            MelonLogger.Msg("Right joystick pressed - resuming music");
                            AudioManager.ResumePlayback(CurrentAudio);
                        }
                        else
                        {
                            MelonLogger.Msg("Right joystick pressed - pausing music");
                            AudioManager.PausePlayback(CurrentAudio);
                        }
                    }
                    isOnCooldown = true;
                    MelonCoroutines.Start(StartCooldown());
                }
                lastPressedRight = DateTime.Now;
            }

            // Handle left joystick
            if ((double)Calls.ControllerMap.LeftController.GetJoystickClick() == 1.0 && !isOnCooldown)
            {
                if ((DateTime.Now - lastPressedLeft).TotalSeconds < maxDoublePressTime)
                {
                    // Double tap detected - skip backward
                    MelonLogger.Msg("Left joystick double-tap - skipping backward");
                    SkipSongsBy(-1);
                    isOnCooldown = true;
                    MelonCoroutines.Start(StartCooldown());
                }
                // Left joystick single tap does nothing (or could add different functionality)
                isOnCooldown = true;
                MelonCoroutines.Start(StartCooldown());
                lastPressedLeft = DateTime.Now;
            }
        }
        
        private IEnumerator StartCooldown()
        {
            yield return new WaitForSeconds(0.5f);
            isOnCooldown = false;
        }


        private static void SceneLoaded()
        {
            if (Calls.Scene.GetSceneName() == "Gym")
            {
                MelonLogger.Msg("Creating new MP3 Player");
                CreateMp3Player(new Vector3(8.0478f, 2f, 9.4449f),Quaternion.Euler(0, 39.3605f, 0f),true);
            }
            else if (Calls.Scene.GetSceneName() == "Park")
            {
                MelonLogger.Msg("Creating new MP3 Player");
                CreateMp3Player(new Vector3(-15.5555f, -4.5763f, -4.6272f),Quaternion.Euler(0.631f, 216.8802f, -0f),true);
            }
            
            if (CurrentAudio == null) return;
            
            MelonLogger.Msg("New scene loaded, disposing of old music object");
            AudioManager.StopPlayback(CurrentAudio);
            CurrentAudio = null;
        }

        private static void MatchEnded()
        {
            if (Calls.Players.IsHost())
            {
                CreateMp3Player(new Vector3(-0f, 2.9203f, 1.8f),Quaternion.Euler(0f,0f,0f));
            }
            else
            {
                CreateMp3Player(new Vector3(-0f, 2.9203f, -1.8f),Quaternion.Euler(0, 180f, 0));
            }
        }

        [HarmonyLib.HarmonyPatch(typeof(MatchHandler), "ExecuteRound")]
        public static class RoundPatch
        {
            public static void Prefix(ref int roundNo)
            {
                try
                {
                    if ((bool)isModEnabled.Value == false) return;

                    if (roundNo > 0 && CurrentAudio != null && (bool)shouldPauseOnRoundEnd.Value)
                    {
                        MelonLogger.Msg("New round started, resuming music...");
                        MelonCoroutines.Start(ResumeBattleMusic(1f)); // Adjusted to avoid instant restart
                    }
                    else if (roundNo == 0)
                    {
                        MelonCoroutines.Start(BeginBattleMusic(1f)); // Start new music for the first round
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
                try
                {
                    if (!(Calls.Scene.GetSceneName() is "Map0" or "Map1") || (bool)shouldPauseOnRoundEnd.Value) return;

                    if (CurrentAudio != null)
                    {
                        MelonLogger.Msg("Round ended pausing music");
                        AudioManager.PausePlayback(CurrentAudio);
                    }
                }
                catch (Exception e)
                {
                    MelonLogger.Error("Player defeat crashed");
                    MelonLogger.Error(e.Message);
                }
            }
        }

        
        private static IEnumerator ResumeBattleMusic(float delay)
        {
            yield return new WaitForSeconds(delay);

            if (CurrentAudio != null)
            {
                MelonLogger.Msg("Resuming battle music...");
                AudioManager.ResumePlayback(CurrentAudio);
            }
            else
            {
                MelonLogger.Warning("No audio to resume.");
            }
        }
        
        //Thank you to MadLike for showing me this <3
        private static IEnumerator BeginBattleMusic(float delay, bool shouldPlayNext = true)
        {
            if (!(bool)isModEnabled.Value)
            {
                MelonLogger.Msg("Mod disabled stopping");
                yield break;
            }

            yield return new WaitForSeconds(delay);

            MelonLogger.Msg("Playing new song");

            if (mp3Files.Length == 0)
            {
                MelonLogger.Warning("No MP3 files found in the specified folder.");
                yield break;
            }

            // Stop and clean up existing music
            if (CurrentAudio != null)
            {
                MelonLogger.Msg("Stopping previous audio instance...");
                AudioManager.StopPlayback(CurrentAudio);
                CurrentAudio = null;
            }

            // Use the current song in both cases
            string audioPath = nextSong;

            // Only update nextSong if shouldPlayNext is true
            if (shouldPlayNext)
            {
                int currentIndex = Array.IndexOf(playList, nextSong);
                int newIndex = (currentIndex + 1) % playList.Length; // Loop back to first song if at the end
                nextSong = playList[newIndex];
            }

            MelonLogger.Msg("Playing sound at " + audioPath);

            // Assign new audio instance
            CurrentAudio = AudioManager.PlaySoundIfFileExists(audioPath, (float)volume.Value, true);

            if (CurrentAudio == null)
            {
                MelonLogger.Error("Failed to play sound: ClipData is null");
            }
        }

        private static void ChangeMp3PlayerText(String text)
        {
            mp3Text.GetComponent<TextMeshPro>().text = text;
        }

        private static void CreateMp3Player(Vector3 vector, Quaternion rotation, bool isGym = false)
        {
            string fileName = Path.GetFileNameWithoutExtension(nextSong);
    
            mp3Text = Calls.Create.NewText(fileName, 3f, Color.white, new Vector3(), Quaternion.Euler(0f, 0f, 0f));
            mp3Text.transform.position = vector;
            mp3Text.name = "Mp3Player";
            // GameObject.DontDestroyOnLoad(mp3Text);
    
            // Now the prevButton uses the previous 'nextButton' position, and vice versa.
            GameObject prevButton = Calls.Create.NewButton(
                mp3Text.transform.position - new Vector3(0.3f, 0.4f, 0f),
                Quaternion.Euler(90, mp3Text.transform.rotation.y - 180, 0));
            prevButton.transform.SetParent(mp3Text.transform, true);

            GameObject nextButton = Calls.Create.NewButton(
                mp3Text.transform.position + new Vector3(0.3f, -0.4f, 0f),
                Quaternion.Euler(90, mp3Text.transform.rotation.y - 180, 0));
            nextButton.transform.SetParent(mp3Text.transform, true);

            if (isGym)
            {
                // The preview button is now positioned half the previous vertical offset difference
                GameObject previewButton = Calls.Create.NewButton(
                    mp3Text.transform.position + new Vector3(0.0f, -0.6f, 0f),
                    Quaternion.Euler(90, mp3Text.transform.rotation.y - 180, 0));
                previewButton.transform.SetParent(mp3Text.transform, true);
                
                previewButton.transform.GetChild(0).gameObject.GetComponent<InteractionButton>().onPressed.AddListener(new Action(() =>
                {
                    MelonLogger.Msg("Preview button pressed");
                    if (CurrentAudio == null)
                    {
                        MelonLogger.Msg("Starting battle music");
                        MelonCoroutines.Start(BeginBattleMusic(0.0f, false));
                    }
                    else
                    {
                        MelonLogger.Msg("Stopping battle music");
                        AudioManager.StopPlayback(CurrentAudio);
                        CurrentAudio = null;
                    }
                }));
                
                
                GameObject previewLabel = Calls.Create.NewText("Preview", 0.5f, Color.white, Vector3.zero, Quaternion.Euler(0.0f, 0.0f, 0f));
                previewLabel.transform.position = previewButton.transform.position + new Vector3(0f, -0.1f, 0f);
                previewLabel.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0f);
                previewLabel.transform.SetParent(previewButton.transform, true);
            }

    
            // Set the mp3Text rotation last so that button rotations remain unchanged
            mp3Text.transform.rotation = rotation;
    
            prevButton.transform.GetChild(0).gameObject.GetComponent<InteractionButton>().onPressed.AddListener(new Action(() =>
            {
                SkipSongsBy(-1);
            }));
    
            nextButton.transform.GetChild(0).gameObject.GetComponent<InteractionButton>().onPressed.AddListener(new Action(() =>
            {
                SkipSongsBy(1);
            }));

            // Create text labels below each button:
            // Adjust the offsets (here, new Vector3(0, -0.1f, 0)) as needed for your layout.
            GameObject prevLabel = Calls.Create.NewText("Previous", 0.5f, Color.white, Vector3.zero, Quaternion.Euler(0.0f, 0.0f, 0f));
            prevLabel.transform.position = prevButton.transform.position + new Vector3(0f, -0.1f, 0f);
            prevLabel.transform.rotation = Quaternion.Euler(0.0f, 40.0f, 0f);
            prevLabel.transform.SetParent(prevButton.transform, true);
    
            GameObject nextLabel = Calls.Create.NewText("Next", 0.5f, Color.white, Vector3.zero, Quaternion.Euler(0.0f, 0.0f, 0f));
            nextLabel.transform.position = nextButton.transform.position + new Vector3(0f, -0.1f, 0f);
            nextLabel.transform.rotation = Quaternion.Euler(0.0f, 40.0f, 0f);
            nextLabel.transform.SetParent(nextButton.transform, true);
        }


        public static void SkipSongsBy(int number)
        {
            if (mp3Text == null)
            {
                MelonLogger.Error("Cannot skip songs because mp3Text is null.");
                return;
            }

            int currentIndex = Array.IndexOf(playList, nextSong);
            if (currentIndex == -1) return;

            int newIndex = (currentIndex + number) % playList.Length;
            if (newIndex < 0) newIndex += playList.Length; // Ensure valid index

            nextSong = playList[newIndex];
            ChangeMp3PlayerText(Path.GetFileNameWithoutExtension(nextSong)); // Avoid crash if mp3Text is missing
        }
        
        public static string[] Shuffle(string[] array)
        {
            System.Random rng = new System.Random();
            int n = array.Length;

            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                string value = array[k];
                array[k] = array[n];
                array[n] = value;
            }
            
            return array;
        }
    }
}
