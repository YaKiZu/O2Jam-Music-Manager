using System.IO;
using O2JamManager.Models;

namespace O2JamManager.Core;

public class FileManager
{
    public string? MusicDir { get; private set; }

    public void SetMusicDir(string dirPath)
    {
        if (!Directory.Exists(dirPath))
            throw new DirectoryNotFoundException($"Le dossier n'existe pas : {dirPath}");
        MusicDir = dirPath;
    }

    public (List<Song> Songs, List<(string Path, string Error)> Errors) ScanMusicDir()
    {
        if (string.IsNullOrEmpty(MusicDir) || !Directory.Exists(MusicDir))
            throw new InvalidOperationException("Aucun dossier valide n'a été sélectionné.");

        var songs = new List<Song>();
        var errors = new List<(string, string)>();

        var ojnFiles = Directory.EnumerateFiles(MusicDir, "*.ojn", SearchOption.TopDirectoryOnly)
            .OrderBy(f => Path.GetFileName(f).ToLowerInvariant());

        foreach (var file in ojnFiles)
        {
            try
            {
                var song = OjnParser.ReadSong(file);
                songs.Add(song);
            }
            catch (Exception ex)
            {
                errors.Add((file, ex.Message));
            }
        }

        songs.Sort((a, b) =>
        {
            int cmp = a.SongId.CompareTo(b.SongId);
            return cmp != 0 ? cmp : string.Compare(a.FileName, b.FileName, StringComparison.OrdinalIgnoreCase);
        });

        return (songs, errors);
    }

    public int GetNextAvailableId(IEnumerable<Song>? currentSongs = null)
    {
        currentSongs ??= ScanMusicDir().Songs;
        var existingIds = currentSongs.Select(s => s.SongId).ToHashSet();
        for (int candidate = 1000; candidate <= 9999; candidate++)
        {
            if (!existingIds.Contains(candidate))
                return candidate;
        }
        int max = existingIds.Count > 0 ? existingIds.Max() : 999;
        return Math.Max(1000, max + 1);
    }

