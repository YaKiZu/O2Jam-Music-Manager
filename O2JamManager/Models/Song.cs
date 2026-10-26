using System.IO;
using O2JamManager.Core;

namespace O2JamManager.Models;

public class Song
{
    private string _filePath = string.Empty;
    private string _fileName = string.Empty;
    private string _ojmFile = string.Empty;
    private float _bpm = 120.0f;
    private string _bpmDisplay = "120";
    private short _levelEx;
    private short _levelNx;
    private short _levelHx;
    private string _levelsDisplay = "0 / 0 / 0";
    private int _hardSortKey = 0;
    private int _timeEx;
    private int _timeNx;
    private int _timeHx;
    private int _maxDuration = 0;
    private string _durationDisplay = "--:--";
    private string _ojnNaturalSortKey = string.Empty;
    private string _ojmNaturalSortKey = string.Empty;

    public Song()
    {
    }

    public int SongId { get; set; }

    public string FilePath
    {
        get => _filePath;
        set
        {
            _filePath = value;
            FileName = Path.GetFileName(value);
        }
    }

    public string FileName
    {
        get => _fileName;
        set
        {
            _fileName = value;
            _ojnNaturalSortKey = GetNaturalSortKey(value);
        }
    }

    public string OjmFile
    {
        get => _ojmFile;
        set
        {
            _ojmFile = value;
            _ojmNaturalSortKey = GetNaturalSortKey(value);
        }
    }

    public string? OjmPath { get; set; }
    private bool _ojmExists;
    private string _ojmStatusDisplay = "Missing";
    public bool OjmExists
    {
        get => _ojmExists;
        set
        {
            _ojmExists = value;
            _ojmStatusDisplay = value ? "Present" : "Missing";
        }
    }
    public string OjmStatusDisplay => _ojmStatusDisplay;

    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string Noter { get; set; } = string.Empty;
    public int GenreId { get; set; }
    public string GenreName => Constants.GetGenreName(GenreId);

    public float Bpm
    {
        get => _bpm;
        set
        {
            _bpm = value;
            _bpmDisplay = $"{value:0.##}";
        }
    }
    public string BpmDisplay => _bpmDisplay;

    private void UpdateLevelsDisplay()
    {
        _levelsDisplay = $"{_levelEx} / {_levelNx} / {_levelHx}";
        _hardSortKey = (_levelHx * 10000) + (_levelNx * 100) + _levelEx;
    }

    public short LevelEx { get => _levelEx; set { _levelEx = value; UpdateLevelsDisplay(); } }
    public short LevelNx { get => _levelNx; set { _levelNx = value; UpdateLevelsDisplay(); } }
    public short LevelHx { get => _levelHx; set { _levelHx = value; UpdateLevelsDisplay(); } }
    public string LevelsDisplay => _levelsDisplay;
    public int HardSortKey => _hardSortKey;

    private void UpdateDurationDisplay()
    {
        int t = _timeHx > 0 ? _timeHx : (_timeNx > 0 ? _timeNx : _timeEx);
        _maxDuration = t;
        _durationDisplay = t <= 0 ? "--:--" : $"{t / 60:D2}:{t % 60:D2}";
    }

    public int TimeEx { get => _timeEx; set { _timeEx = value; UpdateDurationDisplay(); } }
    public int TimeNx { get => _timeNx; set { _timeNx = value; UpdateDurationDisplay(); } }
    public int TimeHx { get => _timeHx; set { _timeHx = value; UpdateDurationDisplay(); } }
    public string DurationDisplay => _durationDisplay;
    public int MaxDuration => _maxDuration;

    private static string GetNaturalSortKey(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        return System.Text.RegularExpressions.Regex.Replace(
            input,
            @"\d+",
            m => m.Value.PadLeft(10, '0')
        ).ToLowerInvariant();
    }

    public string OjnNaturalSortKey => _ojnNaturalSortKey;
    public string OjmNaturalSortKey => _ojmNaturalSortKey;

