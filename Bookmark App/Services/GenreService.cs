using Bookmark_App.DataAccess;

namespace Bookmark_App.Services
{
    public class GenreService
    {
        private readonly DataAccess.GenreRepository _genreRepository;
        public GenreService(GenreRepository genreRepository)
        {
            _genreRepository = genreRepository;
        }
        public List<Models.Genre> GetAllGenres()
        {
            return _genreRepository.GetAll();
        }
    }
}
