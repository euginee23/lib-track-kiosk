namespace lib_track_kiosk.models
{
    public class LoginWrapper
    {
        public string message { get; set; }
        public LoginResponse user { get; set; }
        public string token { get; set; }
    }

    public class LoginRequest
    {
        public string identifier { get; set; }
        public string password { get; set; }
    }

    public class LoginResponse
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string StudentId { get; set; }
        public string ContactNumber { get; set; }
        public int Email_Verification { get; set; }
        public int Librarian_Approval { get; set; }
    }
}
