using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace O2JamManager.Core;

public class OjmSampleInfo
{
    public int SampleId { get; set; }
    public short CodecCode { get; set; } // 0 = M0/BGM, 5 = W0/Keysound
    public short Ref { get; set; }       // 0-based index in M30
    public string Name { get; set; } = string.Empty;
    public byte[] AudioData { get; set; } = [];
    public int SampleSize => AudioData.Length;
}

public class OjmContainerInfo
{
    public string Format { get; set; } = "UNKNOWN";
    public int EncryptionFlag { get; set; }
    public List<OjmSampleInfo> Samples { get; set; } = [];
    public OjmSampleInfo? LargestSample => Samples.OrderByDescending(s => s.SampleSize).FirstOrDefault();
}

/// <summary>
/// O2Jam OJM file decoder for M30, OMC, and OJM container formats.
/// Supports XOR decryption (nami / 0412), OMC/WAV/OGG parsing, and Universal Scanner fallback.
/// </summary>
public static class OjmDecoder
{
    private static readonly byte[] NamiKey = [0x6E, 0x61, 0x6D, 0x69]; // "nami"
    private static readonly byte[] Key0412 = [0x30, 0x34, 0x31, 0x32]; // "0412"

    private static readonly byte[] M30Sig = [0x4D, 0x33, 0x30, 0x00];  // "M30\0"
    private static readonly byte[] OmcSig = [0x4F, 0x4D, 0x43, 0x00];  // "OMC\0"
    private static readonly byte[] OjmSig = [0x4F, 0x4A, 0x4D, 0x00];  // "OJM\0"
    private static readonly byte[] OggSig = [0x4F, 0x67, 0x67, 0x53];  // "OggS"
    private static readonly byte[] RiffSig = [0x52, 0x49, 0x46, 0x46]; // "RIFF"

    /// <summary>
    /// Parses an entire OJM file into an OjmContainerInfo structure containing all samples.
    /// </summary>
    public static OjmContainerInfo ParseOjm(byte[] data)
    {
        var result = new OjmContainerInfo();

        if (data == null || data.Length < 4)
            return result;

        // 1. Check if raw OGG or WAV
        if (StartsWith(data, OggSig))
        {
            result.Format = "RAW-OGG";
            result.Samples.Add(new OjmSampleInfo
            {
                SampleId = 1,
                CodecCode = 0,
                Name = "raw_stream.ogg",
                AudioData = data
            });
            return result;
        }

        if (StartsWith(data, RiffSig))
        {
            result.Format = "RAW-WAV";
            result.Samples.Add(new OjmSampleInfo
            {
                SampleId = 1,
                CodecCode = 0,
                Name = "raw_stream.wav",
                AudioData = data
            });
            return result;
        }

        // 2. M30 format ("M30\0")
        if (StartsWith(data, M30Sig))
        {
            result.Format = "M30";
            ParseM30(data, result);
            if (result.Samples.Count > 0)
                return result;
        }

        // 3. OMC / OJM format ("OMC\0" or "OJM\0")
        if (StartsWith(data, OmcSig) || StartsWith(data, OjmSig))
        {
            result.Format = StartsWith(data, OmcSig) ? "OMC" : "OJM";
            ParseOmc(data, result);
            if (result.Samples.Count > 0)
                return result;
        }

        // 4. Fallback Universal Binary Scanner
        result.Format = (result.Format == "UNKNOWN") ? "SCANNER" : $"{result.Format}+SCANNER";
        var scannedSamples = ScanForEmbeddedAudioStreams(data);
        if (scannedSamples.Count > 0)
        {
            for (int i = 0; i < scannedSamples.Count; i++)
            {
                result.Samples.Add(new OjmSampleInfo
                {
                    SampleId = i + 1,
                    CodecCode = (short)(i == 0 ? 0 : 5),
                    Ref = (short)i,
                    Name = $"scanned_{i + 1:D3}",
                    AudioData = scannedSamples[i]
                });
            }
        }

        return result;
    }

    private static void ParseM30(byte[] data, OjmContainerInfo result)
    {
        if (data.Length < 28) return;

        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);

        reader.ReadBytes(4); // "M30\0"
        int fileFormatVersion = reader.ReadInt32();
        int encryptionFlag = reader.ReadInt32();
        int sampleCount = reader.ReadInt32();
        int samplesOffset = reader.ReadInt32();
        int payloadSize = reader.ReadInt32();

        result.EncryptionFlag = encryptionFlag;

        if (samplesOffset < 28 || samplesOffset >= data.Length)
            samplesOffset = 28;

        ms.Seek(samplesOffset, SeekOrigin.Begin);

