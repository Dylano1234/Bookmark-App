using Bookmark_App.Models;

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

        /// <summary>
        /// Updates a ListItem after validation. Validation is enforced before any update occurs.
        /// </summary>
        /// <returns>ValidationResult indicating success or failure with error messages.</returns>
        public ValidationResult UpdateItem(Models.ListItem listItem)
        {
            // Validate first
            string validationErrors = ValidateListItem(listItem);
            if (!string.IsNullOrEmpty(validationErrors))
            {
                return ValidationResult.Failure(validationErrors);
            }

            if(listItem.coverImage != null)
            {
                listItem.coverImage = ImageService.ResizeImage(listItem.coverImage, 250, 250, 85);
            }
            
            _itemRepo.Update(listItem);
            return ValidationResult.Success();
        }

        /// <summary>
        /// Adds a new ListItem after validation. Validation is enforced before any insertion occurs.
        /// </summary>
        /// <returns>ValidationResult indicating success or failure with error messages.</returns>
        public ValidationResult AddItem(Models.ListItem listItem, int listid)
        {
            // Validate first
            string validationErrors = ValidateListItem(listItem);
            if (!string.IsNullOrEmpty(validationErrors))
            {
                return ValidationResult.Failure(validationErrors);
            }

            if (listItem.coverImage != null)
            {
                listItem.coverImage = ImageService.ResizeImage(listItem.coverImage, 250, 250, 85);
            }
            _itemRepo.Insert(listItem, listid);
            return ValidationResult.Success();
        }

        public void DeleteItem(Models.ListItem listItem)
        {
            _itemRepo.Delete(listItem);
        }

        public int GetItemCount(Models.List list, Genre genreFilter, string titleSearch, ItemStatus status)
        {
            return _itemRepo.GetItemCount(list, genreFilter, titleSearch, status);
        }

        /// <summary>
        /// Validates a ListItem before saving. Returns validation errors if any.
        /// </summary>
        /// <returns>Empty string if valid, otherwise contains error messages separated by newlines.</returns>
        public string ValidateListItem(Models.ListItem item)
        {
            string errorMessage = string.Empty;

            // Check for empty title
            if (string.IsNullOrWhiteSpace(item.title))
            {
                errorMessage += "- Title is required.\n";
            }

            // Check rating range (0 is valid as "no rating", but if set must be 1-10)
            if (item.rating != 0 && (item.rating < 1 || item.rating > 10))
            {
                errorMessage += "- Rating must either be 0 or between 1 and 10.\n";
            }

            // Check for duplicate genres
            bool hasDuplicate = item.genres
                .Where(g => g != null && g.id != -1)
                .GroupBy(g => g.id)
                .Any(group => group.Count() > 1);

            if (hasDuplicate)
            {
                errorMessage += "- Duplicate genres selected.\n";
            }

            return errorMessage;
        }
    }
}
