using Bookmark_App.DataAccess;
using Bookmark_App.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bookmark_App.Services
{
    public class ListService
    {
        private readonly ListRepository _listRepo;
        private readonly ItemRepository _itemRepo;

        public ListService(ListRepository listRepo, ItemRepository itemRepo)
        {
            _listRepo = listRepo;
            _itemRepo = itemRepo;
        }

        public List<List> GetAllLists()
        {
            return _listRepo.GetAll();
        }
        public List CreateList(string title, byte[]? coverImage)
        {
            var list = new List
            {
                title = title,
                coverImage = coverImage
            };
            if (coverImage != null)
            {
                list.coverImage = ImageService.ResizeImage(coverImage, 400, 300, 85);
            }
            var newId = _listRepo.Insert(list);
            list.id = newId;
            return list;
        }
    }
}
