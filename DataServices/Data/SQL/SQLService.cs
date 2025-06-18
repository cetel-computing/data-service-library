using Dapper;
using MySqlConnector;

namespace DataServices.Data
{
    /// <summary>
    /// primary class that is responsible for interacting with sql database data
    /// </summary>
    public class SQLService
    {
        private readonly MySqlConnection _client;

        public SQLService(string connectionString)
        {
            _client = new MySqlConnection(connectionString);

            SimpleCRUD.SetDialect(SimpleCRUD.Dialect.MySQL);

            _client.Open();
        }

        public async Task<int> Count(string sql, object? parameters = null)
        {
            return await _client.ExecuteScalarAsync<int>(sql, parameters);
        }

        public async Task<IEnumerable<T>> Get<T>() where T : class
        {
            return await _client.GetListAsync<T>();
        }

        public async Task<IEnumerable<T>> Get<T>(string sql, object? parameters = null) where T : class
        {
            return await _client.GetListAsync<T>(sql, parameters);
        }

        public async Task<T> Get<T>(string id) where T : class
        {
            return await _client.GetAsync<T>(id);
        }

        public async Task<IEnumerable<T>> GetByQuery<T>(string sql, object? parameters = null) where T : class
        {
            //eg "select b_name as ListDomain, b_d_id as DId, d_name as Domain, b_u_id as UId, c_email as Email, b_date as Date from blacklist
            //left join domain on d_id = b_d_id left join user on u_id = b_u_id left join contact on c_id = u_c_id"

            return await _client.QueryAsync<T>(sql, parameters);
        }

        public async Task<int?> Execute(string sql, object? parameters = null)
        {
            return await _client.ExecuteAsync(sql, parameters);
        }

        public async Task<int?> Create<T>(T document) where T : class
        {
            return await _client.InsertAsync(document);
        }

        public async Task<int> Update<T>(T document) where T : class
        {
            return await _client.UpdateAsync(document);
        }

        public async Task<int> Remove<T>(string id) where T : class
        {
            var document = await _client.GetAsync<T>(id);

            if (document == null) return 0;

            return await _client.DeleteAsync(document);
        }

        public void Dispose()
        {
            _client.Close();
        }
    }
}
