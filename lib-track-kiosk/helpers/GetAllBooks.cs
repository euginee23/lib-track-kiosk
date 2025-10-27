using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using lib_track_kiosk.configs;

namespace lib_track_kiosk.helpers
{
    internal class GetAllBooks
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private static readonly string baseApiUrl = $"{API_Backend.BaseUrl}/api/books";

        /// <summary>
        /// Fetches all books from the backend and returns a list of BookInfo objects.
        /// For each book, attempts to decode the cover image and load available copies (when batch key is present).
        /// Also maps genre/department fields and the isUsingDepartment flag returned by the API so the UI can filter by either genre or department.
        /// </summary>
        public static async Task<List<BookInfo>> GetAllAsync()
        {
            var result = new List<BookInfo>();

            try
            {
                // GET /api/books/
                string apiUrl = $"{baseApiUrl}/";
                HttpResponseMessage response = await httpClient.GetAsync(apiUrl);

                if (!response.IsSuccessStatusCode)
                    throw new Exception($"Failed to fetch books. Status code: {response.StatusCode}");

                string jsonString = await response.Content.ReadAsStringAsync();
                JObject json = JObject.Parse(jsonString);

                if (!(bool?)json["success"] == true)
                    throw new Exception("Failed to fetch books: success flag is false.");

                // data expected to be an array of books
                JToken dataToken = json["data"];
                if (dataToken == null) return result;

                JArray booksArray = dataToken as JArray ?? new JArray(dataToken);

                foreach (var item in booksArray)
                {
                    try
                    {
                        var bookObj = (JObject)item;

                        var book = new BookInfo
                        {
                            BookId = (int?)(bookObj["book_id"]) ?? 0,
                            BookNumber = bookObj["book_number"]?.ToString(),
                            Title = bookObj["book_title"]?.ToString() ?? "N/A",
                            Author = bookObj["author"]?.ToString() ?? bookObj["book_author"]?.ToString() ?? "N/A",
                            Publisher = bookObj["publisher"]?.ToString() ?? bookObj["publisher"]?.ToString() ?? "N/A",
                            ShelfLocation = $"{bookObj["shelf_number"] ?? "N/A"}-{bookObj["shelf_column"] ?? "N/A"}-{bookObj["shelf_row"] ?? "N/A"}",
                            Year = bookObj["book_year"]?.ToString() ?? "N/A",
                            Edition = bookObj["book_edition"]?.ToString() ?? "N/A",
                            Price = bookObj["book_price"]?.ToString() ?? "N/A",
                            Donor = bookObj["book_donor"]?.ToString() ?? "N/A",
                            Status = bookObj["status"]?.ToString() ?? "N/A",
                            BatchRegistrationKey = bookObj["batch_registration_key"]?.ToString(),
                            // keep previous mapping of available copies blank for now (we may replace after fetching batch info)
                            AvailableCopies = 0
                        };

                        // Map isUsingDepartment / genre / genre_id returned by the API.
                        // The backend route aliases the column "genre" to either the department_name or the book_genre depending on isUsingDepartment.
                        try
                        {
                            // parse isUsingDepartment safely (could be 0/1, boolean, or string)
                            var isUsingDeptToken = bookObj["isUsingDepartment"] ?? bookObj["is_using_department"];
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

                            // genre alias from route (contains book_genre OR department_name depending on isUsingDepartment)
                            var genreVal = bookObj["genre"]?.ToString();
                            if (!string.IsNullOrWhiteSpace(genreVal))
                            {
                                // populate both Genre (generic) and specific DepartmentName / BookGenre fields to make reflection-based lookups in the UI succeed
                                book.Genre = genreVal;
                                if (isUsingDept)
                                    book.DepartmentName = genreVal;
                                else
                                    book.BookGenre = genreVal;
                            }

                            book.GenreId = (int?)(bookObj["genre_id"]) ?? 0;
                        }
                        catch { /* ignore mapping errors but continue */ }

                        // Decode cover image (if present)
                        book.CoverImage = DecodeImage(bookObj["book_cover"]);

                        // Load available copies when batch key is present (best-effort, don't fail the whole list)
                        if (!string.IsNullOrEmpty(book.BatchRegistrationKey))
                        {
                            try
                            {
                                book.AvailableCopies = await GetAvailableCopiesAsync(book.BatchRegistrationKey);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"⚠️ Failed to get available copies for batch {book.BatchRegistrationKey}: {ex.Message}");
                                book.AvailableCopies = 0;
                            }
                        }

                        result.Add(book);
                    }
                    catch (Exception exItem)
                    {
                        // Log the single-item failure and continue with other books
                        Console.WriteLine($"⚠️ Error processing book item: {exItem.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching all books: {ex.Message}");
                // bubble up or return empty list — currently returning what we have
            }

            return result;
        }

        /// <summary>
        /// Calls GET /api/books/{batchKey} to determine quantity or count available books in a batch.
        /// This mirrors the backend batch endpoint used by the single-book fetcher.
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

                if (!(bool?)json["success"] == true) return 0;

                // If the endpoint returns a single object with quantity
                if (json["data"]?["quantity"] != null)
                    return (int)json["data"]["quantity"];

                // If the endpoint returns an array of books, count those with status "Available"
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
        /// Decodes an image token coming from the backend (supports Node Buffer style { data: [...] } and base64/data URI string).
        /// Clones the Image to avoid issues when disposing the underlying MemoryStream.
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