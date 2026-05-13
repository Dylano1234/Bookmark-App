using Bookmark_App.DataAccess;
using Bookmark_App.Models;
using Bookmark_App.Services;
using Moq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows.Media.Imaging;
using Xunit;

namespace Bookmark_App.Tests
{
    public class ItemServiceTests
    {
        private readonly Mock<IItemRepository> _mockItemRepo;
        private readonly ItemService _service;

        public ItemServiceTests()
        {
            _mockItemRepo = new Mock<IItemRepository>();
            _service = new ItemService(_mockItemRepo.Object);
        }

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

        #region GetAllItemsByList Tests
        [Fact]
        public void GetAllItemsByList_ReturnsFilteredItems()
        {
            // Arrange
            var list = new List(1, "Movies", null);
            var items = new List<ListItem>
            {
                new ListItem { id = 1, title = "Item 1", status = ItemStatus.InProgress },
                new ListItem { id = 2, title = "Item 2", status = ItemStatus.Completed }
            };
            var genre = new Genre(1, "Action");
            _mockItemRepo.Setup(r => r.GetAllByList(list, genre, "title", "", ItemStatus.All, 10, 1))
                .Returns(items);

            // Act
            var result = _service.GetAllItemsByList(list, genre, "title", "", ItemStatus.All, 10, 1);

            // Assert
            Assert.Equal(2, result.Count);
            _mockItemRepo.Verify(r => r.GetAllByList(list, genre, "title", "", ItemStatus.All, 10, 1), Times.Once);
        }

        [Fact]
        public void GetAllItemsByList_WithEmptyList_ReturnsEmpty()
        {
            // Arrange
            var list = new List(1, "Movies", null);
            _mockItemRepo.Setup(r => r.GetAllByList(list, null, "title", "", ItemStatus.All, 10, 1))
                .Returns(new List<ListItem>());

            // Act
            var result = _service.GetAllItemsByList(list, null, "title", "", ItemStatus.All, 10, 1);

            // Assert
            Assert.Empty(result);
        }
        #endregion

        #region ValidateListItem Tests
        [Fact]
        public void ValidateListItem_WithEmptyTitle_ReturnsError()
        {
            // Arrange
            var item = new ListItem { title = "", genres = new ObservableCollection<Genre>() };

            // Act
            var result = _service.ValidateListItem(item);

            // Assert
            Assert.Contains("Title is required", result);
        }

        [Fact]
        public void ValidateListItem_WithNullTitle_ReturnsError()
        {
            // Arrange
            var item = new ListItem { title = null, genres = new ObservableCollection<Genre>() };

            // Act
            var result = _service.ValidateListItem(item);

            // Assert
            Assert.Contains("Title is required", result);
        }

        [Fact]
        public void ValidateListItem_WithValidTitle_NoTitleError()
        {
            // Arrange
            var item = new ListItem { title = "Valid Title", rating = 0, genres = new ObservableCollection<Genre>() };

            // Act
            var result = _service.ValidateListItem(item);

            // Assert
            Assert.DoesNotContain("Title is required", result);
        }

        [Fact]
        public void ValidateListItem_WithInvalidRating_ReturnsError()
        {
            // Arrange
            var item = new ListItem { title = "Valid", rating = 11, genres = new ObservableCollection<Genre>() };

            // Act
            var result = _service.ValidateListItem(item);

            // Assert
            Assert.Contains("Rating must either be 0 or between 1 and 10", result);
        }

        [Fact]
        public void ValidateListItem_WithRatingZero_IsValid()
        {
            // Arrange
            var item = new ListItem { title = "Valid", rating = 0, genres = new ObservableCollection<Genre>() };

            // Act
            var result = _service.ValidateListItem(item);

            // Assert
            Assert.DoesNotContain("Rating", result);
        }

