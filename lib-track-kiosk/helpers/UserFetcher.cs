using lib_track_kiosk.configs;
using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace lib_track_kiosk.helpers
{
    public static class UserFetcher
    {
        private static readonly string apiUrl = $"{API_Backend.BaseUrl}/api/users/registrations";

        /// <summary>
        /// Fetches user info including a new 'restriction' flag returned by the API.
        /// Returns: (FullName, Email, ContactNumber, Department, Position, YearLevel, ProfilePhoto, IsRestricted)
        /// </summary>
        public static async Task<(string FullName, string Email, string ContactNumber, string Department, string Position, string YearLevel, Image ProfilePhoto, bool IsRestricted)> GetUserInfoAsync(int userId)
        {
            using (HttpClient client = new HttpClient())
            {
                string userApiUrl = $"{apiUrl}/{userId}";
                HttpResponseMessage response = await client.GetAsync(userApiUrl);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Failed to fetch user info. Status code: {response.StatusCode}");
                }

                string jsonResponse = await response.Content.ReadAsStringAsync();
                JObject data = JObject.Parse(jsonResponse);
                var user = data["user"];

                if (user == null)
                {
                    throw new Exception("User data not found in the response.");
                }

                string fullName = $"{user["first_name"]} {user["last_name"]}";
                string email = user["email"]?.ToString() ?? "N/A";
                string contactNumber = user["contact_number"]?.ToString() ?? "N/A";
                string department = user["department_name"]?.ToString() ?? "N/A";
                string position = user["position"]?.ToString() ?? "N/A";
                string yearLevelRaw = user["year_level"]?.ToString() ?? "N/A";

                // ✅ Format year level
                string yearLevel = FormatYearLevel(yearLevelRaw);

                // 🖼️ Decode profile photo if available
                Image profilePhoto = null;
                var photoToken = user["profile_photo"];

                if (photoToken != null && photoToken.Type != JTokenType.Null)
                {
                    try
                    {
                        if (photoToken["data"] != null && photoToken["data"] is JArray byteArray)
                        {
                            byte[] imageBytes = byteArray.ToObject<byte[]>();
                            using (var ms = new MemoryStream(imageBytes))
                            {
                                profilePhoto = Image.FromStream(ms);
                            }
                        }
                        else if (photoToken.Type == JTokenType.String)
                        {
                            string base64 = photoToken.ToString();
                            if (base64.StartsWith("data:image"))
                                base64 = base64.Substring(base64.IndexOf(",") + 1);

                            byte[] imageBytes = Convert.FromBase64String(base64);
                            using (var ms = new MemoryStream(imageBytes))
                            {
                                profilePhoto = Image.FromStream(ms);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ Error decoding profile photo: {ex.Message}");
                    }
                }

                bool isRestricted = false;
                try
                {
                    var restrictionToken = user["restriction"];
                    if (restrictionToken != null && restrictionToken.Type != JTokenType.Null)
                    {
                        if (restrictionToken.Type == JTokenType.Integer || restrictionToken.Type == JTokenType.Float)
                        {
                            isRestricted = restrictionToken.Value<int>() != 0;
                        }
                        else
                        {
                            string rstr = restrictionToken.ToString();
                            if (int.TryParse(rstr, out int rInt))
                            {
                                isRestricted = rInt != 0;
                            }
                            else if (bool.TryParse(rstr, out bool rBool))
                            {
                                isRestricted = rBool;
                            }
                            else
                            {
                                // unknown format — default to false but log for debugging
                                Console.WriteLine($"⚠️ Unknown restriction token format: {restrictionToken.Type} / '{rstr}'");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Error parsing restriction flag: {ex.Message}");
                    isRestricted = false;
                }

                return (fullName, email, contactNumber, department, position, yearLevel, profilePhoto, isRestricted);
            }
        }

        /// <summary>
        /// Converts numeric year level into human-readable format.
        /// </summary>
        private static string FormatYearLevel(string yearLevel)
        {
            return yearLevel switch
            {
                "1" => "1st Year",
                "2" => "2nd Year",
                "3" => "3rd Year",
                "4" => "4th Year",
                _ => yearLevel // fallback
            };
        }
    }
}