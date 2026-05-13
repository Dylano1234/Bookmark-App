using Bookmark_App.DataAccess;
using Bookmark_App.Models;

namespace Bookmark_App.Services
{
    public class ListService
    {
        private readonly IListRepository _listRepo;
        private readonly IItemRepository _itemRepo;

        public ListService(IListRepository listRepo, IItemRepository itemRepo)
        {
            _listRepo = listRepo;
            _itemRepo = itemRepo;
        }

        public List<List> GetAllLists()
        {
            return _listRepo.GetAll();
        }
        public ValidationResult CreateList(string title, byte[]? coverImage, out List? createdList)
        {
            createdList = null;

            // Validate first
            string validationErrors = ValidateListTitle(title);
            if (!string.IsNullOrEmpty(validationErrors))
            {
                return ValidationResult.Failure(validationErrors);
            }

            var list = new List
            {
                title = title,
                coverImage = coverImage
            };
            if (coverImage != null)
            {
                list.coverImage = ImageService.ResizeImage(coverImage, 600, 600, 95);
            }
            var newId = _listRepo.Insert(list);
            list.id = newId;
            
            createdList = list;
            return ValidationResult.Success();
        }
        public ValidationResult UpdateList(List list, string title, byte[]? coverImage)
        {
            // Validate first
            string validationErrors = ValidateListTitle(title, list);
            if (!string.IsNullOrEmpty(validationErrors))
            {
                return ValidationResult.Failure(validationErrors);
            }

            if (coverImage != null)
            {
                coverImage = ImageService.ResizeImage(coverImage, 600, 600, 95);
            }
            _listRepo.Update(list, title, coverImage);
            list.title = title;
            list.coverImage = coverImage;

            return ValidationResult.Success();
        }
        public void DeleteList(List list)
        {
            _listRepo.Delete(list);
        }

        /// <summary>
        /// Validates a list title for creation or update. Returns validation errors if any.
        /// </summary>
        /// <param name="title">The title to validate</param>
        /// <param name="currentList">If updating, pass the current list to exclude it from duplicate check. Pass null for new lists.</param>
        /// <returns>Empty string if valid, otherwise contains error messages separated by newlines.</returns>
        public string ValidateListTitle(string title, List? currentList = null)
        {
            string errorMessage = string.Empty;

            // Check if title is empty
            if (string.IsNullOrWhiteSpace(title))
            {
                errorMessage += "- List title cannot be empty.\n";
                return errorMessage;
            }

            // Check for duplicate titles
            List<List> allLists = GetAllLists();
            bool titleExists = allLists.Any(l =>
                l.title.Equals(title, StringComparison.OrdinalIgnoreCase));

            if (currentList != null)
            {
                // When updating, allow the same title but reject if different list has this title
                if (titleExists && title != currentList.title)
                {
                    errorMessage += "- A list with this title already exists. Please choose a different title.\n";
                }
            }
            else if (titleExists)
            {
                // When creating new, reject if any list has this title
                errorMessage += "- A list with this title already exists. Please choose a different title.\n";
            }

            return errorMessage;
        }
    }
}
