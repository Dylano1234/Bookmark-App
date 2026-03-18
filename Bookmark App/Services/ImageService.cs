using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

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
            using var image = Image.Load(inputStream);

            // Check if resizing is necessary
            if (image.Width <= maxWidth && image.Height <= maxHeight)
            {
                // No rescale needed → keep original bytes 
                return originalBytes;
            }

            double scale = Math.Min(
                (double)maxWidth / image.Width,
                (double)maxHeight / image.Height);

            int newWidth = (int)Math.Round(image.Width * scale);
            int newHeight = (int)Math.Round(image.Height * scale);

            image.Mutate(x => x.Resize(
                newWidth,
                newHeight,
                KnownResamplers.Lanczos3));

            using var outputStream = new MemoryStream();
            image.SaveAsJpeg(outputStream, new JpegEncoder
            {
                Quality = jpegQuality
            });

            return outputStream.ToArray();
        }
    }
}
