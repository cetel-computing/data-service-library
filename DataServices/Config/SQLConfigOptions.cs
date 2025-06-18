namespace DataServices.Config
{
    public class SQLConfigOptions
    {
        public IEnumerable<string> Hosts { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public string Database { get; set; }

        public string ConnectionString => $"host={string.Join(",", Hosts.ToList())};port=3306;user id={Username};password={Password};database={Database};";
    }
}
