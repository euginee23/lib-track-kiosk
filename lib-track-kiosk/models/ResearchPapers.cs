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
    }
}
