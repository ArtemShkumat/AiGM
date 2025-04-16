using System;
using System.Threading.Tasks;

namespace AiGMBackEnd.Services
{
    /// <summary>
    /// Service responsible for deserializing JSON responses from the LLM 
    /// with added resilience features like timeouts.
    /// </summary>
    public interface ILlmResponseDeserializer
    {
        /// <summary>
        /// Attempts to deserialize the provided JSON string into the specified type T.
        /// Includes timeout logic to prevent hangs.
        /// </summary>
        /// <typeparam name="T">The target type to deserialize into.</typeparam>
        /// <param name="jsonString">The raw JSON string from the LLM.</param>
        /// <param name="timeout">The maximum time allowed for deserialization.</param>
        /// <returns>
        /// A tuple containing:
        /// - bool success: True if deserialization succeeded and the result is not null, false otherwise.
        /// - T result: The deserialized object if successful, default(T) otherwise.
        /// - Exception error: The exception encountered during failure (JsonException, TimeoutException, or other), null otherwise.
        /// </returns>
        Task<(bool success, T result, Exception error)> TryDeserializeAsync<T>(string jsonString, TimeSpan timeout);
    }
} 