using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using lib_track_kiosk.configs;

namespace lib_track_kiosk.helpers
{
    /// <summary>
    /// BookInfo model used by the UI and helpers.
    /// Added explicit CoverImageUrl and QrUrl string properties so the UI can lazy-download images.
    /// Also provides a backward-compatible CoverUrl alias because other UI code used that name.
    /// </summary>
    public class BookInfo
    {
        // Core identity
        public int BookId { get; set; }
        public string BookNumber { get; set; }

        // Descriptive
        public string Title { get; set; }
        public string Author { get; set; }
        public string Publisher { get; set; }
        public string Year { get; set; }
        public string Edition { get; set; }
        public string Price { get; set; }
        public string Donor { get; set; }
        public string Status { get; set; }

        // Location / batch
        public string ShelfLocation { get; set; }
        public string BatchRegistrationKey { get; set; }

        // Images / UI
        // - CoverImage: optional decoded Image (may be null). UI can use this if already populated.
        // - CoverImageUrl: explicit URL string returned by the server (preferred for lazy loading).
        // - QrUrl: explicit QR code URL returned by the server (if any).
        public Image CoverImage { get; set; }
        public string CoverImageUrl { get; set; }

        // Backwards-compatible alias used in some UI code (CoverUrl)
        public string CoverUrl
        {
            get => CoverImageUrl;
            set => CoverImageUrl = value;
        }

        public string QrUrl { get; set; }

        // Runtime-calculated
        public int AvailableCopies { get; set; }

        // Genre / department mapping
        public bool IsUsingDepartment { get; set; }
        public string Genre { get; set; }
        public string BookGenre { get; set; }
        public string DepartmentName { get; set; }
        public int GenreId { get; set; }

        public override string ToString()
        {
            return $"{Title} — {Author} ({Year})";
        }
    }

    /// <summary>
    /// BookFetcher: used to load single-book details from the backend.
    /// Adjusted to preserve server-provided cover/qr URL strings rather than decoding blobs,
    /// since the server now returns full URLs (e.g. https://uploads.codehub.site/book_covers/xyz.jpg).
    /// </summary>
    public static class BookFetcher
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private static readonly string baseApiUrl = $"{API_Backend.BaseUrl.TrimEnd('/')}/api/books";

        /// <summary>
        /// Fetches a single book's details from the backend. Does not download cover image bytes.
        /// Instead it sets BookInfo.CoverImageUrl (and QrUrl) when the server returns URL strings.
        /// </summary>
        public static async Task<BookInfo> GetBookAsync(int bookId, string bookNumber = null)
        {
            // Fetch book details
            string bookApiUrl = $"{baseApiUrl}/book/{bookId}";
            HttpResponseMessage response = await httpClient.GetAsync(bookApiUrl).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Failed to fetch book details. Status code: {response.StatusCode}");

            string jsonString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            JObject json = JObject.Parse(jsonString);

            if (!(bool?)json["success"] == true)
                throw new Exception("Book not found or API returned success=false.");

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

            // Handle cover: server returns a URL string (or null). Preserve the URL for the UI to lazy-download.
            var coverToken = data["book_cover"];
            if (coverToken != null && coverToken.Type == JTokenType.String)
            {
                var s = coverToken.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(s))
                {
                    book.CoverImageUrl = s;
                    book.CoverImage = null;
                }
            }
            else
            {
                // If server unexpectedly supplies embedded image object, try to decode (best-effort).
                // But primary path is to use the URL.
                try
                {
                    var decoded = DecodeImage(data["book_cover"]);
                    book.CoverImage = decoded;
                }
                catch { book.CoverImage = null; }
            }

            // Preserve QR URL if provided (server may return base64 or URL)
            var qrToken = data["book_qr"];
            if (qrToken != null)
            {
                if (qrToken.Type == JTokenType.String)
                {
                    var s = qrToken.ToString().Trim();
                    if (!string.IsNullOrWhiteSpace(s))
                        book.QrUrl = s;
                }
                else
                {
                    // If server returns a buffer or object, attempt to decode to base64 data URI and leave as QrUrl = data:image/...
                    try
                    {
                        var decodedQr = DecodeImage(qrToken);
                        if (decodedQr != null)
                        {
                            using var ms = new MemoryStream();
                            decodedQr.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                            var b = ms.ToArray();
                            book.QrUrl = "data:image/png;base64," + Convert.ToBase64String(b);
                        }
                    }
                    catch { /* ignore */ }
                }
            }

