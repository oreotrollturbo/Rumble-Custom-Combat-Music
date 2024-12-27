using System.Collections;
using MelonLoader;
using MelonLoader.Utils;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using UnityEngine;

namespace CustomBattleMusic;
//This entire class is made by MadLike
public static class AudioManager
{
    public class ClipData
    {
        public WaveOutEvent WaveOut { get; set; }
        public ISampleProvider VolumeProvider { get; set; }
    }

    private static IEnumerator PlaySound(ClipData clipData, AudioFileReader reader, bool loop)
    {
        if (clipData == null || clipData.WaveOut == null || reader == null)
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

            if (loop && clipData.WaveOut != null)
            {
                clipData.WaveOut.Stop();
                reader.Position = 0;
            }

        } while (loop && clipData.WaveOut != null);

        reader.Dispose();

        if (clipData.WaveOut != null)
        {
            if (clipData.WaveOut.PlaybackState == PlaybackState.Playing)
            {
                clipData.WaveOut.Stop();
            }

            clipData.WaveOut.Dispose();
            clipData.WaveOut = null;
        }

        clipData = null;
    }

    public static ClipData PlaySoundIfFileExists(string soundFilePath, float volume = 1.0f, bool loop = false)
    {
        string fullPath = MelonEnvironment.UserDataDirectory + soundFilePath;

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
            VolumeProvider = volumeProvider
        };

        MelonCoroutines.Start(PlaySound(clipData, reader, loop));
        return clipData;

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

        clipData.WaveOut = null;
    }
}