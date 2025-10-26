using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using lib_track_kiosk.configs;

namespace lib_track_kiosk.helpers
{

    internal static class ReturnBookResearch
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        /// <summary>
        /// Result object returned by ReturnAsync
        /// </summary>
        public class ReturnResult
        {
            public bool Success { get; set; }
            public string Message { get; set; }
            public List<ReturnedItem> ReturnedItems { get; set; } = new List<ReturnedItem>();
            public string ReferenceNumber { get; set; }
            public int? UserId { get; set; }
            public string UserName { get; set; }
            public int TotalReturned { get; set; }
            public int TotalActiveBeforeReturn { get; set; }
            public JToken PenaltyChecks { get; set; } // keep raw token for flexibility
            public bool HasReceipt { get; set; }
            public bool ReceiptStamped { get; set; }
            public string StampMethod { get; set; }
            // When request failed due to incomplete items, the server returns expected_items / provided_items
            public JToken ExpectedItems { get; set; }
            public JToken ProvidedItems { get; set; }
            public int HttpStatusCode { get; set; }
            public string RawJson { get; set; }
        }

        public class ReturnedItem
        {
            [JsonProperty("transaction_id")]
            public int? TransactionId { get; set; }

            [JsonProperty("item_type")]
            public string ItemType { get; set; }

            [JsonProperty("item_id")]
            public int? ItemId { get; set; }

            [JsonProperty("item_title")]
            public string ItemTitle { get; set; }
        }

        /// <summary>
        /// Sends a return request to the kiosk return endpoint.
        /// - Either referenceNumber OR transactionId should be provided (server requires reference_number for client case).
        /// - bookIds and researchPaperIds should contain all item ids being returned under the reference (server enforces strict matching).
        /// </summary>
        /// <param name="referenceNumber">Reference number (preferred)</param>
        /// <param name="userId">User id (required if returning by reference number)</param>
        /// <param name="bookIds">Book ids being returned (may be null/empty)</param>
        /// <param name="researchPaperIds">Research paper ids being returned (may be null/empty)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>ReturnResult with parsed server response and raw JSON</returns>
        public static async Task<ReturnResult> ReturnAsync(
            string referenceNumber,
            int userId,
            IEnumerable<int> bookIds = null,
            IEnumerable<int> researchPaperIds = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(referenceNumber))
                throw new ArgumentException("referenceNumber is required", nameof(referenceNumber));

            var url = $"{API_Backend.BaseUrl}/api/kiosk/return/";

            var payload = new JObject
            {
                ["reference_number"] = referenceNumber,
                ["user_id"] = userId
            };

            if (bookIds != null)
                payload["book_ids"] = JArray.FromObject(bookIds);
            if (researchPaperIds != null)
                payload["research_paper_ids"] = JArray.FromObject(researchPaperIds);

            var content = new StringContent(payload.ToString(Formatting.None), Encoding.UTF8, "application/json");

            using var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };

            try
            {
                using var resp = await _httpClient.SendAsync(req, cancellationToken).ConfigureAwait(false);
                var result = new ReturnResult { HttpStatusCode = (int)resp.StatusCode };

                string json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                result.RawJson = json;

                if (string.IsNullOrWhiteSpace(json))
                {
                    result.Success = resp.IsSuccessStatusCode;
                    result.Message = resp.ReasonPhrase ?? "Empty response body";
                    return result;
                }

                JToken root;
                try
                {
                    root = JToken.Parse(json);
                }
                catch (JsonReaderException)
                {
                    // Not JSON -> return raw
                    result.Success = resp.IsSuccessStatusCode;
                    result.Message = resp.ReasonPhrase ?? "Non-JSON response";
                    return result;
                }

                result.Success = root["success"]?.Value<bool>() ?? resp.IsSuccessStatusCode;
                result.Message = root["message"]?.ToString();

                // If success, parse data
                if (root["data"] != null)
                {
                    var data = root["data"];

                    // returned_items may be array
                    if (data["returned_items"] is JArray returnedArray)
                    {
                        foreach (var it in returnedArray)
                        {
                            try
                            {
                                var ri = it.ToObject<ReturnedItem>();
                                if (ri != null) result.ReturnedItems.Add(ri);
                            }
                            catch { /* ignore parse issues for individual items */ }
                        }
                    }

                    result.ReferenceNumber = data["reference_number"]?.ToString();
                    result.UserId = data["user_id"]?.Value<int?>();
                    result.UserName = data["user_name"]?.ToString();
                    result.TotalReturned = data["total_returned"]?.Value<int>() ?? result.ReturnedItems.Count;
                    result.TotalActiveBeforeReturn = data["total_active_before_return"]?.Value<int>() ?? 0;
                    result.PenaltyChecks = data["penalty_checks"];
                    result.HasReceipt = data["has_receipt"]?.Value<bool>() ?? false;
                    result.ReceiptStamped = data["receipt_stamped"]?.Value<bool>() ?? false;
                    result.StampMethod = data["stamp_method"]?.ToString();
                }

                // When server returns expected_items / provided_items on 400 caused by incomplete payload
                if (root["expected_items"] != null || root["provided_items"] != null)
                {
                    result.ExpectedItems = root["expected_items"];
                    result.ProvidedItems = root["provided_items"];
                }

                // On non-success status codes, try to fill message from server fields
                if (!resp.IsSuccessStatusCode && string.IsNullOrWhiteSpace(result.Message))
                {
                    result.Message = root["message"]?.ToString() ?? $"Server returned status {(int)resp.StatusCode}";
                }

                return result;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Network or unexpected error - surface in ReturnResult
                return new ReturnResult
                {
                    Success = false,
                    Message = "Request failed: " + ex.Message,
                    HttpStatusCode = 0,
                    RawJson = null
                };
            }
        }
    }
}