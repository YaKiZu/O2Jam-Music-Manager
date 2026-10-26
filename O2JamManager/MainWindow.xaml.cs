using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using O2JamManager.ViewModels;

namespace O2JamManager;

public partial class MainWindow : Window
{
    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_CAPTION_COLOR = 35;
    private const int DWMWA_TEXT_COLOR = 36;

    public static void EnableImmersiveDarkMode(Window window)
    {
        try
        {
            var hwnd = new WindowInteropHelper(window).EnsureHandle();
            if (hwnd == IntPtr.Zero) return;

            int useDarkMode = 1;
            int hr = DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDarkMode, sizeof(int));
            if (hr != 0)
            {
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref useDarkMode, sizeof(int));
            }

            // Windows 11 title bar styling matching app dark theme (#121222)
            int captionColor = 0x00221212; // COLORREF format: 0x00BBGGRR
            DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ref captionColor, sizeof(int));
            int textColor = 0x00FFFFFF;
            DwmSetWindowAttribute(hwnd, DWMWA_TEXT_COLOR, ref textColor, sizeof(int));
        }
        catch { }
    }
    private readonly MainViewModel _vm;
    private static readonly string ConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "O2JamManager",
        "last_music_dir.txt"
    );

    static MainWindow()
    {
        // Intercept RequestBringIntoView so hovering over bottom/clipped items never triggers auto-scroll
        EventManager.RegisterClassHandler(
            typeof(ComboBoxItem),
            FrameworkElement.RequestBringIntoViewEvent,
            new RequestBringIntoViewEventHandler((s, e) => e.Handled = true)
        );

        EventManager.RegisterClassHandler(
            typeof(ComboBox),
            FrameworkElement.RequestBringIntoViewEvent,
            new RequestBringIntoViewEventHandler((s, e) => e.Handled = true)
        );
    }

    public MainWindow()
    {
        InitializeComponent();
        _vm = (MainViewModel)DataContext;
        // UpdateGridHeaders omitted

        Loaded += OnWindowLoaded;
        Closing += OnWindowClosing;
        KeyDown += OnWindowKeyDown;
        PreviewMouseDown += OnGlobalPreviewMouseDown;

        StatusFilterComboBox.DropDownOpened += OnDropDownOpenedClearCapture;
        GenreComboBox.DropDownOpened += OnDropDownOpenedClearCapture;

        // LanguageChanged listener removed for hardcoded English performance
        _vm.OrderChanged += () => Dispatcher.Invoke(() =>
        {
            ClearGridColumnSort();
            if (_vm.SelectedSong != null)
            {
                SongsGrid.ScrollIntoView(_vm.SelectedSong);
            }
        });
    }

    private void OnDropDownOpenedClearCapture(object? sender, EventArgs e)
    {
        if (Mouse.Captured is System.Windows.Controls.Primitives.ToggleButton)
        {
            Mouse.Capture(null);
        }
    }

    private void OnGlobalPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        CloseDropDownIfClickedOutside(StatusFilterComboBox);
        CloseDropDownIfClickedOutside(GenreComboBox);
    }

    private static void CloseDropDownIfClickedOutside(ComboBox? cb)
    {
        if (cb != null && cb.IsDropDownOpen && !cb.IsMouseOver)
        {
            cb.IsDropDownOpen = false;
        }
    }

    private void UpdateGridHeaders()
    {
        if (SongsGrid?.Columns.Count >= 10)
        {
            SongsGrid.Columns[0].Header = _vm.ColId;
            SongsGrid.Columns[1].Header = _vm.ColOjnFile;
            SongsGrid.Columns[2].Header = _vm.ColOjmFile;
            SongsGrid.Columns[3].Header = _vm.ColTitle;
            SongsGrid.Columns[4].Header = _vm.ColArtist;
            SongsGrid.Columns[5].Header = _vm.ColNoter;
            SongsGrid.Columns[6].Header = _vm.ColBpm;
            SongsGrid.Columns[7].Header = _vm.ColLevels;
            SongsGrid.Columns[8].Header = _vm.ColDuration;
            SongsGrid.Columns[9].Header = _vm.ColOjmStatus;
        }
    }

    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        EnableImmersiveDarkMode(this);
        // UpdateGridHeaders omitted
        string[] args = Environment.GetCommandLineArgs();
        if (args.Length > 1 && Directory.Exists(args[1]))
        {
            _vm.LoadDirectory(args[1]);
            return;
        }

        // Try load last saved folder
        try
        {
            if (File.Exists(ConfigPath))
            {
                string saved = File.ReadAllText(ConfigPath).Trim();
                if (Directory.Exists(saved))
                {
                    _vm.LoadDirectory(saved);
                }
            }
        }
        catch { }
    }

    private void OnWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _vm.StopAudio();

        try
        {
            if (!string.IsNullOrEmpty(_vm.MusicDir) && Directory.Exists(_vm.MusicDir))
            {
                string dir = Path.GetDirectoryName(ConfigPath)!;
                Directory.CreateDirectory(dir);
                File.WriteAllText(ConfigPath, _vm.MusicDir);
            }
        }
        catch { }
    }

    private void OnSliderMouseDown(object sender, MouseButtonEventArgs e)
    {
        _vm.IsUserSeeking = true;
    }

    private void OnSliderMouseUp(object sender, MouseButtonEventArgs e)
    {
        _vm.IsUserSeeking = false;
        if (sender is Slider slider)
        {
            _vm.SeekAudio(slider.Value);
        }
    }



    public void ClearGridColumnSort()
    {
        if (SongsGrid != null)
        {
            foreach (var col in SongsGrid.Columns)
            {
                col.SortDirection = null;
            }
        }
    }

    private void OnGridMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_vm.SelectedSong != null && _vm.TogglePlayCommand.CanExecute(null))
        {
            _vm.TogglePlayCommand.Execute(null);
        }
    }

    private void OnWindowKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete && _vm.DeleteSongCommand.CanExecute(null))
        {
            _vm.DeleteSongCommand.Execute(null);
            e.Handled = true;
        }
        else if (Keyboard.Modifiers == ModifierKeys.Alt && e.Key == Key.Up && _vm.MoveUpCommand.CanExecute(null))
        {
            _vm.MoveUpCommand.Execute(null);
            e.Handled = true;
        }
        else if (Keyboard.Modifiers == ModifierKeys.Alt && e.Key == Key.Down && _vm.MoveDownCommand.CanExecute(null))
        {
            _vm.MoveDownCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void OnOpenReindexWindowClick(object sender, RoutedEventArgs e)
    {
        if (_vm.Songs.Count == 0)
        {
            ModernMessageDialog.ShowInfo(this, "Information", "No songs found to reindex.");
            return;
        }

        var dlg = new ReindexWindow(_vm.Songs)
        {
            Owner = this
        };

        if (dlg.ShowDialog() == true)
        {
            _vm.ApplyReindex(dlg.StartId, dlg.Step);
        }
    }
}