        [Fact]
        public void ValidateListItem_WithRatingInRange_IsValid()
        {
            // Arrange
            var item = new ListItem { title = "Valid", rating = 5, genres = new ObservableCollection<Genre>() };

            // Act
            var result = _service.ValidateListItem(item);

            // Assert
            Assert.DoesNotContain("Rating", result);
        }

        [Fact]
        public void ValidateListItem_WithDuplicateGenres_ReturnsError()
        {
            // Arrange
            var genre = new Genre(1, "Action");
            var item = new ListItem
            {
                title = "Valid",
                rating = 5,
                genres = new ObservableCollection<Genre> { genre, genre }
            };

            // Act
            var result = _service.ValidateListItem(item);

            // Assert
            Assert.Contains("duplicate", result, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ValidateListItem_WithUniqueGenres_IsValid()
        {
            // Arrange
            var item = new ListItem
            {
                title = "Valid",
                rating = 5,
                genres = new ObservableCollection<Genre>
                {
                    new Genre(1, "Action"),
                    new Genre(2, "Drama")
                }
            };

            // Act
            var result = _service.ValidateListItem(item);

            // Assert
            Assert.DoesNotContain("duplicate", result, StringComparison.OrdinalIgnoreCase);
        }
        #endregion

        #region AddItem Tests
        [Fact]
        public void AddItem_WithValidItem_ReturnsSuccess()
        {
            // Arrange
            var item = new ListItem { title = "Valid Item", rating = 5, genres = new ObservableCollection<Genre>(), status = ItemStatus.Planning };
            _mockItemRepo.Setup(r => r.Insert(item, 1));

            // Act
            var result = _service.AddItem(item, 1);

            // Assert
            Assert.True(result.IsSuccess);
            _mockItemRepo.Verify(r => r.Insert(item, 1), Times.Once);
        }

        [Fact]
        public void AddItem_WithInvalidItem_ReturnsFailure()
        {
            // Arrange
            var item = new ListItem { title = "", rating = 0, genres = new ObservableCollection<Genre>(), status = ItemStatus.Planning };

            // Act
            var result = _service.AddItem(item, 1);

            // Assert
            Assert.False(result.IsSuccess);
            _mockItemRepo.Verify(r => r.Insert(It.IsAny<ListItem>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public void AddItem_WithCoverImage_ResizesImage()
        {
            // Arrange
            var testImage = CreateValidTestImage(2000, 2000);

            var item = new ListItem
            {
                title = "Valid Item",
                rating = 5,
                coverImage = testImage,
                genres = new ObservableCollection<Genre>(),
                status = ItemStatus.InProgress
            };

            ListItem? insertedItem = null;

            _mockItemRepo
                .Setup(r => r.Insert(It.IsAny<ListItem>(), 1))
                .Callback<ListItem, int>((i, _) => insertedItem = i);

            // Act
            var result = _service.AddItem(item, 1);

            // Assert
            Assert.True(result.IsSuccess);

            Assert.NotNull(insertedItem);
            Assert.NotNull(insertedItem.coverImage);

            using var originalMs = new MemoryStream(testImage);

            var originalBitmap = new BitmapImage();
            originalBitmap.BeginInit();
            originalBitmap.CacheOption = BitmapCacheOption.OnLoad;
            originalBitmap.StreamSource = originalMs;
            originalBitmap.EndInit();

            using var resizedMs = new MemoryStream(insertedItem.coverImage);

            var resizedBitmap = new BitmapImage();
            resizedBitmap.BeginInit();
            resizedBitmap.CacheOption = BitmapCacheOption.OnLoad;
            resizedBitmap.StreamSource = resizedMs;
            resizedBitmap.EndInit();

            Assert.True(resizedBitmap.PixelWidth < originalBitmap.PixelWidth);
            Assert.True(resizedBitmap.PixelHeight < originalBitmap.PixelHeight);

            _mockItemRepo.Verify(
                r => r.Insert(It.IsAny<ListItem>(), 1),
                Times.Once);
        }
        #endregion

        #region UpdateItem Tests
        [Fact]
        public void UpdateItem_WithValidItem_ReturnsSuccess()
        {
            // Arrange
            var item = new ListItem { id = 1, title = "Updated", rating = 7, genres = new ObservableCollection<Genre>(), status = ItemStatus.Completed };
            _mockItemRepo.Setup(r => r.Update(item));

            // Act
            var result = _service.UpdateItem(item);

            // Assert
            Assert.True(result.IsSuccess);
            _mockItemRepo.Verify(r => r.Update(item), Times.Once);
        }

        [Fact]
        public void UpdateItem_WithInvalidItem_ReturnsFailure()
        {
            // Arrange
            var item = new ListItem { id = 1, title = "", rating = 0, genres = new ObservableCollection<Genre>(), status = ItemStatus.Planning };

            // Act
            var result = _service.UpdateItem(item);

            // Assert
            Assert.False(result.IsSuccess);
            _mockItemRepo.Verify(r => r.Update(It.IsAny<ListItem>()), Times.Never);
        }

        [Fact]
        public void UpdateItem_WithCoverImage_ResizesImage()
        {
            // Arrange
            var testImage = CreateValidTestImage(2000, 2000);

            var item = new ListItem
            {
                id = 1,
                title = "Valid",
                rating = 5,
                coverImage = testImage,
                genres = new ObservableCollection<Genre>(),
                status = ItemStatus.OnHold
            };

            ListItem? updatedItem = null;

            _mockItemRepo
                .Setup(r => r.Update(It.IsAny<ListItem>()))
                .Callback<ListItem>(i => updatedItem = i);

            // Act
            var result = _service.UpdateItem(item);

            // Assert
            Assert.True(result.IsSuccess);

            Assert.NotNull(updatedItem);
            Assert.NotNull(updatedItem.coverImage);

            using var originalMs = new MemoryStream(testImage);

            var originalBitmap = new BitmapImage();
            originalBitmap.BeginInit();
            originalBitmap.CacheOption = BitmapCacheOption.OnLoad;
            originalBitmap.StreamSource = originalMs;
            originalBitmap.EndInit();

            using var resizedMs = new MemoryStream(updatedItem.coverImage);

            var resizedBitmap = new BitmapImage();
            resizedBitmap.BeginInit();
            resizedBitmap.CacheOption = BitmapCacheOption.OnLoad;
            resizedBitmap.StreamSource = resizedMs;
            resizedBitmap.EndInit();

            Assert.True(resizedBitmap.PixelWidth < originalBitmap.PixelWidth);
            Assert.True(resizedBitmap.PixelHeight < originalBitmap.PixelHeight);

            _mockItemRepo.Verify(
                r => r.Update(It.IsAny<ListItem>()),
                Times.Once);
        }
        #endregion

        #region DeleteItem Tests
        [Fact]
        public void DeleteItem_CallsRepositoryDelete()
        {
            // Arrange
            var item = new ListItem { id = 1, title = "Item", genres = new ObservableCollection<Genre>(), status = ItemStatus.Planning };

            // Act
            _service.DeleteItem(item);

            // Assert
            _mockItemRepo.Verify(r => r.Delete(item), Times.Once);
        }
        #endregion

        #region GetItemCount Tests
        [Fact]
        public void GetItemCount_ReturnsCorrectCount()
        {
            // Arrange
            var list = new List(1, "Movies", null);
            var genre = new Genre(1, "Action");
            _mockItemRepo.Setup(r => r.GetItemCount(list, genre, "Search", ItemStatus.InProgress))
                .Returns(5);

            // Act
            var result = _service.GetItemCount(list, genre, "Search", ItemStatus.InProgress);

            // Assert
            Assert.Equal(5, result);
            _mockItemRepo.Verify(r => r.GetItemCount(list, genre, "Search", ItemStatus.InProgress), Times.Once);
        }
        #endregion
    }
}
