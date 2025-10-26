using System.Collections.Generic;
using lib_track_kiosk.helpers;

namespace lib_track_kiosk.models
{
    public class GroupedBook
    {
        public string BatchKey { get; set; }
        public BookInfo Representative { get; set; }
        public int TotalCopies { get; set; }
        public int AvailableCopies { get; set; }
        public Dictionary<string, int> StatusCounts { get; set; } = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);
        public List<BookInfo> Copies { get; set; } = new List<BookInfo>();
    }
}