using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_track_kiosk.models
{
    public class ResearchPaper
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Authors { get; set; }
        public int Year { get; set; }
        public string Abstract { get; set; }

        // Added fields to carry department and shelf info from the API
        public int? DepartmentId { get; set; }
        public string DepartmentName { get; set; }
        public string ShelfLocation { get; set; }
    }
}