            // Load available copies via batch endpoint if a batch key exists. The endpoint may return quantity or array.
            if (!string.IsNullOrEmpty(book.BatchRegistrationKey))
            {
                try
                {
                    book.AvailableCopies = await GetAvailableCopiesAsync(book.BatchRegistrationKey).ConfigureAwait(false);
                }
                catch
                {
                    book.AvailableCopies = 0;
                }
            }

            // Map isUsingDepartment and genre fields if present (helpful if UI relies on them)
            try
            {
                var isUsingDeptToken = data["isUsingDepartment"] ?? data["is_using_department"];
                bool isUsingDept = false;
                if (isUsingDeptToken != null)
                {
                    if (isUsingDeptToken.Type == JTokenType.Boolean)
                        isUsingDept = (bool)isUsingDeptToken;
                    else if (isUsingDeptToken.Type == JTokenType.Integer)
                        isUsingDept = ((int)isUsingDeptToken) == 1;
                    else
                    {
                        bool.TryParse(isUsingDeptToken.ToString(), out isUsingDept);
                        if (!isUsingDept && int.TryParse(isUsingDeptToken.ToString(), out var vi))
                            isUsingDept = vi == 1;
                    }
                }
                book.IsUsingDepartment = isUsingDept;

                var genreVal = data["genre"]?.ToString();
                if (!string.IsNullOrWhiteSpace(genreVal))
                {
                    book.Genre = genreVal;
                    if (isUsingDept) book.DepartmentName = genreVal;
                    else book.BookGenre = genreVal;
                }

                book.GenreId = (int?)(data["genre_id"]) ?? 0;
            }
            catch { /* ignore mapping problems */ }

            return book;
        }

        /// <summary>
        /// Calls GET /api/books/{batchKey} to determine quantity or count available books in a batch.
        /// Handles both the case where the endpoint returns { quantity: N } or returns an array of copies.
        /// </summary>
        private static async Task<int> GetAvailableCopiesAsync(string batchKey)
        {
            try
            {
                string apiUrl = $"{baseApiUrl}/{batchKey}";
                HttpResponseMessage response = await httpClient.GetAsync(apiUrl).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode) return 0;

                string jsonString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                JObject json = JObject.Parse(jsonString);

                if (!(bool?)json["success"] == true) return 0;

                // If the endpoint returns a single object with quantity
                if (json["data"]?["quantity"] != null && int.TryParse(json["data"]["quantity"].ToString(), out int q))
                    return q;

                // If the endpoint returns an array of books, count those with status "Available"
                if (json["data"] is JArray allBooks)
                {
                    int availableCount = 0;
                    foreach (var book in allBooks)
                    {
                        if (string.Equals(book["status"]?.ToString(), "Available", StringComparison.OrdinalIgnoreCase))
                            availableCount++;
                    }
                    return availableCount;
                }

                // If data is an object that contains an array, try to find it
                if (json["data"] is JObject dataObj)
                {
                    foreach (var prop in dataObj.Properties())
                    {
                        if (prop.Value is JArray arr)
                        {
                            int available = 0;
                            foreach (var book in arr)
                            {
                                if (string.Equals(book["status"]?.ToString(), "Available", StringComparison.OrdinalIgnoreCase))
                                    available++;
                            }
                            return available;
                        }
                    }
                }

                return 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Decodes image token when the server unexpectedly sends inline image data.
        /// This remains for backward compatibility but is rarely used because server now returns URLs.
        /// </summary>
        private static Image DecodeImage(JToken imageToken)
        {
            if (imageToken == null || imageToken.Type == JTokenType.Null) return null;

            try
            {
                // Node Buffer: { data: [ ... ] }
                if (imageToken["data"] != null && imageToken["data"] is JArray byteArray)
                {
                    byte[] imageBytes = byteArray.ToObject<byte[]>();
                    using var ms = new MemoryStream(imageBytes);
                    using var srcImg = Image.FromStream(ms);
                    return (Image)srcImg.Clone();
                }

                // Base64 or data URI string
                if (imageToken.Type == JTokenType.String)
                {
                    string base64 = imageToken.ToString().Trim();
                    if (string.IsNullOrEmpty(base64)) return null;

                    if (base64.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
                        base64 = base64.Substring(base64.IndexOf(",") + 1);

                    try
                    {
                        byte[] imageBytes = Convert.FromBase64String(base64);
                        using var ms = new MemoryStream(imageBytes);
                        using var srcImg = Image.FromStream(ms);
                        return (Image)srcImg.Clone();
                    }
                    catch (FormatException fe)
                    {
                        Console.WriteLine($"⚠️ Invalid base64 image data: {fe.Message}");
                        return null;
                    }
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