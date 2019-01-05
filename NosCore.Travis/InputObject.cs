namespace NosCore.Travis
{
    public class InputObject
    {
        public int Build_Id { get; set; }
        public string Travis_Branch { get; set; } = "";
        public string Travis_Commit { get; set; }
        public bool Travis_Pull_Request { get; set; }
        public string Travis_Repo_Slug { get; set; } = "";
        public int Travis_Test_Result { get; set; }
    }
}