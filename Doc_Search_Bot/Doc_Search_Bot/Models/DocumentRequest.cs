namespace Query_Quasar_Bot_API.Models
{
    public class DocumentRequest
    {
        internal readonly object DocumentFile;

        public string DocumentPath { get; set; }
        public List<string> UserQueries { get; set; }

    }

}
