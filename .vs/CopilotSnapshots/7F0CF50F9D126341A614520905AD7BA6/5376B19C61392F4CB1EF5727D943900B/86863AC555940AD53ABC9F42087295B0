using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace lib_track_kiosk.configs
{
    public static class SystemSettingsFetcher
    {
        private static readonly string apiUrl = $"{API_Backend.BaseUrl}/api/settings/system-settings";

        /// <summary>
        /// Fetches the full system settings from the backend.
        /// </summary>
        public static async Task<JObject> GetSystemSettingsAsync()
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(apiUrl);
                response.EnsureSuccessStatusCode();

                string json = await response.Content.ReadAsStringAsync();
                return JObject.Parse(json);
            }
        }

        /// <summary>
        /// Extracts borrowing limits (student and faculty) as a strongly typed object.
        /// </summary>
        public static async Task<(int studentBorrowDays, int facultyBorrowDays, int studentMaxBooks, int facultyMaxBooks)> GetBorrowingLimitsAsync()
        {
            JObject data = await GetSystemSettingsAsync();
            var limits = data["data"]?["borrowingLimits"];

            int studentBorrowDays = limits?["student"]?["borrowPeriod"]?.Value<int>() ?? 3;
            int facultyBorrowDays = limits?["faculty"]?["borrowPeriod"]?.Value<int>() ?? 5;
            int studentMaxBooks = limits?["student"]?["maxBooks"]?.Value<int>() ?? 3;
            int facultyMaxBooks = limits?["faculty"]?["maxBooks"]?.Value<int>() ?? 5;

            return (studentBorrowDays, facultyBorrowDays, studentMaxBooks, facultyMaxBooks);
        }

        /// <summary>
        /// Extracts fine structure for both student and faculty.
        /// </summary>
        public static async Task<(double studentDailyFine, double facultyDailyFine)> GetFineStructureAsync()
        {
            JObject data = await GetSystemSettingsAsync();
            var fine = data["data"]?["fineStructure"];

            double studentFine = fine?["student"]?["dailyFine"]?.Value<double>() ?? 5.00;
            double facultyFine = fine?["faculty"]?["dailyFine"]?.Value<double>() ?? 10.00;

            return (studentFine, facultyFine);
        }

        /// <summary>
        /// Extracts kiosk-related configuration flags.
        /// </summary>
        public static async Task<bool> GetPreventLowQuantityBorrowingAsync()
        {
            JObject data = await GetSystemSettingsAsync();
            return data["data"]?["kioskSettings"]?["preventLowQuantityBorrowing"]?.Value<bool>() ?? false;
        }

        /// <summary>
        /// Utility: Get borrow period days based on user type.
        /// </summary>
        public static async Task<int> GetBorrowDaysAsync(string userType)
        {
            var (studentBorrowDays, facultyBorrowDays, _, _) = await GetBorrowingLimitsAsync();

            if (userType.Equals("Student", StringComparison.OrdinalIgnoreCase))
                return studentBorrowDays;

            return facultyBorrowDays;
        }
    }
}
