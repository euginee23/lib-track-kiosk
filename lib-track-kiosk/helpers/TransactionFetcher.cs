using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using lib_track_kiosk.configs;
using System.Globalization;

namespace lib_track_kiosk.helpers
{
    /// <summary>
    /// Small helper responsible for fetching transaction records from the backend.
    /// Encapsulates the HTTP call and JSON parsing so UI code can consume structured data.
    /// Does not alter or infer transaction status values — it preserves whatever the backend provides.
    /// </summary>
    internal static class TransactionsFetcher
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        /// <summary>
        /// Fetches transactions for a given user and returns the backend "data" JArray.
        /// Throws an exception if the request fails or the response is invalid.
        /// The returned JArray is returned as-is except for ensuring a "status" field exists when possible.
        /// </summary>
        /// <param name="userId">User id to fetch transactions for</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>JArray of transaction objects from backend's data field</returns>
        public static async Task<JArray> GetTransactionsForUserAsync(int userId, CancellationToken cancellationToken = default)
        {
            if (userId <= 0) throw new ArgumentException("userId must be a positive integer", nameof(userId));

            var url = $"{API_Backend.BaseUrl}/api/transactions/user/{userId}";

            using var req = new HttpRequestMessage(HttpMethod.Get, url);

            using var resp = await _httpClient.SendAsync(req, cancellationToken).ConfigureAwait(false);

            var content = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!resp.IsSuccessStatusCode)
            {
                // Attempt to extract message from JSON if available
                try
                {
                    var parsed = JToken.Parse(content);
                    var msg = parsed["message"]?.ToString();
                    throw new HttpRequestException($"Failed to fetch transactions. Status: {(int)resp.StatusCode}. Message: {msg ?? resp.ReasonPhrase}");
                }
                catch (Exception)
                {
                    throw new HttpRequestException($"Failed to fetch transactions. Status: {(int)resp.StatusCode}. Reason: {resp.ReasonPhrase}");
                }
            }

            if (string.IsNullOrWhiteSpace(content))
                throw new Exception("Empty response from transactions endpoint.");

            JToken root;
            try
            {
                root = JToken.Parse(content);
            }
            catch (Exception ex)
            {
                throw new Exception("Invalid JSON received from transactions endpoint.", ex);
            }

            bool success = root["success"]?.Value<bool>() ?? false;
            if (!success)
            {
                var msg = root["message"]?.ToString() ?? "Transactions API returned success=false";
                throw new Exception(msg);
            }

            var data = root["data"] as JArray;
            if (data == null)
                throw new Exception("Transactions API returned unexpected data format (expected array).");

            // Preserve the status provided by the backend. Do not apply business-logic normalization here.
            foreach (var item in data)
            {
                if (item is JObject obj)
                {
                    // If "status" exists and is non-empty, leave it alone.
                    var statusToken = obj["status"];
                    var statusStr = statusToken?.ToString()?.Trim();

                    if (string.IsNullOrWhiteSpace(statusStr))
                    {
                        // If "status" is missing or empty, fall back to transaction_status or transaction_type exactly as provided.
                        var fallback = obj.Value<string>("status")
                                       ?? obj.Value<string>("transaction_type");

                        if (!string.IsNullOrWhiteSpace(fallback))
                        {
                            obj["status"] = fallback.Trim();
                        }
                        // If there is no fallback, we leave "status" absent/empty — do not invent values.
                    }
                    // If status was present and non-empty, we do nothing — preserve backend value.
                }
            }

            return data;
        }
    }
}