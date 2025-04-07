using System.Collections;
using MelonLoader;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using UnityEngine;

namespace CustomBattleMusic;
public static class AudioManager
{
    public class ClipData
    {
        public WaveOutEvent WaveOut { get; set; }
        public ISampleProvider VolumeProvider { get; set; }
        public AudioFileReader Reader { get; set; }
        public long PausedPosition { get; set; } = 0;
        public bool IsPaused { get; set; } = false;
    }

    private static IEnumerator PlaySound(ClipData clipData, bool loop)
    {
        if (clipData == null || clipData.WaveOut == null || clipData.Reader == null)
        {
            MelonLogger.Error("clipData is null or has missing components.");
            yield break;
        }

        do
        {
            // If not resuming from a pause, reset to the start of the clip.
            // Otherwise, keep the current Reader.Position.
            if (!clipData.IsPaused)
            {
                clipData.Reader.Position = 0;
            }

            // Start or resume playing the sound.
            clipData.WaveOut.Play();

            // Wait until the sound finishes playing.
            // If the clip is paused, continue yielding until it is resumed.
            while (clipData.WaveOut != null)
            {
                if (clipData.IsPaused)
                {
                    // When paused, simply yield and wait.
                    yield return null;
                    continue;
                }

                // If playback is no longer active (finished or stopped), break out.
                if (clipData.WaveOut.PlaybackState != PlaybackState.Playing)
                {
                    break;
                }

                yield return null;
            }

            // If looping and still active, stop the current playback before restarting.
            if (loop && clipData.WaveOut != null)
            {
                clipData.WaveOut.Stop();
            }

        } while (loop && clipData.WaveOut != null);

        // Only clean up if we're not paused.
        if (!clipData.IsPaused)
        {
            clipData.Reader.Dispose();
            clipData.WaveOut.Dispose();
            clipData.WaveOut = null;
        }
    }
    

    public static ClipData PlaySoundIfFileExists(string soundFilePath, float volume = 1.0f, bool loop = false)
    {
        if (!File.Exists(soundFilePath))
        {
            MelonLogger.Error($"Audio file not found: {soundFilePath}");
            return null;
        }

        var reader = new AudioFileReader(soundFilePath);
        var volumeProvider = new VolumeSampleProvider(reader)
        {
            Volume = Mathf.Clamp01(volume)
        };

        var waveOut = new WaveOutEvent();
        waveOut.Init(volumeProvider);

        var clipData = new ClipData
        {
            WaveOut = waveOut,
            VolumeProvider = volumeProvider,
            Reader = reader
        };

        MelonCoroutines.Start(PlaySound(clipData, loop));
        return clipData;
    }


    public static void PausePlayback(ClipData clipData)
    {
        if (clipData == null || clipData.WaveOut == null)
        {
            MelonLogger.Warning("Clipdata or waveout is null cant pause");
            return;
        }
        
        clipData.PausedPosition = clipData.Reader.Position;
        clipData.IsPaused = true;
        clipData.WaveOut.Pause();
    }

    public static void ResumePlayback(ClipData clipData)
    {
        if (clipData == null || clipData.WaveOut == null)
        {
            MelonLogger.Warning("Clipdata or waveout is null cant resume");
            return;
        }

        clipData.IsPaused = false;
        clipData.WaveOut.Play();
    }

    public static void ChangeVolume(ClipData clipData, float volume)
    {
        if (clipData == null || clipData.VolumeProvider == null)
        {
            return;
        }

        if (clipData.VolumeProvider is VolumeSampleProvider volumeProvider)
        {
            volumeProvider.Volume = Mathf.Clamp01(volume);
        }
    }

    public static void StopPlayback(ClipData clipData)
    {
        if (clipData == null)
        {
            MelonLogger.Warning("Attempted to stop playback on a null clipData.");
            return;
        }

        if (clipData.WaveOut != null)
        {
            clipData.WaveOut.Stop();
            clipData.WaveOut.Dispose();
        }

        if (clipData.Reader != null)
        {
            clipData.Reader.Dispose();
        }

        clipData.WaveOut = null;
    }
}