using System.IO;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace O2JamManager.Core;

public class LanguageItem
{
    public required string Code { get; init; }
    public required string DisplayName { get; init; }
    public override string ToString() => DisplayName;
}

public class LocalizationService : INotifyPropertyChanged
{
    private static readonly Lazy<LocalizationService> _instance = new(() => new LocalizationService());
    public static LocalizationService Instance => _instance.Value;

    public static readonly LanguageItem[] SupportedLanguages =
    [
        new() { Code = "FR", DisplayName = "🌐 FR - Français" },
        new() { Code = "EN", DisplayName = "🌐 EN - English" },
        new() { Code = "JA", DisplayName = "🌐 JA - 日本語" }
    ];

    private string _currentLanguage = "EN";

    public LocalizationService()
    {
        try
        {
            string? procDir = null;
            try { procDir = Path.GetDirectoryName(Environment.ProcessPath); } catch { }
            string baseDir = !string.IsNullOrEmpty(procDir) ? procDir : AppDomain.CurrentDomain.BaseDirectory;
            string langFile = Path.Combine(baseDir, "language.txt");
            if (File.Exists(langFile))
            {
                string read = File.ReadAllText(langFile).Trim().ToUpperInvariant();
                if (read.StartsWith("EN")) _currentLanguage = "EN";
                else if (read.StartsWith("JA")) _currentLanguage = "JA";
                else if (read.StartsWith("FR")) _currentLanguage = "FR";
            }
            foreach (var arg in Environment.GetCommandLineArgs())
            {
                if (arg.Equals("--lang=en", StringComparison.OrdinalIgnoreCase) || arg.Equals("-en", StringComparison.OrdinalIgnoreCase) || arg.Equals("/en", StringComparison.OrdinalIgnoreCase))
                {
                    _currentLanguage = "EN";
                }
                else if (arg.Equals("--lang=fr", StringComparison.OrdinalIgnoreCase) || arg.Equals("-fr", StringComparison.OrdinalIgnoreCase) || arg.Equals("/fr", StringComparison.OrdinalIgnoreCase))
                {
                    _currentLanguage = "FR";
                }
            }
        }
        catch { }
    }

