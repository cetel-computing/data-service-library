namespace DataServices.Config
{
    public class CassandraConfigOptions
    {
        public IEnumerable<string> Hosts { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public string Database { get; set; }
    }
}