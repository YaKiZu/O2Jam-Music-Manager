using System.IO;
using System.Text;
using O2JamManager.Models;

namespace O2JamManager.Core;

public static class OjnParser
{
    static OjnParser()
    {
        // Register CodePages for CP949 (Korean), Shift-JIS, GBK, CP1252 support
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public static string DecodeString(byte[] raw)
    {
        int nullIdx = Array.IndexOf(raw, (byte)0);
        int len = nullIdx >= 0 ? nullIdx : raw.Length;
        if (len == 0) return string.Empty;

        ReadOnlySpan<byte> span = raw.AsSpan(0, len);

        // 1. Try UTF-8 strict (for modern charts)
        try
        {
            var utf8Strict = new UTF8Encoding(false, true);
            return utf8Strict.GetString(span);
        }
        catch (DecoderFallbackException) { }

        // 2. Try CP949 (Korean native O2Jam)
        try
        {
            var cp949 = Encoding.GetEncoding(949, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
            return cp949.GetString(span);
        }
        catch { }

        // 3. Try Shift-JIS (Japanese)
        try
        {
            var sjis = Encoding.GetEncoding(932, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
            return sjis.GetString(span);
        }
        catch { }

        // 4. Try GBK (Chinese)
        try
        {
            var gbk = Encoding.GetEncoding(936, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
            return gbk.GetString(span);
        }
        catch { }

        // 5. Try CP1252 / Western Latin1
        try
        {
            var cp1252 = Encoding.GetEncoding(1252);
            return cp1252.GetString(span);
        }
        catch { }

        return Encoding.Latin1.GetString(span);
    }

    public static byte[] EncodeString(string text, int maxLen, string preferredEncoding = "ks_c_5601-1987")
    {
        byte[] result = new byte[maxLen];
        if (string.IsNullOrEmpty(text))
            return result;

        Encoding enc;
        try
        {
            enc = Encoding.GetEncoding(949); // CP949
        }
        catch
        {
            enc = Encoding.UTF8;
        }

        byte[] raw;
        try
        {
            raw = enc.GetBytes(text);
        }
        catch
        {
            raw = Encoding.UTF8.GetBytes(text);
        }

        if (raw.Length > maxLen)
        {
            int trimLen = maxLen;
            while (trimLen > 0)
            {
                try
                {
                    enc.GetString(raw, 0, trimLen);
                    break;
                }
                catch
                {
                    trimLen--;
                }
            }
            Array.Copy(raw, result, trimLen);
        }
        else
        {
            Array.Copy(raw, result, raw.Length);
        }

        return result;
    }

    public static Song ReadSong(string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists)
            throw new FileNotFoundException($"Le fichier n'existe pas : {filePath}");

        if (fileInfo.Length < Constants.HeaderSize)
            throw new InvalidDataException($"Fichier trop petit ({fileInfo.Length} o < 300 o) : {fileInfo.Name}");

        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new BinaryReader(fs);

        int songId = reader.ReadInt32();
        byte[] signature = reader.ReadBytes(4);

        if (signature[0] != 'o' || signature[1] != 'j' || signature[2] != 'n')
            throw new InvalidDataException($"Signature OJN invalide dans {fileInfo.Name}");

        float encodeVersion = reader.ReadSingle();
        int genreId = reader.ReadInt32();
        float bpm = reader.ReadSingle();

        short levelEx = reader.ReadInt16();
        short levelNx = reader.ReadInt16();
        short levelHx = reader.ReadInt16();
        short levelUnused = reader.ReadInt16();

        int eventsEx = reader.ReadInt32();
        int eventsNx = reader.ReadInt32();
        int eventsHx = reader.ReadInt32();

        int notesEx = reader.ReadInt32();
        int notesNx = reader.ReadInt32();
        int notesHx = reader.ReadInt32();

        int measuresEx = reader.ReadInt32();
        int measuresNx = reader.ReadInt32();
        int measuresHx = reader.ReadInt32();

        int packagesEx = reader.ReadInt32();
        int packagesNx = reader.ReadInt32();
        int packagesHx = reader.ReadInt32();

        short oldEncodeVer = reader.ReadInt16();
        short oldSongId = reader.ReadInt16();
        byte[] oldGenre = reader.ReadBytes(20);

        int bmpSize = reader.ReadInt32();
        int oldFileVer = reader.ReadInt32();

        byte[] rawTitle = reader.ReadBytes(Constants.LenTitle);
        byte[] rawArtist = reader.ReadBytes(Constants.LenArtist);
        byte[] rawNoter = reader.ReadBytes(Constants.LenNoter);
        byte[] rawOjmFile = reader.ReadBytes(Constants.LenOjmFile);

        int coverSize = reader.ReadInt32();

        int timeEx = reader.ReadInt32();
        int timeNx = reader.ReadInt32();
        int timeHx = reader.ReadInt32();

        int offsetEx = reader.ReadInt32();
        int offsetNx = reader.ReadInt32();
        int offsetHx = reader.ReadInt32();
        int offsetCover = reader.ReadInt32();

        string title = DecodeString(rawTitle);
        string artist = DecodeString(rawArtist);
        string noter = DecodeString(rawNoter);
        string ojmFile = DecodeString(rawOjmFile).Trim();

        // Audio .ojm detection
        string? musicDir = fileInfo.DirectoryName;
        string? ojmPath = null;
        bool ojmExists = false;

        if (musicDir != null)
        {
            if (!string.IsNullOrEmpty(ojmFile))
            {
                string cand = Path.Combine(musicDir, ojmFile);
                if (File.Exists(cand))
                {
                    ojmPath = cand;
                    ojmExists = true;
                }
            }

            if (!ojmExists)
            {
                string stem = Path.GetFileNameWithoutExtension(filePath);
                string candStem = Path.Combine(musicDir, stem + ".ojm");
                if (File.Exists(candStem))
                {
                    ojmPath = candStem;
                    ojmExists = true;
                    if (string.IsNullOrEmpty(ojmFile))
                        ojmFile = Path.GetFileName(candStem);
                }
            }

            if (!ojmExists && !string.IsNullOrEmpty(ojmFile))
            {
                // Case-insensitive fallback scan
                string lowerOjm = ojmFile.ToLowerInvariant();
                foreach (var file in Directory.EnumerateFiles(musicDir, "*.ojm"))
                {
                    if (Path.GetFileName(file).ToLowerInvariant() == lowerOjm)
                    {
                        ojmPath = file;
                        ojmExists = true;
                        break;
                    }
                }
            }
        }

        if (string.IsNullOrEmpty(ojmFile))
            ojmFile = Path.GetFileNameWithoutExtension(filePath) + ".ojm";

        long ojmFileSize = 0;
        if (ojmExists && !string.IsNullOrEmpty(ojmPath) && File.Exists(ojmPath))
        {
            try { ojmFileSize = new FileInfo(ojmPath).Length; } catch { }
        }

        return new Song
        {
            SongId = songId,
            FilePath = filePath,
            FileName = fileInfo.Name,
            OjmFile = ojmFile,
            OjmPath = ojmPath,
            OjmExists = ojmExists,
            OjmFileSize = ojmFileSize,
            Title = title,
            Artist = artist,
            Noter = noter,
            GenreId = genreId,
            Bpm = (float)Math.Round(bpm, 2),
            LevelEx = levelEx,
            LevelNx = levelNx,
            LevelHx = levelHx,
            TimeEx = timeEx,
            TimeNx = timeNx,
            TimeHx = timeHx,
            NoteCountEx = notesEx,
            NoteCountNx = notesNx,
            NoteCountHx = notesHx,
            EventCountEx = eventsEx,
            EventCountNx = eventsNx,
            EventCountHx = eventsHx,
            MeasureCountEx = measuresEx,
            MeasureCountNx = measuresNx,
            MeasureCountHx = measuresHx,
            PackageCountEx = packagesEx,
            PackageCountNx = packagesNx,
            PackageCountHx = packagesHx,
            EncodeVersion = (float)Math.Round(encodeVersion, 2),
            OldEncodeVersion = oldEncodeVer,
            OldSongId = oldSongId,
            OldFileVersion = oldFileVer,
            CoverSize = coverSize,
            BmpSize = bmpSize,
            CoverOffset = offsetCover,
            FileSize = fileInfo.Length
        };
    }

    public static byte[]? ReadCoverBytes(string filePath)
    {
        if (!File.Exists(filePath)) return null;

        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new BinaryReader(fs);

        fs.Seek(Constants.OffsetCoverOffset, SeekOrigin.Begin);
        int coverOffset = reader.ReadInt32();

        fs.Seek(Constants.OffsetCoverSize, SeekOrigin.Begin);
        int coverSize = reader.ReadInt32();

        if (coverSize <= 0 || coverOffset < Constants.HeaderSize || coverOffset + coverSize > fs.Length)
            return null;

        fs.Seek(coverOffset, SeekOrigin.Begin);
        return reader.ReadBytes(coverSize);
    }

    public static byte[]? ReadThumbnailBytes(string filePath)
    {
        if (!File.Exists(filePath)) return null;

        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new BinaryReader(fs);

        fs.Seek(Constants.OffsetCoverOffset, SeekOrigin.Begin);
        int coverOffset = reader.ReadInt32();

        fs.Seek(Constants.OffsetCoverSize, SeekOrigin.Begin);
        int coverSize = Math.Max(0, reader.ReadInt32());

        fs.Seek(Constants.OffsetBmpSize, SeekOrigin.Begin);
        int bmpSize = reader.ReadInt32();

        if (bmpSize <= 0 || coverOffset < Constants.HeaderSize)
            return null;

        long bmpOffset = coverOffset + coverSize;
        if (bmpOffset + bmpSize > fs.Length)
            return null;

        fs.Seek(bmpOffset, SeekOrigin.Begin);
        return reader.ReadBytes(bmpSize);
    }

    public static (int JpgLen, int BmpLen) WriteCoverAndThumbnail(string filePath, string imagePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Fichier inexistant : {filePath}");

        var (jpgBytes, bmpBytes) = ImageHelper.ConvertImageToOjnAssets(imagePath);

        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        using (var reader = new BinaryReader(fs))
        using (var writer = new BinaryWriter(fs))
        {
            // Read cover_offset at 0x128
            fs.Seek(Constants.OffsetCoverOffset, SeekOrigin.Begin);
            int coverOffset = reader.ReadInt32();

            if (coverOffset < Constants.HeaderSize)
                throw new InvalidDataException($"Offset de couverture corrompu : {coverOffset}");

            // Seek to cover offset (where note charts end)
            fs.Seek(coverOffset, SeekOrigin.Begin);

            // Write JPG cover bytes
            writer.Write(jpgBytes);

            // Write BMP thumbnail bytes
            writer.Write(bmpBytes);

            // Truncate any obsolete excess bytes from previous larger images
            fs.SetLength(fs.Position);

            // Update cover_size at 0x10C
            fs.Seek(Constants.OffsetCoverSize, SeekOrigin.Begin);
            writer.Write(jpgBytes.Length);

            // Update bmp_size at 0x64
            fs.Seek(Constants.OffsetBmpSize, SeekOrigin.Begin);
            writer.Write(bmpBytes.Length);

            fs.Flush();
        }

        return (jpgBytes.Length, bmpBytes.Length);
    }

    public static int WriteThumbnailOnly(string filePath, string imagePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Fichier inexistant : {filePath}");

        byte[] bmpBytes = ImageHelper.ConvertImageToOjnThumbnail(imagePath);

        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        using (var reader = new BinaryReader(fs))
        using (var writer = new BinaryWriter(fs))
        {
            fs.Seek(Constants.OffsetCoverOffset, SeekOrigin.Begin);
            int coverOffset = reader.ReadInt32();

            fs.Seek(Constants.OffsetCoverSize, SeekOrigin.Begin);
            int coverSize = Math.Max(0, reader.ReadInt32());

            if (coverOffset < Constants.HeaderSize)
                throw new InvalidDataException($"Offset de couverture corrompu : {coverOffset}");

            long bmpOffset = coverOffset + coverSize;
            fs.Seek(bmpOffset, SeekOrigin.Begin);

            writer.Write(bmpBytes);
            fs.SetLength(fs.Position);

            fs.Seek(Constants.OffsetBmpSize, SeekOrigin.Begin);
            writer.Write(bmpBytes.Length);

            fs.Flush();
        }

        return bmpBytes.Length;
    }

    public static void UpdateMetadata(
        string filePath,
        string title,
        string artist,
        string noter,
        int genreId,
        float bpm,
        short levelEx,
        short levelNx,
        short levelHx)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Fichier inexistant : {filePath}");

        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        using var writer = new BinaryWriter(fs);

        // Genre (0x0C, int32)
        fs.Seek(Constants.OffsetGenre, SeekOrigin.Begin);
        writer.Write(genreId);

        // BPM (0x10, float)
        fs.Seek(Constants.OffsetBpm, SeekOrigin.Begin);
        writer.Write(bpm);

        // Levels (0x14, short[3]) [Ex, Nx, Hx]
        fs.Seek(Constants.OffsetLevel, SeekOrigin.Begin);
        writer.Write(levelEx);
        writer.Write(levelNx);
        writer.Write(levelHx);

        // Title (0x6C, 64 bytes)
        fs.Seek(Constants.OffsetTitle, SeekOrigin.Begin);
        writer.Write(EncodeString(title, Constants.LenTitle));

        // Artist (0xAC, 32 bytes)
        fs.Seek(Constants.OffsetArtist, SeekOrigin.Begin);
        writer.Write(EncodeString(artist, Constants.LenArtist));

        // Noter (0xCC, 32 bytes)
        fs.Seek(Constants.OffsetNoter, SeekOrigin.Begin);
        writer.Write(EncodeString(noter, Constants.LenNoter));

        fs.Flush();
    }

    public static void UpdateSongId(string filePath, int newId, string? newOjmFile = null)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Fichier inexistant : {filePath}");

        newOjmFile ??= $"o2ma{newId}.ojm";

        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        using var writer = new BinaryWriter(fs);

        // song_id at 0x00 (int32)
        fs.Seek(Constants.OffsetSongId, SeekOrigin.Begin);
        writer.Write(newId);

        // old_song_id at 0x4E (int16)
        fs.Seek(Constants.OffsetOldSongId, SeekOrigin.Begin);
        writer.Write((short)(newId % 65536));

        // ojm_file at 0xEC (32 bytes)
        fs.Seek(Constants.OffsetOjmFile, SeekOrigin.Begin);
        writer.Write(EncodeString(newOjmFile, Constants.LenOjmFile));

        fs.Flush();
    }
}
