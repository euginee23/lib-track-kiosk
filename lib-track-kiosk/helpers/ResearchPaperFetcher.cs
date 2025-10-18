using lib_track_kiosk.configs;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace lib_track_kiosk.helpers
{
    public class ResearchPaper
    {
        public string Title { get; set; }
        public string Authors { get; set; }
        public string Abstract { get; set; }
        public string ShelfLocation { get; set; }
        public string Year { get; set; }
        public string Department { get; set; }
        public string Price { get; set; }
        public string Status { get; set; }
    }

    public static class ResearchPaperFetcher
    {
        private static readonly HttpClient client = new HttpClient();
        private static readonly string apiUrl = $"{API_Backend.BaseUrl}/api/research-papers";

        public static async Task<ResearchPaper> GetResearchPaperAsync(int researchPaperId)
        {
            HttpResponseMessage response = await client.GetAsync($"{apiUrl}/{researchPaperId}");

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Failed to fetch research paper. Status code: {response.StatusCode}");

            string jsonString = await response.Content.ReadAsStringAsync();
            JObject json = JObject.Parse(jsonString);

            if (!(bool)json["success"])
                throw new Exception("Research paper not found.");

            JObject data = (JObject)json["data"];

            return new ResearchPaper
            {
                Title = data["research_title"]?.ToString() ?? "N/A",
                Authors = data["authors"]?.ToString()?.Replace(",", ", ") ?? "N/A",
                Abstract = data["research_abstract"]?.ToString() ?? "N/A",
                ShelfLocation = $"{data["shelf_number"] ?? "N/A"}-{data["shelf_column"] ?? "N/A"}-{data["shelf_row"] ?? "N/A"}",
                Year = data["year_publication"]?.ToString() ?? "N/A",
                Department = data["department_name"]?.ToString() ?? "N/A",
                Price = data["research_paper_price"]?.ToString() ?? "N/A",
                Status = data["status"]?.ToString() ?? "N/A"
            };
        }
    }
}
