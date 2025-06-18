using DataServices.Config;
using MongoDB.Driver;

namespace DataServices.Data
{
    /// <summary>
    /// primary class that is responsible for interacting with mongo database data
    /// </summary>
    public class MongoService
    {
        private readonly IMongoDatabase _database;

        public MongoService(MongoConfigOptions settings)
        {
            var client = new MongoClient(settings.ConnectionString);
            _database = client.GetDatabase(settings.DatabaseName);
        }

        public async Task<List<T>> Get<T>(string collectionName, int? skip = null, int? limit = null) where T : class
        {
            var collection = _database.GetCollection<T>(collectionName);
            var filter = Builders<T>.Filter.Exists("_id");

            return await collection.Find(filter).Skip(skip).Limit(limit).ToListAsync();
        }

        public async Task<T> Get<T>(string collectionName, string id, string searchField = null, string searchVal = null) where T : class
        {
            var collection = _database.GetCollection<T>(collectionName);
            var filter = Builders<T>.Filter.Eq("_id", id);

            //allow optional field/value search (eg if the id must be within a certain domain)
            if (searchField != null && searchVal != null)
            {
                filter &= Builders<T>.Filter.Eq(searchField, searchVal);
            }

            return await collection.Find<T>(filter).FirstOrDefaultAsync();
        }

        public async Task<List<T>> Search<T>(string collectionName, string searchField, string searchVal) where T : class
        {
            var collection = _database.GetCollection<T>(collectionName);
            var filter = Builders<T>.Filter.Eq(searchField, searchVal);

            return await collection.Find<T>(filter).ToListAsync();
        }

        public async Task<T> Create<T>(string collectionName, T document) where T : class
        {
            var collection = _database.GetCollection<T>(collectionName);

            await collection.InsertOneAsync(document);

            return document;
        }

        public async Task<T> Update<T>(string collectionName, string id, T document, string searchField = null, string searchVal = null) where T : class
        {
            var collection = _database.GetCollection<T>(collectionName);
            var filter = Builders<T>.Filter.Eq("_id", id);

            //allow optional field/value search (eg if the id must be within a certain domain)
            if (searchField != null && searchVal != null)
            {
                filter &= Builders<T>.Filter.Eq(searchField, searchVal);
            }

            var rec = await collection.Find<T>(filter).FirstOrDefaultAsync();

            if (rec == null)
            {
                return null;
            }

            await collection.ReplaceOneAsync(filter, document);

            return document;
        }

        public async Task<T> UpdateMappedFields<T>(string collectionName, string id, T document, string searchField = null, string searchVal = null) where T : class
        {
            var collection = _database.GetCollection<T>(collectionName);
            var filter = Builders<T>.Filter.Eq("_id", id);

            //allow optional field/value search (eg if the id must be within a certain domain)
            if (searchField != null && searchVal != null)
            {
                filter &= Builders<T>.Filter.Eq(searchField, searchVal);
            }

            var rec = await collection.Find<T>(filter).FirstOrDefaultAsync();

            if (rec == null)
            {
                return null;
            }

            //work out what fields we have in the model to update (leaves any unmapped fields alone)
            var updateDefs = Builders<T>.Update.Combine(
                                typeof(T).GetProperties().Where(prop => prop.GetValue(document) != null)
                                                        .Select(prop => Builders<T>.Update.Set(prop.Name, prop.GetValue(document)))
                            );

            await collection.UpdateOneAsync(filter, updateDefs);

            return document;
        }

        public async Task<T> Remove<T>(string collectionName, string id, string searchField = null, string searchVal = null) where T : class
        {
            var collection = _database.GetCollection<T>(collectionName);
            var filter = Builders<T>.Filter.Eq("_id", id);

            //allow optional field/value search (eg if the id must be within a certain domain)
            if (searchField != null && searchVal != null)
            {
                filter &= Builders<T>.Filter.Eq(searchField, searchVal);
            }

            var rec = await collection.Find<T>(filter).FirstOrDefaultAsync();

            if (rec == null)
            {
                return null;
            }

            await collection.DeleteOneAsync(filter);

            return rec;
        }
    }
}
