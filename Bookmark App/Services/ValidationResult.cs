namespace Bookmark_App.Services
{
    public class ValidationResult
    {
        public bool IsSuccess { get; }
        public string ErrorMessage { get; }

        public ValidationResult(bool isSuccess, string errorMessage = "")
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
        }

        public static ValidationResult Success() => new(true);
        public static ValidationResult Failure(string errorMessage) => new(false, errorMessage);
    }
}
