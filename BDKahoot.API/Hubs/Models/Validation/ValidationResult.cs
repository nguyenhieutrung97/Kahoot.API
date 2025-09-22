namespace BDKahoot.API.Hubs.Models.Validation
{
    /// <summary>
    /// Generic validation result for hub operations
    /// </summary>
    public class ValidationResult
    {
        public bool IsSuccess { get; private set; }
        public string? ErrorMessage { get; private set; }

        private ValidationResult(bool isSuccess, string? errorMessage = null)
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
        }

        public static ValidationResult Success() => new(true);
        public static ValidationResult Failure(string errorMessage) => new(false, errorMessage);
    }
}