    public string DurationsAll => $"{FormatDuration(TimeEx)} / {FormatDuration(TimeNx)} / {FormatDuration(TimeHx)}";

    private static string FormatDuration(int seconds)
    {
        if (seconds <= 0) return "--:--";
        return $"{seconds / 60:D2}:{seconds % 60:D2}";
    }

    public int NoteCountEx { get; set; }
    public int NoteCountNx { get; set; }
    public int NoteCountHx { get; set; }
    public string NotesAll => $"{NoteCountEx} / {NoteCountNx} / {NoteCountHx}";

    public int EventCountEx { get; set; }
    public int EventCountNx { get; set; }
    public int EventCountHx { get; set; }

    public int MeasureCountEx { get; set; }
    public int MeasureCountNx { get; set; }
    public int MeasureCountHx { get; set; }

    public int PackageCountEx { get; set; }
    public int PackageCountNx { get; set; }
    public int PackageCountHx { get; set; }

    public float EncodeVersion { get; set; } = 2.9f;
    public short OldEncodeVersion { get; set; }
    public short OldSongId { get; set; }
    public int OldFileVersion { get; set; }

    public int CoverSize { get; set; }
    public int BmpSize { get; set; }
    public int CoverOffset { get; set; } = 300;
    public long FileSize { get; set; }

    public string FileSizeDisplay => OjnFileSizeDisplay;

    public string OjnFileSizeDisplay
    {
        get
        {
            if (FileSize <= 0) return "--";
            if (FileSize >= 1048576)
                return $"{FileSize / (1024.0 * 1024.0):F2} MB ({FileSize:N0} bytes)";
            return $"{FileSize / 1024.0:F1} KB ({FileSize:N0} bytes)";
        }
    }

    public long OjmFileSize { get; set; }

    public string OjmFileSizeDisplay
    {
        get
        {
            if (!OjmExists)
                return "Missing";
            long size = OjmFileSize;
            if (size >= 1048576)
                return $"{size / (1024.0 * 1024.0):F2} MB ({size:N0} bytes)";
            return $"{size / 1024.0:F1} KB ({size:N0} bytes)";
        }
    }

    public bool HasCover => CoverSize > 0;
    public bool HasThumbnail => BmpSize > 0;

    public Song Clone()
    {
        return new Song
        {
            SongId = this.SongId,
            FilePath = this.FilePath,
            FileName = this.FileName,
            OjmFile = this.OjmFile,
            OjmPath = this.OjmPath,
            OjmExists = this.OjmExists,
            Title = this.Title,
            Artist = this.Artist,
            Noter = this.Noter,
            GenreId = this.GenreId,
            Bpm = this.Bpm,
            LevelEx = this.LevelEx,
            LevelNx = this.LevelNx,
            LevelHx = this.LevelHx,
            TimeEx = this.TimeEx,
            TimeNx = this.TimeNx,
            TimeHx = this.TimeHx,
            NoteCountEx = this.NoteCountEx,
            NoteCountNx = this.NoteCountNx,
            NoteCountHx = this.NoteCountHx,
            EventCountEx = this.EventCountEx,
            EventCountNx = this.EventCountNx,
            EventCountHx = this.EventCountHx,
            MeasureCountEx = this.MeasureCountEx,
            MeasureCountNx = this.MeasureCountNx,
            MeasureCountHx = this.MeasureCountHx,
            PackageCountEx = this.PackageCountEx,
            PackageCountNx = this.PackageCountNx,
            PackageCountHx = this.PackageCountHx,
            EncodeVersion = this.EncodeVersion,
            OldEncodeVersion = this.OldEncodeVersion,
            OldSongId = this.OldSongId,
            OldFileVersion = this.OldFileVersion,
            CoverSize = this.CoverSize,
            BmpSize = this.BmpSize,
            CoverOffset = this.CoverOffset,
            FileSize = this.FileSize,
            OjmFileSize = this.OjmFileSize
        };
    }
}
