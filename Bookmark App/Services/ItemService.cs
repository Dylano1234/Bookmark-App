using Bookmark_App.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bookmark_App.Services
{
    public class ItemService
    {
        private readonly DataAccess.ItemRepository _itemRepo;
        //private readonly Services.ImageService _imageService;
        public ItemService(DataAccess.ItemRepository itemRepo)
        {
            _itemRepo = itemRepo;

        }

        public List<Models.ListItem> GetAllItemsByList(Models.List list, Genre genreFilter, string sort, string titleSearch, ItemStatus status, int itemsPerPage, int currentPage)
        {
            return _itemRepo.GetAllByList(list, genreFilter, sort, titleSearch, status, itemsPerPage, currentPage);
        }
        public void UpdateItem(Models.ListItem listItem)
        {
            if(listItem.coverImage != null)
            {
                listItem.coverImage = ImageService.ResizeImage(listItem.coverImage, 300, 400, 85);
            }
            
            _itemRepo.Update(listItem);
        }
        public void AddItem(Models.ListItem listItem, int listid)
        {
            if (listItem.coverImage != null)
            {
                listItem.coverImage = ImageService.ResizeImage(listItem.coverImage, 300, 400, 85);
            }
            _itemRepo.Insert(listItem, listid);
        }
        public void DeleteItem(Models.ListItem listItem)
        {
            _itemRepo.Delete(listItem);
        }
        public int GetItemCount(Models.List list, Genre genreFilter, string titleSearch, ItemStatus status)
        {
            return _itemRepo.GetItemCount(list, genreFilter, titleSearch, status);
        }
    }
}
