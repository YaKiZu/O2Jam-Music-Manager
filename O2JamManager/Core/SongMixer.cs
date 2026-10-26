using System.IO;
using NAudio.Wave;
using NVorbis;
using O2JamManager.Models;

namespace O2JamManager.Core;

public class DecodedPcm
{
    public float[] Left { get; set; } = [];
    public float[] Right { get; set; } = [];
    public int LengthFrames => Left.Length;
    public double DurationSeconds => Left.Length / 44100.0;
}

public class MixedSongResult
{
    public required MixedSongSampleProvider Provider { get; set; }
    public string ContainerFormat { get; set; } = "M30";
    public string SampleName { get; set; } = string.Empty;
    public int SampleSize { get; set; }
    public int TotalSamplesInContainer { get; set; }
    public double DurationSeconds { get; set; }
    public string ModeDescription => $"Audio ({SampleName}, {SampleSize / 1024:N0} Ko)";
}

public class MixedSongSampleProvider : ISampleProvider
{
    private readonly float[] _interleavedSamples;
    private readonly WaveFormat _waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
    private long _position;

    public MixedSongSampleProvider(float[] left, float[] right)
    {
        int count = Math.Min(left.Length, right.Length);
        if (count <= 0)
        {
            count = 44100;
            left = new float[count];
            right = new float[count];
        }

        _interleavedSamples = new float[count * 2];
        for (int i = 0; i < count; i++)
        {
            _interleavedSamples[i * 2] = left[i];
            _interleavedSamples[i * 2 + 1] = right[i];
        }
    }

    public WaveFormat WaveFormat => _waveFormat;

    public double TotalSeconds => _interleavedSamples.Length / 2.0 / 44100.0;

    public double CurrentSeconds
    {
        get => _position / 2.0 / 44100.0;
        set
        {
            long targetPos = (long)(value * 44100.0 * 2.0);
            _position = Math.Clamp(targetPos, 0, _interleavedSamples.Length);
        }
    }

    public int Read(float[] buffer, int offset, int count)
    {
        long available = _interleavedSamples.Length - _position;
        int toCopy = (int)Math.Min(count, available);
        if (toCopy <= 0)
            return 0;

        Array.Copy(_interleavedSamples, _position, buffer, offset, toCopy);
        _position += toCopy;
        return toCopy;
    }
}

public static class SongMixer
{
    private const int TargetSampleRate = 44100;

    /// <summary>
    /// Decodes raw audio bytes (OGG Vorbis, WAV, or raw PCM) into 44100 Hz stereo float arrays.
    /// Mono samples are cleanly duplicated into left and right channels without pitch or interleaving artifacts.
    /// </summary>
    public static DecodedPcm? DecodeAudioToPcm(byte[] audioBytes)
    {
        if (audioBytes == null || audioBytes.Length < 4)
            return null;

        try
        {
            DecodedPcm? decoded = null;

            // 1. OGG Vorbis ("OggS")
            if (audioBytes.Length >= 4 && audioBytes[0] == 0x4F && audioBytes[1] == 0x67 && audioBytes[2] == 0x67 && audioBytes[3] == 0x53)
            {
                using var ms = new MemoryStream(audioBytes);
                using var reader = new VorbisReader(ms);

                int channels = reader.Channels;
                int srcRate = reader.SampleRate;
                long totalSamples = reader.TotalSamples;

                if (totalSamples <= 0 || totalSamples > 200_000_000)
                    totalSamples = 44100 * 420; // 7 min safety cap

                float[] rawBuffer = new float[totalSamples * channels];
                int readSamples = reader.ReadSamples(rawBuffer, 0, rawBuffer.Length);

                int srcFrames = readSamples / channels;
                if (srcFrames <= 0) return null;

                float[] srcLeft = new float[srcFrames];
                float[] srcRight = new float[srcFrames];

                if (channels == 1)
                {
                    // Clean Mono duplication: left[i] = sample[i], right[i] = sample[i]
                    for (int i = 0; i < srcFrames; i++)
                    {
                        srcLeft[i] = rawBuffer[i];
                        srcRight[i] = rawBuffer[i];
                    }
                }
                else
                {
                    for (int i = 0; i < srcFrames; i++)
                    {
                        srcLeft[i] = rawBuffer[i * channels];
                        srcRight[i] = rawBuffer[i * channels + 1];
                    }
                }

                decoded = ResampleTo44100(srcLeft, srcRight, srcRate);
            }
            // 2. WAV PCM ("RIFF")
            else if (audioBytes.Length >= 12 && audioBytes[0] == 'R' && audioBytes[1] == 'I' && audioBytes[2] == 'F' && audioBytes[3] == 'F')
            {
                using var ms = new MemoryStream(audioBytes);
                using var reader = new WaveFileReader(ms);
                var sampleProvider = reader.ToSampleProvider();

                int channels = sampleProvider.WaveFormat.Channels;
                int srcRate = sampleProvider.WaveFormat.SampleRate;

                var rawFloats = new List<float>();
                float[] chunk = new float[4096];
                int count;
                while ((count = sampleProvider.Read(chunk, 0, chunk.Length)) > 0)
                {
                    for (int k = 0; k < count; k++)
                        rawFloats.Add(chunk[k]);
                }

                int srcFrames = rawFloats.Count / channels;
                if (srcFrames <= 0) return null;

                float[] srcLeft = new float[srcFrames];
                float[] srcRight = new float[srcFrames];

                if (channels == 1)
                {
                    for (int i = 0; i < srcFrames; i++)
                    {
                        srcLeft[i] = rawFloats[i];
                        srcRight[i] = rawFloats[i];
                    }
                }
                else
                {
                    for (int i = 0; i < srcFrames; i++)
                    {
                        srcLeft[i] = rawFloats[i * channels];
                        srcRight[i] = rawFloats[i * channels + 1];
                    }
                }

                decoded = ResampleTo44100(srcLeft, srcRight, srcRate);
            }
            // 3. Fallback: Raw 16-bit PCM
            else if (audioBytes.Length >= 32)
            {
                int numFrames = audioBytes.Length / 2;
                float[] srcLeft = new float[numFrames];
                float[] srcRight = new float[numFrames];
                for (int i = 0; i < numFrames; i++)
                {
                    short val = BitConverter.ToInt16(audioBytes, i * 2);
                    float f = val / 32768.0f;
                    srcLeft[i] = f;
                    srcRight[i] = f;
                }
                decoded = new DecodedPcm { Left = srcLeft, Right = srcRight };
            }

            if (decoded != null)
            {
                for (int i = 0; i < decoded.LengthFrames; i++)
                {
                    decoded.Left[i] = Math.Clamp(decoded.Left[i], -1.0f, 1.0f);
                    decoded.Right[i] = Math.Clamp(decoded.Right[i], -1.0f, 1.0f);
                }
                return decoded;
            }
        }
        catch { }

        return null;
    }

