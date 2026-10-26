using System.IO;
using System.Text;
using O2JamManager.Models;

namespace O2JamManager.Core;

public static class OjnListGenerator
{
    private record MarkerEntry(int SongId, int P1, int P2, int P3);
    private record PremiumEntry(int SongId, int PricePoint, int PriceGem, int Unused);
    private record MetadataEntry(int SongId, byte[] DateBytes, byte P2, int P3, int P4, int P5);

    public static (int SongCount, string TargetPath) Generate(string musicDir, string? targetFilePath = null, IEnumerable<Song>? songs = null)
    {
        if (string.IsNullOrEmpty(musicDir) || !Directory.Exists(musicDir))
            throw new DirectoryNotFoundException($"Le dossier selectionne n'existe pas : {musicDir}");

        // 1. Gather valid OJN headers
        var candidatePaths = new List<string>();
        if (songs != null)
        {
            foreach (var s in songs)
            {
                if (!string.IsNullOrEmpty(s.FilePath) && File.Exists(s.FilePath))
                    candidatePaths.Add(s.FilePath);
            }
        }

        if (candidatePaths.Count == 0)
        {
            candidatePaths = Directory.EnumerateFiles(musicDir, "*.ojn", SearchOption.TopDirectoryOnly)
                .OrderBy(f => Path.GetFileName(f).ToLowerInvariant())
                .ToList();
        }

        var headerList = new List<(int SongId, string Path, byte[] Header)>();
        foreach (var file in candidatePaths)
        {
            try
            {
                using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                if (fs.Length < Constants.HeaderSize) continue;

                byte[] hdr = new byte[Constants.HeaderSize];
                int read = fs.Read(hdr, 0, Constants.HeaderSize);
                if (read == Constants.HeaderSize && hdr[4] == 'o' && hdr[5] == 'j' && hdr[6] == 'n')
                {
                    int sid = BitConverter.ToInt32(hdr, 0);
                    headerList.Add((sid, file, hdr));
                }
            }
            catch { }
        }

        // Sort strictly by Song ID ascending (O2Jam specification)
        headerList.Sort((a, b) => a.SongId.CompareTo(b.SongId));

        if (headerList.Count == 0)
            throw new InvalidOperationException($"Aucun fichier .ojn valide trouvé dans {musicDir}");

        // 2. Parse existing OJNList.dat if available to preserve metadata
        var (existingMarkers, existingPremium, existingMetadata) = FindAndParseExistingOjnList(musicDir);

        // 3. Construct binary data
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Header: Song Count
        writer.Write(headerList.Count);

        // 300 bytes per song
        foreach (var (_, _, hdr) in headerList)
        {
            writer.Write(hdr);
        }

        // Section 1: Markers
        var activeMarkers = headerList
            .Where(h => existingMarkers.ContainsKey(h.SongId))
            .Select(h => existingMarkers[h.SongId])
            .ToList();
        writer.Write(activeMarkers.Count);
        foreach (var m in activeMarkers)
        {
            writer.Write(m.SongId);
            writer.Write(m.P1);
            writer.Write(m.P2);
            writer.Write(m.P3);
        }

        // Section 2: Premium
        var activePremium = headerList
            .Where(h => existingPremium.ContainsKey(h.SongId))
            .Select(h => existingPremium[h.SongId])
            .ToList();
        writer.Write(activePremium.Count);
        foreach (var p in activePremium)
        {
            writer.Write(p.SongId);
            writer.Write(p.PricePoint);
            writer.Write(p.PriceGem);
            writer.Write(p.Unused);
        }

        // Section 3: Extra Metadata (all songs)
        writer.Write(headerList.Count);
        string todayStr = DateTime.Now.ToString("yyyy-MM-dd");
        byte[] defaultDateBytes = new byte[11];
        byte[] rawToday = Encoding.UTF8.GetBytes(todayStr);
        Array.Copy(rawToday, defaultDateBytes, Math.Min(rawToday.Length, 10));

        foreach (var (sid, _, _) in headerList)
        {
            writer.Write(sid);
            if (existingMetadata.TryGetValue(sid, out var meta))
            {
                writer.Write(meta.DateBytes);
                writer.Write(meta.P2);
                writer.Write(meta.P3);
                writer.Write(meta.P4);
                writer.Write(meta.P5);
            }
            else
            {
                writer.Write(defaultDateBytes);
                writer.Write((byte)0);
                writer.Write(0);
                writer.Write(1243692); // 0x12FA2C
                writer.Write(4540192); // 0x454720
            }
        }

        writer.Flush();
        byte[] outputData = ms.ToArray();

        // 4. Determine write target
        string target = !string.IsNullOrEmpty(targetFilePath)
            ? Path.GetFullPath(targetFilePath)
            : Path.Combine(musicDir, "OJNList.dat");

        // 5. Write binary to target
        string? dir = Path.GetDirectoryName(target);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllBytes(target, outputData);

        return (headerList.Count, target);
    }

