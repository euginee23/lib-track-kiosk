using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using System.Reflection;
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
        /// IMPORTANT CHANGE:
        /// - The server returns book_cover as a URL (not a blob). We preserve that URL on BookInfo (via common property names)
        ///   and do NOT try to download/ decode it here.
        /// - To avoid overwhelming the server with per-book batch requests, we no longer call GET /api/books/{batchKey}
        ///   for every book. Instead we compute available-copy counts from the main GET /api/books response by grouping
        ///   rows by batch_registration_key and counting status == "Available". This dramatically reduces request volume.
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

                // data expected to be an array of books (each row typically represents a copy)
                JToken dataToken = json["data"];
                if (dataToken == null) return result;

                JArray booksArray = dataToken as JArray ?? new JArray(dataToken);

                // First pass: build BookInfo list and a map of available counts per batch key.
                var availableCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var tempList = new List<BookInfo>();

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
                            Publisher = bookObj["publisher"]?.ToString() ?? "N/A",
                            ShelfLocation = $"{bookObj["shelf_number"] ?? "N/A"}-{bookObj["shelf_column"] ?? "N/A"}-{bookObj["shelf_row"] ?? "N/A"}",
                            Year = bookObj["book_year"]?.ToString() ?? "N/A",
                            Edition = bookObj["book_edition"]?.ToString() ?? "N/A",
                            Price = bookObj["book_price"]?.ToString() ?? "N/A",
                            Donor = bookObj["book_donor"]?.ToString() ?? "N/A",
                            Status = bookObj["status"]?.ToString() ?? "N/A",
                            BatchRegistrationKey = bookObj["batch_registration_key"]?.ToString(),
                            // AvailableCopies will be set after we compute counts
                            AvailableCopies = 0
                        };

                        // Map isUsingDepartment / genre / genre_id returned by the API.
                        try
                        {
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

                            var genreVal = bookObj["genre"]?.ToString();
                            if (!string.IsNullOrWhiteSpace(genreVal))
                            {
                                book.Genre = genreVal;
                                if (isUsingDept)
                                    book.DepartmentName = genreVal;
                                else
                                    book.BookGenre = genreVal;
                            }

                            book.GenreId = (int?)(bookObj["genre_id"]) ?? 0;
                        }
                        catch { /* ignore mapping errors but continue */ }

                        // Preserve cover URL (server returns URL) on BookInfo via common property names.
                        var coverToken = bookObj["book_cover"];
                        if (coverToken != null && coverToken.Type == JTokenType.String)
                        {
                            var s = coverToken.ToString().Trim();
                            if (!string.IsNullOrWhiteSpace(s))
                            {
                                TrySetStringPropertyIfExists(book, "CoverImageUrl", s);
                                TrySetStringPropertyIfExists(book, "CoverUrl", s);
                                TrySetStringPropertyIfExists(book, "ImageUrl", s);
                                // leave inline CoverImage null so UI can lazy-download when needed
                                book.CoverImage = null;
                            }
                        }
                        else
                        {
                            book.CoverImage = null;
                        }

                        // Preserve QR URL if present
                        var qrToken = bookObj["book_qr"];
                        if (qrToken != null && qrToken.Type == JTokenType.String)
                        {
                            var s = qrToken.ToString().Trim();
                            if (!string.IsNullOrWhiteSpace(s))
                            {
                                TrySetStringPropertyIfExists(book, "QrUrl", s);
                                TrySetStringPropertyIfExists(book, "BookQrUrl", s);
                            }
                        }

                        // Track available counts per batch key (or per-book if no batch key)
                        string batchKey = book.BatchRegistrationKey ?? $"__single__{book.BookId}";
                        if (!availableCounts.ContainsKey(batchKey))
                            availableCounts[batchKey] = 0;
                        if (string.Equals(book.Status, "Available", StringComparison.OrdinalIgnoreCase))
                            availableCounts[batchKey]++;

                        tempList.Add(book);
                    }
                    catch (Exception exItem)
                    {
                        Console.WriteLine($"⚠️ Error processing book item: {exItem.Message}");
                    }
                }

                // Second pass: set AvailableCopies on each BookInfo using the computed map
                foreach (var b in tempList)
                {
                    string batchKey = b.BatchRegistrationKey ?? $"__single__{b.BookId}";
                    if (availableCounts.TryGetValue(batchKey, out int cnt))
                        b.AvailableCopies = cnt;
                    else
                        b.AvailableCopies = 0;

                    result.Add(b);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching all books: {ex.Message}");
                // return whatever we've collected so far (resilient)
            }

            return result;
        }

        /// <summary>
        /// Tries to set a string property on the BookInfo instance by name using reflection.
        /// Silently ignores when the property doesn't exist or cannot be written.
        /// </summary>
        private static void TrySetStringPropertyIfExists(BookInfo book, string propName, string value)
        {
            if (book == null || string.IsNullOrWhiteSpace(propName)) return;
            try
            {
                var t = book.GetType();
                var p = t.GetProperty(propName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (p != null && p.CanWrite && (p.PropertyType == typeof(string) || p.PropertyType == typeof(object)))
                {
                    p.SetValue(book, value);
                }
            }
            catch { /* ignore */ }
        }
    }
}