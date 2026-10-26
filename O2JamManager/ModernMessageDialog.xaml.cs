using System.Windows;
using System.Windows.Media;

namespace O2JamManager;

public partial class ModernMessageDialog : Window
{
    public ModernMessageDialog()
    {
        InitializeComponent();
        Loaded += (_, _) => MainWindow.EnableImmersiveDarkMode(this);
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void OnConfirmClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    /// <summary>
    /// Legacy/OJNList popup displaying a message with an optional path card.
    /// </summary>
    public static bool? Show(Window? owner, string title, string message, string locationPath = "", bool isError = false)
    {
        var dlg = new ModernMessageDialog
        {
            Owner = owner ?? Application.Current?.MainWindow
        };

        dlg.TitleBlock.Text = title;
        dlg.MessageBlock.Text = message;

        if (isError)
        {
            dlg.IconBlock.Text = "⚠️";
            dlg.TitleBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f43f5e"));
        }
        else
        {
            dlg.IconBlock.Text = "📄";
            dlg.TitleBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#38bdf8"));
        }

        dlg.BtnOk.Content = Core.LocalizationService.Instance["DialogOk"];
        dlg.LocationHeaderBlock.Text = Core.LocalizationService.Instance["LocationUpdated"];

        if (string.IsNullOrWhiteSpace(locationPath))
        {
            dlg.CardBorder.Visibility = Visibility.Collapsed;
        }
        else
        {
            dlg.CardBorder.Visibility = Visibility.Visible;
            dlg.PathBox.Text = locationPath;
        }

        dlg.BtnOk.Visibility = Visibility.Visible;
        dlg.ConfirmButtonsPanel.Visibility = Visibility.Collapsed;

        return dlg.ShowDialog();
    }

    /// <summary>
    /// Displays an information dialog with modern dark violet / neon styling.
    /// </summary>
    public static void ShowInfo(Window? owner, string title, string message)
    {
        var dlg = new ModernMessageDialog
        {
            Owner = owner ?? Application.Current?.MainWindow
        };

        dlg.IconBlock.Text = "ℹ️";
        dlg.TitleBlock.Text = title;
        dlg.TitleBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#38bdf8"));
        dlg.MessageBlock.Text = message;
        dlg.CardBorder.Visibility = Visibility.Collapsed;
        dlg.BtnOk.Content = Core.LocalizationService.Instance["DialogOk"];
        dlg.BtnOk.Visibility = Visibility.Visible;
        dlg.ConfirmButtonsPanel.Visibility = Visibility.Collapsed;

        dlg.ShowDialog();
    }

    /// <summary>
    /// Displays a warning dialog with modern dark violet / amber styling.
    /// </summary>
    public static void ShowWarning(Window? owner, string title, string message)
    {
        var dlg = new ModernMessageDialog
        {
            Owner = owner ?? Application.Current?.MainWindow
        };

        dlg.IconBlock.Text = "⚠️";
        dlg.TitleBlock.Text = title;
        dlg.TitleBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#fbbf24"));
        dlg.MessageBlock.Text = message;
        dlg.CardBorder.Visibility = Visibility.Collapsed;
        dlg.BtnOk.Content = Core.LocalizationService.Instance["DialogOk"];
        dlg.BtnOk.Visibility = Visibility.Visible;
        dlg.ConfirmButtonsPanel.Visibility = Visibility.Collapsed;

        dlg.ShowDialog();
    }

    /// <summary>
    /// Displays an error dialog with modern dark violet / rose styling.
    /// </summary>
    public static void ShowError(Window? owner, string title, string message)
    {
        var dlg = new ModernMessageDialog
        {
            Owner = owner ?? Application.Current?.MainWindow
        };

        dlg.IconBlock.Text = "❌";
        dlg.TitleBlock.Text = title;
        dlg.TitleBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f43f5e"));
        dlg.MessageBlock.Text = message;
        dlg.CardBorder.Visibility = Visibility.Collapsed;
        dlg.BtnOk.Content = Core.LocalizationService.Instance["DialogOk"];
        dlg.BtnOk.Visibility = Visibility.Visible;
        dlg.ConfirmButtonsPanel.Visibility = Visibility.Collapsed;

        dlg.ShowDialog();
    }

    /// <summary>
    /// Displays a confirmation dialog (Yes / No or Confirm / Cancel) with modern dark styling.
    /// Returns true if confirmed, false otherwise.
    /// </summary>
    public static bool ShowConfirm(Window? owner, string title, string message, string? yesText = null, string? noText = null, bool isDanger = false)
    {
        var dlg = new ModernMessageDialog
        {
            Owner = owner ?? Application.Current?.MainWindow
        };

        dlg.IconBlock.Text = isDanger ? "🗑️" : "❓";
        dlg.TitleBlock.Text = title;
        dlg.TitleBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isDanger ? "#f43f5e" : "#ec4899"));
        dlg.MessageBlock.Text = message;
        dlg.CardBorder.Visibility = Visibility.Collapsed;

        dlg.BtnOk.Visibility = Visibility.Collapsed;
        dlg.ConfirmButtonsPanel.Visibility = Visibility.Visible;

        dlg.BtnConfirmYes.Content = yesText ?? Core.LocalizationService.Instance["DialogConfirm"];
        dlg.BtnConfirmNo.Content = noText ?? Core.LocalizationService.Instance["DialogCancel"];

        if (isDanger && dlg.FindResource("DialogDangerBtn") is Style dangerStyle)
        {
            dlg.BtnConfirmYes.Style = dangerStyle;
        }

        return dlg.ShowDialog() == true;
    }
}
