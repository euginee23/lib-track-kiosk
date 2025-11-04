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
    /// <summary>
    /// Helper to fetch research papers from the backend.
    /// This version does NOT decode base64 -> Image for every row (avoids holding many GDI objects).
    /// Keep QRBase64 and decode on demand when the user requests details.
    /// </summary>
    internal class GetAllResearchPapers
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private static readonly string baseApiUrl = $"{API_Backend.BaseUrl}/api/research-papers";

        /// <summary>
        /// Fetches research papers. Optional comma-separated ids can be supplied (same as server supports).
        /// This method intentionally does not create Image instances for each row (keeps QRBase64 only).
        /// </summary>
        /// <param name="ids">Optional comma-separated ids query parameter to filter results.</param>
        public static async Task<List<AllResearchPaperInfo>> GetAllAsync(string ids = null)
        {
            var result = new List<AllResearchPaperInfo>();

            try
            {
                string apiUrl = $"{baseApiUrl}/";
                if (!string.IsNullOrWhiteSpace(ids))
                {
                    apiUrl = $"{apiUrl}?ids={Uri.EscapeDataString(ids)}";
                }

                HttpResponseMessage response = await httpClient.GetAsync(apiUrl);

                if (!response.IsSuccessStatusCode)
                    throw new Exception($"Failed to fetch research papers. Status code: {response.StatusCode}");

                string jsonString = await response.Content.ReadAsStringAsync();
                JObject json = JObject.Parse(jsonString);

                if (!(bool?)json["success"] == true)
                    throw new Exception("Failed to fetch research papers: success flag is false.");

                JToken dataToken = json["data"];
                if (dataToken == null) return result;

                JArray papersArray = dataToken as JArray ?? new JArray(dataToken);

                foreach (var item in papersArray)
                {
                    try
                    {
                        var obj = (JObject)item;

                        var paper = new AllResearchPaperInfo
                        {
                            ResearchPaperId = (int?)(obj["research_paper_id"]) ?? 0,
                            Title = obj["research_title"]?.ToString(),
                            YearPublication = obj["year_publication"]?.ToString(),
                            Abstract = obj["research_abstract"]?.ToString(),
                            Price = obj["research_paper_price"]?.ToString(),
                            CreatedAt = obj["created_at"]?.ToString(),
                            DepartmentId = (int?)obj["department_id"],
                            DepartmentName = obj["department_name"]?.ToString(),
                            Authors = obj["authors"]?.ToString(),
                            ShelfNumber = obj["shelf_number"]?.ToString(),
                            ShelfColumn = obj["shelf_column"]?.ToString(),
                            ShelfRow = obj["shelf_row"]?.ToString()
                        };

                        // Keep raw base64 only — do NOT decode to Image here.
                        var qrToken = obj["research_paper_qr"];
                        if (qrToken != null && qrToken.Type == JTokenType.String)
                        {
                            string base64 = qrToken.ToString();
                            paper.QRBase64 = string.IsNullOrWhiteSpace(base64) ? null : base64;
                            paper.QRImage = null; // decode on demand
                        }
                        else
                        {
                            paper.QRBase64 = null;
                            paper.QRImage = null;
                        }

                        // Compose shelf location string
                        paper.ShelfLocation = $"{paper.ShelfNumber ?? "N/A"}-{paper.ShelfColumn ?? "N/A"}-{paper.ShelfRow ?? "N/A"}";

                        result.Add(paper);
                    }
                    catch (Exception exItem)
                    {
                        Console.WriteLine($"⚠️ Error processing research paper item: {exItem.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching research papers: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Decodes a base64 (or data URI) string to an Image instance. Returns null on failure.
        /// This method is kept for on-demand decoding; call it only when you need to display a single QR.
        /// The caller is responsible for disposing the returned Image.
        /// </summary>
        public static Image DecodeBase64Image(string base64OrDataUri)
        {
            if (string.IsNullOrWhiteSpace(base64OrDataUri)) return null;

            try
            {
                string base64 = base64OrDataUri.Trim();

                if (base64.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
                {
                    int comma = base64.IndexOf(',');
                    if (comma >= 0) base64 = base64.Substring(comma + 1);
                }

                byte[] bytes = Convert.FromBase64String(base64);
                using var ms = new MemoryStream(bytes);
                using var srcImg = Image.FromStream(ms);
                return (Image)srcImg.Clone(); // caller must Dispose
            }
            catch (FormatException fe)
            {
                Console.WriteLine($"⚠️ Invalid base64 QR data: {fe.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error decoding research paper QR image: {ex.Message}");
                return null;
            }
        }
    }

    internal class AllResearchPaperInfo
    {
        public int ResearchPaperId { get; set; }
        public string Title { get; set; }
        public string YearPublication { get; set; }
        public string Abstract { get; set; }
        public string Price { get; set; }
        public string QRBase64 { get; set; }
        public Image QRImage { get; set; } // null by default; decode on demand
        public string CreatedAt { get; set; }
        public int? DepartmentId { get; set; }
        public string DepartmentName { get; set; }
        public string Authors { get; set; }

        public string ShelfNumber { get; set; }
        public string ShelfColumn { get; set; }
        public string ShelfRow { get; set; }
        public string ShelfLocation { get; set; }
    }
}