namespace lib_track_kiosk.models
{
    public class BackendResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Type { get; set; } // "book" or "research_paper"
        public BackendData Data { get; set; }
    }

    public class BackendData
    {
        public BookData Book { get; set; }
        public ResearchPaperData ResearchPaper { get; set; }
        public QrInfoData QrInfo { get; set; }
    }

    public class BookData
    {
        public string BookTitle { get; set; }
    }

    public class ResearchPaperData
    {
        public string ResearchTitle { get; set; }
    }

    public class QrInfoData
    {
        public int? BookId { get; set; }
        public string BookNumber { get; set; }
        public int? ResearchPaperId { get; set; }
    }
}