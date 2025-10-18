using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using lib_track_kiosk.configs;

namespace lib_track_kiosk.helpers
{
    public class BookInfo
    {
        public int BookId { get; set; }
        public string BookNumber { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string Publisher { get; set; }
        public string ShelfLocation { get; set; }
        public string Year { get; set; }
        public string Edition { get; set; }
        public string Price { get; set; }
        public string Donor { get; set; }
        public string Status { get; set; }
        public int AvailableCopies { get; set; }
        public Image CoverImage { get; set; }
        public string BatchRegistrationKey { get; set; }
    }

    public static class BookFetcher
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private static readonly string baseApiUrl = $"{API_Backend.BaseUrl}/api/books";

        /// <summary>
        /// Fetches the book information including cover image and available copies.
        /// </summary>
        public static async Task<BookInfo> GetBookAsync(int bookId, string bookNumber = null)
        {
            // Fetch book details
            string bookApiUrl = $"{baseApiUrl}/book/{bookId}";
            HttpResponseMessage response = await httpClient.GetAsync(bookApiUrl);

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Failed to fetch book details. Status code: {response.StatusCode}");

            string jsonString = await response.Content.ReadAsStringAsync();
            JObject json = JObject.Parse(jsonString);

            if (!(bool)json["success"])
                throw new Exception("Book not found.");

            JObject data = (JObject)json["data"];

            var book = new BookInfo
            {
                BookId = bookId,
                BookNumber = bookNumber,
                Title = data["book_title"]?.ToString() ?? "N/A",
                Author = data["author"]?.ToString() ?? "N/A",
                Publisher = data["publisher"]?.ToString() ?? "N/A",
                ShelfLocation = $"{data["shelf_number"] ?? "N/A"}-{data["shelf_column"] ?? "N/A"}-{data["shelf_row"] ?? "N/A"}",
                Year = data["book_year"]?.ToString() ?? "N/A",
                Edition = data["book_edition"]?.ToString() ?? "N/A",
                Price = data["book_price"]?.ToString() ?? "N/A",
                Donor = data["book_donor"]?.ToString() ?? "N/A",
                Status = data["status"]?.ToString() ?? "N/A",
                BatchRegistrationKey = data["batch_registration_key"]?.ToString()
            };

            // 🖼️ Decode cover image
            book.CoverImage = DecodeImage(data["book_cover"]);

            // ✅ Load available copies
            if (!string.IsNullOrEmpty(book.BatchRegistrationKey))
            {
                book.AvailableCopies = await GetAvailableCopiesAsync(book.BatchRegistrationKey);
            }

            return book;
        }

        /// <summary>
        /// Counts available copies from batch registration key
        /// </summary>
        private static async Task<int> GetAvailableCopiesAsync(string batchKey)
        {
            try
            {
                string apiUrl = $"{baseApiUrl}/{batchKey}";
                HttpResponseMessage response = await httpClient.GetAsync(apiUrl);

                if (!response.IsSuccessStatusCode) return 0;

                string jsonString = await response.Content.ReadAsStringAsync();
                JObject json = JObject.Parse(jsonString);

                if (!(bool)json["success"]) return 0;

                // Single quantity
                if (json["data"]["quantity"] != null)
                    return (int)json["data"]["quantity"];

                // Multiple books
                if (json["data"] is JArray allBooks)
                {
                    int availableCount = 0;
                    foreach (var book in allBooks)
                    {
                        if (book["status"]?.ToString() == "Available")
                            availableCount++;
                    }
                    return availableCount;
                }

                return 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Decodes an image from Node Buffer or Base64 string
        /// </summary>
        private static Image DecodeImage(JToken imageToken)
        {
            if (imageToken == null || imageToken.Type == JTokenType.Null) return null;

            try
            {
                // Node Buffer
                if (imageToken["data"] != null && imageToken["data"] is JArray byteArray)
                {
                    byte[] imageBytes = byteArray.ToObject<byte[]>();
                    using var ms = new MemoryStream(imageBytes);
                    return Image.FromStream(ms);
                }
                // Base64 string
                if (imageToken.Type == JTokenType.String)
                {
                    string base64 = imageToken.ToString();
                    if (base64.StartsWith("data:image"))
                        base64 = base64.Substring(base64.IndexOf(",") + 1);

                    byte[] imageBytes = Convert.FromBase64String(base64);
                    using var ms = new MemoryStream(imageBytes);
                    return Image.FromStream(ms);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error decoding book cover: {ex.Message}");
            }

            return null;
        }
    }
}
