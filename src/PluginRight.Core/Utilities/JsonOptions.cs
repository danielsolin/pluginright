using System.Text.Json;

namespace PluginRight.Core.Utilities
{
    /// <summary>
    /// Provides shared JSON serializer options.
    /// </summary>
    public static class JsonOptions
    {
        /// <summary>
        /// Default JSON serializer options with case-insensitive property matching.
        /// </summary>
        public static readonly JsonSerializerOptions Default = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }
}
