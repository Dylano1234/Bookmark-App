using Bookmark_App.Models;

namespace Bookmark_App.DataAccess
{
    public interface IItemRepository
    {
        List<ListItem> GetAllByList(List list, Genre genreFilter, string sort, string titleSearch, ItemStatus status, int itemsPerPage, int currentPage);
        int GetItemCount(List list, Genre genreFilter, string titleSearch, ItemStatus status);
        void Update(ListItem listItem);
        void Insert(ListItem listItem, int listId);
        void Delete(ListItem listItem);
    }
}