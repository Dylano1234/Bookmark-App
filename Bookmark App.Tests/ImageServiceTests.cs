using Bookmark_App.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Media.Imaging;
using Xunit;

namespace Bookmark_App.Tests
{
    public class ImageServiceTests
    {
        #region Helper Methods
        /// <summary>
        /// Creates a valid test image with specified dimensions
        /// </summary>
        private byte[] CreateValidTestImage(int width, int height)
        {
            using var image = new Image<Rgba32>(width, height);

            // Fill with a solid color by directly setting all pixels
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    image[x, y] = SixLabors.ImageSharp.Color.Blue.ToPixel<Rgba32>();
                }
            }

            using var outputStream = new MemoryStream();
            image.SaveAsJpeg(outputStream, new JpegEncoder { Quality = 90 });

            return outputStream.ToArray();
        }
        #endregion

        #region ResizeImage Tests
        [Fact]
        public void ResizeImage_WithImageSmallerThanMax_ReturnsOriginalBytes()
        {
            // Arrange
            var originalImage = CreateValidTestImage(400, 400);

            // Act
            var result = ImageService.ResizeImage(originalImage, 600, 600, 95);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(originalImage, result);
        }

        [Fact]
        public void ResizeImage_WithImageLargerThanMax_ReturnsResizedImage()
        {
            // Arrange
            var originalImage = CreateValidTestImage(800, 800);

            // Act
            var result = ImageService.ResizeImage(originalImage, 600, 600, 95);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Length > 0);

            using var ms = new MemoryStream(result);

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = ms;
            bitmap.EndInit();

            Assert.Equal(600, bitmap.PixelWidth);
            Assert.Equal(600, bitmap.PixelHeight);
        }

        [Fact]
        public void ResizeImage_WithMaxQuality_ProducesOutput()
        {
            // Arrange
            var originalImage = CreateValidTestImage(800, 800);

            // Act
            var result = ImageService.ResizeImage(originalImage, 600, 600, 100);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Length > 0);
        }

        [Fact]
        public void ResizeImage_WithLowQuality_ProducesOutput()
        {
            // Arrange
            var originalImage = CreateValidTestImage(800, 800);

            // Act
            var result = ImageService.ResizeImage(originalImage, 600, 600, 50);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Length > 0);
        }

        [Fact]
        public void ResizeImage_WithDefaultQuality_ProducesOutput()
        {
            // Arrange
            var originalImage = CreateValidTestImage(800, 800);

            // Act
            var result = ImageService.ResizeImage(originalImage, 600, 600);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Length > 0);
        }

        [Fact]
        public void ResizeImage_WithRectangularImage_MaintainsAspectRatio()
        {
            // Arrange
            var originalImage = CreateValidTestImage(1200, 600);

            // Act
            var result = ImageService.ResizeImage(originalImage, 600, 600, 95);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Length > 0);

            using var ms = new MemoryStream(result);

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = ms;
            bitmap.EndInit();

            Assert.Equal(600, bitmap.PixelWidth);
            Assert.Equal(300, bitmap.PixelHeight);
        }

        [Fact]
        public void ResizeImage_WithImageExactlyAtMax_ReturnsOriginalBytes()
        {
            // Arrange
            var originalImage = CreateValidTestImage(600, 600);

            // Act
            var result = ImageService.ResizeImage(originalImage, 600, 600, 95);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(originalImage, result);
        }

        [Fact]
        public void ResizeImage_WithNullImage_ThrowsException()
        {
            // Arrange
            byte[] nullImage = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                ImageService.ResizeImage(nullImage, 600, 600, 95));
        }

        [Fact]
        public void ResizeImage_WithSmallImage_HandlesEdgeCase()
        {
            // Arrange
            var smallImage = CreateValidTestImage(10, 10);

            // Act
            var result = ImageService.ResizeImage(smallImage, 600, 600, 95);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(smallImage, result);
        }
        #endregion
    }
}
