using System.Windows;
using System.Windows.Controls;
using O2JamManager.Models;

namespace O2JamManager;

public partial class ReindexWindow : Window
{
    private readonly List<Song> _songs;

    public int StartId { get; private set; } = 1;
    public int Step { get; private set; } = 1;

    public class PreviewItem
    {
        public int Order { get; set; }
        public string Title { get; set; } = string.Empty;
        public int OldId { get; set; }
        public int NewId { get; set; }
        public string OldFile { get; set; } = string.Empty;
        public string NewFile { get; set; } = string.Empty;
    }

    public ReindexWindow(IEnumerable<Song> songs)
    {
        InitializeComponent();
        _songs = songs.ToList();
        ApplyLocalization();
        UpdatePreview();
    }

    private void ApplyLocalization()
    {
        var loc = Core.LocalizationService.Instance;
        Title = loc["ReindexTitle"];

        if (TxtBanner != null)
        {
            TxtBanner.Inlines.Clear();
            var bold = new System.Windows.Documents.Bold(new System.Windows.Documents.Run(loc["ReindexBannerTitle"] + " "));
            bold.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#38bdf8"));
            TxtBanner.Inlines.Add(bold);
            TxtBanner.Inlines.Add(new System.Windows.Documents.LineBreak());
            TxtBanner.Inlines.Add(new System.Windows.Documents.Run(loc["ReindexBannerDesc"]));
        }
        if (LblStartId != null) LblStartId.Text = loc["ReindexStartId"];
        if (LblStep != null) LblStep.Text = loc["ReindexStep"];
        if (BtnCancel != null) BtnCancel.Content = loc["ReindexBtnCancel"];
        if (BtnApply != null) BtnApply.Content = loc["ReindexBtnApply"];

        if (PreviewGrid?.Columns.Count >= 6)
        {
            PreviewGrid.Columns[0].Header = loc["ReindexColOrder"];
            PreviewGrid.Columns[1].Header = loc["ReindexColTitle"];
            PreviewGrid.Columns[2].Header = loc["ReindexColOldId"];
            PreviewGrid.Columns[3].Header = loc["ReindexColNewId"];
            PreviewGrid.Columns[4].Header = loc["ReindexColOldFile"];
            PreviewGrid.Columns[5].Header = loc["ReindexColNewFile"];
        }
    }

    private void OnParamsChanged(object sender, TextChangedEventArgs e)
    {
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        if (PreviewGrid == null) return;

        if (!int.TryParse(TxtStartId?.Text, out int startId) || startId < 1)
            startId = 1;

        if (!int.TryParse(TxtStep?.Text, out int step) || step <= 0)
            step = 1;

        StartId = startId;
        Step = step;

        var items = new List<PreviewItem>();
        int currentId = startId;

        for (int i = 0; i < _songs.Count; i++)
        {
            var s = _songs[i];
            int newId = currentId;
            string newFile = $"o2ma{currentId}.ojn";

            items.Add(new PreviewItem
            {
                Order = i + 1,
                Title = string.IsNullOrEmpty(s.Title) ? Core.LocalizationService.Instance["Untitled"] : s.Title,
                OldId = s.SongId,
                NewId = newId,
                OldFile = s.FileName,
                NewFile = newFile
            });

            if (currentId < 9999)
            {
                currentId += step;
            }
        }

        PreviewGrid.ItemsSource = items;
    }

    private void OnApplyClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
