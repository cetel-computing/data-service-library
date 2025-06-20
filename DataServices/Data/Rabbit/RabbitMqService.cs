using System.Text;
using DataServices.Config;
using RabbitMQ.Client;

namespace DataServices.Data
{
    public class RabbitMqService : IRabbitMqService
    {
        private readonly string _rabbitMqQueueName;
        private readonly ConnectionFactory _rabbitMqFactoryDef;

        private IConnection _connection;
        private readonly IDictionary<string, IModel> _channelSessionModels;

        public RabbitMqService(RabbitMqConfigOptions config)
        {
            _rabbitMqQueueName = config.RabbitMqQueueName;

            _rabbitMqFactoryDef = new ConnectionFactory()
            {
                HostName = config.RabbitMqHost,
                Port = config.RabbitMqPort,
                UserName = config.RabbitMqUser,
                Password = config.RabbitMqPassword,
                VirtualHost = config.RabbitMqVirtualHost
            };

            _channelSessionModels = new Dictionary<string, IModel>();
        }

        public void Publish(string message)
        {
            var body = Encoding.UTF8.GetBytes(message);

            if (_connection == null)
            {
                _connection = _rabbitMqFactoryDef.CreateConnection();
            }

            using var channel = _connection.CreateModel();
            var props = channel.CreateBasicProperties();

            channel.QueueDeclarePassive(_rabbitMqQueueName);

            channel.BasicPublish(exchange: "",
                routingKey: _rabbitMqQueueName,
                basicProperties: props,
                body: body);
        }

        public void Disconnect()
        {
            //disconnect and destroy persistent rabbit connection and channel
            foreach (var (consumerTag, channel) in _channelSessionModels)
            {
                if (channel == null)
                {
                    continue;
                }

                if (!channel.IsOpen)
                {
                    continue;
                }

                if (consumerTag == null)
                {
                    continue;
                }

                channel.BasicCancel(consumerTag);
                channel.Close(200, $"Closing RabbitMQ channel {consumerTag}");
            }

            if (_connection == null)
            {
                return;
            }

            if (_connection.IsOpen)
            {
                _connection.Close();
            }
        }
    }
}
