namespace backend.Models
{
    /// <summary>
    /// A generic wrapper class for API responses, handling success, data, and errors.
    /// </summary>
    /// <typeparam name="T">The type of data returned (e.g., List<AutocompleteResult>).</typeparam>
    public class ApiResponse<T>
    {
        /// <summary>
        /// Indicates whether the API request was successful.
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// The data returned by the API, if successful.
        /// </summary>
        public T Data { get; }

        /// <summary>
        /// The error message, if the request was unsuccessful.
        /// </summary>
        public string Error { get; }

        public string Message { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiResponse{T}"/> class with success and data.
        /// </summary>
        /// <param name="success">Whether the request was successful.</param>
        /// <param name="data">The data to return.</param>
        /// <param name="error">The error message (if any).</param>
        public ApiResponse(bool success, T data = default, string error = null, string message = null)
        {
            Success = success;
            Data = data;
            Error = error;
            Message = message;
        }
    }
}