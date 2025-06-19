using Cassandra;

namespace DataServices.Data
{
    /// <summary>
    /// responsible for interacting with cassandra database data
    /// </summary>
    public interface ICassandraService : IDisposable
    {
        Task<List<T>> Get<T>() where T : class;

        Task<List<T>> Get<T>(UdtMap udtmap) where T : class;

        Task<T> Get<T>(IDictionary<string, string> fieldValuePairs) where T : class;

        Task<List<T>> Search<T>(IDictionary<string, string> fieldValuePairs) where T : class;

        Task<List<T>> Search<T>(UdtMap udtmap, IDictionary<string, string> fieldValuePairs) where T : class;

        Task<T> Create<T>(T document) where T : class;

        Task<T> Update<T>(IDictionary<string, string> fieldValuePairs, T document) where T : class;

        Task<T> Remove<T>(IDictionary<string, string> fieldValuePairs) where T : class;
    }
}