        for (int i = 0; i < sampleCount && ms.Position + 52 <= ms.Length; i++)
        {
            byte[] nameBytes = reader.ReadBytes(32);
            int sampleSize = reader.ReadInt32();
            short codecCode = reader.ReadInt16();
            short musicFlag = reader.ReadInt16();
            short sampleRef = reader.ReadInt16();
            short unk = reader.ReadInt16();
            int pcmSamples = reader.ReadInt32();
            reader.ReadBytes(4); // padding (52 octets total)

            if (sampleSize <= 0 || ms.Position + sampleSize > ms.Length)
                break;

            byte[] audioData = reader.ReadBytes(sampleSize);
            byte[] decryptedData = (byte[])audioData.Clone();

            // Decryption XOR
            if ((encryptionFlag & 16) != 0)
            {
                ApplyXorKey(decryptedData, NamiKey);
            }
            else if ((encryptionFlag & 32) != 0)
            {
                ApplyXorKey(decryptedData, Key0412);
            }

            // Verify if decrypted data is valid OggS; if not, check if original audio was plaintext OggS
            byte[] finalData = decryptedData;
            if (!StartsWith(decryptedData, OggSig) && StartsWith(audioData, OggSig))
            {
                finalData = audioData;
            }

            string name = Encoding.ASCII.GetString(nameBytes).TrimEnd('\0').Trim();

            // Calculate Base-1 ID:
            // codec_code == 0 (M0 / BGM) -> ref + 1
            // codec_code == 5 (W0 / Keysound) -> ref + 1001
            int sampleId = (codecCode == 5) ? (sampleRef + 1001) : (sampleRef + 1);

            result.Samples.Add(new OjmSampleInfo
            {
                SampleId = sampleId,
                CodecCode = codecCode,
                Ref = sampleRef,
                Name = name,
                AudioData = finalData
            });
        }
    }

    private static void ParseOmc(byte[] data, OjmContainerInfo result)
    {
        if (data.Length < 20) return;

        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);

        reader.ReadBytes(4); // "OMC\0" or "OJM\0"
        short wavCount = reader.ReadInt16();
        short oggCount = reader.ReadInt16();
        int wavStart = reader.ReadInt32();
        int oggStart = reader.ReadInt32();
        int fileSize = reader.ReadInt32();

        // 1. WAV Section
        if (wavStart >= 20 && wavStart < data.Length)
        {
            ms.Seek(wavStart, SeekOrigin.Begin);
            for (int i = 0; i < wavCount && ms.Position + 56 <= ms.Length; i++)
            {
                byte[] nameBytes = reader.ReadBytes(32);
                short audioFormat = reader.ReadInt16();
                short numChannels = reader.ReadInt16();
                int sampleRate = reader.ReadInt32();
                int bitRate = reader.ReadInt32();
                short blockAlign = reader.ReadInt16();
                short bitsPerSample = reader.ReadInt16();
                int unkData = reader.ReadInt32();
                int chunkSize = reader.ReadInt32();

                if (chunkSize <= 0 || ms.Position + chunkSize > ms.Length)
                    break;

                byte[] rawPcm = reader.ReadBytes(chunkSize);
                byte[] wavAudio = StartsWith(rawPcm, RiffSig)
                    ? rawPcm
                    : WrapPcmInWav(rawPcm, numChannels, sampleRate, bitsPerSample);

                string name = Encoding.ASCII.GetString(nameBytes).TrimEnd('\0').Trim();
                int parsedNum = ExtractDigits(name, i + 1);

                result.Samples.Add(new OjmSampleInfo
                {
                    SampleId = parsedNum + 1000, // Keysound Base-1 W0
                    CodecCode = 5,
                    Ref = (short)i,
                    Name = name,
                    AudioData = wavAudio
                });
            }
        }

        // 2. OGG Section
        if (oggStart >= 20 && oggStart < data.Length)
        {
            ms.Seek(oggStart, SeekOrigin.Begin);
            for (int i = 0; i < oggCount && ms.Position + 36 <= ms.Length; i++)
            {
                byte[] nameBytes = reader.ReadBytes(32);
                int sampleSize = reader.ReadInt32();

                if (sampleSize <= 0 || ms.Position + sampleSize > ms.Length)
                    break;

                byte[] oggData = reader.ReadBytes(sampleSize);
                string name = Encoding.ASCII.GetString(nameBytes).TrimEnd('\0').Trim();
                int parsedNum = ExtractDigits(name, i + 1);

                result.Samples.Add(new OjmSampleInfo
                {
                    SampleId = parsedNum, // BGM Base-1 M0
                    CodecCode = 0,
                    Ref = (short)i,
                    Name = name,
                    AudioData = oggData
                });
            }
        }
    }

    /// <summary>
    /// Scans binary data for embedded RIFF (WAV) and OggS (Vorbis) streams.
    /// </summary>
    public static List<byte[]> ScanForEmbeddedAudioStreams(byte[] data)
    {
        var streams = new List<byte[]>();

        // 1. Scan for valid RIFF (WAVE) chunks
        for (int i = 0; i < data.Length - 12; i++)
        {
            if (data[i] == 'R' && data[i + 1] == 'I' && data[i + 2] == 'F' && data[i + 3] == 'F' &&
                data[i + 8] == 'W' && data[i + 9] == 'A' && data[i + 10] == 'V' && data[i + 11] == 'E')
            {
                int chunkSize = BitConverter.ToInt32(data, i + 4) + 8;
                if (chunkSize > 12 && i + chunkSize <= data.Length)
                {
                    byte[] wavBytes = new byte[chunkSize];
                    Array.Copy(data, i, wavBytes, 0, chunkSize);
                    streams.Add(wavBytes);
                    i += chunkSize - 1; // skip past this chunk
                }
            }
        }

        // 2. Scan for OggS streams
        var oggOffsets = new List<int>();
        for (int i = 0; i < data.Length - 4; i++)
        {
            if (data[i] == 0x4F && data[i + 1] == 0x67 && data[i + 2] == 0x67 && data[i + 3] == 0x53)
            {
                // Check if Beginning Of Stream (BOS flag = 0x02) at header offset 5
                if (i + 6 < data.Length && (data[i + 5] & 0x02) != 0)
                {
                    oggOffsets.Add(i);
                }
            }
        }

        // If no BOS flagged, add first OggS offset
        if (oggOffsets.Count == 0)
        {
            for (int i = 0; i < data.Length - 4; i++)
            {
                if (data[i] == 0x4F && data[i + 1] == 0x67 && data[i + 2] == 0x67 && data[i + 3] == 0x53)
                {
                    oggOffsets.Add(i);
                    break;
                }
            }
        }

        for (int k = 0; k < oggOffsets.Count; k++)
        {
            int start = oggOffsets[k];
            int end = (k + 1 < oggOffsets.Count) ? oggOffsets[k + 1] : data.Length;
            int len = end - start;
            if (len >= 64)
            {
                byte[] oggBytes = new byte[len];
                Array.Copy(data, start, oggBytes, 0, len);
                streams.Add(oggBytes);
            }
        }

        return streams;
    }

    /// <summary>
    /// Extracts the main preview track (heaviest BGM sample or full track) directly in memory.
    /// </summary>
    public static byte[]? ExtractPreviewAudio(string ojmPath)
    {
        if (!File.Exists(ojmPath))
            return null;

        byte[] allBytes = File.ReadAllBytes(ojmPath);
        var container = ParseOjm(allBytes);
        return container.LargestSample?.AudioData;
    }

    /// <summary>
    /// Wraps raw PCM audio bytes into a standard 44-byte RIFF/WAVE header.
    /// </summary>
    public static byte[] WrapPcmInWav(byte[] pcmData, short channels, int sampleRate, short bitsPerSample)
    {
        if (channels <= 0) channels = 1;
        if (sampleRate <= 0) sampleRate = 44100;
        if (bitsPerSample <= 0) bitsPerSample = 16;

        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        int byteRate = sampleRate * channels * (bitsPerSample / 8);
        short blockAlign = (short)(channels * (bitsPerSample / 8));
        int subChunk2Size = pcmData.Length;
        int chunkSize = 36 + subChunk2Size;

        writer.Write(new[] { (byte)'R', (byte)'I', (byte)'F', (byte)'F' });
        writer.Write(chunkSize);
        writer.Write(new[] { (byte)'W', (byte)'A', (byte)'V', (byte)'E' });
        writer.Write(new[] { (byte)'f', (byte)'m', (byte)'t', (byte)' ' });
        writer.Write(16); // subChunk1Size for PCM
        writer.Write((short)1); // AudioFormat = 1 (PCM)
        writer.Write(channels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write(blockAlign);
        writer.Write(bitsPerSample);
        writer.Write(new[] { (byte)'d', (byte)'a', (byte)'t', (byte)'a' });
        writer.Write(subChunk2Size);
        writer.Write(pcmData);

        return ms.ToArray();
    }

    /// <summary>
    /// Applies 4-byte XOR encryption key in blocks of 4 bytes.
    /// Trailing bytes &lt; 4 are unencrypted per O2Jam M30 specification.
    /// </summary>
    public static void ApplyXorKey(byte[] data, byte[] key)
    {
        int fullBlocks = (data.Length / 4) * 4;
        for (int i = 0; i < fullBlocks; i += 4)
        {
            data[i] ^= key[0];
            data[i + 1] ^= key[1];
            data[i + 2] ^= key[2];
            data[i + 3] ^= key[3];
        }
    }

    private static int ExtractDigits(string name, int fallback)
    {
        var match = Regex.Match(name, @"\d+");
        if (match.Success && int.TryParse(match.Value, out int num))
            return num;
        return fallback;
    }

    private static bool StartsWith(byte[] data, byte[] prefix)
    {
        if (data.Length < prefix.Length)
            return false;
        for (int i = 0; i < prefix.Length; i++)
        {
            if (data[i] != prefix[i])
                return false;
        }
        return true;
    }
}
