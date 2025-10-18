using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
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
        public string ReceiptFilePath { get; set; }
    }

    public class BorrowResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public JObject Data { get; set; }
        public string Error { get; set; }
    }

    public static class BorrowFetcher
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private static readonly string baseApiUrl = $"{API_Backend.BaseUrl}/api/kiosk";

        /// <summary>
        /// Sends a borrow request to the backend.
        /// </summary>
        public static async Task<BorrowResponse> BorrowAsync(BorrowRequest request)
        {
            try
            {
                using var form = new MultipartFormDataContent();

                form.Add(new StringContent(request.ReferenceNumber), "reference_number");
                form.Add(new StringContent(request.UserId.ToString()), "user_id");
                form.Add(new StringContent(request.TransactionType), "transaction_type");

                if (request.DueDate.HasValue)
                    form.Add(new StringContent(request.DueDate.Value.ToString("yyyy-MM-dd")), "due_date");

                // Add books
                foreach (var bookId in request.BookIds)
                    form.Add(new StringContent(bookId.ToString()), "book_ids");

                // Add research papers
                foreach (var rpId in request.ResearchPaperIds)
                    form.Add(new StringContent(rpId.ToString()), "research_paper_ids");

                // Add receipt image if provided
                if (!string.IsNullOrEmpty(request.ReceiptFilePath) && File.Exists(request.ReceiptFilePath))
                {
                    var fileBytes = await File.ReadAllBytesAsync(request.ReceiptFilePath);
                    var fileContent = new ByteArrayContent(fileBytes);
                    fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg"); // adjust if needed
                    form.Add(fileContent, "receipt_image", Path.GetFileName(request.ReceiptFilePath));
                }

                HttpResponseMessage response = await httpClient.PostAsync($"{baseApiUrl}/borrow", form);

                string jsonString = await response.Content.ReadAsStringAsync();
                JObject json = JObject.Parse(jsonString);

                return new BorrowResponse
                {
                    Success = json.Value<bool>("success"),
                    Message = json.Value<string>("message"),
                    Data = json["data"] as JObject,
                    Error = json.Value<string>("error")
                };
            }
            catch (Exception ex)
            {
                return new BorrowResponse
                {
                    Success = false,
                    Message = "Failed to borrow items",
                    Error = ex.Message
                };
            }
        }
    }
}
