using lib_track_kiosk.test_forms;

namespace lib_track_kiosk.test_forms
{
    public class StoredFingerprint
    {
        public int UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FingerType { get; set; }
        public byte[] Template { get; set; }
    }
}