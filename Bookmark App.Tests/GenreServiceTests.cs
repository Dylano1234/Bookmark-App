using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Moq;
using Bookmark_App.Services;
using Bookmark_App.DataAccess;
using Bookmark_App.Models;

namespace Bookmark_App.Tests
{
    public class GenreServiceTests
    {
        private readonly Mock<IGenreRepository> _mockGenreRepo;
        private readonly GenreService _service;

        public GenreServiceTests()
        {
            _mockGenreRepo = new Mock<IGenreRepository>();
            _service = new GenreService(_mockGenreRepo.Object);
        }

        #region GetAllGenres Tests
        [Fact]
        public void GetAllGenres_ReturnsAllGenres()
        {
            // Arrange
            var genres = new List<Genre>
            {
                new Genre(1, "Action"),
                new Genre(2, "Drama"),
                new Genre(3, "Comedy")
            };
            _mockGenreRepo.Setup(r => r.GetAll()).Returns(genres);

            // Act
            var result = _service.GetAllGenres();

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Equal("Action", result[0].name);
            Assert.Equal("Drama", result[1].name);
            Assert.Equal("Comedy", result[2].name);
            _mockGenreRepo.Verify(r => r.GetAll(), Times.Once);
        }

        [Fact]
        public void GetAllGenres_WithEmptyDatabase_ReturnsEmptyList()
        {
            // Arrange
            _mockGenreRepo.Setup(r => r.GetAll()).Returns(new List<Genre>());

            // Act
            var result = _service.GetAllGenres();

            // Assert
            Assert.Empty(result);
            _mockGenreRepo.Verify(r => r.GetAll(), Times.Once);
        }

        [Fact]
        public void GetAllGenres_VerifiesRepositoryCalled()
        {
            // Arrange
            _mockGenreRepo.Setup(r => r.GetAll()).Returns(new List<Genre>());

            // Act
            _service.GetAllGenres();

            // Assert
            _mockGenreRepo.Verify(r => r.GetAll(), Times.Once);
        }
    }
}
    #endregion