    private static DecodedPcm ResampleTo44100(float[] srcLeft, float[] srcRight, int srcRate)
    {
        if (srcRate == TargetSampleRate || srcRate <= 0)
        {
            return new DecodedPcm { Left = srcLeft, Right = srcRight };
        }

        double ratio = (double)srcRate / TargetSampleRate;
        int dstFrames = (int)(srcLeft.Length / ratio);
        float[] dstLeft = new float[dstFrames];
        float[] dstRight = new float[dstFrames];

        for (int i = 0; i < dstFrames; i++)
        {
            double srcIdx = i * ratio;
            int idx0 = (int)srcIdx;
            int idx1 = Math.Min(idx0 + 1, srcLeft.Length - 1);
            float frac = (float)(srcIdx - idx0);

            dstLeft[i] = srcLeft[idx0] * (1.0f - frac) + srcLeft[idx1] * frac;
            dstRight[i] = srcRight[idx0] * (1.0f - frac) + srcRight[idx1] * frac;
        }

        return new DecodedPcm { Left = dstLeft, Right = dstRight };
    }

    /// <summary>
    /// Normalizes the entire audio buffer so its maximum absolute peak is exactly 0.98 (98%).
    /// Guarantees full loudness, crystal-clear punch, and 0% digital clipping.
    /// </summary>
    public static void NormalizeTo098(float[] left, float[] right)
    {
        int len = Math.Min(left.Length, right.Length);
        if (len == 0) return;

        float maxPeak = 0.0f;
        for (int i = 0; i < len; i++)
        {
            float absL = Math.Abs(left[i]);
            float absR = Math.Abs(right[i]);
            if (absL > maxPeak) maxPeak = absL;
            if (absR > maxPeak) maxPeak = absR;
        }

        if (maxPeak > 0.0001f)
        {
            float scale = 0.98f / maxPeak;
            for (int i = 0; i < len; i++)
            {
                left[i] *= scale;
                right[i] *= scale;
            }
        }

        // Final safety clamp to prevent integer wrap-around
        for (int i = 0; i < len; i++)
        {
            left[i] = Math.Clamp(left[i], -1.0f, 1.0f);
            right[i] = Math.Clamp(right[i], -1.0f, 1.0f);
        }
    }

    /// <summary>
    /// Cas A Uniquement : Lecture directe du sample audio principal (BGM brute) du conteneur .ojm.
    /// Zéro mixage de partition .ojn, chargement instantané, acoustique pure avec normalisation à 0.98.
    /// </summary>
    public static MixedSongResult MixFullSong(Song song, Action<string>? log = null)
    {
        if (song.OjmPath == null || !File.Exists(song.OjmPath))
            throw new FileNotFoundException("Fichier audio .ojm introuvable.");

        log?.Invoke("Analyse du conteneur audio OJM...");
        byte[] ojmBytes = File.ReadAllBytes(song.OjmPath);

        // 1. Parse OJM container (M30, OMC, OJM, or Universal Scanner)
        var container = OjmDecoder.ParseOjm(ojmBytes);
        log?.Invoke($"Format OJM: {container.Format} ({container.Samples.Count} samples détectés)");

        if (container.Samples.Count == 0)
            throw new InvalidOperationException($"Impossible d'extraire des samples audio valides du fichier {song.OjmFile}.");

        // 2. Select the largest audio sample (the main BGM track)
        var sortedSamples = container.Samples.OrderByDescending(s => s.SampleSize).ToList();
        DecodedPcm? mainPcm = null;
        OjmSampleInfo? chosenSample = null;

        foreach (var sample in sortedSamples)
        {
            var pcm = DecodeAudioToPcm(sample.AudioData);
            if (pcm != null && pcm.LengthFrames > 0)
            {
                mainPcm = pcm;
                chosenSample = sample;
                break;
            }
        }

        if (mainPcm == null)
            throw new InvalidOperationException("Aucun sample audio OGG/WAV n'a pu être décodé pour la piste principale.");

        // 3. Normalisation pleine puissance à 0.98 (Cas A)
        NormalizeTo098(mainPcm.Left, mainPcm.Right);

        log?.Invoke("Piste principale BGM prête.");
        var provider = new MixedSongSampleProvider(mainPcm.Left, mainPcm.Right);

        return new MixedSongResult
        {
            Provider = provider,
            ContainerFormat = container.Format,
            SampleName = chosenSample?.Name ?? "BGM",
            SampleSize = chosenSample?.SampleSize ?? 0,
            TotalSamplesInContainer = container.Samples.Count,
            DurationSeconds = provider.TotalSeconds
        };
    }
}