    public string CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            if (_currentLanguage != value)
            {
                _currentLanguage = value;
                OnPropertyChanged(string.Empty);
                LanguageChanged?.Invoke();
            }
        }
    }

    public event Action? LanguageChanged;
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public static readonly Dictionary<string, Dictionary<string, string>> Translations = new(StringComparer.OrdinalIgnoreCase)
    {
        ["FR"] = new(StringComparer.OrdinalIgnoreCase)
        {
            // Top Bar
            ["BrandTitle"] = "O2JAM MUSIC MANAGER",
            ["BrandSubtitle"] = "GESTIONNAIRE DE MUSIQUES",
            ["MusicFolder"] = "Dossier sélectionné :",
            ["BrowseFolder"] = "📁 Parcourir",
            ["RefreshFolder"] = "🔄 Rafraîchir",
            ["LanguageLabel"] = "Langue :",

            // Actions Toolbar
            ["MoveUp"] = "▲ Monter",
            ["MoveUpTip"] = "Monter d'une ligne (Alt+Haut)",
            ["MoveDown"] = "▼ Descendre",
            ["MoveDownTip"] = "Descendre d'une ligne (Alt+Bas)",
            ["Reindex"] = "⚡ Réindexer l'Ordre",
            ["ReindexTip"] = "Renuméroter les IDs et synchroniser les fichiers physiques",
            ["ReindexCustom"] = "⚡ Réindexer Custom (1000-9999)",
            ["ReindexCustomTip"] = "Réindexer uniquement les musiques custom (ID >= 1000) et protéger strictement les IDs 0 à 999",
            ["AddSong"] = "➕ Ajouter",
            ["AddSongTip"] = "Importer une nouvelle musique (.ojn / .ojm)",
            ["DeleteSong"] = "🗑 Supprimer",
            ["DeleteSongTip"] = "Supprimer la musique sélectionnée",
            ["SearchPlaceholder"] = "Rechercher par titre, artiste, ID...",
            ["StatusFilterAll"] = "Tous les statuts",
            ["StatusFilterPresent"] = "OJM Présent uniquement",
            ["StatusFilterMissing"] = "OJM Manquant uniquement",

            // DataGrid Columns
            ["ColId"] = "ID",
            ["ColOjnFile"] = "FICHIER .OJN",
            ["ColOjmFile"] = "FICHIER .OJM",
            ["ColTitle"] = "TITRE",
            ["ColArtist"] = "ARTISTE",
            ["ColNoter"] = "NOTER",
            ["ColBpm"] = "BPM",
            ["ColLevels"] = "NIVEAUX (EX/NX/HX)",
            ["ColDuration"] = "DURÉE",
            ["ColOjmStatus"] = "STATUT .OJM",
            ["StatusPresent"] = "Présent",
            ["StatusMissing"] = "Manquant",

            // Context Menu
            ["CtxPlay"] = "▶ Lecture audio",
            ["CtxChangeCover"] = "🖼 Changer la jaquette",
            ["CtxChangeId"] = "🔢 Modifier l'ID...",
            ["CtxMoveUp"] = "▲ Monter (Alt+Haut)",
            ["CtxMoveDown"] = "▼ Descendre (Alt+Bas)",
            ["CtxOpenFolder"] = "📁 Ouvrir l'emplacement du fichier",
            ["CtxDelete"] = "🗑 Supprimer la musique (Suppr)",

            // Cover & Thumbnail Card
            ["CardCoverTitle"] = "JAQUETTE & MINIATURE",
            ["NoCoverJpg"] = "Aucune jaquette JPG",
            ["CoverInfo"] = "Jaquette : {0}x{1} ({2:F1} Ko)",
            ["CoverInfoNone"] = "Jaquette : -",
            ["ThumbInfo"] = "Miniature BMP 8x8 : {0:F1} Ko",
            ["ThumbInfoNone"] = "Miniature : -",
            ["ThumbInfoOptimized"] = "Miniature : Nette (optimisée {0}x{1})",
            ["ThumbInfoFormat"] = "Miniature : {0}x{1} ({2:F1} Ko)",
            ["BtnChangeCover"] = "🖼 Changer",
            ["BtnExportCover"] = "💾 Exporter",

            // Audio Player Card
            ["CardAudioTitle"] = "LECTEUR AUDIO",
            ["AudioReady"] = "Prêt pour la lecture",
            ["AudioReadyListen"] = "Prêt pour l'écoute",
            ["AudioSelectSong"] = "Sélectionnez une musique",
            ["AudioPlaying"] = "Lecture : {0} ({1:F0}s)",
            ["AudioPlayingSimple"] = "Lecture : {0}",
            ["AudioPaused"] = "En pause ⏸",
            ["AudioStopped"] = "Lecture terminée",
            ["AudioMissingOjm"] = "Fichier .ojm manquant ou introuvable.",
            ["AudioLoading"] = "Lecture audio...",
            ["AudioExtracting"] = "Extraction et décodage du flux audio...",
            ["AudioFailed"] = "Échec du démarrage de la lecture audio.",
            ["AudioError"] = "Erreur audio : {0}",
            ["AudioDiagDefault"] = "Format OJM : - | Durée : -",
            ["AudioDiagFormat"] = "Format OJM: {0} | {1} | Durée: {2}",
            ["AudioDiagError"] = "Erreur : {0}",
            ["BtnPlay"] = "▶ Lecture",
            ["BtnPause"] = "⏸ Pause",
            ["BtnStop"] = "⏹ Stop",
            ["LblVolume"] = "Volume :",

            // Metadata Form Card
            ["CardMetadataTitle"] = "MÉTADONNÉES DU HEADER .OJN",
            ["LblSongId"] = "Song ID :",
            ["BtnRenameId"] = "Renommer l'ID",
            ["LblTitle"] = "Titre :",
            ["LblArtist"] = "Artiste :",
            ["LblNoter"] = "Charter / Noter :",
            ["LblBpm"] = "BPM :",
            ["LblGenre"] = "Genre :",
            ["LblLevelsGroup"] = "Niveaux de difficulté (Easy / Normal / Hard) :",
            ["LblEx"] = "Ex (Easy)",
            ["LblNx"] = "Nx (Normal)",
            ["LblHx"] = "Hx (Hard)",
            ["BtnSaveMetadata"] = "💾 Sauvegarder les modifications sur le .OJN",

            // Technical Details Card
            ["CardTechTitle"] = "INFORMATIONS TECHNIQUES",
            ["LblTechOjn"] = "Fichier .OJN :",
            ["LblTechOjm"] = "Audio .OJM :",
            ["LblTechDurations"] = "Durées (Ex/Nx/Hx) :",
            ["LblTechNotes"] = "Notes (Ex/Nx/Hx) :",
            ["LblTechSizeOjn"] = "Taille fichier .OJN :",
            ["LblTechSizeOjm"] = "Taille fichier .OJM :",

            // Status Bar
            ["StatusTotalCount"] = "{0} musiques détectées",
            ["StatusOjmStats"] = "OJM : {0}, {1}",
            ["StatusSelection"] = "Sélection : ",
            ["StatusNone"] = "Aucune",

            // Reindex Window
            ["ReindexTitle"] = "Réindexer l'Ordre des Musiques (IDs)",
            ["ReindexBannerTitle"] = "⚡ Réindexation Séquentielle Automatique :",
            ["ReindexBannerDesc"] = "Cette opération va réassigner les numéros d'ID des fichiers .OJN selon l'ordre ci-dessous.\nElle met à jour l'en-tête binaire (song_id, old_song_id, ojm_file) et renomme les fichiers physiques .ojn et .ojm sans risque de collision.",
            ["ReindexStartId"] = "ID de début :",
            ["ReindexStep"] = "Incrémentation (Pas) :",
            ["ReindexColOrder"] = "ORDRE",
            ["ReindexColTitle"] = "TITRE",
            ["ReindexColOldId"] = "ID ACTUEL",
            ["ReindexColNewId"] = "NOUVEL ID",
            ["ReindexColOldFile"] = "FICHIER ACTUEL",
            ["ReindexColNewFile"] = "NOUVEAU FICHIER",
            ["ReindexBtnCancel"] = "Annuler",
            ["ReindexBtnApply"] = "⚡ Appliquer la réindexation",

            // Dialogs & Messages
            ["MsgSavedTitle"] = "Succès",
            ["MsgSaved"] = "Métadonnées sauvegardées avec succès pour {0} !",
            ["MsgConfirmDeleteTitle"] = "Confirmer la suppression",
            ["MsgConfirmDelete"] = "Êtes-vous sûr de vouloir supprimer définitivement cette musique du dossier sélectionné ?\n\nFichiers ciblés :\n- {0}\n- {1}",
            ["MsgFolderNotFound"] = "Le dossier sélectionné est introuvable.",

            // Extra Buttons & Tooltips
            ["GenerateOjnList"] = "📄 Générer OJNList.dat",
            ["GenerateOjnListTip"] = "Générer ou mettre à jour le fichier OJNList.dat pour O2Jam",
            ["CtxChangeThumbnail"] = "🖼 Changer la miniature (80x80)",
            ["BtnCoverText"] = "🖼 Jaquette",
            ["BtnCoverTip"] = "Remplacer la jaquette (800x600) et la miniature",
            ["BtnThumbText"] = "🖼 Miniature",
            ["BtnThumbTip"] = "Remplacer la miniature (80x80)",
            ["BtnExportText"] = "💾 Exporter",
            ["BtnExportTip"] = "Exporter la jaquette ou miniature",
            ["CoverClickTip"] = "Cliquer pour remplacer la jaquette (800x600 et miniature 80x80)",
            ["ThumbClickTip"] = "Cliquer pour remplacer la miniature (80x80)",
            ["DialogOk"] = "D'accord",
            ["DialogConfirm"] = "Confirmer",
            ["DialogCancel"] = "Annuler",
            ["LocationUpdated"] = "Emplacement mis à jour :",

            // Dialogs & Messages
            ["MsgWarning"] = "Avertissement",
            ["MsgInformation"] = "Information",
            ["MsgError"] = "Erreur",
            ["MsgSuccess"] = "Succès",
            ["MsgCoverUpdatedTitle"] = "Jaquette mise à jour",
            ["MsgCoverUpdated"] = "La jaquette (800x600) et la miniature (80x80) de '{0}' ont été converties au standard natif O2Jam et enregistrées avec succès !",
            ["MsgCoverFailed"] = "Échec du remplacement de la jaquette :\n{0}",
            ["MsgThumbUpdatedTitle"] = "Miniature mise à jour",
            ["MsgThumbUpdated"] = "La miniature (80x80) de '{0}' a été convertie au format natif O2Jam et enregistrée avec succès !",
            ["MsgThumbFailed"] = "Échec du remplacement de la miniature :\n{0}",
            ["MsgExportSuccess"] = "Image exportée avec succès !",
            ["MsgExportFailed"] = "Erreur d'exportation : {0}",
            ["MsgIdAlreadyUsed"] = "L'ID {0} est déjà l'identifiant actuel de cette musique.",
            ["MsgIdRenameFailed"] = "Impossible de modifier l'ID :\n{0}",
            ["MsgReindexSuccessTitle"] = "Réindexation terminée",
            ["MsgReindexSuccess"] = "La réindexation de {0} musiques a été appliquée avec succès !",
            ["MsgReindexFailed"] = "Erreur lors de la réindexation :\n{0}",
            ["MsgReindexCustomConfirmTitle"] = "Réindexer Custom (1000-9999)",
            ["MsgReindexCustomConfirm"] = "Voulez-vous réindexer automatiquement les {0} musiques custom de 1000 à {1} ?\n\n🔒 Protection active : Les musiques originales (ID 0 à 999) restent strictement protégées et inchangées.\nLes fichiers .ojn/.ojm physiques seront renommés sans risque de collision et l'OJNList sera synchronisé.",
            ["MsgReindexCustomBtn"] = "⚡ Réindexer (1000-9999)",
            ["MsgReindexCustomNoSongs"] = "Aucune musique custom (ID >= 1000) trouvée dans le dossier sélectionné.",
            ["MsgReindexCustomSuccessTitle"] = "Réindexation Custom terminée",
            ["MsgReindexCustomSuccess"] = "La réindexation de {0} musiques custom a été effectuée avec succès !\nIDs assignés : 1000 à {1}.\nLes musiques originales (0 à 999) ont été préservées intactes.",
            ["MsgReindexCustomOjnListSync"] = "\n\n📄 OJNList.dat a été synchronisé : {0}",
            ["MsgSongAddedTitle"] = "Musique ajoutée",
            ["MsgSongAdded"] = "La musique '{0}' a été importée sous le nom 'o2ma{1}.ojn'.",
            ["MsgSongAddFailed"] = "Impossible d'ajouter la musique :\n{0}",
            ["MsgDeleteFailed"] = "Impossible de supprimer le fichier :\n{0}",
            ["MsgScanError"] = "Erreur lors du scan : {0}",
            ["MsgFilesFailedLoad"] = "{0} fichier(s) n'ont pas pu être chargés :\n\n{1}",

            // Status Bar & Operations
            ["StatusReady"] = "Prêt.",
            ["StatusReindexingProgress"] = "Réindexation en cours... ({0} / {1})",
            ["StatusScanningProgress"] = "Actualisation de la bibliothèque...",
            ["StatusScanned"] = "Dossier scanné : {0} {1}.",
            ["WordSongDetectedSingular"] = "musique détectée",
            ["WordSongDetectedPlural"] = "musiques détectées",
            ["WordSongSingular"] = "musique",
            ["WordSongPlural"] = "musiques",
            ["WordIndexedSingular"] = "musique indexée",
            ["WordIndexedPlural"] = "musiques indexées",
            ["StatusSavedSuccess"] = "Modifications enregistrées pour o2ma{0} ({1}) !",
            ["StatusCoverUpdated"] = "Jaquette mise à jour pour o2ma{0} (JPG : {1:N0} octets, BMP : {2} octets) !",
            ["StatusThumbUpdated"] = "Miniature mise à jour pour o2ma{0} (BMP 80x80 natif : {1} octets) !",
            ["StatusRenamedSuccess"] = "Musique renommée en o2ma{0} avec succès !",
            ["StatusCustomReindexSuccess"] = "Réindexation custom de {0} musiques terminée (IDs {1} à {2}).",
            ["StatusReindexSuccess"] = "Réindexation de {0} musiques terminée avec succès !",
            ["StatusSongImported"] = "Nouvelle musique o2ma{0} importée avec succès !",
            ["StatusSongDeleted"] = "Musique o2ma{0} supprimée.",

            // File Dialogs
            ["DlgBrowseMusicFolder"] = "Sélectionner un dossier",
            ["DlgImportOjnTitle"] = "Sélectionner un fichier .ojn à importer",
            ["DlgImportOjnFilter"] = "Charts O2Jam (*.ojn)|*.ojn|Tous les fichiers (*.*)|*.*",
            ["DlgCoverTitle"] = "Choisir une nouvelle jaquette",
            ["DlgCoverFilter"] = "Images (*.png;*.jpg;*.jpeg;*.bmp;*.webp)|*.png;*.jpg;*.jpeg;*.bmp;*.webp|Tous les fichiers (*.*)|*.*",
            ["DlgThumbTitle"] = "Choisir une nouvelle miniature (80x80)",
            ["DlgExportTitle"] = "Exporter la jaquette",
            ["DlgExportFilter"] = "Image JPEG (*.jpg)|*.jpg|Miniature BMP (*.bmp)|*.bmp",

            // Delete Dialog
            ["DeleteConfirmTitle"] = "Confirmation de suppression",
            ["DeleteConfirmPrompt"] = "Voulez-vous vraiment supprimer définitivement la musique suivante ?\n\n• ID : o2ma{0}\n• Titre : {1}\n• Fichier .OJN : {2}\n• Fichier .OJM : {3}\n\nCette action supprimera physiquement ces fichiers du dossier sélectionné.",
            ["StatusNotPresent"] = "Non présent",
            ["DeleteSongClean"] = "Supprimer",

            // General
            ["Untitled"] = "(Sans titre)",
            ["MsgOpenExplorerFailed"] = "Impossible d'ouvrir l'explorateur : {0}",
            ["MsgSaveMetadataFailed"] = "Impossible d'enregistrer les métadonnées :\n{0}",

            // OJNList Generation
            ["OjnListChooseLocation"] = "Choisir l'emplacement pour enregistrer OJNList.dat",
            ["OjnListFilter"] = "Fichier OJNList (*.dat)|*.dat|Tous les fichiers (*.*)|*.*",
            ["OjnListSuccess"] = "Le fichier OJNList.dat a été généré avec succès pour {0} {1} !",
            ["OjnListStatus"] = "OJNList.dat généré ({0} {1}).",
            ["OjnListSelectFolderFirst"] = "Veuillez d'abord sélectionner un dossier valide.",
            ["OjnListNoSongs"] = "Aucune musique dans le dossier pour générer OJNList.dat.",
            ["OjnListError"] = "Impossible de générer OJNList.dat :\n{0}",
            ["OjnListTitle"] = "Génération OJNList.dat",

        },

        ["EN"] = new(StringComparer.OrdinalIgnoreCase)
        {
            // Top Bar
            ["BrandTitle"] = "O2JAM MUSIC MANAGER",
            ["BrandSubtitle"] = "SONG & CHART MANAGER",
            ["MusicFolder"] = "Selected Folder:",
            ["BrowseFolder"] = "📁 Browse",
            ["RefreshFolder"] = "🔄 Refresh",
            ["LanguageLabel"] = "Language:",

            // Actions Toolbar
            ["MoveUp"] = "▲ Move Up",
            ["MoveUpTip"] = "Move row up (Alt+Up)",
            ["MoveDown"] = "▼ Move Down",
            ["MoveDownTip"] = "Move row down (Alt+Down)",
            ["Reindex"] = "⚡ Auto-Index (0-9999)",
            ["ReindexTip"] = "Renumber IDs and synchronize physical files",
            ["ReindexCustom"] = "⚡ Reindex Custom (1000-9999)",
            ["ReindexCustomTip"] = "Reindex only custom songs (ID >= 1000) and strictly preserve IDs 0 to 999",
            ["AddSong"] = "➕ Add Song",
            ["AddSongTip"] = "Import a new song (.ojn / .ojm)",
            ["DeleteSong"] = "🗑 Delete",
            ["DeleteSongTip"] = "Delete selected song",
            ["SearchPlaceholder"] = "Search by title, artist, ID...",
            ["StatusFilterAll"] = "All statuses",
            ["StatusFilterPresent"] = "OJM Present only",
            ["StatusFilterMissing"] = "OJM Missing only",

            // DataGrid Columns
            ["ColId"] = "ID",
            ["ColOjnFile"] = ".OJN FILE",
            ["ColOjmFile"] = ".OJM FILE",
            ["ColTitle"] = "TITLE",
            ["ColArtist"] = "ARTIST",
            ["ColNoter"] = "NOTER / CHARTER",
            ["ColBpm"] = "BPM",
            ["ColLevels"] = "LEVELS (EX/NX/HX)",
            ["ColDuration"] = "DURATION",
            ["ColOjmStatus"] = ".OJM STATUS",
            ["StatusPresent"] = "Present",
            ["StatusMissing"] = "Missing",

            // Context Menu
            ["CtxPlay"] = "▶ Play Audio",
            ["CtxChangeCover"] = "🖼 Change Cover",
            ["CtxChangeId"] = "🔢 Modify ID...",
            ["CtxMoveUp"] = "▲ Move Up (Alt+Up)",
            ["CtxMoveDown"] = "▼ Move Down (Alt+Down)",
            ["CtxOpenFolder"] = "📁 Open File Location",
            ["CtxDelete"] = "🗑 Delete Song (Del)",

            // Cover & Thumbnail Card
            ["CardCoverTitle"] = "COVER & THUMBNAIL",
            ["NoCoverJpg"] = "No JPG cover",
            ["CoverInfo"] = "Cover: {0}x{1} ({2:F1} KB)",
            ["CoverInfoNone"] = "Cover: None",
            ["ThumbInfo"] = "BMP 8x8 Thumbnail: {0:F1} KB",
            ["ThumbInfoNone"] = "Thumbnail: None",
            ["ThumbInfoOptimized"] = "Thumbnail: Crisp (optimized {0}x{1})",
            ["ThumbInfoFormat"] = "Thumbnail: {0}x{1} ({2:F1} KB)",
            ["BtnChangeCover"] = "🖼 Change",
            ["BtnExportCover"] = "💾 Export",

            // Audio Player Card
            ["CardAudioTitle"] = "AUDIO PLAYER",
            ["AudioReady"] = "Ready to play",
            ["AudioReadyListen"] = "Ready to listen",
            ["AudioSelectSong"] = "Select a song",
            ["AudioPlaying"] = "Playing: {0} ({1:F0}s)",
            ["AudioPlayingSimple"] = "Playing: {0}",
            ["AudioPaused"] = "Paused ⏸",
            ["AudioStopped"] = "Playback finished",
            ["AudioMissingOjm"] = "Missing or unreadable .ojm audio file.",
            ["AudioLoading"] = "Loading audio...",
            ["AudioExtracting"] = "Extracting and decoding audio stream...",
            ["AudioFailed"] = "Failed to start audio playback.",
            ["AudioError"] = "Audio error: {0}",
            ["AudioDiagDefault"] = "OJM Format: - | Duration: -",
            ["AudioDiagFormat"] = "OJM Format: {0} | {1} | Duration: {2}",
            ["AudioDiagError"] = "Error: {0}",
            ["BtnPlay"] = "▶ Play",
            ["BtnPause"] = "⏸ Pause",
            ["BtnStop"] = "⏹ Stop",
            ["LblVolume"] = "Volume:",

            // Metadata Form Card
            ["CardMetadataTitle"] = ".OJN HEADER METADATA",
            ["LblSongId"] = "Song ID:",
            ["BtnRenameId"] = "Rename ID",
            ["LblTitle"] = "Title:",
            ["LblArtist"] = "Artist:",
            ["LblNoter"] = "Charter / Noter:",
            ["LblBpm"] = "BPM:",
            ["LblGenre"] = "Genre:",
            ["LblLevelsGroup"] = "Difficulty Levels (Easy / Normal / Hard):",
            ["LblEx"] = "Ex (Easy)",
            ["LblNx"] = "Nx (Normal)",
            ["LblHx"] = "Hx (Hard)",
            ["BtnSaveMetadata"] = "💾 Save Changes",

            // Technical Details Card
            ["CardTechTitle"] = "TECHNICAL DETAILS",
            ["LblTechTitle"] = "TECHNICAL DETAILS",
            ["LblTechOjn"] = ".OJN File:",
            ["LblTechOjm"] = "Audio .OJM:",
            ["LblTechDurations"] = "Durations (Ex/Nx/Hx):",
            ["LblTechNotes"] = "Notes (Ex/Nx/Hx):",
            ["LblTechSizeOjn"] = ".OJN File Size:",
            ["LblTechSizeOjm"] = ".OJM File Size:",

            // Status Bar
            ["StatusTotalCount"] = "{0} songs detected",
            ["StatusOjmStats"] = "OJM: {0}, {1}",
            ["StatusSelection"] = "Selection: ",
            ["StatusNone"] = "None",

            // Reindex Window
            ["ReindexTitle"] = "Reindex Song Order (IDs)",
            ["ReindexBannerTitle"] = "⚡ Automatic Sequential Reindexing:",
            ["ReindexBannerDesc"] = "This operation will reassign .OJN IDs according to the order below.\nIt updates internal binary headers (song_id, old_song_id, ojm_file) and renames physical .ojn and .ojm files without collisions.",
            ["ReindexStartId"] = "Start ID:",
            ["ReindexStep"] = "Step (Increment):",
            ["ReindexColOrder"] = "ORDER",
            ["ReindexColTitle"] = "TITLE",
            ["ReindexColOldId"] = "CURRENT ID",
            ["ReindexColNewId"] = "NEW ID",
            ["ReindexColOldFile"] = "CURRENT FILE",
            ["ReindexColNewFile"] = "NEW FILE",
            ["ReindexBtnCancel"] = "Cancel",
            ["ReindexBtnApply"] = "⚡ Apply Auto-Index",

            // Dialogs & Messages
            ["MsgSavedTitle"] = "Success",
            ["MsgSaved"] = "Metadata successfully saved for {0}!",
            ["MsgConfirmDeleteTitle"] = "Confirm deletion",
            ["MsgConfirmDelete"] = "Are you sure you want to permanently delete this song from the selected folder?\n\nTarget files:\n- {0}\n- {1}",
            ["MsgFolderNotFound"] = "The selected folder does not exist.",

            // Extra Buttons & Tooltips
            ["GenerateOjnList"] = "📄 Generate OJNList.dat",
            ["GenerateOjnListTip"] = "Generate or update OJNList.dat file for O2Jam",
            ["CtxChangeThumbnail"] = "🖼 Change Thumbnail (80x80)",
            ["BtnCoverText"] = "🖼 Cover",
            ["BtnCoverTip"] = "Replace cover (800x600) and thumbnail",
            ["BtnThumbText"] = "🖼 Thumbnail",
            ["BtnThumbTip"] = "Replace thumbnail (80x80)",
            ["BtnExportText"] = "💾 Export",
            ["BtnExportTip"] = "Export cover or thumbnail",
            ["CoverClickTip"] = "Click to replace cover (800x600 and 80x80 thumbnail)",
            ["ThumbClickTip"] = "Click to replace thumbnail (80x80)",
            ["DialogOk"] = "OK",
            ["DialogConfirm"] = "Confirm",
            ["DialogCancel"] = "Cancel",
            ["LocationUpdated"] = "Location updated:",

            // Dialogs & Messages
            ["MsgWarning"] = "Warning",
            ["MsgInformation"] = "Information",
            ["MsgError"] = "Error",
            ["MsgSuccess"] = "Success",
            ["MsgCoverUpdatedTitle"] = "Cover updated",
            ["MsgCoverUpdated"] = "Cover (800x600) and thumbnail (80x80) of '{0}' converted to native O2Jam standard and saved successfully!",
            ["MsgCoverFailed"] = "Failed to replace cover:\n{0}",
            ["MsgThumbUpdatedTitle"] = "Thumbnail updated",
            ["MsgThumbUpdated"] = "Thumbnail (80x80) of '{0}' converted to native O2Jam format and saved successfully!",
            ["MsgThumbFailed"] = "Failed to replace thumbnail:\n{0}",
            ["MsgExportSuccess"] = "Image exported successfully!",
            ["MsgExportFailed"] = "Export error: {0}",
            ["MsgIdAlreadyUsed"] = "ID {0} is already the current identifier for this song.",
            ["MsgIdRenameFailed"] = "Unable to modify ID:\n{0}",
            ["MsgReindexSuccessTitle"] = "Reindexing completed",
            ["MsgReindexSuccess"] = "Reindexing of {0} songs applied successfully!",
            ["MsgReindexFailed"] = "Error during reindexing:\n{0}",
            ["MsgReindexCustomConfirmTitle"] = "Reindex Custom (1000-9999)",
            ["MsgReindexCustomConfirm"] = "Do you want to automatically reindex the {0} custom songs from 1000 to {1}?\n\n🔒 Active Protection: Original songs (ID 0 to 999) remain strictly protected and unchanged.\nPhysical .ojn/.ojm files will be renamed collision-free and OJNList will be synchronized.",
            ["MsgReindexCustomBtn"] = "⚡ Reindex (1000-9999)",
            ["MsgReindexCustomNoSongs"] = "No custom songs (ID >= 1000) found in the selected folder.",
            ["MsgReindexCustomSuccessTitle"] = "Custom Reindexing Complete",
            ["MsgReindexCustomSuccess"] = "Successfully reindexed {0} custom songs!\nAssigned IDs: 1000 to {1}.\nOriginal songs (0 to 999) were preserved intact.",
            ["MsgReindexCustomOjnListSync"] = "\n\n📄 OJNList.dat was synchronized: {0}",
            ["MsgSongAddedTitle"] = "Song added",
            ["MsgSongAdded"] = "Song '{0}' imported as 'o2ma{1}.ojn'.",
            ["MsgSongAddFailed"] = "Unable to add song:\n{0}",
            ["MsgDeleteFailed"] = "Unable to delete file:\n{0}",
            ["MsgScanError"] = "Error scanning folder: {0}",
            ["MsgFilesFailedLoad"] = "{0} file(s) could not be loaded:\n\n{1}",

            // Status Bar & Operations
            ["StatusReady"] = "Ready.",
            ["StatusReindexingProgress"] = "Reindexing in progress... ({0} / {1})",
            ["StatusScanningProgress"] = "Refreshing music library...",
            ["StatusScanned"] = "Folder scanned: {0} {1}.",
            ["WordSongDetectedSingular"] = "song detected",
            ["WordSongDetectedPlural"] = "songs detected",
            ["WordSongSingular"] = "song",
            ["WordSongPlural"] = "songs",
            ["WordIndexedSingular"] = "song indexed",
            ["WordIndexedPlural"] = "songs indexed",
            ["StatusSavedSuccess"] = "Changes saved for o2ma{0} ({1})!",
            ["StatusCoverUpdated"] = "Cover updated for o2ma{0} (JPG: {1:N0} bytes, BMP: {2} bytes)!",
            ["StatusThumbUpdated"] = "Thumbnail updated for o2ma{0} (native 80x80 BMP: {1} bytes)!",
            ["StatusRenamedSuccess"] = "Song renamed to o2ma{0} successfully!",
            ["StatusCustomReindexSuccess"] = "Custom reindexing of {0} songs completed (IDs {1} to {2}).",
            ["StatusReindexSuccess"] = "Reindexing of {0} songs completed successfully!",
            ["StatusSongImported"] = "New song o2ma{0} imported successfully!",
            ["StatusSongDeleted"] = "Song o2ma{0} deleted.",

            // File Dialogs
            ["DlgBrowseMusicFolder"] = "Select a folder",
            ["DlgImportOjnTitle"] = "Select .ojn chart to import",
            ["DlgImportOjnFilter"] = "O2Jam Charts (*.ojn)|*.ojn|All files (*.*)|*.*",
            ["DlgCoverTitle"] = "Select new cover image",
            ["DlgCoverFilter"] = "Images (*.png;*.jpg;*.jpeg;*.bmp;*.webp)|*.png;*.jpg;*.jpeg;*.bmp;*.webp|All files (*.*)|*.*",
            ["DlgThumbTitle"] = "Select new thumbnail (80x80)",
            ["DlgExportTitle"] = "Export cover image",
            ["DlgExportFilter"] = "JPEG Image (*.jpg)|*.jpg|BMP Thumbnail (*.bmp)|*.bmp",

            // Delete Dialog
            ["DeleteConfirmTitle"] = "Confirm deletion",
            ["DeleteConfirmPrompt"] = "Are you sure you want to permanently delete the following song?\n\n• ID: o2ma{0}\n• Title: {1}\n• .OJN File: {2}\n• .OJM File: {3}\n\nThis will physically delete these files from the selected folder.",
            ["StatusNotPresent"] = "Not present",
            ["DeleteSongClean"] = "Delete",

            // General
            ["Untitled"] = "(Untitled)",
            ["MsgOpenExplorerFailed"] = "Failed to open File Explorer: {0}",
            ["MsgSaveMetadataFailed"] = "Failed to save metadata:\n{0}",

            // OJNList Generation
            ["OjnListChooseLocation"] = "Choose location to save OJNList.dat",
            ["OjnListFilter"] = "OJNList File (*.dat)|*.dat|All Files (*.*)|*.*",
            ["OjnListSuccess"] = "OJNList.dat successfully generated for {0} {1}!",
            ["OjnListStatus"] = "OJNList.dat generated ({0} {1}).",
            ["OjnListSelectFolderFirst"] = "Please select a valid folder first.",
            ["OjnListNoSongs"] = "No songs in folder to generate OJNList.dat.",
            ["OjnListError"] = "Unable to generate OJNList.dat:\n{0}",
            ["OjnListTitle"] = "OJNList.dat Generation",

        },

        ["JA"] = new(StringComparer.OrdinalIgnoreCase)
        {
            // Top Bar
            ["BrandTitle"] = "O2JAM MUSIC MANAGER",
            ["BrandSubtitle"] = "SONG & CHART MANAGER",
            ["MusicFolder"] = "Music フォルダ :",
            ["BrowseFolder"] = "📁 参照",
            ["RefreshFolder"] = "🔄 更新",
            ["LanguageLabel"] = "言語 :",

            // Actions Toolbar
            ["MoveUp"] = "▲ 上へ",
            ["MoveUpTip"] = "1行上へ移動 (Alt+上)",
            ["MoveDown"] = "▼ 下へ",
            ["MoveDownTip"] = "1行下へ移動 (Alt+下)",
            ["Reindex"] = "⚡ 連番リインデックス",
            ["ReindexTip"] = "IDを再採番し物理ファイルを同期",
            ["ReindexCustom"] = "⚡ カスタム曲リインデックス (1000-9999)",
            ["ReindexCustomTip"] = "カスタム曲（ID 1000以上）のみを自動リインデックスし、0〜999を厳格に保護",
            ["AddSong"] = "➕ 曲を追加",
            ["AddSongTip"] = "新しい曲をインポート (.ojn / .ojm)",
            ["DeleteSong"] = "🗑 削除",
            ["DeleteSongTip"] = "選択した曲を削除",
            ["SearchPlaceholder"] = "タイトル、アーティスト、IDで検索...",
            ["StatusFilterAll"] = "すべてのステータス",
            ["StatusFilterPresent"] = "OJM あり のみ",
            ["StatusFilterMissing"] = "OJM なし のみ",

            // DataGrid Columns
            ["ColId"] = "ID",
            ["ColOjnFile"] = ".OJN ファイル",
            ["ColOjmFile"] = ".OJM ファイル",
            ["ColTitle"] = "タイトル",
            ["ColArtist"] = "アーティスト",
            ["ColNoter"] = "譜面制作者",
            ["ColBpm"] = "BPM",
            ["ColLevels"] = "難易度 (EX/NX/HX)",
            ["ColDuration"] = "演奏時間",
            ["ColOjmStatus"] = ".OJM 状態",
            ["StatusPresent"] = "あり",
            ["StatusMissing"] = "なし",

            // Context Menu
            ["CtxPlay"] = "▶ 音楽再生",
            ["CtxChangeCover"] = "🖼 ジャケット変更...",
            ["CtxChangeId"] = "🔢 ID変更...",
            ["CtxMoveUp"] = "▲ 上へ (Alt+上)",
            ["CtxMoveDown"] = "▼ 下へ (Alt+下)",
            ["CtxOpenFolder"] = "📁 ファイルの場所を開く",
            ["CtxDelete"] = "🗑 Musicから削除 (Del)",

            // Cover & Thumbnail Card
            ["CardCoverTitle"] = "ジャケット & サムネイル",
            ["NoCoverJpg"] = "JPG ジャケットなし",
            ["CoverInfo"] = "ジャケット : {0}x{1} ({2:F1} KB)",
            ["CoverInfoNone"] = "ジャケット : なし",
            ["ThumbInfo"] = "8x8 BMP サムネイル : {0:F1} KB",
            ["ThumbInfoNone"] = "サムネイル : なし",
            ["ThumbInfoOptimized"] = "サムネイル : 鮮明 (最適化 {0}x{1})",
            ["ThumbInfoFormat"] = "サムネイル : {0}x{1} ({2:F1} KB)",
            ["BtnChangeCover"] = "🖼 画像変更...",
            ["BtnExportCover"] = "💾 画像保存...",

            // Audio Player Card
            ["CardAudioTitle"] = "オーディオプレーヤー",
            ["AudioReady"] = "再生待機中",
            ["AudioReadyListen"] = "再生準備完了",
            ["AudioSelectSong"] = "楽曲を選択してください",
            ["AudioPlaying"] = "再生中 : {0} ({1:F0}秒)",
            ["AudioPlayingSimple"] = "再生中 : {0}",
            ["AudioPaused"] = "一時停止中 ⏸",
            ["AudioStopped"] = "再生終了",
            ["AudioMissingOjm"] = ".ojm 音声ファイルが見つかりません。",
            ["AudioLoading"] = "音声読み込み中...",
            ["AudioExtracting"] = "音声ストリームを抽出・デコード中...",
            ["AudioFailed"] = "音声の再生開始に失敗しました。",
            ["AudioError"] = "音声エラー : {0}",
            ["AudioDiagDefault"] = "OJM フォーマット : - | 演奏時間 : -",
            ["AudioDiagFormat"] = "OJM フォーマット : {0} | {1} | 演奏時間 : {2}",
            ["AudioDiagError"] = "エラー : {0}",
            ["BtnPlay"] = "▶ 再生",
            ["BtnPause"] = "⏸ 一時停止",
            ["BtnStop"] = "⏹ 停止",
            ["LblVolume"] = "音量 :",

            // Metadata Form Card
            ["CardMetadataTitle"] = ".OJN ヘッダー メタデータ",
            ["LblSongId"] = "楽曲 ID :",
            ["BtnRenameId"] = "ID変更",
            ["LblTitle"] = "曲名 :",
            ["LblArtist"] = "アーティスト :",
            ["LblNoter"] = "譜面制作者 :",
            ["LblBpm"] = "BPM :",
            ["LblGenre"] = "ジャンル :",
            ["LblLevelsGroup"] = "難易度レベル (Easy / Normal / Hard) :",
            ["LblEx"] = "Ex (Easy)",
            ["LblNx"] = "Nx (Normal)",
            ["LblHx"] = "Hx (Hard)",
            ["BtnSaveMetadata"] = "💾 .OJN に変更を保存",

            // Technical Details Card
            ["CardTechTitle"] = "詳細技術情報",
            ["LblTechTitle"] = "詳細技術情報",
            ["LblTechOjn"] = ".OJN ファイル :",
            ["LblTechOjm"] = ".OJM 音声 :",
            ["LblTechDurations"] = "演奏時間 (Ex/Nx/Hx) :",
            ["LblTechNotes"] = "総ノーツ数 (Ex/Nx/Hx) :",
            ["LblTechSizeOjn"] = ".OJN ファイルサイズ :",
            ["LblTechSizeOjm"] = ".OJM ファイルサイズ :",

            // Status Bar
            ["StatusTotalCount"] = "{0} 曲読み込み完了",
            ["StatusOjmStats"] = "OJM : {0} あり、{1} なし",
            ["StatusSelection"] = "選択中 : ",
            ["StatusNone"] = "なし",

            // Reindex Window
            ["ReindexTitle"] = "楽曲連番リインデックス (ID)",
            ["ReindexBannerTitle"] = "⚡ 自動連続リインデックス :",
            ["ReindexBannerDesc"] = "この操作は下記の一覧順に従って .OJN の ID 番号を再採番します。\nバイナリヘッダー (song_id, old_song_id, ojm_file) を更新し、物理ファイル (.ojn, .ojm) を安全にリネームします。",
            ["ReindexStartId"] = "開始 ID :",
            ["ReindexStep"] = "増分ステップ :",
            ["ReindexColOrder"] = "順序",
            ["ReindexColTitle"] = "曲名",
            ["ReindexColOldId"] = "現在のID",
            ["ReindexColNewId"] = "新規ID",
            ["ReindexColOldFile"] = "現在のファイル",
            ["ReindexColNewFile"] = "新規ファイル",
            ["ReindexBtnCancel"] = "キャンセル",
            ["ReindexBtnApply"] = "⚡ リインデックスを適用",

            // Dialogs & Messages
            ["MsgSavedTitle"] = "成功",
            ["MsgSaved"] = "{0} のメタデータを保存しました！",
            ["MsgConfirmDeleteTitle"] = "削除の確認",
            ["MsgConfirmDelete"] = "この楽曲を Music フォルダから完全に削除しますか？\n\n対象ファイル :\n- {0}\n- {1}",
            ["MsgFolderNotFound"] = "指定された Music フォルダが見つかりません。",

            // Extra Buttons & Tooltips
            ["GenerateOjnList"] = "📄 OJNList.dat 生成",
            ["GenerateOjnListTip"] = "O2Jam 用の OJNList.dat ファイルを生成または更新",
            ["CtxChangeThumbnail"] = "🖼 サムネイル変更 (80x80)",
            ["BtnCoverText"] = "🖼 ジャケット",
            ["BtnCoverTip"] = "ジャケットとサムネイルを変更",
            ["BtnThumbText"] = "🖼 サムネイル",
            ["BtnThumbTip"] = "サムネイルを変更 (80x80)",
            ["BtnExportText"] = "💾 エクスポート",
            ["BtnExportTip"] = "ジャケットまたはサムネイルを保存",
            ["CoverClickTip"] = "クリックしてジャケットを変更",
            ["ThumbClickTip"] = "クリックしてサムネイルを変更",
            ["DialogOk"] = "OK",
            ["DialogConfirm"] = "確認",
            ["DialogCancel"] = "キャンセル",
            ["LocationUpdated"] = "更新された保存先 :",

            // Dialogs & Messages
            ["MsgWarning"] = "警告",
            ["MsgInformation"] = "情報",
            ["MsgError"] = "エラー",
            ["MsgSuccess"] = "成功",
            ["MsgCoverUpdatedTitle"] = "ジャケット更新完了",
            ["MsgCoverUpdated"] = "'{0}' のジャケットとサムネイルを正常に保存しました！",
            ["MsgCoverFailed"] = "ジャケットの変更に失敗しました :\n{0}",
            ["MsgThumbUpdatedTitle"] = "サムネイル更新完了",
            ["MsgThumbUpdated"] = "'{0}' のサムネイルを正常に保存しました！",
            ["MsgThumbFailed"] = "サムネイルの変更に失敗しました :\n{0}",
            ["MsgExportSuccess"] = "画像を保存しました！",
            ["MsgExportFailed"] = "保存エラー : {0}",
            ["MsgIdAlreadyUsed"] = "ID {0} は既に現在の曲のIDです。",
            ["MsgIdRenameFailed"] = "IDを変更できません :\n{0}",
            ["MsgReindexSuccessTitle"] = "連番リインデックス完了",
            ["MsgReindexSuccess"] = "{0} 曲のリインデックスが正常に完了しました！",
            ["MsgReindexFailed"] = "リインデックス中にエラーが発生しました :\n{0}",
            ["MsgReindexCustomConfirmTitle"] = "カスタム曲リインデックス (1000-9999)",
            ["MsgReindexCustomConfirm"] = "{0} 曲のカスタム曲を 1000 から {1} まで自動リインデックスしますか？\n\n🔒 保護機能：オリジナル曲（ID 0〜999）は完全に保護され変更されません。\n物理ファイル（.ojn/.ojm）は安全にリネームされ、OJNList も同期されます。",
            ["MsgReindexCustomBtn"] = "⚡ リインデックス (1000-9999)",
            ["MsgReindexCustomNoSongs"] = "Music フォルダ内にカスタム曲（ID 1000以上）が見つかりませんでした。",
            ["MsgReindexCustomSuccessTitle"] = "カスタム曲リインデックス完了",
            ["MsgReindexCustomSuccess"] = "{0} 曲のカスタム曲のリインデックスが正常に完了しました！\n割り当て ID : 1000 〜 {1}。\nオリジナル曲（0〜999）は保護されました。",
            ["MsgReindexCustomOjnListSync"] = "\n\n📄 OJNList.dat が同期されました : {0}",
            ["MsgSongAddedTitle"] = "曲を追加しました",
            ["MsgSongAdded"] = "曲 '{0}' を 'o2ma{1}.ojn' として追加しました。",
            ["MsgSongAddFailed"] = "曲を追加できません :\n{0}",
            ["MsgDeleteFailed"] = "ファイルを削除できません :\n{0}",
            ["MsgScanError"] = "スキャンエラー : {0}",
            ["MsgFilesFailedLoad"] = "{0} 個のファイルを読み込めませんでした :\n\n{1}",

            // Status Bar & Operations
            ["StatusReady"] = "準備完了。",
            ["StatusReindexingProgress"] = "リインデックス実行中... ({0} / {1})",
            ["StatusScanningProgress"] = "ライブラリを更新中...",
            ["StatusScanned"] = "Music フォルダをスキャンしました : {0} {1}。",
            ["WordSongDetectedSingular"] = "曲検出",
            ["WordSongDetectedPlural"] = "曲検出",
            ["WordSongSingular"] = "曲",
            ["WordSongPlural"] = "曲",
            ["WordIndexedSingular"] = "曲インデックス済み",
            ["WordIndexedPlural"] = "曲インデックス済み",
            ["StatusSavedSuccess"] = "o2ma{0} ({1}) の変更を保存しました！",
            ["StatusCoverUpdated"] = "o2ma{0} のジャケットを更新しました (JPG: {1:N0} バイト, BMP: {2} バイト)！",
            ["StatusThumbUpdated"] = "o2ma{0} のサムネイルを更新しました (ネイティブ 80x80 BMP: {1} バイト)！",
            ["StatusRenamedSuccess"] = "o2ma{0} へのリネームが完了しました！",
            ["StatusCustomReindexSuccess"] = "カスタム曲 {0} 曲のリインデックスが完了しました (ID {1} 〜 {2})。",
            ["StatusReindexSuccess"] = "{0} 曲のリインデックスが完了しました！",
            ["StatusSongImported"] = "新しい楽曲 o2ma{0} をインポートしました！",
            ["StatusSongDeleted"] = "楽曲 o2ma{0} を削除しました。",

            // File Dialogs
            ["DlgBrowseMusicFolder"] = "O2JamのMusicフォルダを選択",
            ["DlgImportOjnTitle"] = "インポートする .ojn ファイルを選択",
            ["DlgImportOjnFilter"] = "O2Jam 譜面 (*.ojn)|*.ojn|すべてのファイル (*.*)|*.*",
            ["DlgCoverTitle"] = "新しいジャケット画像を選択",
            ["DlgCoverFilter"] = "画像 (*.png;*.jpg;*.jpeg;*.bmp;*.webp)|*.png;*.jpg;*.jpeg;*.bmp;*.webp|すべてのファイル (*.*)|*.*",
            ["DlgThumbTitle"] = "新しいサムネイルを選択 (80x80)",
            ["DlgExportTitle"] = "ジャケット画像をエクスポート",
            ["DlgExportFilter"] = "JPEG 画像 (*.jpg)|*.jpg|BMP サムネイル (*.bmp)|*.bmp",

            // Delete Dialog
            ["DeleteConfirmTitle"] = "削除の確認",
            ["DeleteConfirmPrompt"] = "次の楽曲を Music フォルダから完全に削除しますか？\n\n• ID : o2ma{0}\n• 曲名 : {1}\n• .OJN ファイル : {2}\n• .OJM ファイル : {3}\n\nこの操作により、ファイルはディスクから完全に削除されます。",
            ["StatusNotPresent"] = "なし",
            ["DeleteSongClean"] = "削除",

            // General
            ["Untitled"] = "(無題)",
            ["MsgOpenExplorerFailed"] = "エクスプローラーを開けませんでした : {0}",
            ["MsgSaveMetadataFailed"] = "メタデータを保存できません :\n{0}",

            // OJNList Generation
            ["OjnListChooseLocation"] = "OJNList.dat の保存先を選択",
            ["OjnListFilter"] = "OJNList ファイル (*.dat)|*.dat|すべてのファイル (*.*)|*.*",
            ["OjnListSuccess"] = "{0} 曲分の OJNList.dat を正常に生成しました！",
            ["OjnListStatus"] = "OJNList.dat 生成完了 ({0} 曲インデックス済み)",
            ["OjnListSelectFolderFirst"] = "先に有効な Music フォルダを選択してください。",
            ["OjnListNoSongs"] = "OJNList.dat を生成するための曲がありません。",
            ["OjnListError"] = "OJNList.dat を生成できません :\n{0}",
            ["OjnListTitle"] = "OJNList.dat 生成",

        }
    };

    public string Get(string key)
    {
        if (Translations.TryGetValue(CurrentLanguage, out var dict) && dict.TryGetValue(key, out var val))
            return val;
        if (Translations["EN"].TryGetValue(key, out var fallback))
            return fallback;
        if (Translations["FR"].TryGetValue(key, out var frFallback))
            return frFallback;
        return key;
    }

    public string Get(string key, params object[] args)
    {
        string pattern = Get(key);
        try
        {
            return string.Format(pattern, args);
        }
        catch
        {
            return pattern;
        }
    }

    public string this[string key] => Get(key);
}
