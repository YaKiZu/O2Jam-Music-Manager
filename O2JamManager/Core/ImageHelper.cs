using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace O2JamManager.Core;

public static class ImageHelper
{
    /// <summary>
    /// Loads a byte array into a frozen WPF BitmapImage.
    /// </summary>
        public static BitmapImage? LoadBitmapFromBytes(byte[]? bytes)
    {
        if (bytes == null || bytes.Length == 0)
            return null;

        try
        {
            var image = new BitmapImage();
            using (var stream = new MemoryStream(bytes))
            {
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = stream;
                image.EndInit();
            }
            image.Freeze();
            return image;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Creates a sharp, high quality thumbnail BitmapImage from any BitmapSource using HighQuality scaling.
    /// </summary>
        public static BitmapImage? CreateHighQualityThumbnail(BitmapSource? source, int targetWidth, int targetHeight)
    {
        if (source == null) return null;

        try
        {
            var cropped = CreateResizedCenterCrop(source, targetWidth, targetHeight);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(cropped));

            using var ms = new MemoryStream();
            encoder.Save(ms);
            ms.Position = 0;

            var result = new BitmapImage();
            result.BeginInit();
            result.CacheOption = BitmapCacheOption.OnLoad;
            result.StreamSource = ms;
            result.EndInit();
            result.Freeze();
            return result;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Center-crops and scales any source image to exact targetWidth x targetHeight using HighQuality interpolation.
    /// Preserves natural aspect ratio without stretching, completely filling the target area.
    /// </summary>
    public static BitmapSource CreateResizedCenterCrop(BitmapSource source, int targetWidth, int targetHeight)
    {
        double scale = Math.Max((double)targetWidth / source.PixelWidth, (double)targetHeight / source.PixelHeight);
        int scaledW = Math.Max(targetWidth, (int)Math.Round(source.PixelWidth * scale));
        int scaledH = Math.Max(targetHeight, (int)Math.Round(source.PixelHeight * scale));

        // 1. High-quality scale
        var group = new DrawingGroup();
        RenderOptions.SetBitmapScalingMode(group, BitmapScalingMode.HighQuality);
        using (var dc = group.Open())
        {
            dc.DrawImage(source, new System.Windows.Rect(0, 0, scaledW, scaledH));
        }

        var rtb = new RenderTargetBitmap(scaledW, scaledH, 96, 96, PixelFormats.Pbgra32);
        var dv = new DrawingVisual();
        using (var ctx = dv.RenderOpen())
        {
            ctx.DrawDrawing(group);
        }
        rtb.Render(dv);

        // 2. Center crop to exact target rectangle
        int cropX = Math.Max(0, (scaledW - targetWidth) / 2);
        int cropY = Math.Max(0, (scaledH - targetHeight) / 2);

        if (cropX + targetWidth > rtb.PixelWidth) cropX = Math.Max(0, rtb.PixelWidth - targetWidth);
        if (cropY + targetHeight > rtb.PixelHeight) cropY = Math.Max(0, rtb.PixelHeight - targetHeight);

        return new CroppedBitmap(rtb, new System.Windows.Int32Rect(cropX, cropY, targetWidth, targetHeight));
    }

    /// <summary>
    /// Encodes any BitmapSource into the standard native O2Jam cover JPEG (800x600, Quality 95, Baseline RGB).
    /// </summary>
    public static byte[] EncodeToOjnCoverJpg(BitmapSource source)
    {
        BitmapSource cropped = CreateResizedCenterCrop(source, Constants.CoverStandardWidth, Constants.CoverStandardHeight);
        var bgr24 = new FormatConvertedBitmap(cropped, PixelFormats.Bgr24, null, 0);

        using var ms = new MemoryStream();
        var encoder = new JpegBitmapEncoder
        {
            QualityLevel = 95
        };
        encoder.Frames.Add(BitmapFrame.Create(bgr24));
        encoder.Save(ms);
        return ms.ToArray();
    }

    /// <summary>
    /// Encodes any BitmapSource into the standard native O2Jam thumbnail BMP (80x80, 24bpp uncompressed, exactly 19254 bytes).
    /// Matches original O2Jam songs identically for 1:1 crisp display in the in-game lobby track selector.
    /// </summary>
    public static byte[] EncodeToOjnThumbnailBmp(BitmapSource source)
    {
        int width = Constants.ThumbnailStandardWidth;   // 80
        int height = Constants.ThumbnailStandardHeight; // 80

        BitmapSource cropped = CreateResizedCenterCrop(source, width, height);
        var bgr24 = new FormatConvertedBitmap(cropped, PixelFormats.Bgr24, null, 0);

        int stride = width * 3; // 240 bytes (already multiple of 4, no pad bytes needed)
        byte[] pixelBuffer = new byte[height * stride]; // 19200 bytes
        bgr24.CopyPixels(pixelBuffer, stride, 0);

        int totalFileSize = 54 + pixelBuffer.Length; // 19254 bytes
        byte[] bmpBytes = new byte[totalFileSize];

        using (var msBmp = new MemoryStream(bmpBytes))
        using (var writer = new BinaryWriter(msBmp))
        {
            // BITMAPFILEHEADER (14 bytes)
            writer.Write((byte)'B');
            writer.Write((byte)'M');
            writer.Write((int)totalFileSize);   // 19254
            writer.Write((short)0);             // bfReserved1
            writer.Write((short)0);             // bfReserved2
            writer.Write((int)54);               // bfOffBits (14 + 40)

            // BITMAPINFOHEADER (40 bytes)
            writer.Write((int)40);               // biSize
            writer.Write((int)width);            // 80
            writer.Write((int)height);           // 80 (positive = bottom-up)
            writer.Write((short)1);              // biPlanes
            writer.Write((short)24);             // biBitCount (24 bpp BGR)
            writer.Write((int)0);                // biCompression (BI_RGB uncompressed)
            writer.Write((int)pixelBuffer.Length);// biSizeImage (19200)
            writer.Write((int)2835);             // biXPelsPerMeter (72 DPI)
            writer.Write((int)2835);             // biYPelsPerMeter (72 DPI)
            writer.Write((int)0);                // biClrUsed
            writer.Write((int)0);                // biClrImportant

            // Raw BGR pixel data: Windows BMP stores scanlines bottom-up (row 79 down to 0)
            for (int row = height - 1; row >= 0; row--)
            {
                writer.Write(pixelBuffer, row * stride, stride);
            }
        }

        return bmpBytes;
    }

    /// <summary>
    /// Converts any external image (PNG, JPG, BMP, WEBP, etc.) into native O2Jam assets:
    /// 1. 800x600 high quality baseline JPG cover (loading & in-game background)
    /// 2. 80x80 uncompressed 24bpp BMP thumbnail (19254 bytes, lobby song selection box)
    /// </summary>
    public static (byte[] JpgBytes, byte[] BmpBytes) ConvertImageToOjnAssets(string imagePath)
    {
        using var stream = File.OpenRead(imagePath);
        var decoder = BitmapDecoder.Create(
            stream,
            BitmapCreateOptions.PreservePixelFormat,
            BitmapCacheOption.OnLoad
        );
        BitmapSource source = decoder.Frames[0];

        byte[] jpgBytes = EncodeToOjnCoverJpg(source);
        byte[] bmpBytes = EncodeToOjnThumbnailBmp(source);

        return (jpgBytes, bmpBytes);
    }

    /// <summary>
    /// Converts any external image into a standalone 80x80 BMP thumbnail for O2Jam.
    /// </summary>
    public static byte[] ConvertImageToOjnThumbnail(string imagePath)
    {
        using var stream = File.OpenRead(imagePath);
        var decoder = BitmapDecoder.Create(
            stream,
            BitmapCreateOptions.PreservePixelFormat,
            BitmapCacheOption.OnLoad
        );
        BitmapSource source = decoder.Frames[0];
        return EncodeToOjnThumbnailBmp(source);
    }

    /// <summary>
    /// Converts any external image into a standalone 800x600 JPG cover for O2Jam.
    /// </summary>
    public static byte[] ConvertImageToOjnCover(string imagePath)
    {
        using var stream = File.OpenRead(imagePath);
        var decoder = BitmapDecoder.Create(
            stream,
            BitmapCreateOptions.PreservePixelFormat,
            BitmapCacheOption.OnLoad
        );
        BitmapSource source = decoder.Frames[0];
        return EncodeToOjnCoverJpg(source);
    }
}
