using Cassandra;
using Cassandra.Mapping;
using DataServices.Config;

namespace DataServices.Data
{
    /// <summary>
    /// primary class that is responsible for interacting with cassandra database data
    /// </summary>
    public class CassandraService : ICassandraService
    {
        private readonly ICluster _database;

        private readonly ISession _session;

        private readonly IMapper _mapper;

        public CassandraService(CassandraConfigOptions settings)
        {
            _database = Cluster.Builder()
                            .AddContactPoints(settings.Hosts)
                            .WithCredentials(settings.Username, settings.Password)
                            .WithExecutionProfiles(opts => opts
                                .WithProfile("select", profile => profile
                                    .WithConsistencyLevel(ConsistencyLevel.LocalOne))
                                .WithProfile("update", profile => profile
                                    .WithConsistencyLevel(ConsistencyLevel.Quorum)))
                            .Build();

            _session = _database.Connect(settings.Database);

            _mapper = new Mapper(_session);
        }

        public async Task<List<T>> Get<T>() where T : class
        {
            var cql = Cql.New().WithExecutionProfile("select");

            return (await _mapper.FetchAsync<T>(cql)).ToList();
        }

        public async Task<List<T>> Get<T>(UdtMap udtmap) where T : class
        {
            _session.UserDefinedTypes.Define(udtmap);

            var cql = Cql.New().WithExecutionProfile("select");

            return (await _mapper.FetchAsync<T>(cql)).ToList();
        }

        public async Task<T> Get<T>(IDictionary<string, string> fieldValuePairs) where T : class
        {
            var statement = "WHERE " + fieldValuePairs.First().Key + "=?";

            //allow optional field/value search (eg if the id must be within a certain domain)
            if (fieldValuePairs.Count > 1)
            {
                foreach (var pair in fieldValuePairs.Skip(1))
                {
                    statement += " AND " + pair.Key + "=?";
                }
            }

            var cql = Cql.New(statement, fieldValuePairs.Select(p => p.Value).ToArray()).WithExecutionProfile("select");

            return await _mapper.SingleOrDefaultAsync<T>(cql);
        }

        public async Task<List<T>> Search<T>(IDictionary<string, string> fieldValuePairs) where T : class
        {
            var statement = "WHERE " + fieldValuePairs.First().Key + "=?";

            //allow optional field/value search (eg if the id must be within a certain domain)
            if (fieldValuePairs.Count > 1)
            {
                foreach (var pair in fieldValuePairs.Skip(1))
                {
                    statement += " AND " + pair.Key + "=?";
                }
            }

            var cql = Cql.New(statement, fieldValuePairs.Select(p => p.Value).ToArray()).WithExecutionProfile("select");

            return (await _mapper.FetchAsync<T>(cql)).ToList();
        }

        public async Task<List<T>> Search<T>(UdtMap udtmap, IDictionary<string, string> fieldValuePairs) where T : class
        {
            _session.UserDefinedTypes.Define(udtmap);

            var statement = "WHERE " + fieldValuePairs.First().Key + "=?";

            //allow optional field/value search (eg if the id must be within a certain domain)
            if (fieldValuePairs.Count > 1)
            {
                foreach (var pair in fieldValuePairs.Skip(1))
                {
                    statement += " AND " + pair.Key + "=?";
                }
            }

            var cql = Cql.New(statement, fieldValuePairs.Select(p => p.Value).ToArray()).WithExecutionProfile("select");

            return (await _mapper.FetchAsync<T>(cql)).ToList();
        }

        public async Task<T> Create<T>(T document) where T : class
        {
            var existing = await _mapper.InsertIfNotExistsAsync(document, "update");

            if (existing.Existing != null)
            {
                return null;
            }

            return document;
        }

        public async Task<T> Update<T>(IDictionary<string, string> fieldValuePairs, T document) where T : class
        {
            var statement = "WHERE " + fieldValuePairs.First().Key + "=?";

            //allow optional field/value search (eg if the id must be within a certain domain)
            if (fieldValuePairs.Count > 1)
            {
                foreach (var pair in fieldValuePairs.Skip(1))
                {
                    statement += " AND " + pair.Key + "=?";
                }
            }

            var cql = Cql.New(statement, fieldValuePairs.Select(p => p.Value).ToArray()).WithExecutionProfile("select");

            var recs = (await _mapper.FetchAsync<T>(cql)).ToList();

            if (recs == null || !recs.Any())
            {
                return null;
            }

            if (recs.Count > 1)
            {
                return null;
            }

            await _mapper.UpdateAsync(document, "update");

            return document;
        }

        public async Task<T> Remove<T>(IDictionary<string, string> fieldValuePairs) where T : class
        {
            var statement = "WHERE " + fieldValuePairs.First().Key + "=?";

            //allow optional field/value search (eg if the id must be within a certain domain)
            if (fieldValuePairs.Count > 1)
            {
                foreach (var pair in fieldValuePairs.Skip(1))
                {
                    statement += " AND " + pair.Key + "=?";
                }
            }

            var cql = Cql.New(statement, fieldValuePairs.Select(p => p.Value).ToArray()).WithExecutionProfile("select");

            var recs = (await _mapper.FetchAsync<T>(cql)).ToList();

            if (recs == null || !recs.Any())
            {
                return null;
            }

            if (recs.Count > 1)
            {
                return null;
            }

            var rec = recs.First();

            await _mapper.DeleteAsync(rec, "update");

            return rec;
        }

        public void Dispose()
        {
            _session.Dispose();

            _database.Dispose();
        }
    }
}
