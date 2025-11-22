using lib_track_kiosk.configs;
using lib_track_kiosk.models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace lib_track_kiosk.helpers
{
    internal class GetRatings
    {
        private static readonly HttpClient httpClient = new HttpClient();

        /// <summary>
        /// GET /api/kiosk/ratings
        /// Fetches all ratings with optional filters
        /// </summary>
        public static async Task<RatingsResponse> GetAllAsync(
            int? bookId = null,
            int? researchPaperId = null,
            int? userId = null,
            int page = 1,
            int limit = 25,
            string sort = "created_at",
            string order = "DESC")
        {
            try
            {
                var queryParams = new List<string>();

                if (bookId.HasValue)
                    queryParams.Add($"book_id={bookId.Value}");
                if (researchPaperId.HasValue)
                    queryParams.Add($"research_paper_id={researchPaperId.Value}");
                if (userId.HasValue)
                    queryParams.Add($"user_id={userId.Value}");

                queryParams.Add($"page={page}");
                queryParams.Add($"limit={limit}");
                queryParams.Add($"sort={sort}");
                queryParams.Add($"order={order}");

                var queryString = string.Join("&", queryParams);
                var url = $"{API_Backend.BaseUrl}/api/kiosk/ratings?{queryString}";

                var response = await httpClient.GetAsync(url);
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<RatingsResponse>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return result;
                }
                else
                {
                    Console.WriteLine($"⚠️ Failed to fetch ratings: {response.StatusCode} - {json}");
                    return new RatingsResponse { Success = false, Results = new List<RatingInfo>() };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error fetching ratings: {ex.Message}");
                return new RatingsResponse { Success = false, Results = new List<RatingInfo>() };
            }
        }

        /// <summary>
        /// GET /api/kiosk/ratings/batch/{batch_key}
        /// Fetches aggregated ratings for all books with the same batch_registration_key
        /// </summary>
        public static async Task<BatchRatingsResponse> GetByBatchKeyAsync(string batchKey)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(batchKey))
                {
                    Console.WriteLine("⚠️ Batch key is required");
                    return new BatchRatingsResponse { Success = false };
                }

                var url = $"{API_Backend.BaseUrl}/api/kiosk/ratings/batch/{Uri.EscapeDataString(batchKey)}";

                var response = await httpClient.GetAsync(url);
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };

                    var result = JsonSerializer.Deserialize<BatchRatingsResponse>(json, options);

                    if (result == null)
                    {
                        Console.WriteLine("⚠️ Deserialization returned NULL");
                        return new BatchRatingsResponse { Success = false };
                    }

                    return result;
                }
                else
                {
                    Console.WriteLine($"⚠️ Failed to fetch batch ratings: {response.StatusCode} - {json}");
                    return new BatchRatingsResponse { Success = false };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error fetching batch ratings: {ex.Message}");
                return new BatchRatingsResponse { Success = false };
            }
        }

        /// <summary>
        /// GET /api/kiosk/ratings/research/{research_paper_id}
        /// Fetches aggregated ratings for a research paper
        /// </summary>
        public static async Task<ResearchRatingsResponse> GetByResearchPaperIdAsync(int researchPaperId)
        {
            try
            {
                if (researchPaperId <= 0)
                {
                    Console.WriteLine("⚠️ Invalid research paper ID");
                    return new ResearchRatingsResponse { Success = false };
                }

                var url = $"{API_Backend.BaseUrl}/api/kiosk/ratings/research/{researchPaperId}";

                var response = await httpClient.GetAsync(url);
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };

                    var result = JsonSerializer.Deserialize<ResearchRatingsResponse>(json, options);
                    return result;
                }
                else
                {
                    Console.WriteLine($"⚠️ Failed to fetch research ratings: {response.StatusCode} - {json}");
                    return new ResearchRatingsResponse { Success = false };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error fetching research ratings: {ex.Message}");
                return new ResearchRatingsResponse { Success = false };
            }
        }

        /// <summary>
        /// GET /api/kiosk/ratings/{id}
        /// Fetches a single rating by ID
        /// </summary>
        public static async Task<SingleRatingResponse> GetByIdAsync(int ratingId)
        {
            try
            {
                if (ratingId <= 0)
                {
                    Console.WriteLine("⚠️ Invalid rating ID");
                    return new SingleRatingResponse { Success = false };
                }

                var url = $"{API_Backend.BaseUrl}/api/kiosk/ratings/{ratingId}";

                var response = await httpClient.GetAsync(url);
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<SingleRatingResponse>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return result;
                }
                else
                {
                    Console.WriteLine($"⚠️ Failed to fetch rating: {response.StatusCode} - {json}");
                    return new SingleRatingResponse { Success = false };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error fetching rating by ID: {ex.Message}");
                return new SingleRatingResponse { Success = false };
            }
        }
    }

    #region Response Models

    public class RatingsResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("limit")]
        public int Limit { get; set; }

        [JsonPropertyName("results")]
        public List<RatingInfo> Results { get; set; } = new List<RatingInfo>();

        [JsonPropertyName("message")]
        public string Message { get; set; }
    }

    public class BatchRatingsResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("batch_registration_key")]
        public string Batch_Registration_Key { get; set; }

        [JsonPropertyName("ratings_count")]
        public int Ratings_Count { get; set; }

        [JsonPropertyName("avg_rating")]
        public double? Avg_Rating { get; set; }

        [JsonPropertyName("comments")]
        public List<RatingComment> Comments { get; set; } = new List<RatingComment>();

        [JsonPropertyName("per_book")]
        public List<PerBookRating> Per_Book { get; set; } = new List<PerBookRating>();

        [JsonPropertyName("message")]
        public string Message { get; set; }
    }

    public class ResearchRatingsResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("research_paper_id")]
        public int Research_Paper_Id { get; set; }

        [JsonPropertyName("ratings_count")]
        public int Ratings_Count { get; set; }

        [JsonPropertyName("avg_rating")]
        public double? Avg_Rating { get; set; }

        [JsonPropertyName("comments")]
        public List<RatingComment> Comments { get; set; } = new List<RatingComment>();

        [JsonPropertyName("message")]
        public string Message { get; set; }
    }

    public class SingleRatingResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("result")]
        public RatingInfo Result { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }
    }

    public class RatingInfo
    {
        [JsonPropertyName("rating_id")]
        public int Rating_Id { get; set; }

        [JsonPropertyName("user_id")]
        public int? User_Id { get; set; }

        [JsonPropertyName("book_id")]
        public int? Book_Id { get; set; }

        [JsonPropertyName("research_paper_id")]
        public int? Research_Paper_Id { get; set; }

        [JsonPropertyName("star_rating")]
        public int Star_Rating { get; set; }

        [JsonPropertyName("comment")]
        public string Comment { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime Created_At { get; set; }

        [JsonPropertyName("user_email")]
        public string User_Email { get; set; }

        [JsonPropertyName("user_first_name")]
        public string User_First_Name { get; set; }

        [JsonPropertyName("user_last_name")]
        public string User_Last_Name { get; set; }

        [JsonPropertyName("book_title")]
        public string Book_Title { get; set; }

        [JsonPropertyName("research_title")]
        public string Research_Title { get; set; }
    }

    public class RatingComment
    {
        [JsonPropertyName("rating_id")]
        public int Rating_Id { get; set; }

        [JsonPropertyName("user_id")]
        public int? User_Id { get; set; }

        [JsonPropertyName("star_rating")]
        public int Star_Rating { get; set; }

        [JsonPropertyName("comment")]
        public string Comment { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime Created_At { get; set; }

        [JsonPropertyName("user_email")]
        public string User_Email { get; set; }

        [JsonPropertyName("user_first_name")]
        public string User_First_Name { get; set; }

        [JsonPropertyName("user_last_name")]
        public string User_Last_Name { get; set; }

        [JsonPropertyName("book_id")]
        public int? Book_Id { get; set; }
    }

    public class PerBookRating
    {
        [JsonPropertyName("book_id")]
        public int Book_Id { get; set; }

        [JsonPropertyName("batch_registration_key")]
        public string Batch_Registration_Key { get; set; }

        [JsonPropertyName("book_title")]
        public string Book_Title { get; set; }

        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("avg_rating")]
        public string Avg_Rating_String { get; set; }

        [JsonIgnore]
        public double? Avg_Rating
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Avg_Rating_String))
                    return null;
                if (double.TryParse(Avg_Rating_String, out double val))
                    return val;
                return null;
            }
        }
    }

    #endregion
}