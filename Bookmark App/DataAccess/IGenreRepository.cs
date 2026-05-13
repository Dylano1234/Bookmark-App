using Bookmark_App.Models;

namespace Bookmark_App.DataAccess
{
    public interface IGenreRepository
    {
        List<Genre> GetAll();
    }
}