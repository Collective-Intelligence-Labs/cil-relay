using MongoDB.Driver;

namespace Cila.Database {

    public class MongoDatabase
    {
        private MongoClient _client;

        public MongoDatabase(OmniChainRelaySettings settings)
        {
            _client = new MongoClient(settings.MongoDBConnectionString);
        }

        public IMongoCollection<DomainEvent> GetEvents()
        {
            return _client.GetDatabase("relay").GetCollection<DomainEvent>("events");
        }
    }
}
