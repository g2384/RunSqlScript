namespace RunSqlScript
{
    public sealed class Settings
    {
        public string[] Files { get; set; }
        public string ConnectionString { get; set; }

        public void Init()
        {
            Files = new string[0];
        }
    }
}
