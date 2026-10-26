namespace O2JamManager.Core;

public static class Constants
{
    public const int HeaderSize = 300;
    public static readonly byte[] OjnSignature = [0x6F, 0x6A, 0x6E, 0x00]; // "ojn\0"

    // Byte offsets in .ojn file
    public const int OffsetSongId = 0x00;          // int32 (4 bytes)
    public const int OffsetSignature = 0x04;       // char[4] (4 bytes)
    public const int OffsetEncodeVersion = 0x08;   // float (4 bytes)
    public const int OffsetGenre = 0x0C;           // int32 (4 bytes)
    public const int OffsetBpm = 0x10;             // float (4 bytes)
    public const int OffsetLevel = 0x14;           // int16[4] (8 bytes) [Ex, Nx, Hx, unused]
    public const int OffsetEventCount = 0x1C;      // int32[3] (12 bytes) [Ex, Nx, Hx]
    public const int OffsetNoteCount = 0x28;       // int32[3] (12 bytes) [Ex, Nx, Hx]
    public const int OffsetMeasureCount = 0x34;    // int32[3] (12 bytes) [Ex, Nx, Hx]
    public const int OffsetPackageCount = 0x40;    // int32[3] (12 bytes) [Ex, Nx, Hx]
    public const int OffsetOldEncodeVersion = 0x4C;// int16 (2 bytes)
    public const int OffsetOldSongId = 0x4E;       // int16 (2 bytes)
    public const int OffsetOldGenre = 0x50;        // char[20] (20 bytes)
    public const int OffsetBmpSize = 0x64;         // int32 (4 bytes)
    public const int OffsetOldFileVersion = 0x68;  // int32 (4 bytes)
    public const int OffsetTitle = 0x6C;           // char[64] (64 bytes)
    public const int OffsetArtist = 0xAC;          // char[32] (32 bytes)
    public const int OffsetNoter = 0xCC;           // char[32] (32 bytes)
    public const int OffsetOjmFile = 0xEC;         // char[32] (32 bytes)
    public const int OffsetCoverSize = 0x10C;      // int32 (4 bytes)
    public const int OffsetTime = 0x110;           // int32[3] (12 bytes) [Ex, Nx, Hx]
    public const int OffsetNoteOffset = 0x11C;     // int32[4] (16 bytes) [Ex, Nx, Hx, CoverOffset]
    public const int OffsetCoverOffset = 0x128;    // int32 (4 bytes) = note_offset[3]

    public const int LenTitle = 64;
    public const int LenArtist = 32;
    public const int LenNoter = 32;
    public const int LenOjmFile = 32;

    public const int CoverStandardWidth = 800;
    public const int CoverStandardHeight = 600;
    public const int ThumbnailStandardWidth = 80;
    public const int ThumbnailStandardHeight = 80;
    public const int StandardBmp80x80Size = 19254;
    public const int ThumbnailWidth = 80;
    public const int ThumbnailHeight = 80;
    public const int StandardBmp8x8Size = 246; // 14 file header + 40 info header + 192 pixel data

    public static readonly string[] Genres =
    [
        "Ballad",       // 0
        "Rock",         // 1
        "Dance",        // 2
        "Techno",       // 3
        "Hip-hop",      // 4
        "Soul/R&B",     // 5
        "Jazz",         // 6
        "Funk",         // 7
        "Classical",    // 8
        "Traditional",  // 9
        "Etc"           // 10
    ];

    public static string GetGenreName(int id)
    {
        if (id >= 0 && id < Genres.Length)
            return Genres[id];
        return $"Genre {id}";
    }
}
