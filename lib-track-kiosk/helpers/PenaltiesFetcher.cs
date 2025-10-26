using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using lib_track_kiosk.configs;

namespace lib_track_kiosk.helpers
{
    /// <summary>
    /// Helper to fetch penalties for a given user from the backend.
    /// Returns the raw penalties array (if present) so callers can inspect status/fine and map by transaction_id.
    /// </summary>
    internal static class PenaltiesFetcher
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        /// <summary>
        /// Fetch penalties for a user. Returns JArray (may be empty) containing penalty objects.
        /// Throws on network / unexpected server errors.
        /// </summary>
        public static async Task<JArray> GetPenaltiesForUserAsync(int userId, CancellationToken cancellationToken = default)
        {
            if (userId <= 0) throw new ArgumentException("userId must be positive", nameof(userId));

            var url = $"{API_Backend.BaseUrl}/api/penalties/user/{userId}";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            using var resp = await _httpClient.SendAsync(req, cancellationToken).ConfigureAwait(false);

            var content = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!resp.IsSuccessStatusCode)
            {
                // try pull message from body
                try
                {
                    var parsed = JToken.Parse(content);
                    var msg = parsed["message"]?.ToString();
                    throw new HttpRequestException($"Failed to fetch penalties. Status {(int)resp.StatusCode}. {msg ?? resp.ReasonPhrase}");
                }
                catch
                {
                    throw new HttpRequestException($"Failed to fetch penalties. Status {(int)resp.StatusCode}. Reason: {resp.ReasonPhrase}");
                }
            }

            if (string.IsNullOrWhiteSpace(content))
                return new JArray();

            JToken root;
            try
            {
                root = JToken.Parse(content);
            }
            catch (Exception ex)
            {
                throw new Exception("Invalid JSON from penalties endpoint", ex);
            }

            // Prefer data.penalties, then data (if array), else empty
            var penalties = root["data"]?["penalties"] as JArray;
            if (penalties != null) return penalties;

            if (root["data"] is JArray arr) return (JArray)arr;

            // Some backends return object with a 'penalties' property at root
            if (root["penalties"] is JArray arr2) return (JArray)arr2;

            return new JArray();
        }
    }
}