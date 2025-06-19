namespace DataServices.Data
{
    /// <summary>
    /// responsible for interacting with min.io database data
    /// </summary>
    public interface IMinioService
    {
        Task<byte[]> GetObject(string id);
    }
}
