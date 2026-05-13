using Bookmark_App.DataAccess;

namespace Bookmark_App.Services
{
    public class GenreService
    {
        private readonly IGenreRepository _genreRepository;
        public GenreService(IGenreRepository genreRepository)
        {
            _genreRepository = genreRepository;
        }
        public List<Models.Genre> GetAllGenres()
        {
            return _genreRepository.GetAll();
        }
    }
}