    public static string? FindExistingOjnListPath(string musicDir)
    {
        if (string.IsNullOrEmpty(musicDir) || !Directory.Exists(musicDir))
            return null;

        var candidates = new List<string>
        {
            Path.Combine(musicDir, "OJNList.dat"),
        };

        string? parent = Directory.GetParent(musicDir)?.FullName;
        if (!string.IsNullOrEmpty(parent))
        {
            candidates.Add(Path.Combine(parent, "Image", "OJNList.dat"));
            candidates.Add(Path.Combine(parent, "OJNList.dat"));
        }

        candidates.Add(Path.Combine(musicDir, "Image", "OJNList.dat"));

        foreach (var path in candidates)
        {
            if (File.Exists(path))
                return path;
        }

        return null;
    }

    private static (Dictionary<int, MarkerEntry>, Dictionary<int, PremiumEntry>, Dictionary<int, MetadataEntry>)
        FindAndParseExistingOjnList(string musicDir)
    {
        var markers = new Dictionary<int, MarkerEntry>();
        var premium = new Dictionary<int, PremiumEntry>();
        var metadata = new Dictionary<int, MetadataEntry>();

        string? existingPath = FindExistingOjnListPath(musicDir);
        if (!string.IsNullOrEmpty(existingPath) && File.Exists(existingPath))
        {
            TryParseOjnList(existingPath, markers, premium, metadata);
        }

        return (markers, premium, metadata);
    }

    private static bool TryParseOjnList(
        string filePath,
        Dictionary<int, MarkerEntry> markers,
        Dictionary<int, PremiumEntry> premium,
        Dictionary<int, MetadataEntry> metadata)
    {
        try
        {
            byte[] data = File.ReadAllBytes(filePath);
            if (data.Length < 4) return false;

            int songCount = BitConverter.ToInt32(data, 0);
            int offset = 4 + songCount * Constants.HeaderSize;
            if (data.Length < offset) return false;

            // Marker section
            if (offset + 4 <= data.Length)
            {
                int s1 = BitConverter.ToInt32(data, offset);
                offset += 4;
                for (int i = 0; i < s1 && offset + 16 <= data.Length; i++)
                {
                    int sid = BitConverter.ToInt32(data, offset);
                    int p1 = BitConverter.ToInt32(data, offset + 4);
                    int p2 = BitConverter.ToInt32(data, offset + 8);
                    int p3 = BitConverter.ToInt32(data, offset + 12);
                    markers[sid] = new MarkerEntry(sid, p1, p2, p3);
                    offset += 16;
                }
            }

            // Premium section
            if (offset + 4 <= data.Length)
            {
                int s2 = BitConverter.ToInt32(data, offset);
                offset += 4;
                for (int i = 0; i < s2 && offset + 16 <= data.Length; i++)
                {
                    int sid = BitConverter.ToInt32(data, offset);
                    int pt = BitConverter.ToInt32(data, offset + 4);
                    int gem = BitConverter.ToInt32(data, offset + 8);
                    int un = BitConverter.ToInt32(data, offset + 12);
                    premium[sid] = new PremiumEntry(sid, pt, gem, un);
                    offset += 16;
                }
            }

            // Extra metadata section
            if (offset + 4 <= data.Length)
            {
                int s3 = BitConverter.ToInt32(data, offset);
                offset += 4;
                for (int i = 0; i < s3 && offset + 28 <= data.Length; i++)
                {
                    int sid = BitConverter.ToInt32(data, offset);
                    byte[] dateBytes = new byte[11];
                    Array.Copy(data, offset + 4, dateBytes, 0, 11);
                    byte p2 = data[offset + 15];
                    int p3 = BitConverter.ToInt32(data, offset + 16);
                    int p4 = BitConverter.ToInt32(data, offset + 20);
                    int p5 = BitConverter.ToInt32(data, offset + 24);
                    metadata[sid] = new MetadataEntry(sid, dateBytes, p2, p3, p4, p5);
                    offset += 28;
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}
