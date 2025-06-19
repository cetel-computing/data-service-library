namespace DataServices.Config
{
    public class MinioConfigOptions
    {
        public string MinioUri { get; set; }

        public string AccessKey { get; set; }

        public string SecretKey { get; set; }

        public bool EnableHTTPS { get; set; } = false;

        public int Port { get; set; } = 80;

        public string Bucket { get; set; } = "";
    }
}