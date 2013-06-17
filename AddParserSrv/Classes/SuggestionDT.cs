namespace AddParserSrv.Classes
{
    public enum SuggestionType
    {
        City,
        District,
        County
    }

    public class SuggestionDT
    {
        public string SuggestedWord { get; set; }
        public SuggestionType SuggestedType { get; set; }
        public bool IsFound { get; set; }
    }

}