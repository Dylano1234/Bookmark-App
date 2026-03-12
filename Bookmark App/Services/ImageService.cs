using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Bookmark_App.Services
{
    public static class ImageService
    {
        public static byte[] ResizeImage(
        byte[] originalBytes,
        int maxWidth,
        int maxHeight,
        int jpegQuality = 90)
        {
            using var inputStream = new MemoryStream(originalBytes);

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = inputStream;
            bitmap.EndInit();
            bitmap.Freeze();

            // Check of resizen nodig is
            if (bitmap.PixelWidth <= maxWidth && bitmap.PixelHeight <= maxHeight)
            {
                // Geen rescale nodig → originele bytes behouden
                return originalBytes;
            }

            double scale = Math.Min(
                (double)maxWidth / bitmap.PixelWidth,
                (double)maxHeight / bitmap.PixelHeight);

            int newWidth = (int)Math.Round(bitmap.PixelWidth * scale);
            int newHeight = (int)Math.Round(bitmap.PixelHeight * scale);

            var drawingVisual = new DrawingVisual();
            RenderOptions.SetBitmapScalingMode(drawingVisual, BitmapScalingMode.Fant);
            using (var dc = drawingVisual.RenderOpen())
            {
                dc.DrawImage(bitmap, new Rect(0, 0, newWidth, newHeight));
            }

            var resizedBitmap = new RenderTargetBitmap(
                newWidth,
                newHeight,
                96,
                96,
                PixelFormats.Pbgra32);

            resizedBitmap.Render(drawingVisual);

            var encoder = new JpegBitmapEncoder
            {
                QualityLevel = jpegQuality
            };

            encoder.Frames.Add(BitmapFrame.Create(resizedBitmap));

            using var outputStream = new MemoryStream();
            encoder.Save(outputStream);

            return outputStream.ToArray();
        }

    }
}
