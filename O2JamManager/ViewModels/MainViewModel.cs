using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using O2JamManager.Core;
using O2JamManager.Models;

namespace O2JamManager.ViewModels;

public class StatusFilterOption
{
    public string Id { get; init; } = "ALL";
    public string Name { get; init; } = "All statuses";
    public override string ToString() => Name;
}

public class MainViewModel : INotifyPropertyChanged
{
    private readonly FileManager _fileManager = new();
    private readonly AudioPlayer _audioPlayer = new();
    private readonly System.Windows.Threading.DispatcherTimer _audioTimer = new();

    private string _musicDir = string.Empty;
    private string _searchText = string.Empty;
    private string _statusFilter = "ALL";
    private Song? _selectedSong;
    private string _statusMessage = "Ready.";
    private BitmapImage? _coverImage;
    private BitmapImage? _thumbnailImage;
    private string _coverInfoText = "Cover: None";
    private string _thumbInfoText = "Thumbnail: None";

    // Audio Player State
    private bool _isAudioPlaying;
    private bool _isAudioPaused;
    private double _audioPosition;
    private double _audioDuration;
    private string _audioTimeDisplay = "00:00 / 00:00";
    private float _audioVolume = 0.50f; // 50% par defaut au demarrage
    private string _audioStatusText = "Ready to play";
    private string _diagnosticInfoText = "OJM Format: - | Duration: -";
    public bool IsUserSeeking { get; set; }

    // Language selection
    public LanguageItem[] AvailableLanguages => LocalizationService.SupportedLanguages;
    private LanguageItem _selectedLanguage = LocalizationService.SupportedLanguages.FirstOrDefault(l => l.Code == LocalizationService.Instance.CurrentLanguage) ?? LocalizationService.SupportedLanguages[0];

