using Elasticsearch.Net;
using Nest;

namespace DataServices.Data
{
    /// <summary>
    /// responsible for interacting with elastic database data
    /// </summary>
    public interface IElasticService
    {
        Task<IndexResponse> IndexAsync<T>(T document, Func<IndexDescriptor<T>, IIndexRequest<T>> selector,
            CancellationToken cancellationToken = default) where T : class;

        BulkAllObservable<T> BulkAll<T>(IEnumerable<T> docList, Func<BulkAllDescriptor<T>, IBulkAllRequest<T>> selector,
            CancellationToken cancellationToken = default) where T : class;

        Task<BulkResponse> IndexManyAsync<T>(IEnumerable<T> objects, IndexName index = null,
            CancellationToken cancellationToken = default) where T : class;

        Task<ISearchResponse<T>> SearchAsync<T>(Func<SearchDescriptor<T>, ISearchRequest> selector = null,
            CancellationToken cancellationToken = default) where T : class;

        Task<StringResponse> LowLevelSearchAsync(string index, PostData body, SearchRequestParameters requestParameters = null,
            CancellationToken cancellationToken = default);

        Task<StringResponse> LowLevelCountAsync(string index, PostData body, CountRequestParameters requestParameters = null,
            CancellationToken cancellationToken = default);

        Task<CountResponse> CountAsync<T>(Func<CountDescriptor<T>, ICountRequest> selector = null,
            CancellationToken cancellationToken = default) where T : class;

        Task<BulkAliasResponse> AliasAsync(Func<BulkAliasDescriptor, IBulkAliasRequest> selector,
            CancellationToken cancellationToken = default);
    }
}
