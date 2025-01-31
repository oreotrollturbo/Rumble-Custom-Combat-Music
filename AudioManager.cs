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
            yield break;
        }

        do
        {
            clipData.WaveOut.Play();

            while (clipData.WaveOut != null && clipData.WaveOut.PlaybackState == PlaybackState.Playing)
            {
                yield return null;
            }

            if (loop && clipData.WaveOut != null && !clipData.IsPaused)
            {
                clipData.WaveOut.Stop();
                clipData.Reader.Position = 0;
            }

        } while (loop && clipData.WaveOut != null && !clipData.IsPaused);

        if (!clipData.IsPaused)
        {
            clipData.Reader.Dispose();
            clipData.WaveOut.Dispose();
            clipData.WaveOut = null;
        }
    }

    public static ClipData PlaySoundIfFileExists(string soundFilePath, float volume = 1.0f, bool loop = false)
    {
        string fullPath = soundFilePath;
        var reader = new AudioFileReader(fullPath);

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
        if (clipData == null || clipData.WaveOut == null)
        {
            return;
        }

        clipData.WaveOut.Stop();
        clipData.WaveOut.Dispose();
        clipData.Reader.Dispose();
        clipData.WaveOut = null;
    }
}