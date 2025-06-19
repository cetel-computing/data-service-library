using DataServices.Config;
using Elasticsearch.Net;
using Nest;

namespace DataServices.Data
{
    /// <summary>
    /// primary class that is responsible for interacting with database data as object
    /// </summary>
    public class ElasticService : IElasticService
    {
        private readonly ElasticClient _elasticClient;

        /// <summary>
        /// Default CORE constructor
        /// </summary>
        /// <param name="config">Dependency-injected options</param>
        public ElasticService(ElasticConfigOptions config)
        {
            var indexName = config.ElasticIndexName;

            var uris = config.ElasticUri.Split(',');

            var pool = new SniffingConnectionPool(uris.Select(u => new Node(new Uri(u))));

            var settings = new ConnectionSettings(pool)
                .DefaultIndex(indexName);

            _elasticClient = new ElasticClient(settings);
            _elasticClient.Ping();
        }

        /// <summary>
        /// Default .NET Framework constructor, configure using supplied params
        /// </summary>
        /// <param name="connectionUri"></param>
        /// <param name="indexName"></param>
        public ElasticService(string connectionUri, string indexName)
        {
            var uris = connectionUri.Split(',');

            var pool = new SniffingConnectionPool(uris.Select(u => new Node(new Uri(u))));

            var settings = new ConnectionSettings(pool)
                .DefaultIndex(indexName);

            _elasticClient = new ElasticClient(settings);
        }

        //public static Task<BulkResponse> IndexManyAsync<T>(this IElasticClient client,IEnumerable<T> objects,IndexName index = null,
        //CancellationToken cancellationToken = default(CancellationToken)) where T : class;
        public Task<BulkResponse> IndexManyAsync<T>(IEnumerable<T> objects, IndexName index = null,
            CancellationToken cancellationToken = default) where T : class
        {
            return _elasticClient.IndexManyAsync(objects, index, cancellationToken);
        }

        public Task<IndexResponse> IndexAsync<T>(T document, Func<IndexDescriptor<T>, IIndexRequest<T>> selector,
            CancellationToken cancellationToken = default) where T : class
        {
            return _elasticClient.IndexAsync(document, selector, cancellationToken);
        }

        //public BulkAllObservable<T> BulkAll<T>(IEnumerable<T> documents,Func<BulkAllDescriptor<T>, IBulkAllRequest<T>> selector,
        //CancellationToken cancellationToken = default(CancellationToken)) where T : class;
        public BulkAllObservable<T> BulkAll<T>(IEnumerable<T> docList, Func<BulkAllDescriptor<T>, IBulkAllRequest<T>> selector,
            CancellationToken cancellationToken = default) where T : class
        {
            return _elasticClient.BulkAll(docList, selector, cancellationToken);
        }

        public Task<ISearchResponse<T>> SearchAsync<T>(Func<SearchDescriptor<T>, ISearchRequest> selector = null,
            CancellationToken cancellationToken = default) where T : class
        {
            return _elasticClient.SearchAsync(selector, cancellationToken);
        }

        public Task<StringResponse> LowLevelSearchAsync(string index, PostData body, SearchRequestParameters requestParameters = null,
            CancellationToken cancellationToken = default)
        {
            return _elasticClient.LowLevel.SearchAsync<StringResponse>(index, body, requestParameters, cancellationToken);
        }

        public Task<StringResponse> LowLevelCountAsync(string index, PostData body, CountRequestParameters requestParameters = null,
            CancellationToken cancellationToken = default)
        {
            return _elasticClient.LowLevel.CountAsync<StringResponse>(index, body, requestParameters, cancellationToken);
        }

        public Task<CountResponse> CountAsync<T>(Func<CountDescriptor<T>, ICountRequest> selector = null,
            CancellationToken cancellationToken = default) where T : class
        {
            return _elasticClient.CountAsync(selector, cancellationToken);
        }

        public Task<BulkAliasResponse> AliasAsync(Func<BulkAliasDescriptor, IBulkAliasRequest> selector,
            CancellationToken cancellationToken = default)
        {
            return _elasticClient.Indices.BulkAliasAsync(selector, cancellationToken);
        }
    }
}
