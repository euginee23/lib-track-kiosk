using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using lib_track_kiosk.configs;

namespace lib_track_kiosk.helpers
{
    public class BorrowRequest
    {
        public string ReferenceNumber { get; set; }
        public int UserId { get; set; }
        public List<int> BookIds { get; set; } = new List<int>();
        public List<int> ResearchPaperIds { get; set; } = new List<int>();
        public DateTime? DueDate { get; set; }
        public string TransactionType { get; set; } = "Borrow";
        // Can be either a local kiosk temp path to the generated receipt image (local file)
        // or a server-side path/URL returned from an uploads endpoint. If it is a server path,
        // BorrowFetcher will send it as a text field instead of attaching a file.
        public string ReceiptFilePath { get; set; }
    }

    public class BorrowResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public JObject Data { get; set; }
        public string Error { get; set; }
        public int StatusCode { get; set; }
    }

    public static class BorrowFetcher
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private static readonly string baseApiUrl = $"{API_Backend.BaseUrl?.TrimEnd('/')}/api/kiosk";

        /// <summary>
        /// Sends a borrow request to the backend.
        /// If a local receipt file exists (ReceiptFilePath points to a local file), attaches it as file content under "receipt_image".
        /// If ReceiptFilePath appears to be a server path/URL (starts with "/" or "http"), it will be sent as a text field "receipt_image".
        /// </summary>
        public static async Task<BorrowResponse> BorrowAsync(BorrowRequest request)
        {
            try
            {
                using var form = new MultipartFormDataContent();

                // Required fields (text parts)
                form.Add(new StringContent(request.ReferenceNumber ?? "", Encoding.UTF8, "text/plain"), "reference_number");
                form.Add(new StringContent(request.UserId.ToString(), Encoding.UTF8, "text/plain"), "user_id");
                form.Add(new StringContent(request.TransactionType ?? "Borrow", Encoding.UTF8, "text/plain"), "transaction_type");

                if (request.DueDate.HasValue)
                    form.Add(new StringContent(request.DueDate.Value.ToString("yyyy-MM-dd"), Encoding.UTF8, "text/plain"), "due_date");

                // Add book_ids as repeated fields
                foreach (var bookId in request.BookIds)
                    form.Add(new StringContent(bookId.ToString(), Encoding.UTF8, "text/plain"), "book_ids");

                // Add research_paper_ids as repeated fields
                foreach (var rpId in request.ResearchPaperIds)
                    form.Add(new StringContent(rpId.ToString(), Encoding.UTF8, "text/plain"), "research_paper_ids");

                // Handle receipt path intelligently:
                // - If it's a local file path, attach as file under 'receipt_image' (existing behavior).
                // - If it looks like a server path/URL (starts with '/' or 'http'), send it as a text field 'receipt_image'.
                if (!string.IsNullOrWhiteSpace(request.ReceiptFilePath))
                {
                    var path = request.ReceiptFilePath.Trim();
                    bool looksLikeUrlOrServerPath = path.StartsWith("/") || path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || path.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

                    if (looksLikeUrlOrServerPath)
                    {
                        // Send server-side path as text; server should store this directly.
                        form.Add(new StringContent(path, Encoding.UTF8, "text/plain"), "receipt_image");
                    }
                    else if (File.Exists(path))
                    {
                        byte[] fileBytes = await File.ReadAllBytesAsync(path);
                        var fileContent = new ByteArrayContent(fileBytes);

                        // Determine mime type by extension
                        string ext = Path.GetExtension(path)?.ToLowerInvariant();
                        string mime = ext switch
                        {
                            ".png" => "image/png",
                            ".gif" => "image/gif",
                            ".webp" => "image/webp",
                            ".jpg" or ".jpeg" => "image/jpeg",
                            _ => "application/octet-stream"
                        };
                        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(mime);

                        // Add the file under the field name expected by the server/multer
                        form.Add(fileContent, "receipt_image", Path.GetFileName(path));
                    }
                    else
                    {
                        // Path supplied but not a local file and doesn't look like a server path - send as-is
                        form.Add(new StringContent(path, Encoding.UTF8, "text/plain"), "receipt_image");
                    }
                }

                var url = $"{baseApiUrl}/borrow";
                HttpResponseMessage response = await httpClient.PostAsync(url, form);

                string responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    // try to extract message from server response body
                    string serverMessage = null;
                    try
                    {
                        var parsed = JObject.Parse(responseBody);
                        serverMessage = parsed.Value<string>("message") ?? parsed.Value<string>("error") ?? responseBody;
                    }
                    catch { /* ignore parse errors */ }

                    return new BorrowResponse
                    {
                        Success = false,
                        Message = $"HTTP {(int)response.StatusCode}",
                        Error = serverMessage ?? responseBody,
                        StatusCode = (int)response.StatusCode
                    };
                }

                JObject json = null;
                try
                {
                    json = JObject.Parse(responseBody);
                }
                catch (Exception parseEx)
                {
                    // If the response isn't valid JSON, still indicate success but include parse error.
                    return new BorrowResponse
                    {
                        Success = true,
                        Message = "OK (unparsed response)",
                        Data = null,
                        Error = $"Failed to parse JSON response: {parseEx.Message}",
                        StatusCode = (int)response.StatusCode
                    };
                }

                return new BorrowResponse
                {
                    Success = json.Value<bool?>("success") ?? true,
                    Message = json.Value<string>("message"),
                    Data = json["data"] as JObject,
                    Error = json.Value<string>("error"),
                    StatusCode = (int)response.StatusCode
                };
            }
            catch (Exception ex)
            {
                return new BorrowResponse
                {
                    Success = false,
                    Message = "Failed to borrow items",
                    Error = ex.Message,
                    StatusCode = 0
                };
            }
        }
    }
}