namespace DataServices.Config
{
    public class RabbitMqConfigOptions
    {
        public string RabbitMqUser { get; set; }

        public string RabbitMqPassword { get; set; }

        public string RabbitMqHost { get; set; }

        public int RabbitMqPort { get; set; }

        public string RabbitMqVirtualHost { get; set; }

        public string RabbitMqQueueName { get; set; }
    }
}
