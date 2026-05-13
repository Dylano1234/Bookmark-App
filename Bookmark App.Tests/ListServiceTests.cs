using Bookmark_App.DataAccess;
using Bookmark_App.Models;
using Bookmark_App.Services;
using Moq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using System.Windows.Media.Imaging;

namespace Bookmark_App.Tests
{
    public class ListServiceTests
    {
        private readonly Mock<IListRepository> _mockListRepo;
        private readonly Mock<IItemRepository> _mockItemRepo;
        private readonly ListService _service;

        public ListServiceTests()
        {
            _mockListRepo = new Mock<IListRepository>();
            _mockItemRepo = new Mock<IItemRepository>();
            _service = new ListService(_mockListRepo.Object, _mockItemRepo.Object);
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

        #region GetAllLists Tests
        [Fact]
        public void GetAllLists_ReturnsAllLists()
        {
            // Arrange
            var lists = new List<List>
            {
                new List(1, "Movies", null),
                new List(2, "Books", null)
            };
            _mockListRepo.Setup(r => r.GetAll()).Returns(lists);

            // Act
            var result = _service.GetAllLists();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal("Movies", result[0].title);
            Assert.Equal("Books", result[1].title);
            _mockListRepo.Verify(r => r.GetAll(), Times.Once);
        }

        [Fact]
        public void GetAllLists_WithEmptyDatabase_ReturnsEmptyList()
        {
            // Arrange
            _mockListRepo.Setup(r => r.GetAll()).Returns(new List<List>());

            // Act
            var result = _service.GetAllLists();

            // Assert
            Assert.Empty(result);
            _mockListRepo.Verify(r => r.GetAll(), Times.Once);
        }
        #endregion

        #region ValidateListTitle Tests
        [Fact]
        public void ValidateListTitle_WithEmptyTitle_ReturnsError()
        {
            // Arrange
            _mockListRepo.Setup(r => r.GetAll()).Returns(new List<List>());

            // Act
            var result = _service.ValidateListTitle("");

            // Assert
            Assert.Contains("cannot be empty", result);
        }

        [Fact]
        public void ValidateListTitle_WithWhitespaceTitle_ReturnsError()
        {
            // Arrange
            _mockListRepo.Setup(r => r.GetAll()).Returns(new List<List>());

            // Act
            var result = _service.ValidateListTitle("   ");

            // Assert
            Assert.Contains("cannot be empty", result);
        }

        [Fact]
        public void ValidateListTitle_WithValidUniqueTitle_ReturnsEmpty()
        {
            // Arrange
            var existingLists = new List<List> { new List(1, "Movies", null) };
            _mockListRepo.Setup(r => r.GetAll()).Returns(existingLists);

            // Act
            var result = _service.ValidateListTitle("Books");

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void ValidateListTitle_WithDuplicateTitle_ReturnsError()
        {
            // Arrange
            var existingLists = new List<List> { new List(1, "Movies", null) };
            _mockListRepo.Setup(r => r.GetAll()).Returns(existingLists);

            // Act
            var result = _service.ValidateListTitle("Movies");

            // Assert
            Assert.Contains("already exists", result);
        }

        [Fact]
        public void ValidateListTitle_WithDuplicateTitleCaseInsensitive_ReturnsError()
        {
            // Arrange
            var existingLists = new List<List> { new List(1, "Movies", null) };
            _mockListRepo.Setup(r => r.GetAll()).Returns(existingLists);

            // Act
            var result = _service.ValidateListTitle("MOVIES");

            // Assert
            Assert.Contains("already exists", result);
        }

        [Fact]
        public void ValidateListTitle_UpdateWithSameTitle_ReturnsEmpty()
        {
            // Arrange
            var currentList = new List(1, "Movies", null);
            var existingLists = new List<List> { currentList };
            _mockListRepo.Setup(r => r.GetAll()).Returns(existingLists);

            // Act
            var result = _service.ValidateListTitle("Movies", currentList);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void ValidateListTitle_UpdateWithDuplicateTitle_ReturnsError()
        {
            // Arrange
            var currentList = new List(1, "Movies", null);
            var existingLists = new List<List>
            {
                currentList,
                new List(2, "Books", null)
            };
            _mockListRepo.Setup(r => r.GetAll()).Returns(existingLists);

            // Act
            var result = _service.ValidateListTitle("Books", currentList);

            // Assert
            Assert.Contains("already exists", result);
        }
        #endregion

        #region CreateList Tests
        [Fact]
        public void CreateList_WithValidTitle_ReturnsSuccessAndCreatedList()
        {
            // Arrange
            _mockListRepo.Setup(r => r.GetAll()).Returns(new List<List>());
            _mockListRepo.Setup(r => r.Insert(It.IsAny<List>())).Returns(1);

            // Act
            var result = _service.CreateList("Movies", null, out var createdList);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(createdList);
            Assert.Equal("Movies", createdList.title);
            Assert.Equal(1, createdList.id);
            _mockListRepo.Verify(r => r.Insert(It.IsAny<List>()), Times.Once);
        }

        [Fact]
        public void CreateList_WithEmptyTitle_ReturnsFailure()
        {
            // Arrange
            _mockListRepo.Setup(r => r.GetAll()).Returns(new List<List>());

            // Act
            var result = _service.CreateList("", null, out var createdList);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Null(createdList);
            Assert.Contains("cannot be empty", result.ErrorMessage);
            _mockListRepo.Verify(r => r.Insert(It.IsAny<List>()), Times.Never);
        }

        [Fact]
        public void CreateList_WithDuplicateTitle_ReturnsFailure()
        {
            // Arrange
            var existingLists = new List<List> { new List(1, "Movies", null) };
            _mockListRepo.Setup(r => r.GetAll()).Returns(existingLists);

            // Act
            var result = _service.CreateList("Movies", null, out var createdList);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Null(createdList);
            _mockListRepo.Verify(r => r.Insert(It.IsAny<List>()), Times.Never);
        }

        [Fact]
        public void CreateList_WithCoverImage_ResizesImage()
        {
            // Arrange
            var testImage = CreateValidTestImage(1000, 1000);

            _mockListRepo
                .Setup(r => r.GetAll())
                .Returns(new List<List>());

            List? insertedList = null;

            _mockListRepo
                .Setup(r => r.Insert(It.IsAny<List>()))
                .Callback<List>(l => insertedList = l)
                .Returns(1);

            // Act
            var result = _service.CreateList("Movies", testImage, out var createdList);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(createdList);

            Assert.NotNull(insertedList);
            Assert.NotNull(insertedList.coverImage);

            using var originalMs = new MemoryStream(testImage);

            var originalBitmap = new BitmapImage();
            originalBitmap.BeginInit();
            originalBitmap.CacheOption = BitmapCacheOption.OnLoad;
            originalBitmap.StreamSource = originalMs;
            originalBitmap.EndInit();

            using var resizedMs = new MemoryStream(insertedList.coverImage);

            var resizedBitmap = new BitmapImage();
            resizedBitmap.BeginInit();
            resizedBitmap.CacheOption = BitmapCacheOption.OnLoad;
            resizedBitmap.StreamSource = resizedMs;
            resizedBitmap.EndInit();

            Assert.True(resizedBitmap.PixelWidth < originalBitmap.PixelWidth);
            Assert.True(resizedBitmap.PixelHeight < originalBitmap.PixelHeight);

            _mockListRepo.Verify(
                r => r.Insert(It.IsAny<List>()),
                Times.Once);
        }
        #endregion

        #region UpdateList Tests
        [Fact]
        public void UpdateList_WithValidTitle_ReturnsSuccess()
        {
            // Arrange
            var list = new List(1, "Movies", null);
            _mockListRepo.Setup(r => r.GetAll()).Returns(new List<List> { list });

            // Act
            var result = _service.UpdateList(list, "Updated Movies", null);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("Updated Movies", list.title);
            _mockListRepo.Verify(r => r.Update(list, "Updated Movies", null), Times.Once);
        }

        [Fact]
        public void UpdateList_WithEmptyTitle_ReturnsFailure()
        {
            // Arrange
            var list = new List(1, "Movies", null);
            _mockListRepo.Setup(r => r.GetAll()).Returns(new List<List> { list });

            // Act
            var result = _service.UpdateList(list, "", null);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("Movies", list.title);
            _mockListRepo.Verify(r => r.Update(It.IsAny<List>(), It.IsAny<string>(), It.IsAny<byte[]>()), Times.Never);
        }

        [Fact]
        public void UpdateList_WithConflictingTitle_ReturnsFailure()
        {
            // Arrange
            var list1 = new List(1, "Movies", null);
            var list2 = new List(2, "Books", null);
            _mockListRepo.Setup(r => r.GetAll()).Returns(new List<List> { list1, list2 });

            // Act
            var result = _service.UpdateList(list1, "Books", null);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("Movies", list1.title);
        }

        [Fact]
        public void UpdateList_WithCoverImage_ResizesImage()
        {
            // Arrange
            var list = new List(1, "Movies", null);
            var testImage = CreateValidTestImage(1000, 1000);

            _mockListRepo
                .Setup(r => r.GetAll())
                .Returns(new List<List> { list });

            byte[]? updatedImage = null;

            _mockListRepo
                .Setup(r => r.Update(It.IsAny<List>(), "Movies", It.IsAny<byte[]>()))
                .Callback<List, string, byte[]>((_, _, img) => updatedImage = img);

            // Act
            var result = _service.UpdateList(list, "Movies", testImage);

            // Assert
            Assert.True(result.IsSuccess);

            Assert.NotNull(updatedImage);

            using var originalMs = new MemoryStream(testImage);

            var originalBitmap = new BitmapImage();
            originalBitmap.BeginInit();
            originalBitmap.CacheOption = BitmapCacheOption.OnLoad;
            originalBitmap.StreamSource = originalMs;
            originalBitmap.EndInit();

            using var resizedMs = new MemoryStream(updatedImage);

            var resizedBitmap = new BitmapImage();
            resizedBitmap.BeginInit();
            resizedBitmap.CacheOption = BitmapCacheOption.OnLoad;
            resizedBitmap.StreamSource = resizedMs;
            resizedBitmap.EndInit();

            Assert.True(resizedBitmap.PixelWidth < originalBitmap.PixelWidth);
            Assert.True(resizedBitmap.PixelHeight < originalBitmap.PixelHeight);

            _mockListRepo.Verify(
                r => r.Update(It.IsAny<List>(), "Movies", It.IsAny<byte[]>()),
                Times.Once);
        }
        #endregion

        #region DeleteList Tests
        [Fact]
        public void DeleteList_CallsRepositoryDelete()
        {
            // Arrange
            var list = new List(1, "Movies", null);

            // Act
            _service.DeleteList(list);

            // Assert
            _mockListRepo.Verify(r => r.Delete(list), Times.Once);
        }
        #endregion
    }
}
