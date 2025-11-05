using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using lib_track_kiosk.configs;

namespace lib_track_kiosk.helpers
{
    internal static class RateHelper
    {
        private static readonly HttpClient _httpClient;
        private static readonly JsonSerializerOptions _jsonOptions;

        static RateHelper()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = false
            };
        }

        // Represents a single rating item to send to the server.
        public class RatingItem
        {
            [JsonPropertyName("book_id")]
            public int? BookId { get; set; }

            [JsonPropertyName("research_paper_id")]
            public int? ResearchPaperId { get; set; }

            [JsonPropertyName("star_rating")]
            public int? StarRating { get; set; }

            [JsonPropertyName("comment")]
            public string Comment { get; set; }
        }

        // Typed representation of a per-item result returned by the server.
        public class RatingResult
        {
            [JsonPropertyName("success")]
            public bool? Success { get; set; }

            [JsonPropertyName("action")]
            public string Action { get; set; }

            [JsonPropertyName("reason")]
            public string Reason { get; set; }

            [JsonPropertyName("rating_id")]
            public int? RatingId { get; set; }

            [JsonPropertyName("book_id")]
            public int? BookId { get; set; }

            [JsonPropertyName("research_paper_id")]
            public int? ResearchPaperId { get; set; }

            [JsonPropertyName("star_rating")]
            public int? StarRating { get; set; }

            // catch-all for any additional fields the server may return
            [JsonExtensionData]
            public Dictionary<string, JsonElement> Extra { get; set; }
        }

        // Lightweight representation of the server response.
        public class ServerResponse
        {
            [JsonPropertyName("success")]
            public bool Success { get; set; }

            [JsonPropertyName("message")]
            public string Message { get; set; }

            [JsonPropertyName("total")]
            public int Total { get; set; }

            // Keep results flexible: raw JsonElements so we can try typed parse later.
            [JsonPropertyName("results")]
            public JsonElement[] Results { get; set; }

            [JsonPropertyName("error")]
            public string Error { get; set; }

            // Try to convert raw results to typed RatingResult objects, ignoring parse failures.
            public List<RatingResult> ToTypedResults(JsonSerializerOptions options = null)
            {
                var list = new List<RatingResult>();
                if (Results == null) return list;
                var opts = options ?? new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                foreach (var el in Results)
                {
                    try
                    {
                        var rr = JsonSerializer.Deserialize<RatingResult>(el.GetRawText(), opts);
                        if (rr != null) list.Add(rr);
                    }
                    catch
                    {
                        // ignore per-item parse errors; caller can inspect raw Results
                    }
                }
                return list;
            }
        }

        // Sends a rating payload to /api/rating/.
        // userId: required
        // starRating: optional global star rating (0..5). If you want per-item ratings, set this to null and set each RatingItem.StarRating
        // items: required non-empty list of RatingItem (book_id or research_paper_id must be present in each)
        // comment: optional global comment (per-item comment can be set in each RatingItem)
        public static async Task<ServerResponse> SendRatingAsync(
            int userId,
            int? starRating,
            List<RatingItem> items,
            string comment = null,
            CancellationToken cancellationToken = default)
        {
            if (userId <= 0)
                throw new ArgumentException("userId must be > 0", nameof(userId));

            if (items == null || items.Count == 0)
                throw new ArgumentException("items must be a non-empty list", nameof(items));

            var payload = new
            {
                user_id = userId,
                star_rating = starRating,
                items = items,
                comment = comment
            };

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var endpoint = $"{API_Backend.BaseUrl.TrimEnd('/')}/api/rating/";

            try
            {
                using var resp = await _httpClient.PostAsync(endpoint, content, cancellationToken).ConfigureAwait(false);
                var respText = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                if (!resp.IsSuccessStatusCode)
                {
                    // Try to parse server error shape if present, otherwise return generic failure
                    try
                    {
                        var maybe = JsonSerializer.Deserialize<ServerResponse>(respText, _jsonOptions);
                        if (maybe != null)
                            return maybe;
                    }
                    catch { /* ignore parse errors */ }

                    return new ServerResponse
                    {
                        Success = false,
                        Message = $"Request failed with status {(int)resp.StatusCode} {resp.ReasonPhrase}",
                        Error = respText
                    };
                }

                // parse success response
                try
                {
                    var parsed = JsonSerializer.Deserialize<ServerResponse>(respText, _jsonOptions);
                    if (parsed != null) return parsed;

                    return new ServerResponse
                    {
                        Success = false,
                        Message = "Failed to parse server response",
                        Error = respText
                    };
                }
                catch (Exception ex)
                {
                    return new ServerResponse
                    {
                        Success = false,
                        Message = "Failed to parse server response",
                        Error = ex.Message + "\n" + respText
                    };
                }
            }
            catch (OperationCanceledException)
            {
                return new ServerResponse
                {
                    Success = false,
                    Message = "Request canceled"
                };
            }
            catch (Exception ex)
            {
                return new ServerResponse
                {
                    Success = false,
                    Message = "Network or serialization error",
                    Error = ex.Message
                };
            }
        }

        // Convenience helper for sending a single book rating
        public static Task<ServerResponse> SendSingleBookRatingAsync(int userId, int bookId, int starRating, string comment = null, CancellationToken cancellationToken = default)
        {
            var items = new List<RatingItem>
            {
                new RatingItem { BookId = bookId, StarRating = starRating, Comment = comment }
            };
            // pass global star_rating as null so server uses per-item star_rating
            return SendRatingAsync(userId, null, items, null, cancellationToken);
        }

        // Convenience helper for sending a single research paper rating
        public static Task<ServerResponse> SendSingleResearchPaperRatingAsync(int userId, int researchPaperId, int starRating, string comment = null, CancellationToken cancellationToken = default)
        {
            var items = new List<RatingItem>
            {
                new RatingItem { ResearchPaperId = researchPaperId, StarRating = starRating, Comment = comment }
            };
            return SendRatingAsync(userId, null, items, null, cancellationToken);
        }
    }
}