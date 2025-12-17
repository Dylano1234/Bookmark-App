using Bookmark_App.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bookmark_App.Services
{
    public class ItemService
    {
        private readonly DataAccess.ItemRepository _itemRepo;
        public ItemService(DataAccess.ItemRepository itemRepo)
        {
            _itemRepo = itemRepo;
        }

        public List<Models.ListItem> GetAllItemsByList(Models.List list, Genre genreFilter, string sort, string titleSearch, ItemStatus status)
        {
            return _itemRepo.GetAllByList(list, genreFilter, sort, titleSearch, status);
        }
    }
}
