using Microsoft.VisualStudio.TestTools.UnitTesting;
using O2JamManager.Core;
using O2JamManager.Models;
using System.IO;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace O2JamManager.Tests;

[TestClass]
public class OjnCoreTests
{
    private string _tempDir = string.Empty;
    private string _musicDir = string.Empty;

    [TestInitialize]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "O2JamTest_" + Guid.NewGuid().ToString("N"));
        _musicDir = Path.Combine(_tempDir, "Music");
        Directory.CreateDirectory(_musicDir);

        // Copy files from Sample_Music into test directory
        string sourceDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "Sample_Music"));
        if (!Directory.Exists(sourceDir))
        {
            sourceDir = @"A:\Antigravity app\Sample_Music";
        }
        if (!Directory.Exists(sourceDir))
        {
            sourceDir = @"A:\Application\AntiGravity\Sample_Music";
        }
        if (Directory.Exists(sourceDir))
        {
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                File.Copy(file, Path.Combine(_musicDir, Path.GetFileName(file)), true);
            }
        }
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, true); } catch { }
        }
    }

    [TestMethod]
    public void TestReadSong()
    {
        string ojnPath = Path.Combine(_musicDir, "o2ma101.ojn");
        Assert.IsTrue(File.Exists(ojnPath));

        var song = OjnParser.ReadSong(ojnPath);

        Assert.AreEqual(101, song.SongId);
        Assert.AreEqual("Electro Fantasy", song.Title);
        Assert.AreEqual("SHK", song.Artist);
        Assert.AreEqual("Porin", song.Noter);
        Assert.AreEqual(2, song.GenreId);
        Assert.AreEqual("Dance", song.GenreName);
        Assert.AreEqual(140.0f, song.Bpm, 0.01f);
        Assert.AreEqual(6, song.LevelEx);
        Assert.AreEqual(14, song.LevelNx);
        Assert.AreEqual(28, song.LevelHx);
        Assert.IsTrue(song.OjmExists);
        Assert.IsTrue(song.HasCover);
        Assert.IsTrue(song.HasThumbnail);
    }

    [TestMethod]
    public void TestUpdateMetadata()
    {
        string ojnPath = Path.Combine(_musicDir, "o2ma101.ojn");

        OjnParser.UpdateMetadata(
            ojnPath,
            title: "C# Native WPF Hits",
            artist: "Dev Master",
            noter: "O2Jam Legend",
            genreId: 3, // Techno
            bpm: 182.5f,
            levelEx: 11,
            levelNx: 22,
            levelHx: 44
        );

        var refreshed = OjnParser.ReadSong(ojnPath);
        Assert.AreEqual("C# Native WPF Hits", refreshed.Title);
        Assert.AreEqual("Dev Master", refreshed.Artist);
        Assert.AreEqual("O2Jam Legend", refreshed.Noter);
        Assert.AreEqual(3, refreshed.GenreId);
        Assert.AreEqual("Techno", refreshed.GenreName);
        Assert.AreEqual(182.5f, refreshed.Bpm, 0.01f);
        Assert.AreEqual(11, refreshed.LevelEx);
        Assert.AreEqual(22, refreshed.LevelNx);
        Assert.AreEqual(44, refreshed.LevelHx);
    }

    [TestMethod]
    public void TestCoverReplacementPreservesNotes()
    {
        string ojnPath = Path.Combine(_musicDir, "o2ma101.ojn");
        var originalSong = OjnParser.ReadSong(ojnPath);

        // Read original note chart bytes from 0x12C to cover_offset
        byte[] originalNotes;
        using (var fs = File.OpenRead(ojnPath))
        {
            fs.Seek(Constants.HeaderSize, SeekOrigin.Begin);
            int notesLen = originalSong.CoverOffset - Constants.HeaderSize;
            originalNotes = new byte[notesLen];
            fs.ReadExactly(originalNotes);
        }

        // Create a dummy image to inject
        string dummyImg = Path.Combine(_tempDir, "test_cover.jpg");
        CreateDummyImage(dummyImg, 400, 300);

        var (jpgLen, bmpLen) = OjnParser.WriteCoverAndThumbnail(ojnPath, dummyImg);
        Assert.IsTrue(jpgLen > 0);
        Assert.AreEqual(Constants.StandardBmp80x80Size, bmpLen); // Standard 80x80 24bpp BMP (19254 bytes)

        var refreshed = OjnParser.ReadSong(ojnPath);
        Assert.AreEqual(jpgLen, refreshed.CoverSize);
        Assert.AreEqual(bmpLen, refreshed.BmpSize);
        Assert.AreEqual(originalSong.CoverOffset, refreshed.CoverOffset);

        // Verify note charts bytes are 100% identical!
        byte[] afterNotes;
        using (var fs = File.OpenRead(ojnPath))
        {
            fs.Seek(Constants.HeaderSize, SeekOrigin.Begin);
            int notesLen = refreshed.CoverOffset - Constants.HeaderSize;
            afterNotes = new byte[notesLen];
            fs.ReadExactly(afterNotes);
        }

        CollectionAssert.AreEqual(originalNotes, afterNotes);
    }

    [TestMethod]
    public void TestReindexingCollisionFree()
    {
        var fm = new FileManager();
        fm.SetMusicDir(_musicDir);

        var (songs, _) = fm.ScanMusicDir();
        Assert.IsTrue(songs.Count >= 4);

        // General reindexing from 500 step 10
        var reindexed = fm.ReindexSongs(songs, startId: 500, step: 10);
        Assert.AreEqual(songs.Count, reindexed.Count);
        Assert.AreEqual(500, reindexed[0].SongId);
        Assert.AreEqual(510, reindexed[1].SongId);
        Assert.AreEqual(520, reindexed[2].SongId);
        Assert.AreEqual(530, reindexed[3].SongId);
    }

    [TestMethod]
    public void TestReindexCustomSongsStrictLock()
    {
        string customDir = Path.Combine(_tempDir, "reindex_custom_test");
        Directory.CreateDirectory(customDir);

        var fmBase = new FileManager();
        fmBase.SetMusicDir(_musicDir);
        var (baseSongs, _) = fmBase.ScanMusicDir();

        // Create 2 original charts (ID 101, 102)
        string orig101 = Path.Combine(customDir, "o2ma101.ojn");
        string orig102 = Path.Combine(customDir, "o2ma102.ojn");
        File.Copy(baseSongs[0].FilePath, orig101, true);
        OjnParser.UpdateSongId(orig101, 101, "o2ma101.ojm");
        File.Copy(baseSongs[0].FilePath, orig102, true);
        OjnParser.UpdateSongId(orig102, 102, "o2ma102.ojm");

        // Create 3 custom charts (ID 1500, 2500, 3500)
        string cust1500 = Path.Combine(customDir, "o2ma1500.ojn");
        string cust2500 = Path.Combine(customDir, "o2ma2500.ojn");
        string cust3500 = Path.Combine(customDir, "o2ma3500.ojn");
        File.Copy(baseSongs[0].FilePath, cust1500, true);
        OjnParser.UpdateSongId(cust1500, 1500, "o2ma1500.ojm");
        File.Copy(baseSongs[0].FilePath, cust2500, true);
        OjnParser.UpdateSongId(cust2500, 2500, "o2ma2500.ojm");
        File.Copy(baseSongs[0].FilePath, cust3500, true);
        OjnParser.UpdateSongId(cust3500, 3500, "o2ma3500.ojm");

        var fm = new FileManager();
        fm.SetMusicDir(customDir);
        var (allSongs, _) = fm.ScanMusicDir();
        Assert.AreEqual(5, allSongs.Count);

        // Execute ReindexCustomSongs
        var (reindexedCount, startId, endId, _) = fm.ReindexCustomSongs(allSongs);
        Assert.AreEqual(3, reindexedCount);
        Assert.AreEqual(1000, startId);
        Assert.AreEqual(1002, endId);

        var (afterSongs, _) = fm.ScanMusicDir();
        Assert.AreEqual(5, afterSongs.Count);

        // Condition a: Original songs 101 and 102 are 100% UNTOUCHED!
        Assert.AreEqual(101, afterSongs[0].SongId);
        Assert.AreEqual("o2ma101.ojn", afterSongs[0].FileName);
        Assert.IsTrue(File.Exists(orig101));

        Assert.AreEqual(102, afterSongs[1].SongId);
        Assert.AreEqual("o2ma102.ojn", afterSongs[1].FileName);
        Assert.IsTrue(File.Exists(orig102));

        // Condition b & c: Custom songs reindexed to 1000, 1001, 1002
        Assert.AreEqual(1000, afterSongs[2].SongId);
        Assert.AreEqual("o2ma1000.ojn", afterSongs[2].FileName);

        Assert.AreEqual(1001, afterSongs[3].SongId);
        Assert.AreEqual("o2ma1001.ojn", afterSongs[3].FileName);

        Assert.AreEqual(1002, afterSongs[4].SongId);
        Assert.AreEqual("o2ma1002.ojn", afterSongs[4].FileName);
    }

    [TestMethod]
    public void TestM30DecoderWithNamiEncryption()
    {
        string ojmPath = Path.Combine(_tempDir, "test_encrypted_nami.ojm");

        // Create dummy audio payload starting with OggS
        byte[] expectedOgg = new byte[800];
        expectedOgg[0] = 0x4F; expectedOgg[1] = 0x67; expectedOgg[2] = 0x67; expectedOgg[3] = 0x53; // "OggS"
        for (int i = 4; i < expectedOgg.Length; i++) expectedOgg[i] = (byte)(i % 256);

        // Encrypt with 'nami' (flag 16)
        byte[] namiKey = [0x6E, 0x61, 0x6D, 0x69];
        byte[] encryptedAudio = (byte[])expectedOgg.Clone();
        int fullBlocks = (encryptedAudio.Length / 4) * 4;
        for (int j = 0; j < fullBlocks; j += 4)
        {
            encryptedAudio[j] ^= namiKey[0];
            encryptedAudio[j + 1] ^= namiKey[1];
            encryptedAudio[j + 2] ^= namiKey[2];
            encryptedAudio[j + 3] ^= namiKey[3];
        }

        // Build M30 file
        using (var fs = File.Create(ojmPath))
        using (var writer = new BinaryWriter(fs))
        {
            // M30 Header (28 bytes)
            writer.Write((byte)'M'); writer.Write((byte)'3'); writer.Write((byte)'0'); writer.Write((byte)0); // 0x00
            writer.Write(1);        // 0x04 format version
            writer.Write(16);       // 0x08 encryption_flag = 16 (XOR 'nami')
            writer.Write(2);        // 0x0C sample_count = 2
            writer.Write(28);       // 0x10 samples_offset = 28
            writer.Write(1000);     // 0x14 payload_size
            writer.Write(0);        // 0x18 padding (to reach 28 bytes = 0x1C)

            // Sample 1: small sound effect (100 bytes)
            byte[] s1Name = new byte[32];
            writer.Write(s1Name);
            writer.Write(100);      // sample_size
            writer.Write(new byte[16]); // metadata
            writer.Write(new byte[100]); // 100 bytes data

            // Sample 2: main BGM track (800 bytes)
            byte[] s2Name = new byte[32];
            Encoding.ASCII.GetBytes("bgm.ogg").CopyTo(s2Name, 0);
            writer.Write(s2Name);
            writer.Write(encryptedAudio.Length);
            writer.Write(new byte[16]);
            writer.Write(encryptedAudio);
        }

        // Test decoder
        byte[]? previewAudio = OjmDecoder.ExtractPreviewAudio(ojmPath);
        Assert.IsNotNull(previewAudio);
        Assert.AreEqual(expectedOgg.Length, previewAudio.Length);
        CollectionAssert.AreEqual(expectedOgg, previewAudio);
    }

    [TestMethod]
    public void TestOmcContainerParser()
    {
        string omcPath = Path.Combine(_tempDir, "test_omc.ojm");

        byte[] dummyWavPcm = new byte[800];
        for (int i = 0; i < dummyWavPcm.Length; i++) dummyWavPcm[i] = (byte)(i % 128);

        using (var fs = File.Create(omcPath))
        using (var writer = new BinaryWriter(fs))
        {
            // OMC Header (20 bytes)
            writer.Write((byte)'O'); writer.Write((byte)'M'); writer.Write((byte)'C'); writer.Write((byte)0); // 0x00
            writer.Write((short)1);   // wav_count = 1
            writer.Write((short)0);   // ogg_count = 0
            writer.Write(20);         // wav_start = 20
            writer.Write(20 + 56 + dummyWavPcm.Length); // ogg_start
            writer.Write(20 + 56 + dummyWavPcm.Length); // filesize

            // WAV Sample Header (56 bytes)
            byte[] sName = new byte[32];
            Encoding.ASCII.GetBytes("W01.wav").CopyTo(sName, 0);
            writer.Write(sName);
            writer.Write((short)1);     // audio_format = PCM
            writer.Write((short)1);     // channels = mono
            writer.Write(44100);        // sample_rate
            writer.Write(44100 * 2);    // bit_rate
            writer.Write((short)2);     // block_align
            writer.Write((short)16);    // bits_per_sample
            writer.Write(0);            // unk_data
            writer.Write(dummyWavPcm.Length); // chunk_size

            // Raw PCM data
            writer.Write(dummyWavPcm);
        }

        byte[] fileBytes = File.ReadAllBytes(omcPath);
        var container = OjmDecoder.ParseOjm(fileBytes);

        Assert.AreEqual("OMC", container.Format);
        Assert.AreEqual(1, container.Samples.Count);
        Assert.AreEqual(1001, container.Samples[0].SampleId); // W01 -> 1 + 1000 = 1001
        Assert.IsTrue(container.Samples[0].AudioData.Length > dummyWavPcm.Length); // Wrapped in RIFF/WAVE header
        Assert.AreEqual('R', (char)container.Samples[0].AudioData[0]);
        Assert.AreEqual('I', (char)container.Samples[0].AudioData[1]);
        Assert.AreEqual('F', (char)container.Samples[0].AudioData[2]);
        Assert.AreEqual('F', (char)container.Samples[0].AudioData[3]);
    }

    [TestMethod]
    public void TestUniversalScannerFallback()
    {
        // Construct garbage data containing an embedded RIFF chunk
        byte[] dummyPcm = new byte[200];
        byte[] embeddedWav = OjmDecoder.WrapPcmInWav(dummyPcm, 1, 44100, 16);

        byte[] corruptedData = new byte[1000];
        for (int i = 0; i < corruptedData.Length; i++) corruptedData[i] = 0xAA;

        // Place the embedded WAV at offset 150
        Array.Copy(embeddedWav, 0, corruptedData, 150, embeddedWav.Length);

        var container = OjmDecoder.ParseOjm(corruptedData);
        Assert.IsTrue(container.Format.Contains("SCANNER"));
        Assert.IsTrue(container.Samples.Count >= 1);
        Assert.AreEqual(embeddedWav.Length, container.Samples[0].AudioData.Length);
    }

    [TestMethod]
    public void TestSongMixerFullReconstitutionAndSeek()
    {
        string ojnPath = Path.Combine(_musicDir, "o2ma101.ojn");
        var song = OjnParser.ReadSong(ojnPath);

        // Extract pure BGM track in RAM
        var bgmResult = SongMixer.MixFullSong(song);
        Assert.IsNotNull(bgmResult);
        Assert.IsNotNull(bgmResult.Provider);
        Assert.IsTrue(bgmResult.DurationSeconds > 0.5, $"Duration should be positive, got {bgmResult.DurationSeconds}");
        Assert.AreEqual("M30", bgmResult.ContainerFormat);

        // Test instantaneous seeking in memory
        bgmResult.Provider.CurrentSeconds = 0.5;
        Assert.AreEqual(0.5, bgmResult.Provider.CurrentSeconds, 0.05);

        // Read audio samples at seek point
        float[] buffer = new float[1024];
        int read = bgmResult.Provider.Read(buffer, 0, buffer.Length);
        Assert.AreEqual(1024, read);
    }

    [TestMethod]
    public void TestCasBMultiSegmentReconstructionAndPeakNormalization()
    {
        // Test normalization to 0.98
        float[] left = new float[100];
        float[] right = new float[100];
        for (int i = 0; i < 100; i++)
        {
            left[i] = 0.5f;
            right[i] = 0.5f;
        }
        SongMixer.NormalizeTo098(left, right);

        float maxPeak = 0.0f;
        for (int i = 0; i < 100; i++)
        {
            if (Math.Abs(left[i]) > maxPeak) maxPeak = Math.Abs(left[i]);
            if (Math.Abs(right[i]) > maxPeak) maxPeak = Math.Abs(right[i]);
        }
        Assert.AreEqual(0.98f, maxPeak, 0.001f);
    }

    [TestMethod]
    public void TestM30DecoderWith0412Encryption()
    {
        string ojmPath = Path.Combine(_tempDir, "test_encrypted_0412.ojm");

        byte[] expectedOgg = new byte[500];
        expectedOgg[0] = 0x4F; expectedOgg[1] = 0x67; expectedOgg[2] = 0x67; expectedOgg[3] = 0x53; // "OggS"
        for (int i = 4; i < expectedOgg.Length; i++) expectedOgg[i] = (byte)(i % 256);

        // Encrypt with '0412' (flag 32)
        byte[] key0412 = [0x30, 0x34, 0x31, 0x32];
        byte[] encryptedAudio = (byte[])expectedOgg.Clone();
        int fullBlocks = (encryptedAudio.Length / 4) * 4;
        for (int j = 0; j < fullBlocks; j += 4)
        {
            encryptedAudio[j] ^= key0412[0];
            encryptedAudio[j + 1] ^= key0412[1];
            encryptedAudio[j + 2] ^= key0412[2];
            encryptedAudio[j + 3] ^= key0412[3];
        }

        using (var fs = File.Create(ojmPath))
        using (var writer = new BinaryWriter(fs))
        {
            // M30 Header (28 bytes)
            writer.Write((byte)'M'); writer.Write((byte)'3'); writer.Write((byte)'0'); writer.Write((byte)0); // 0x00
            writer.Write(1);        // 0x04 format version
            writer.Write(32);       // 0x08 encryption_flag = 32 (XOR '0412')
            writer.Write(1);        // 0x0C sample_count = 1
            writer.Write(28);       // 0x10 samples_offset = 28
            writer.Write(500);      // 0x14 payload_size
            writer.Write(0);        // 0x18 padding (to reach 28 bytes = 0x1C)

            // Sample: main BGM
            byte[] sName = new byte[32];
            Encoding.ASCII.GetBytes("main.ogg").CopyTo(sName, 0);
            writer.Write(sName);
            writer.Write(encryptedAudio.Length);
            writer.Write(new byte[16]);
            writer.Write(encryptedAudio);
        }

        byte[]? previewAudio = OjmDecoder.ExtractPreviewAudio(ojmPath);
        Assert.IsNotNull(previewAudio);
        Assert.AreEqual(expectedOgg.Length, previewAudio.Length);
        CollectionAssert.AreEqual(expectedOgg, previewAudio);
    }

    [TestMethod]
    public void TestInitialVolumeFiftyPercent()
    {
        using var player = new AudioPlayer();
        Assert.AreEqual(0.50f, player.Volume, 0.001f);
    }

    [TestMethod]
    public void TestSeparateOjnAndOjmFileSizeDisplays()
    {
        var song = new Song
        {
            SongId = 100,
            FileSize = 512 * 1024, // 512 KB
            OjmExists = true,
            OjmFileSize = 2 * 1024 * 1024 // 2 MB
        };

        Assert.IsTrue(song.OjnFileSizeDisplay.Contains("KB") || song.OjnFileSizeDisplay.Contains("512"));
        Assert.IsTrue(song.OjmFileSizeDisplay.Contains("MB") || song.OjmFileSizeDisplay.Contains("2"));

        song.OjmExists = false;
        Assert.AreEqual("Missing", song.OjmFileSizeDisplay);
        Assert.AreEqual("Missing", song.OjmStatusDisplay);

        song.OjmExists = true;
        Assert.AreEqual("Present", song.OjmStatusDisplay);
    }

    [TestMethod]
    public void TestLocalizationServiceDynamicSwitching()
    {
        var loc = LocalizationService.Instance;
        bool notified = false;
        loc.LanguageChanged += () => notified = true;

        loc.CurrentLanguage = "FR";
        notified = false;
        loc.CurrentLanguage = "EN";
        Assert.IsTrue(notified);
        Assert.AreEqual("Browse", loc["BrowseFolder"].Replace("📁", "").Trim());
        Assert.AreEqual(".OJN File Size:", loc["LblTechSizeOjn"]);
        Assert.AreEqual(".OJM File Size:", loc["LblTechSizeOjm"]);

        notified = false;
        loc.CurrentLanguage = "JA";
        Assert.IsTrue(notified);
        Assert.AreEqual("参照", loc["BrowseFolder"].Replace("📁", "").Trim());
        Assert.AreEqual(".OJN ファイルサイズ :", loc["LblTechSizeOjn"]);
        Assert.AreEqual(".OJM ファイルサイズ :", loc["LblTechSizeOjm"]);

        // Reset to default
        loc.CurrentLanguage = "FR";
        Assert.AreEqual("Parcourir", loc["BrowseFolder"].Replace("📁", "").Trim());
        Assert.AreEqual("Taille fichier .OJN :", loc["LblTechSizeOjn"]);
        Assert.AreEqual("Taille fichier .OJM :", loc["LblTechSizeOjm"]);
    }

    private static void CreateDummyImage(string outputPath, int width, int height)
    {
        var rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen())
        {
            dc.DrawRectangle(Brushes.DarkViolet, null, new System.Windows.Rect(0, 0, width, height));
            dc.DrawRectangle(Brushes.DeepPink, null, new System.Windows.Rect(20, 20, width - 40, height - 40));
        }
        rtb.Render(dv);

        var encoder = new JpegBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(rtb));
        using var fs = File.Create(outputPath);
        encoder.Save(fs);
    }
    [TestMethod]
    public void TestGenerateOjnList()
    {
        string siblingImage = Path.Combine(_tempDir, "Image");
        Directory.CreateDirectory(siblingImage);

        string musicDat = Path.Combine(_musicDir, "OJNList.dat");
        var (count, targetPath) = OjnListGenerator.Generate(_musicDir, musicDat);
        Assert.IsTrue(count > 0);
        Assert.AreEqual(musicDat, targetPath);
        Assert.IsTrue(File.Exists(musicDat));

        byte[] musicBytes = File.ReadAllBytes(musicDat);

        int parsedCount = BitConverter.ToInt32(musicBytes, 0);
        Assert.AreEqual(count, parsedCount);

        // Verify that songs are sorted by ID
        int lastId = -1;
        for (int i = 0; i < parsedCount; i++)
        {
            int sid = BitConverter.ToInt32(musicBytes, 4 + i * Constants.HeaderSize);
            Assert.IsTrue(sid >= lastId);
            lastId = sid;
        }
    }
    [TestMethod]
    public void TestPluralFormatting()
    {
        var vm = new O2JamManager.ViewModels.MainViewModel();
        Assert.AreEqual("0 songs detected", vm.TotalCountText);
        Assert.AreEqual("OJM: 0 present, 0 missing", vm.OjmStatsText);

        vm.Songs.Add(new Song { SongId = 1, Title = "Test 1", OjmExists = true });
        Assert.AreEqual("1 song detected", vm.TotalCountText);
        Assert.AreEqual("OJM: 1 present, 0 missing", vm.OjmStatsText);

        vm.Songs.Add(new Song { SongId = 2, Title = "Test 2", OjmExists = false });
        Assert.AreEqual("2 songs detected", vm.TotalCountText);
        Assert.AreEqual("OJM: 1 present, 1 missing", vm.OjmStatsText);

        vm.Songs.Add(new Song { SongId = 3, Title = "Test 3", OjmExists = false });
        Assert.AreEqual("3 songs detected", vm.TotalCountText);
        Assert.AreEqual("OJM: 1 present, 2 missing", vm.OjmStatsText);
    }

    [TestMethod]
    public void TestLocalizationParityAndEnglish()
    {
        var frKeys = LocalizationService.Translations["FR"].Keys;
        var enDict = LocalizationService.Translations["EN"];
        var jaDict = LocalizationService.Translations["JA"];

        foreach (var key in frKeys)
        {
            Assert.IsTrue(enDict.ContainsKey(key), $"Missing EN translation key: {key}");
            Assert.IsFalse(string.IsNullOrWhiteSpace(enDict[key]), $"Empty EN translation for: {key}");

            Assert.IsTrue(jaDict.ContainsKey(key), $"Missing JA translation key: {key}");
            Assert.IsFalse(string.IsNullOrWhiteSpace(jaDict[key]), $"Empty JA translation for: {key}");
        }

        // Test English VM strings
        var loc = LocalizationService.Instance;
        loc.CurrentLanguage = "EN";

        var vm = new O2JamManager.ViewModels.MainViewModel();
        Assert.AreEqual("0 songs detected", vm.TotalCountText);
        Assert.AreEqual("OJM: 0 present, 0 missing", vm.OjmStatsText);

        vm.Songs.Add(new Song { SongId = 1, Title = "Test 1", OjmExists = true });
        Assert.AreEqual("1 song detected", vm.TotalCountText);
        Assert.AreEqual("OJM: 1 present, 0 missing", vm.OjmStatsText);

        vm.Songs.Add(new Song { SongId = 2, Title = "Test 2", OjmExists = false });
        Assert.AreEqual("2 songs detected", vm.TotalCountText);
        Assert.AreEqual("OJM: 1 present, 1 missing", vm.OjmStatsText);

        Assert.AreEqual(".OJN FILE", vm.ColOjnFile);
        Assert.AreEqual(".OJM FILE", vm.ColOjmFile);
        Assert.AreEqual(".OJM STATUS", vm.ColOjmStatus);
        Assert.AreEqual("Ready to play", loc["AudioReady"]);
        Assert.AreEqual("Confirm deletion", loc["DeleteConfirmTitle"]);

        // Restore French
        loc.CurrentLanguage = "FR";
    }

    [TestMethod]
    public void TestObservableRangeCollectionReplaceAll()
    {
        var col = new ObservableRangeCollection<Song>();
        int resetCount = 0;
        int addCount = 0;

        col.CollectionChanged += (s, e) =>
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
                resetCount++;
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
                addCount++;
        };

        var batch = new List<Song>
        {
            new Song { SongId = 1, Title = "Song 1" },
            new Song { SongId = 2, Title = "Song 2" },
            new Song { SongId = 3, Title = "Song 3" }
        };

        col.ReplaceAll(batch);

        Assert.AreEqual(3, col.Count);
        Assert.AreEqual(1, resetCount, "ReplaceAll must trigger exactly 1 Reset event");
        Assert.AreEqual(0, addCount, "ReplaceAll must not trigger per-item Add events");
    }

    [TestMethod]
    public void TestReindexSongsDiskOptimization()
    {
        var fm = new FileManager(); fm.SetMusicDir(_musicDir);
        var initial = fm.ScanMusicDir().Songs;
        Assert.IsTrue(initial.Count > 0);

        // Record last write times
        var writeTimes = initial.ToDictionary(s => s.FilePath, s => File.GetLastWriteTimeUtc(s.FilePath));

        int progressCalls = 0;
        // Reindex with existing order and IDs (all match)
        var result = fm.ReindexSongs(initial, initial[0].SongId, 1, (cur, tot) => progressCalls++);

        // Check that write times are unchanged (no disk write performed)
        foreach (var s in result)
        {
            if (writeTimes.TryGetValue(s.FilePath, out var oldTime))
            {
                Assert.AreEqual(oldTime, File.GetLastWriteTimeUtc(s.FilePath), $"File {s.FilePath} was written when it should have been skipped.");
            }
        }
    }
    [TestMethod]
    public void TestSongPocoCloneAndReplaceInLists()
    {
        // 1. Song must remain a pure lightweight POCO without INotifyPropertyChanged
        Assert.IsFalse(typeof(System.ComponentModel.INotifyPropertyChanged).IsAssignableFrom(typeof(Song)));

        var song = new Song
        {
            SongId = 1000,
            FilePath = Path.Combine(_musicDir, "o2ma1000.ojn"),
            FileName = "o2ma1000.ojn",
            OjmFile = "o2ma1000.ojm",
            Title = "Original Title",
            Artist = "Original Artist",
            Bpm = 140.0f
        };

        // 2. Clone creates a discrete new instance
        var clone = song.Clone();
        Assert.AreNotSame(song, clone);
        Assert.AreEqual(song.SongId, clone.SongId);
        Assert.AreEqual(song.Title, clone.Title);
        Assert.AreEqual(song.BpmDisplay, clone.BpmDisplay);

        // 3. Test ReplaceSongInLists on ViewModel
        var vm = new O2JamManager.ViewModels.MainViewModel();
        vm.Songs.Add(song);
        vm.FilteredSongs.Add(song);
        vm.SelectedSong = song;

        clone.SongId = 2000;
        clone.Title = "Updated Title";
        vm.ReplaceSongInLists(song, clone);

        Assert.AreEqual(clone, vm.SelectedSong);
        Assert.AreEqual(2000, vm.FilteredSongs[0].SongId);
        Assert.AreEqual("Updated Title", vm.FilteredSongs[0].Title);
        Assert.AreEqual(2000, vm.Songs[0].SongId);
        Assert.AreEqual("Updated Title", vm.Songs[0].Title);
    }
}
