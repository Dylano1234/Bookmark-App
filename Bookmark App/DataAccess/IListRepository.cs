using Bookmark_App.Models;

namespace Bookmark_App.DataAccess
{
    public interface IListRepository
    {
        List<List> GetAll();
        int Insert(List list);
        void Update(List list, string title, byte[]? coverImage);
        void Delete(List list);
    }
}