    public LanguageItem SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (_selectedLanguage != value && value != null)
            {
                _selectedLanguage = value;
                retranslate_ui(value.Code);
                OnPropertyChanged();
            }
        }
    }

    public void retranslate_ui(string langCode)
    {
        LocalizationService.Instance.CurrentLanguage = langCode;
        foreach (var song in Songs)
        {
        }
        RefreshLocalizedTexts();
        OnPropertyChanged(string.Empty);
    }

    private void RefreshLocalizedTexts()
    {
        var loc = LocalizationService.Instance;
        if (_statusMessage == "Prêt." || _statusMessage == "Ready." || _statusMessage == "準備完了。")
        {
            _statusMessage = loc["StatusReady"];
            OnPropertyChanged(nameof(StatusMessage));
        }

        if (SelectedSong == null)
        {
            CoverInfoText = loc["CoverInfoNone"];
            ThumbInfoText = loc["ThumbInfoNone"];
            AudioStatusText = loc["AudioSelectSong"];
            DiagnosticInfoText = loc["AudioDiagDefault"];
        }
        else
        {
            OnSelectedSongChanged();
        }

        OnPropertyChanged(nameof(TotalCountText));
        OnPropertyChanged(nameof(OjmStatsText));
        OnPropertyChanged(nameof(StatusSelection));
    }

    // Edit fields
    private int _editSongId = 1;
    private string _editTitle = string.Empty;
    private string _editArtist = string.Empty;
    private string _editNoter = string.Empty;
    private float _editBpm = 120.0f;
    private int _editGenreIndex = 0;
    private short _editLevelEx;
    private short _editLevelNx;
    private short _editLevelHx;

    public ObservableRangeCollection<Song> Songs { get; } = [];
    public ObservableRangeCollection<Song> FilteredSongs { get; } = [];

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (_isBusy != value)
            {
                _isBusy = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsNotBusy));
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }
        }
    }
    public bool IsNotBusy => !_isBusy;

    private int _progressValue;
    public int ProgressValue
    {
        get => _progressValue;
        set { if (_progressValue != value) { _progressValue = value; OnPropertyChanged(); } }
    }

    private int _progressMax = 100;
    public int ProgressMax
    {
        get => _progressMax;
        set { if (_progressMax != value) { _progressMax = value; OnPropertyChanged(); } }
    }

    private bool _isProgressIndeterminate;
    public bool IsProgressIndeterminate
    {
        get => _isProgressIndeterminate;
        set { if (_isProgressIndeterminate != value) { _isProgressIndeterminate = value; OnPropertyChanged(); } }
    }

    public string[] Genres => Constants.Genres;

    public string MusicDir
    {
        get => _musicDir;
        set { if (_musicDir != value) { _musicDir = value; OnPropertyChanged(); } }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText != value)
            {
                _searchText = value;
                OnPropertyChanged();
                ApplyFilter();
            }
        }
    }

    public static readonly StatusFilterOption[] StatusFilterList =
    [
        new() { Id = "ALL", Name = "All statuses" },
        new() { Id = "PRESENT", Name = "OJM Present only" },
        new() { Id = "MISSING", Name = "OJM Missing only" }
    ];

    public StatusFilterOption[] StatusFilters => StatusFilterList;

    private StatusFilterOption _selectedStatusOption = StatusFilterList[0];
    public StatusFilterOption SelectedStatusOption
    {
        get => _selectedStatusOption;
        set
        {
            if (_selectedStatusOption != value && value != null)
            {
                _selectedStatusOption = value;
                _statusFilter = value.Id;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusFilter));
                ApplyFilter();
            }
        }
    }

    public string StatusFilter
    {
        get => _statusFilter;
        set
        {
            if (_statusFilter != value)
            {
                _statusFilter = value;
                _selectedStatusOption = StatusFilterList.FirstOrDefault(o => o.Id == value) ?? StatusFilterList[0];
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedStatusOption));
                ApplyFilter();
            }
        }
    }

    public Song? SelectedSong
    {
        get => _selectedSong;
        set
        {
            if (_selectedSong != value)
            {
                _selectedSong = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSelectedSong));
                OnSelectedSongChanged();
            }
        }
    }

    public bool HasSelectedSong => SelectedSong != null;

    public string StatusMessage
    {
        get => _statusMessage;
        set { if (_statusMessage != value) { _statusMessage = value; OnPropertyChanged(); } }
    }

    public BitmapImage? CoverImage
    {
        get => _coverImage;
        set { if (_coverImage != value) { _coverImage = value; OnPropertyChanged(); } }
    }

    public BitmapImage? ThumbnailImage
    {
        get => _thumbnailImage;
        set { if (_thumbnailImage != value) { _thumbnailImage = value; OnPropertyChanged(); } }
    }

    public string CoverInfoText
    {
        get => _coverInfoText;
        set { if (_coverInfoText != value) { _coverInfoText = value; OnPropertyChanged(); } }
    }

    public string ThumbInfoText
    {
        get => _thumbInfoText;
        set { if (_thumbInfoText != value) { _thumbInfoText = value; OnPropertyChanged(); } }
    }

    // Editable properties
    public int EditSongId
    {
        get => _editSongId;
        set { if (_editSongId != value) { _editSongId = value; OnPropertyChanged(); } }
    }

    public string EditTitle
    {
        get => _editTitle;
        set { if (_editTitle != value) { _editTitle = value; OnPropertyChanged(); } }
    }

    public string EditArtist
    {
        get => _editArtist;
        set { if (_editArtist != value) { _editArtist = value; OnPropertyChanged(); } }
    }

    public string EditNoter
    {
        get => _editNoter;
        set { if (_editNoter != value) { _editNoter = value; OnPropertyChanged(); } }
    }

    public float EditBpm
    {
        get => _editBpm;
        set { if (Math.Abs(_editBpm - value) > 0.001f) { _editBpm = value; OnPropertyChanged(); } }
    }

    public int EditGenreIndex
    {
        get => _editGenreIndex;
        set { if (_editGenreIndex != value) { _editGenreIndex = value; OnPropertyChanged(); } }
    }

    public short EditLevelEx
    {
        get => _editLevelEx;
        set { if (_editLevelEx != value) { _editLevelEx = value; OnPropertyChanged(); } }
    }

    public short EditLevelNx
    {
        get => _editLevelNx;
        set { if (_editLevelNx != value) { _editLevelNx = value; OnPropertyChanged(); } }
    }

    public short EditLevelHx
    {
        get => _editLevelHx;
        set { if (_editLevelHx != value) { _editLevelHx = value; OnPropertyChanged(); } }
    }

    // Status bar counts (Hardcoded English for 60+ FPS scroll performance)
    public string TotalCountText => Songs.Count == 1 ? "1 song detected" : $"{Songs.Count} songs detected";

    public string OjmStatsText
    {
        get
        {
            int present = Songs.Count(s => s.OjmExists);
            int missing = Songs.Count - present;
            string presentStr = present == 1 ? "1 present" : $"{present} present";
            string missingStr = missing == 1 ? "1 missing" : $"{missing} missing";
            return $"OJM: {presentStr}, {missingStr}";
        }
    }

    // Audio Properties
    public bool IsAudioPlaying
    {
        get => _isAudioPlaying;
        set
        {
            if (_isAudioPlaying != value)
            {
                _isAudioPlaying = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PlayPauseButtonText));
                OnPropertyChanged(nameof(PlayButtonText));
            }
        }
    }

    public bool IsAudioPaused
    {
        get => _isAudioPaused;
        set
        {
            if (_isAudioPaused != value)
            {
                _isAudioPaused = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PlayPauseButtonText));
                OnPropertyChanged(nameof(PlayButtonText));
            }
        }
    }

    public string PlayPauseButtonText
    {
        get
        {
            if (IsAudioPlaying) return LocalizationService.Instance["BtnPause"];
            if (IsAudioPaused) return LocalizationService.Instance["BtnPlay"];
            return LocalizationService.Instance["BtnPlay"];
        }
    }

    public string PlayButtonText => IsAudioPlaying ? LocalizationService.Instance["BtnPause"] : LocalizationService.Instance["CtxPlay"];

    // ================= ENGLISH UI STRINGS (HARDCODED FOR MAXIMUM PERFORMANCE) =================
    public string BrandTitle => "O2Jam Music Manager";
    public string BrandSubtitle => "SONG & CHART MANAGER";
    public string MusicFolderLabel => "Selected Folder:";
    public string BrowseFolderText => "📁 Browse";
    public string RefreshFolderText => "🔄 Refresh";
    public string LanguageLabel => "Language:";

    public string MoveUpText => "▲ Move Up";
    public string MoveUpTip => "Move up one row (Alt+Up)";
    public string MoveDownText => "▼ Move Down";
    public string MoveDownTip => "Move down one row (Alt+Down)";
    public string ReindexText => "⚡ Auto-Index (0-9999)";
    public string ReindexTip => "Auto-index and reassign IDs and synchronize physical files";
    public string ReindexCustomText => "⚡ Auto-Index (1000-9999)";
    public string ReindexCustomTip => "Auto-index custom charts (ID >= 1000) and strictly preserve original charts (ID 0-999)";
    public string AddSongText => "➕ Add Song";
    public string AddSongTip => "Import an .ojn / .ojm song";
    public string DeleteSongText => "🗑 Delete";
    public string DeleteSongTip => "Delete selected song";
    public string SearchPlaceholder => "Search by title, artist, ID...";

    public string StatusFilterAllText => "All statuses";
    public string StatusFilterPresentText => "OJM Present only";
    public string StatusFilterMissingText => "OJM Missing only";

    // DataGrid Columns
    public string ColId => "ID";
    public string ColOjnFile => ".OJN FILE";
    public string ColOjmFile => ".OJM FILE";
    public string ColTitle => "TITLE";
    public string ColArtist => "ARTIST";
    public string ColNoter => "NOTER";
    public string ColBpm => "BPM";
    public string ColLevels => "LEVELS (EX/NX/HX)";
    public string ColDuration => "DURATION";
    public string ColOjmStatus => ".OJM STATUS";

    // Context Menu
    public string CtxPlay => "▶ Play Audio";
    public string CtxChangeCover => "🖼 Change Cover";
    public string CtxChangeId => "🔢 Modify ID...";
    public string CtxMoveUp => "▲ Move Up (Alt+Up)";
    public string CtxMoveDown => "▼ Move Down (Alt+Down)";
    public string CtxOpenFolder => "📁 Open File Location";
    public string CtxDelete => "🗑 Delete Song (Del)";
    public string GenerateOjnListText => "📋 Generate OJNList.dat";
    public string GenerateOjnListTip => "Scan and create OJNList.dat for O2Jam";
    public string CtxChangeThumbnail => "Change Thumbnail...";
    public string BtnCoverText => "Change Cover";
    public string BtnCoverTip => "Select external cover image (JPG, PNG, BMP)";
    public string BtnThumbText => "Change Thumbnail";
    public string BtnThumbTip => "Select external image to generate thumbnail";
    public string BtnExportText => "Export";
    public string BtnExportTip => "Export cover images";
    public string CoverClickTip => "Click to change cover image";
    public string ThumbClickTip => "Click to change thumbnail";

    // Cover Card
    public string CardCoverTitle => "COVER & THUMBNAIL";
    public string NoCoverJpg => "No JPG cover";
    public string BtnChangeCover => "🖼 Change";
    public string BtnExportCover => "💾 Export";

    // Audio Card
    public string CardAudioTitle => "AUDIO PLAYER";
    public string BtnStop => "⏹ Stop";
    public string LblVolume => "Volume:";

    // Metadata Card
    public string CardMetadataTitle => ".OJN HEADER METADATA";
    public string LblSongId => "Song ID:";
    public string BtnRenameId => "Rename ID";
    public string LblTitle => "Title:";
    public string LblArtist => "Artist:";
    public string LblNoter => "Charter / Noter:";
    public string LblBpm => "BPM:";
    public string LblGenre => "Genre:";
    public string LblLevelsGroup => "Difficulty Levels (Easy / Normal / Hard):";
    public string LblEx => "Ex (Easy)";
    public string LblNx => "Nx (Normal)";
    public string LblHx => "Hx (Hard)";
    public string BtnSaveMetadata => "💾 Save Changes";

    // Tech Card
    public string CardTechTitle => "TECHNICAL DETAILS";
    public string LblTechOjn => ".OJN File:";
    public string LblTechOjm => "Audio .OJM:";
    public string LblTechDurations => "Durations (Ex/Nx/Hx):";
    public string LblTechNotes => "Notes (Ex/Nx/Hx):";
    public string LblTechSizeOjn => ".OJN File Size:";
    public string LblTechSizeOjm => ".OJM File Size:";

    // Status bar
    public string StatusSelection => "Selected: ";
    public string StatusNone => "None";

    public double AudioPosition
    {
        get => _audioPosition;
        set
        {
            if (Math.Abs(_audioPosition - value) > 0.05)
            {
                _audioPosition = value;
                OnPropertyChanged();
            }
        }
    }

    public double AudioDuration
    {
        get => _audioDuration;
        set
        {
            if (Math.Abs(_audioDuration - value) > 0.05)
            {
                _audioDuration = value;
                OnPropertyChanged();
            }
        }
    }

    public string AudioTimeDisplay
    {
        get => _audioTimeDisplay;
        set { if (_audioTimeDisplay != value) { _audioTimeDisplay = value; OnPropertyChanged(); } }
    }

    public float AudioVolume
    {
        get => _audioVolume;
        set
        {
            float v = Math.Clamp(value, 0.0f, 1.0f);
            if (Math.Abs(_audioVolume - v) > 0.01f)
            {
                _audioVolume = v;
                _audioPlayer.Volume = v;
                OnPropertyChanged();
                OnPropertyChanged(nameof(AudioVolumePercentDisplay));
            }
        }
    }

    public string AudioVolumePercentDisplay => $"{(int)(AudioVolume * 100)}%";

    public string AudioStatusText
    {
        get => _audioStatusText;
        set { if (_audioStatusText != value) { _audioStatusText = value; OnPropertyChanged(); } }
    }

    public string DiagnosticInfoText
    {
        get => _diagnosticInfoText;
        set { if (_diagnosticInfoText != value) { _diagnosticInfoText = value; OnPropertyChanged(); } }
    }

    public bool HasAudio => SelectedSong?.OjmExists == true;

    // Commands
    public RelayCommand BrowseFolderCommand { get; }
    public RelayCommand RefreshFolderCommand { get; }
    public RelayCommand GenerateOjnListCommand { get; }
    public event Action? OrderChanged;
    public RelayCommand MoveUpCommand { get; }
    public RelayCommand MoveDownCommand { get; }
    public RelayCommand SaveMetadataCommand { get; }
    public RelayCommand ChangeCoverCommand { get; }
    public RelayCommand ChangeThumbnailCommand { get; }
    public RelayCommand ExportCoverCommand { get; }
    public RelayCommand ChangeIdCommand { get; }
    public RelayCommand ReindexCustomCommand { get; }
    public RelayCommand AddSongCommand { get; }
    public RelayCommand DeleteSongCommand { get; }
    public RelayCommand OpenInExplorerCommand { get; }
    public RelayCommand TogglePlayCommand { get; }
    public RelayCommand StopAudioCommand { get; }

    public MainViewModel()
    {
        ApplyFilter();

        BrowseFolderCommand = new RelayCommand(BrowseFolder, () => IsNotBusy);
        RefreshFolderCommand = new RelayCommand(RefreshFolder, () => IsNotBusy && !string.IsNullOrEmpty(MusicDir));
        GenerateOjnListCommand = new RelayCommand(GenerateOjnList, () => IsNotBusy && !string.IsNullOrEmpty(MusicDir) && Songs.Count > 0);
        ReindexCustomCommand = new RelayCommand(ReindexCustom, () => IsNotBusy && !string.IsNullOrEmpty(MusicDir) && Songs.Count > 0);
        MoveUpCommand = new RelayCommand(MoveUp, () => IsNotBusy && HasSelectedSong);
        MoveDownCommand = new RelayCommand(MoveDown, () => IsNotBusy && HasSelectedSong);
        SaveMetadataCommand = new RelayCommand(SaveMetadata, () => IsNotBusy && HasSelectedSong);
        ChangeCoverCommand = new RelayCommand(ChangeCover, () => IsNotBusy && HasSelectedSong);
        ChangeThumbnailCommand = new RelayCommand(ChangeThumbnail, () => IsNotBusy && HasSelectedSong);
        ExportCoverCommand = new RelayCommand(ExportCover, () => IsNotBusy && HasSelectedSong && SelectedSong?.HasCover == true);
        ChangeIdCommand = new RelayCommand(ChangeId, () => IsNotBusy && HasSelectedSong);
        AddSongCommand = new RelayCommand(AddSong, () => IsNotBusy && !string.IsNullOrEmpty(MusicDir));
        DeleteSongCommand = new RelayCommand(DeleteSong, () => IsNotBusy && HasSelectedSong);
        OpenInExplorerCommand = new RelayCommand(OpenInExplorer, () => IsNotBusy && HasSelectedSong);
        TogglePlayCommand = new RelayCommand(TogglePlayPause, () => HasSelectedSong && HasAudio);
        StopAudioCommand = new RelayCommand(StopAudio, () => IsAudioPlaying || IsAudioPaused);

        _audioPlayer.Volume = _audioVolume;
        _audioPlayer.PlaybackStopped += () =>
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                IsAudioPlaying = false;
                IsAudioPaused = false;
                _audioTimer.Stop();
                AudioPosition = 0;
                UpdateAudioTimeDisplay();
                if (AudioDuration > 0)
                {
                    AudioStatusText = LocalizationService.Instance["AudioStopped"];
                }
            });
        };

        _audioTimer.Interval = TimeSpan.FromMilliseconds(100);
        _audioTimer.Tick += (s, e) =>
        {
            if (_audioPlayer.IsPlaying && !IsUserSeeking)
            {
                AudioPosition = _audioPlayer.Position;
                UpdateAudioTimeDisplay();
            }
        };
    }

    public void ApplyFilter()
    {
        string q = string.IsNullOrWhiteSpace(_searchText) ? string.Empty : _searchText.Trim().ToLowerInvariant();
        string filter = _statusFilter;

        List<Song> result;

        if (string.IsNullOrEmpty(q) && filter == "ALL")
        {
            result = [.. Songs];
        }
        else
        {
            result = Songs.Where(s =>
            {
                // In-memory cached boolean check - ZERO disk access during filter or scroll!
                if (filter == "PRESENT" && !s.OjmExists) return false;
                if (filter == "MISSING" && s.OjmExists) return false;

                if (string.IsNullOrEmpty(q)) return true;

                return s.SongId.ToString().Contains(q)
                    || s.Title.ToLowerInvariant().Contains(q)
                    || s.Artist.ToLowerInvariant().Contains(q)
                    || s.Noter.ToLowerInvariant().Contains(q)
                    || s.FileName.ToLowerInvariant().Contains(q)
                    || s.OjmFile.ToLowerInvariant().Contains(q)
                    || s.GenreName.ToLowerInvariant().Contains(q);
            }).ToList();
        }

        FilteredSongs.ReplaceAll(result);

        if (SelectedSong == null || !FilteredSongs.Contains(SelectedSong))
        {
            SelectedSong = FilteredSongs.FirstOrDefault();
        }
    }

    public void LoadDirectory(string folderPath)
    {
        if (!Directory.Exists(folderPath))
            return;

        try
        {
            _fileManager.SetMusicDir(folderPath);
            MusicDir = folderPath;

            var (scanned, errors) = _fileManager.ScanMusicDir();
            Songs.ReplaceAll(scanned);
            ApplyFilter();

            OnPropertyChanged(nameof(TotalCountText));
            OnPropertyChanged(nameof(OjmStatsText));

            if (errors.Count > 0)
            {
                string msg = string.Join("\n", errors.Take(8).Select(e => $"- {Path.GetFileName(e.Path)}: {e.Error}"));
                ModernMessageDialog.ShowWarning(Application.Current?.MainWindow, LocalizationService.Instance["MsgWarning"], LocalizationService.Instance.Get("MsgFilesFailedLoad", errors.Count, msg));
            }

            int count = Songs.Count;
            var loc = LocalizationService.Instance;
            string word = count == 1 ? loc["WordSongDetectedSingular"] : loc["WordSongDetectedPlural"];
            StatusMessage = loc.Get("StatusScanned", count, word);
            if (Songs.Count > 0)
                SelectedSong = Songs[0];
        }
        catch (Exception ex)
        {
            ModernMessageDialog.ShowError(Application.Current?.MainWindow, LocalizationService.Instance["MsgError"], LocalizationService.Instance.Get("MsgScanError", ex.Message));
        }
    }

    private void BrowseFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Title = LocalizationService.Instance["DlgBrowseMusicFolder"],
            InitialDirectory = Directory.Exists(MusicDir) ? MusicDir : string.Empty
        };

        if (dialog.ShowDialog() == true)
        {
            LoadDirectory(dialog.FolderName);
        }
    }

    private void RefreshFolder()
    {
        if (!string.IsNullOrEmpty(MusicDir))
            LoadDirectory(MusicDir);
    }

    private bool _isGeneratingOjnList = false;

    private void GenerateOjnList()
    {
        if (_isGeneratingOjnList) return;
        _isGeneratingOjnList = true;
        var loc = LocalizationService.Instance;

        try
        {
            if (string.IsNullOrEmpty(MusicDir) || !Directory.Exists(MusicDir))
            {
                ModernMessageDialog.Show(
                    Application.Current?.MainWindow,
                    loc["MsgInformation"],
                    loc["OjnListSelectFolderFirst"],
                    string.Empty);
                return;
            }

            if (Songs.Count == 0)
            {
                ModernMessageDialog.Show(
                    Application.Current?.MainWindow,
                    loc["MsgInformation"],
                    loc["OjnListNoSongs"],
                    string.Empty);
                return;
            }

            var sfd = new Microsoft.Win32.SaveFileDialog
            {
                Title = loc["OjnListChooseLocation"],
                Filter = loc["OjnListFilter"],
                FileName = "OJNList.dat",
                InitialDirectory = Directory.Exists(MusicDir) ? MusicDir : Environment.GetFolderPath(Environment.SpecialFolder.MyComputer)
            };

            if (sfd.ShowDialog(Application.Current?.MainWindow) != true)
                return; // Annulé par l'utilisateur

            string chosenPath = sfd.FileName;
            var result = OjnListGenerator.Generate(MusicDir, chosenPath, Songs);

            string word = loc.CurrentLanguage == "EN" 
                ? (result.SongCount == 1 ? loc["WordSongSingular"] : loc["WordSongPlural"]) 
                : (loc.CurrentLanguage == "JA" ? loc["WordSongPlural"] : (result.SongCount == 1 ? loc["WordSongSingular"] : loc["WordSongPlural"]));
            string indexedWord = loc.CurrentLanguage == "EN" 
                ? (result.SongCount == 1 ? loc["WordIndexedSingular"] : loc["WordIndexedPlural"]) 
                : (loc.CurrentLanguage == "JA" ? loc["WordIndexedPlural"] : (result.SongCount == 1 ? loc["WordIndexedSingular"] : loc["WordIndexedPlural"]));

            ModernMessageDialog.Show(
                Application.Current?.MainWindow,
                loc["OjnListTitle"],
                loc.Get("OjnListSuccess", result.SongCount, word),
                chosenPath);

            StatusMessage = loc.Get("OjnListStatus", result.SongCount, indexedWord);
        }
        catch (Exception ex)
        {
            ModernMessageDialog.Show(
                Application.Current?.MainWindow,
                loc["MsgError"],
                loc.Get("OjnListError", ex.Message),
                string.Empty,
                isError: true);
        }
        finally
        {
            _isGeneratingOjnList = false;
        }
    }


    private void OnSelectedSongChanged()
    {
        // Stop any audio playing when changing song selection
        StopAudio();
        OnPropertyChanged(nameof(HasAudio));

        if (SelectedSong == null)
        {
            EditSongId = 1;
            EditTitle = string.Empty;
            EditArtist = string.Empty;
            EditNoter = string.Empty;
            EditBpm = 120.0f;
            EditGenreIndex = 0;
            EditLevelEx = 0;
            EditLevelNx = 0;
            EditLevelHx = 0;

            CoverImage = null;
            ThumbnailImage = null;
            CoverInfoText = "Cover: None";
            ThumbInfoText = "Thumbnail: None";
            AudioStatusText = "Select a song";
            return;
        }

        AudioStatusText = SelectedSong.OjmExists ? "Ready to listen" : "Missing or unreadable .ojm audio file.";

        EditSongId = SelectedSong.SongId;
        EditTitle = SelectedSong.Title;
        EditArtist = SelectedSong.Artist;
        EditNoter = SelectedSong.Noter;
        EditBpm = SelectedSong.Bpm;
        EditGenreIndex = Math.Clamp(SelectedSong.GenreId, 0, Genres.Length - 1);
        EditLevelEx = SelectedSong.LevelEx;
        EditLevelNx = SelectedSong.LevelNx;
        EditLevelHx = SelectedSong.LevelHx;

        // Load Cover & Thumbnail ONLY when a song is clicked/selected in the details panel
        byte[]? jpgBytes = OjnParser.ReadCoverBytes(SelectedSong.FilePath);
        byte[]? bmpBytes = OjnParser.ReadThumbnailBytes(SelectedSong.FilePath);

        CoverImage = ImageHelper.LoadBitmapFromBytes(jpgBytes);
        var rawThumb = ImageHelper.LoadBitmapFromBytes(bmpBytes);
        if ((rawThumb == null || rawThumb.PixelWidth <= 16 || rawThumb.PixelHeight <= 16) && CoverImage != null)
        {
            ThumbnailImage = ImageHelper.CreateHighQualityThumbnail(CoverImage, 128, 128);
            ThumbInfoText = $"Thumbnail: Crisp (optimized {CoverImage.PixelWidth}x{CoverImage.PixelHeight})";
        }
        else
        {
            ThumbnailImage = rawThumb;
            if (rawThumb != null && bmpBytes != null)
            {
                double kb = bmpBytes.Length / 1024.0;
                ThumbInfoText = $"Thumbnail: {rawThumb.PixelWidth}x{rawThumb.PixelHeight} ({kb:F1} KB)";
            }
            else
            {
                ThumbInfoText = "Thumbnail: None";
            }
        }

        if (CoverImage != null && jpgBytes != null)
        {
            double kb = jpgBytes.Length / 1024.0;
            CoverInfoText = $"Cover: {CoverImage.PixelWidth}x{CoverImage.PixelHeight} ({kb:F1} KB)";
        }
        else
        {
            CoverInfoText = "Cover: None";
        }
    }

    public void ReplaceSongInLists(Song oldSong, Song newSongInstance)
    {
        int masterIdx = Songs.IndexOf(oldSong);
        if (masterIdx >= 0)
        {
            Songs[masterIdx] = newSongInstance;
        }

        int filteredIdx = FilteredSongs.IndexOf(oldSong);
        if (filteredIdx >= 0)
        {
            FilteredSongs[filteredIdx] = newSongInstance;
        }

        SelectedSong = newSongInstance;
    }

    private void SaveMetadata()
    {
        if (SelectedSong == null) return;

        try
        {
            OjnParser.UpdateMetadata(
                SelectedSong.FilePath,
                EditTitle.Trim(),
                EditArtist.Trim(),
                EditNoter.Trim(),
                EditGenreIndex,
                EditBpm,
                EditLevelEx,
                EditLevelNx,
                EditLevelHx
            );

            var oldSong = SelectedSong;
            var newSong = oldSong.Clone();
            newSong.Title = EditTitle.Trim();
            newSong.Artist = EditArtist.Trim();
            newSong.Noter = EditNoter.Trim();
            newSong.GenreId = EditGenreIndex;
            newSong.Bpm = EditBpm;
            newSong.LevelEx = EditLevelEx;
            newSong.LevelNx = EditLevelNx;
            newSong.LevelHx = EditLevelHx;

            ReplaceSongInLists(oldSong, newSong);

            StatusMessage = LocalizationService.Instance.Get("StatusSavedSuccess", newSong.SongId, newSong.Title);
        }
        catch (Exception ex)
        {
            var loc = LocalizationService.Instance;
            ModernMessageDialog.ShowError(Application.Current?.MainWindow, loc["MsgError"], loc.Get("MsgSaveMetadataFailed", ex.Message));
        }
    }

    private void ChangeCover()
    {
        if (SelectedSong == null) return;

        var loc = LocalizationService.Instance;
        var ofd = new OpenFileDialog
        {
            Title = loc["DlgCoverTitle"],
            Filter = loc["DlgCoverFilter"]
        };

        if (ofd.ShowDialog() == true)
        {
            try
            {
                var (jpgLen, bmpLen) = OjnParser.WriteCoverAndThumbnail(SelectedSong.FilePath, ofd.FileName);

                var oldSong = SelectedSong;
                var newSong = oldSong.Clone();
                newSong.CoverSize = jpgLen;
                newSong.BmpSize = bmpLen;
                newSong.FileSize = new FileInfo(newSong.FilePath).Length;

                ReplaceSongInLists(oldSong, newSong);
                StatusMessage = loc.Get("StatusCoverUpdated", SelectedSong.SongId, jpgLen, bmpLen);
                ModernMessageDialog.ShowInfo(Application.Current?.MainWindow, LocalizationService.Instance["MsgCoverUpdatedTitle"], LocalizationService.Instance.Get("MsgCoverUpdated", SelectedSong.Title));
            }
            catch (Exception ex)
            {
                ModernMessageDialog.ShowError(Application.Current?.MainWindow, LocalizationService.Instance["MsgError"], LocalizationService.Instance.Get("MsgCoverFailed", ex.Message));
            }
        }
    }

    private void ChangeThumbnail()
    {
        if (SelectedSong == null) return;

        var loc = LocalizationService.Instance;
        var ofd = new OpenFileDialog
        {
            Title = loc["DlgThumbTitle"],
            Filter = loc["DlgCoverFilter"]
        };

        if (ofd.ShowDialog() == true)
        {
            try
            {
                int bmpLen = OjnParser.WriteThumbnailOnly(SelectedSong.FilePath, ofd.FileName);

                var oldSong = SelectedSong;
                var newSong = oldSong.Clone();
                newSong.BmpSize = bmpLen;
                newSong.FileSize = new FileInfo(newSong.FilePath).Length;

                ReplaceSongInLists(oldSong, newSong);
                StatusMessage = loc.Get("StatusThumbUpdated", SelectedSong.SongId, bmpLen);
                ModernMessageDialog.ShowInfo(Application.Current?.MainWindow, LocalizationService.Instance["MsgThumbUpdatedTitle"], LocalizationService.Instance.Get("MsgThumbUpdated", SelectedSong.Title));
            }
            catch (Exception ex)
            {
                ModernMessageDialog.ShowError(Application.Current?.MainWindow, LocalizationService.Instance["MsgError"], LocalizationService.Instance.Get("MsgThumbFailed", ex.Message));
            }
        }
    }

    private void ExportCover()
    {
        if (SelectedSong == null) return;

        byte[]? jpgBytes = OjnParser.ReadCoverBytes(SelectedSong.FilePath);
        if (jpgBytes == null) return;

        var loc = LocalizationService.Instance;
        var sfd = new SaveFileDialog
        {
            Title = loc["DlgExportTitle"],
            FileName = $"o2ma{SelectedSong.SongId}_cover.jpg",
            Filter = loc["DlgExportFilter"]
        };

        if (sfd.ShowDialog() == true)
        {
            try
            {
                if (sfd.FileName.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase))
                {
                    byte[]? bmpBytes = OjnParser.ReadThumbnailBytes(SelectedSong.FilePath);
                    if (bmpBytes != null)
                        File.WriteAllBytes(sfd.FileName, bmpBytes);
                }
                else
                {
                    File.WriteAllBytes(sfd.FileName, jpgBytes);
                }
                ModernMessageDialog.ShowInfo(Application.Current?.MainWindow, LocalizationService.Instance["MsgSuccess"], LocalizationService.Instance["MsgExportSuccess"]);
            }
            catch (Exception ex)
            {
                ModernMessageDialog.ShowError(Application.Current?.MainWindow, LocalizationService.Instance["MsgError"], LocalizationService.Instance.Get("MsgExportFailed", ex.Message));
            }
        }
    }

    private void ChangeId()
    {
        if (SelectedSong == null) return;
        if (EditSongId == SelectedSong.SongId)
        {
            ModernMessageDialog.ShowWarning(Application.Current?.MainWindow, LocalizationService.Instance["MsgInformation"], LocalizationService.Instance.Get("MsgIdAlreadyUsed", EditSongId));
            return;
        }

        try
        {
            var oldSong = SelectedSong;
            var updated = _fileManager.ChangeSongId(oldSong, EditSongId);

            var newSong = oldSong.Clone();
            newSong.SongId = updated.SongId;
            newSong.FilePath = updated.FilePath;
            newSong.FileName = updated.FileName;
            newSong.OjmFile = updated.OjmFile;
            newSong.OjmPath = updated.OjmPath;
            newSong.OjmExists = updated.OjmExists;

            ReplaceSongInLists(oldSong, newSong);

            EditSongId = newSong.SongId;
            StatusMessage = LocalizationService.Instance.Get("StatusRenamedSuccess", updated.SongId);
        }
        catch (Exception ex)
        {
            ModernMessageDialog.ShowError(Application.Current?.MainWindow, LocalizationService.Instance["MsgError"], LocalizationService.Instance.Get("MsgIdRenameFailed", ex.Message));
        }
    }

    public void MoveUp()
    {
        if (SelectedSong == null) return;

        int idx = FilteredSongs.IndexOf(SelectedSong);
        if (idx > 0)
        {
            var current = SelectedSong;
            FilteredSongs.Move(idx, idx - 1);

            int songsIdx = Songs.IndexOf(current);
            if (songsIdx > 0)
            {
                Songs.Move(songsIdx, songsIdx - 1);
            }

            SelectedSong = current;
            OrderChanged?.Invoke();
            MoveUpCommand.RaiseCanExecuteChanged();
            MoveDownCommand.RaiseCanExecuteChanged();
        }
    }

    public void MoveDown()
    {
        if (SelectedSong == null) return;

        int idx = FilteredSongs.IndexOf(SelectedSong);
        if (idx >= 0 && idx < FilteredSongs.Count - 1)
        {
            var current = SelectedSong;
            FilteredSongs.Move(idx, idx + 1);

            int songsIdx = Songs.IndexOf(current);
            if (songsIdx >= 0 && songsIdx < Songs.Count - 1)
            {
                Songs.Move(songsIdx, songsIdx + 1);
            }

            SelectedSong = current;
            OrderChanged?.Invoke();
            MoveUpCommand.RaiseCanExecuteChanged();
            MoveDownCommand.RaiseCanExecuteChanged();
        }
    }

    public async void ReindexCustom()
    {
        if (string.IsNullOrEmpty(MusicDir) || Songs.Count == 0 || IsBusy) return;

        var loc = LocalizationService.Instance;
        var customSongs = Songs.Where(s => s.SongId < 0 || s.SongId >= 1000).ToList();
        if (customSongs.Count == 0)
        {
            ModernMessageDialog.ShowInfo(
                Application.Current?.MainWindow,
                loc["MsgReindexCustomConfirmTitle"],
                loc["MsgReindexCustomNoSongs"]);
            return;
        }

        int targetEndId = 1000 + customSongs.Count - 1;
        bool confirmed = ModernMessageDialog.ShowConfirm(
            Application.Current?.MainWindow,
            loc["MsgReindexCustomConfirmTitle"],
            loc.Get("MsgReindexCustomConfirm", customSongs.Count, targetEndId),
            yesText: loc["MsgReindexCustomBtn"],
            isDanger: false);

        if (!confirmed) return;

        IsBusy = true;
        IsProgressIndeterminate = true;
        StatusMessage = loc.Get("StatusReindexingProgress", 0, customSongs.Count);

        try
        {
            var songsOrder = Songs.ToList();
            int reindexedCount = 0;
            int startId = 1000;
            int endId = 1000;
            string? ojnListSyncPath = null;
            List<Song> refreshedSongs = [];

            await Task.Run(() =>
            {
                var result = _fileManager.ReindexCustomSongs(songsOrder, (current, total) =>
                {
                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        IsProgressIndeterminate = false;
                        ProgressMax = total;
                        ProgressValue = current;
                        StatusMessage = loc.Get("StatusReindexingProgress", current, total);
                    });
                });

                reindexedCount = result.ReindexedCount;
                startId = result.StartId;
                endId = result.EndId;
                ojnListSyncPath = result.OjnListUpdatedPath;

                Application.Current?.Dispatcher.Invoke(() =>
                {
                    IsProgressIndeterminate = true;
                    StatusMessage = loc["StatusScanningProgress"];
                });

                refreshedSongs = _fileManager.ScanMusicDir().Songs;
            });

            // Single atomic bulk update
            Songs.ReplaceAll(refreshedSongs);
            ApplyFilter();

            OnPropertyChanged(nameof(TotalCountText));
            OnPropertyChanged(nameof(OjmStatsText));

            if (Songs.Count > 0)
                SelectedSong = Songs[0];

            string syncMsg = !string.IsNullOrEmpty(ojnListSyncPath)
                ? loc.Get("MsgReindexCustomOjnListSync", Path.GetFileName(ojnListSyncPath))
                : string.Empty;

            StatusMessage = loc.Get("StatusCustomReindexSuccess", reindexedCount, startId, endId);
            ModernMessageDialog.ShowInfo(
                Application.Current?.MainWindow,
                loc["MsgReindexCustomSuccessTitle"],
                loc.Get("MsgReindexCustomSuccess", reindexedCount, endId) + syncMsg);
        }
        catch (Exception ex)
        {
            ModernMessageDialog.ShowError(
                Application.Current?.MainWindow,
                loc["MsgError"],
                loc.Get("MsgReindexFailed", ex.Message));
        }
        finally
        {
            IsBusy = false;
            IsProgressIndeterminate = false;
            ProgressValue = 0;
        }
    }

    public async void ApplyReindex(int startId, int step)
    {
        if (string.IsNullOrEmpty(MusicDir) || Songs.Count == 0 || IsBusy) return;

        var loc = LocalizationService.Instance;
        IsBusy = true;
        IsProgressIndeterminate = true;
        StatusMessage = loc.Get("StatusReindexingProgress", 0, Songs.Count);

        try
        {
            var songsOrder = Songs.ToList();
            List<Song> reindexed = [];

            await Task.Run(() =>
            {
                reindexed = _fileManager.ReindexSongs(songsOrder, startId, step, (current, total) =>
                {
                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        IsProgressIndeterminate = false;
                        ProgressMax = total;
                        ProgressValue = current;
                        StatusMessage = loc.Get("StatusReindexingProgress", current, total);
                    });
                });
            });

            // Single atomic bulk update
            Songs.ReplaceAll(reindexed);
            ApplyFilter();

            OnPropertyChanged(nameof(TotalCountText));
            OnPropertyChanged(nameof(OjmStatsText));

            if (Songs.Count > 0)
                SelectedSong = Songs[0];

            StatusMessage = LocalizationService.Instance.Get("StatusReindexSuccess", reindexed.Count);
            ModernMessageDialog.ShowInfo(Application.Current?.MainWindow, LocalizationService.Instance["MsgReindexSuccessTitle"], LocalizationService.Instance.Get("MsgReindexSuccess", reindexed.Count));
        }
        catch (Exception ex)
        {
            ModernMessageDialog.ShowError(Application.Current?.MainWindow, LocalizationService.Instance["MsgError"], LocalizationService.Instance.Get("MsgReindexFailed", ex.Message));
        }
        finally
        {
            IsBusy = false;
            IsProgressIndeterminate = false;
            ProgressValue = 0;
        }
    }

    private void AddSong()
    {
        if (string.IsNullOrEmpty(MusicDir)) return;

        var loc = LocalizationService.Instance;
        var ofd = new OpenFileDialog
        {
            Title = loc["DlgImportOjnTitle"],
            Filter = loc["DlgImportOjnFilter"]
        };

        if (ofd.ShowDialog() == true)
        {
            try
            {
                var newSong = _fileManager.AddSong(ofd.FileName);
                Songs.Add(newSong);
                ApplyFilter();

                OnPropertyChanged(nameof(TotalCountText));
                OnPropertyChanged(nameof(OjmStatsText));
                SelectedSong = newSong;

                StatusMessage = loc.Get("StatusSongImported", newSong.SongId);
                ModernMessageDialog.ShowInfo(Application.Current?.MainWindow, LocalizationService.Instance["MsgSongAddedTitle"], LocalizationService.Instance.Get("MsgSongAdded", newSong.Title, newSong.SongId));
            }
            catch (Exception ex)
            {
                ModernMessageDialog.ShowError(Application.Current?.MainWindow, LocalizationService.Instance["MsgError"], LocalizationService.Instance.Get("MsgSongAddFailed", ex.Message));
            }
        }
    }

    private void DeleteSong()
    {
        if (SelectedSong == null) return;

        var loc = LocalizationService.Instance;
        string ojnName = SelectedSong.FileName;
        string ojmName = SelectedSong.OjmExists ? SelectedSong.OjmFile : loc["StatusNotPresent"];
        string prompt = loc.Get("DeleteConfirmPrompt", SelectedSong.SongId, SelectedSong.Title, ojnName, ojmName);

        bool confirmed = ModernMessageDialog.ShowConfirm(
            Application.Current?.MainWindow,
            loc["DeleteConfirmTitle"],
            prompt,
            yesText: loc["DeleteSongClean"],
            noText: loc["DialogCancel"],
            isDanger: true
        );

        if (confirmed)
        {
            try
            {
                var toDelete = SelectedSong;
                _fileManager.DeleteSong(toDelete);
                Songs.Remove(toDelete);
                ApplyFilter();

                OnPropertyChanged(nameof(TotalCountText));
                OnPropertyChanged(nameof(OjmStatsText));

                StatusMessage = loc.Get("StatusSongDeleted", toDelete.SongId);
                SelectedSong = Songs.FirstOrDefault();
            }
            catch (Exception ex)
            {
                ModernMessageDialog.ShowError(Application.Current?.MainWindow, LocalizationService.Instance["MsgError"], LocalizationService.Instance.Get("MsgDeleteFailed", ex.Message));
            }
        }
    }

    private void OpenInExplorer()
    {
        if (SelectedSong == null || !File.Exists(SelectedSong.FilePath)) return;

        try
        {
            Process.Start("explorer.exe", $"/select,\"{SelectedSong.FilePath}\"");
        }
        catch (Exception ex)
        {
            var loc = LocalizationService.Instance;
            ModernMessageDialog.ShowWarning(Application.Current?.MainWindow, loc["MsgError"], loc.Get("MsgOpenExplorerFailed", ex.Message));
        }
    }

    public void TogglePlayPause()
    {
        var loc = LocalizationService.Instance;
        if (IsAudioPlaying)
        {
            _audioPlayer.Pause();
            IsAudioPlaying = false;
            IsAudioPaused = true;
            _audioTimer.Stop();
            AudioStatusText = loc["AudioPaused"];
            return;
        }

        if (IsAudioPaused)
        {
            _audioPlayer.Resume();
            IsAudioPlaying = true;
            IsAudioPaused = false;
            _audioTimer.Start();
            AudioStatusText = loc.Get("AudioPlayingSimple", SelectedSong?.Title ?? "");
            return;
        }

        if (SelectedSong == null || !SelectedSong.OjmExists || string.IsNullOrEmpty(SelectedSong.OjmPath))
        {
            AudioStatusText = loc["AudioMissingOjm"];
            return;
        }

        var targetSong = SelectedSong;

        AudioStatusText = loc["AudioLoading"];
        DiagnosticInfoText = loc["AudioExtracting"];

        Task.Run(() =>
        {
            try
            {
                var mixed = SongMixer.MixFullSong(targetSong, status =>
                {
                    Application.Current?.Dispatcher.Invoke(() => AudioStatusText = status);
                });

                Application.Current?.Dispatcher.Invoke(() =>
                {
                    if (SelectedSong != targetSong)
                        return; // Song selection changed

                    int durMin = (int)mixed.DurationSeconds / 60;
                    int durSec = (int)mixed.DurationSeconds % 60;
                    string durStr = $"{durMin:D2}:{durSec:D2}";

                    DiagnosticInfoText = loc.Get("AudioDiagFormat", mixed.ContainerFormat, mixed.ModeDescription, durStr);

                    _audioPlayer.PlayMixedSong(mixed.Provider);
                    IsAudioPlaying = _audioPlayer.IsPlaying;
                    IsAudioPaused = false;

                    if (IsAudioPlaying)
                    {
                        AudioDuration = _audioPlayer.Duration;
                        AudioPosition = 0;
                        UpdateAudioTimeDisplay();
                        _audioTimer.Start();
                        AudioStatusText = loc.Get("AudioPlayingSimple", targetSong.Title);
                    }
                    else
                    {
                        AudioStatusText = loc["AudioFailed"];
                    }
                });
            }
            catch (Exception ex)
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    AudioStatusText = loc.Get("AudioError", ex.Message);
                    DiagnosticInfoText = loc.Get("AudioDiagError", ex.Message);
                    StopAudio();
                });
            }
        });
    }

    public void StopAudio()
    {
        _audioTimer.Stop();
        _audioPlayer.Stop(notify: false);
        IsAudioPlaying = false;
        IsAudioPaused = false;
        AudioPosition = 0;
        UpdateAudioTimeDisplay();
        if (SelectedSong != null && SelectedSong.OjmExists)
            AudioStatusText = LocalizationService.Instance["AudioReady"];
    }

    public void SeekAudio(double seconds)
    {
        _audioPlayer.Seek(seconds);
        AudioPosition = seconds;
        UpdateAudioTimeDisplay();
    }

    private void UpdateAudioTimeDisplay()
    {
        int cur = (int)AudioPosition;
        int tot = (int)AudioDuration;
        AudioTimeDisplay = $"{cur / 60:D2}:{cur % 60:D2} / {tot / 60:D2}:{tot % 60:D2}";
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