    public Song ChangeSongId(Song song, int newId)
    {
        if (string.IsNullOrEmpty(MusicDir))
            throw new InvalidOperationException("Dossier non défini.");

        if (song.SongId == newId)
            return song;

        string targetOjn = Path.Combine(MusicDir, $"o2ma{newId}.ojn");
        string targetOjm = Path.Combine(MusicDir, $"o2ma{newId}.ojm");

        if (File.Exists(targetOjn) && !string.Equals(Path.GetFullPath(targetOjn), Path.GetFullPath(song.FilePath), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Le fichier cible {Path.GetFileName(targetOjn)} existe déjà !");

        // 1. Update internal header in source .ojn
        string newOjmName = $"o2ma{newId}.ojm";
        OjnParser.UpdateSongId(song.FilePath, newId, newOjmName);

        // 2. Rename physical .ojn file
        File.Move(song.FilePath, targetOjn);

        // 3. Rename matching physical .ojm file if present
        if (song.OjmExists && !string.IsNullOrEmpty(song.OjmPath) && File.Exists(song.OjmPath))
        {
            File.Move(song.OjmPath, targetOjm);
        }

        return OjnParser.ReadSong(targetOjn);
    }

    private class ReindexOp
    {
        public required Song Song { get; set; }
        public int OldId { get; set; }
        public int NewId { get; set; }
        public required string SrcOjn { get; set; }
        public string? SrcOjm { get; set; }
        public required string TmpOjn { get; set; }
        public string? TmpOjm { get; set; }
        public required string TargetOjn { get; set; }
        public string? TargetOjm { get; set; }
    }

    public List<Song> ReindexSongs(List<Song> songsToReindex, int startId = 1, int step = 1, Action<int, int>? progressCallback = null)
    {
        if (string.IsNullOrEmpty(MusicDir))
            throw new InvalidOperationException("Dossier non défini.");

        if (songsToReindex.Count == 0)
            return [];

        startId = Math.Max(1, startId);

        string sessionId = Guid.NewGuid().ToString("N")[..8];
        var operations = new List<ReindexOp>();
        int currentId = startId;

        for (int i = 0; i < songsToReindex.Count; i++)
        {
            var song = songsToReindex[i];
            int newId = currentId;

            string targetOjn = Path.Combine(MusicDir, $"o2ma{newId}.ojn");
            string targetOjm = Path.Combine(MusicDir, $"o2ma{newId}.ojm");

            bool ojnMatch = string.Equals(song.FilePath, targetOjn, StringComparison.OrdinalIgnoreCase);
            bool ojmMatch = !song.OjmExists || string.IsNullOrEmpty(song.OjmPath) || string.Equals(song.OjmPath, targetOjm, StringComparison.OrdinalIgnoreCase);
            bool idMatch = (song.SongId == newId);

            // Optimization 2c: Only rewrite to disk and rename files whose ID or filenames actually changed!
            if (!idMatch || !ojnMatch || !ojmMatch)
            {
                string tmpOjn = Path.Combine(MusicDir, $"__tmp_{sessionId}_{i:D4}.ojn");
                string? tmpOjm = (song.OjmExists && !string.IsNullOrEmpty(song.OjmPath) && File.Exists(song.OjmPath))
                    ? Path.Combine(MusicDir, $"__tmp_{sessionId}_{i:D4}.ojm")
                    : null;

                operations.Add(new ReindexOp
                {
                    Song = song,
                    OldId = song.SongId,
                    NewId = newId,
                    SrcOjn = song.FilePath,
                    SrcOjm = (song.OjmExists && !string.IsNullOrEmpty(song.OjmPath) && File.Exists(song.OjmPath)) ? song.OjmPath : null,
                    TmpOjn = tmpOjn,
                    TmpOjm = tmpOjm,
                    TargetOjn = targetOjn,
                    TargetOjm = targetOjm
                });
            }

            if (currentId < 9999)
                currentId += step;
        }

        if (operations.Count == 0)
        {
            return ScanMusicDir().Songs;
        }

        var renamedTemps = new List<ReindexOp>();
        try
        {
            int totalOps = operations.Count;

            // Phase 1: Rename all modified source files to temporary filenames
            for (int i = 0; i < operations.Count; i++)
            {
                var op = operations[i];
                File.Move(op.SrcOjn, op.TmpOjn);
                if (op.SrcOjm != null && op.TmpOjm != null)
                {
                    File.Move(op.SrcOjm, op.TmpOjm);
                }
                renamedTemps.Add(op);
                progressCallback?.Invoke(i + 1, totalOps);
            }

            // Phase 2: Update internal headers in each temporary file
            for (int i = 0; i < operations.Count; i++)
            {
                var op = operations[i];
                string newOjmRef = $"o2ma{op.NewId}.ojm";
                OjnParser.UpdateSongId(op.TmpOjn, op.NewId, newOjmRef);
                progressCallback?.Invoke(i + 1, totalOps);
            }

            // Phase 3: Rename temporary files to final target filenames
            for (int i = 0; i < operations.Count; i++)
            {
                var op = operations[i];
                File.Move(op.TmpOjn, op.TargetOjn);
                if (op.TmpOjm != null && op.TargetOjm != null)
                {
                    File.Move(op.TmpOjm, op.TargetOjm);
                }
                progressCallback?.Invoke(i + 1, totalOps);
            }
        }
        catch (Exception ex)
        {
            // Rollback attempts
            foreach (var op in renamedTemps)
            {
                try
                {
                    if (File.Exists(op.TmpOjn))
                        File.Move(op.TmpOjn, op.SrcOjn);
                    if (op.TmpOjm != null && op.SrcOjm != null && File.Exists(op.TmpOjm))
                        File.Move(op.TmpOjm, op.SrcOjm);
                }
                catch { }
            }
            throw new InvalidOperationException($"Échec de la réindexation : {ex.Message}", ex);
        }

        return ScanMusicDir().Songs;
    }

    /// <summary>
    /// Strict automated custom reindexing (1000-9999).
    /// Rule a: All charts with ID between 0 and 999 (o2ma000..o2ma999) are strictly untouchable (files, headers, OJNList).
    /// Rule b: Targets only charts with ID >= 1000 (or unindexed / not in 0..999) and reindexes sequentially from 1000 to 9999.
    /// Rule c: Renames .ojn and .ojm collision-free via temp names, updates header SongID and synchronizes OJNList.dat.
    /// </summary>
    public (int ReindexedCount, int StartId, int EndId, string? OjnListUpdatedPath) ReindexCustomSongs(IEnumerable<Song>? songsOrder = null, Action<int, int>? progressCallback = null)
    {
        if (string.IsNullOrEmpty(MusicDir) || !Directory.Exists(MusicDir))
            throw new InvalidOperationException("Dossier non défini.");

        var allSongs = (songsOrder ?? ScanMusicDir().Songs).ToList();

        // Strictly custom charts (ID >= 1000 or negative/unindexed)
        var customSongs = allSongs.Where(s => s.SongId < 0 || s.SongId >= 1000).ToList();
        if (customSongs.Count == 0)
            return (0, 1000, 1000, null);

        string sessionId = Guid.NewGuid().ToString("N")[..8];
        var operations = new List<ReindexOp>();
        int currentId = 1000;

        for (int i = 0; i < customSongs.Count; i++)
        {
            if (currentId > 9999)
                throw new InvalidOperationException("Limite maximale atteinte : impossible d'attribuer un ID supérieur à 9999.");

            var song = customSongs[i];
            int newId = currentId;

            string targetOjn = Path.Combine(MusicDir, $"o2ma{newId}.ojn");
            string targetOjm = Path.Combine(MusicDir, $"o2ma{newId}.ojm");

            bool ojnMatch = string.Equals(song.FilePath, targetOjn, StringComparison.OrdinalIgnoreCase);
            bool ojmMatch = !song.OjmExists || string.IsNullOrEmpty(song.OjmPath) || string.Equals(song.OjmPath, targetOjm, StringComparison.OrdinalIgnoreCase);
            bool idMatch = (song.SongId == newId);

            // Optimization 2c: Only rewrite to disk and rename files whose ID or filenames actually changed!
            if (!idMatch || !ojnMatch || !ojmMatch)
            {
                string tmpOjn = Path.Combine(MusicDir, $"__tmp_{sessionId}_{i:D4}.ojn");
                string? tmpOjm = (song.OjmExists && !string.IsNullOrEmpty(song.OjmPath) && File.Exists(song.OjmPath))
                    ? Path.Combine(MusicDir, $"__tmp_{sessionId}_{i:D4}.ojm")
                    : null;

                operations.Add(new ReindexOp
                {
                    Song = song,
                    OldId = song.SongId,
                    NewId = newId,
                    SrcOjn = song.FilePath,
                    SrcOjm = (song.OjmExists && !string.IsNullOrEmpty(song.OjmPath) && File.Exists(song.OjmPath)) ? song.OjmPath : null,
                    TmpOjn = tmpOjn,
                    TmpOjm = tmpOjm,
                    TargetOjn = targetOjn,
                    TargetOjm = targetOjm
                });
            }

            currentId++;
        }

        int endId = currentId - 1;

        if (operations.Count > 0)
        {
            var renamedTemps = new List<ReindexOp>();
            try
            {
                int totalOps = operations.Count;

                // Phase 1: Rename all source files to temporary names
                for (int i = 0; i < operations.Count; i++)
                {
                    var op = operations[i];
                    File.Move(op.SrcOjn, op.TmpOjn);
                    if (op.SrcOjm != null && op.TmpOjm != null)
                    {
                        File.Move(op.SrcOjm, op.TmpOjm);
                    }
                    renamedTemps.Add(op);
                    progressCallback?.Invoke(i + 1, totalOps);
                }

                // Phase 2: Update internal header SongID, OldSongID, OjmRef
                for (int i = 0; i < operations.Count; i++)
                {
                    var op = operations[i];
                    string newOjmRef = $"o2ma{op.NewId}.ojm";
                    OjnParser.UpdateSongId(op.TmpOjn, op.NewId, newOjmRef);
                    progressCallback?.Invoke(i + 1, totalOps);
                }

                // Phase 3: Rename temporary files to final target filenames
                for (int i = 0; i < operations.Count; i++)
                {
                    var op = operations[i];
                    File.Move(op.TmpOjn, op.TargetOjn);
                    if (op.TmpOjm != null && op.TargetOjm != null)
                    {
                        File.Move(op.TmpOjm, op.TargetOjm);
                    }
                    progressCallback?.Invoke(i + 1, totalOps);
                }
            }
            catch (Exception ex)
            {
                foreach (var op in renamedTemps)
                {
                    try
                    {
                        if (File.Exists(op.TmpOjn))
                            File.Move(op.TmpOjn, op.SrcOjn);
                        if (op.TmpOjm != null && op.SrcOjm != null && File.Exists(op.TmpOjm))
                            File.Move(op.TmpOjm, op.SrcOjm);
                    }
                    catch { }
                }
                throw new InvalidOperationException($"Échec de la réindexation custom : {ex.Message}", ex);
            }
        }

        // Synchronize OJNList.dat if present
        string? updatedOjnList = null;
        try
        {
            string? existingOjnList = OjnListGenerator.FindExistingOjnListPath(MusicDir);
            if (!string.IsNullOrEmpty(existingOjnList) && File.Exists(existingOjnList))
            {
                var refreshed = ScanMusicDir().Songs;
                var res = OjnListGenerator.Generate(MusicDir, existingOjnList, refreshed);
                updatedOjnList = res.TargetPath;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OJNList sync notice: {ex.Message}");
        }

        return (operations.Count, 1000, endId, updatedOjnList);
    }

    public Song AddSong(string srcOjn, string? srcOjm = null, int? assignedId = null)
    {
        if (string.IsNullOrEmpty(MusicDir))
            throw new InvalidOperationException("Dossier non défini.");

        if (!File.Exists(srcOjn))
            throw new FileNotFoundException($"Fichier OJN source introuvable : {srcOjn}");

        assignedId ??= GetNextAvailableId();

        string targetOjn = Path.Combine(MusicDir, $"o2ma{assignedId}.ojn");
        string targetOjm = Path.Combine(MusicDir, $"o2ma{assignedId}.ojm");

        if (File.Exists(targetOjn))
            throw new InvalidOperationException($"Le fichier {Path.GetFileName(targetOjn)} existe déjà dans Music !");

        // Auto-detect src .ojm if not provided
        if (string.IsNullOrEmpty(srcOjm))
        {
            string candOjm = Path.Combine(Path.GetDirectoryName(srcOjn) ?? "", Path.GetFileNameWithoutExtension(srcOjn) + ".ojm");
            if (File.Exists(candOjm))
                srcOjm = candOjm;
        }

        File.Copy(srcOjn, targetOjn, false);
        if (!string.IsNullOrEmpty(srcOjm) && File.Exists(srcOjm))
        {
            File.Copy(srcOjm, targetOjm, false);
        }

        // Update header in copied file
        OjnParser.UpdateSongId(targetOjn, assignedId.Value, $"o2ma{assignedId}.ojm");

        return OjnParser.ReadSong(targetOjn);
    }

    public void DeleteSong(Song song)
    {
        if (File.Exists(song.FilePath))
            File.Delete(song.FilePath);

        if (song.OjmExists && !string.IsNullOrEmpty(song.OjmPath) && File.Exists(song.OjmPath))
        {
            File.Delete(song.OjmPath);
        }
        else if (!string.IsNullOrEmpty(MusicDir))
        {
            string candOjm = Path.Combine(MusicDir, $"o2ma{song.SongId}.ojm");
            if (File.Exists(candOjm))
                File.Delete(candOjm);
        }
    }
}
