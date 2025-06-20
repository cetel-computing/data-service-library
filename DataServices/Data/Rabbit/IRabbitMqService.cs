namespace DataServices.Data
{
    /// <summary>
    /// responsible for interacting with rabbit mq
    /// </summary>
    public interface IRabbitMqService
    {
        void Disconnect();

        void Publish(string message);
    }
}
