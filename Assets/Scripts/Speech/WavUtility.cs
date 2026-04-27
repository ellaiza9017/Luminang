using System;
using System.IO;
using UnityEngine;

public static class WavUtility
{
    public static byte[] FromAudioClip(AudioClip clip)
    {
        using (var stream = new MemoryStream())
        {
            WriteWavFile(clip, stream);
            return stream.ToArray();
        }
    }

    private static void WriteWavFile(AudioClip clip, Stream stream)
    {
        var hz = clip.frequency;
        var channels = clip.channels;
        var samples = clip.samples;

        // Total size of the file (RIFF header + sub-chunks)
        // 4 (WAVE) + 8 (fmt ) + 16 (fmt data) + 8 (data ) + samples * channels * 2
        int fileSize = 36 + samples * channels * 2;

        stream.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"), 0, 4);
        stream.Write(BitConverter.GetBytes(fileSize), 0, 4);
        stream.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"), 0, 4);
        stream.Write(System.Text.Encoding.UTF8.GetBytes("fmt "), 0, 4);
        stream.Write(BitConverter.GetBytes(16), 0, 4);
        stream.Write(BitConverter.GetBytes((ushort)1), 0, 2);
        stream.Write(BitConverter.GetBytes((ushort)channels), 0, 2);
        stream.Write(BitConverter.GetBytes(hz), 0, 4);
        stream.Write(BitConverter.GetBytes(hz * channels * 2), 0, 4);
        stream.Write(BitConverter.GetBytes((ushort)(channels * 2)), 0, 2);
        stream.Write(BitConverter.GetBytes((ushort)16), 0, 2);
        stream.Write(System.Text.Encoding.UTF8.GetBytes("data"), 0, 4);
        stream.Write(BitConverter.GetBytes(samples * channels * 2), 0, 4);

        float[] floatData = new float[samples * channels];
        clip.GetData(floatData, 0);

        foreach (var sample in floatData)
        {
            short shortSample = (short)(sample * short.MaxValue);
            stream.Write(BitConverter.GetBytes(shortSample), 0, 2);
        }
    }
}
