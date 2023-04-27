using MongoDB.Driver;

namespace Cila.Database {

    public class MongoDatabase
    {
        private MongoClient _client;

        private string _dbname = "relay";

        private class Collections {
            public static string Events  = "events";
            public static string Subscriptions  = "subscriptions";
        }

        public MongoDatabase(OmniChainRelaySettings settings)
        {
            _client = new MongoClient(settings.MongoDBConnectionString);
        }

        public IMongoCollection<ExecutionChainEvent> GetEventsCollection()
        {
            return _client.GetDatabase(_dbname).GetCollection<ExecutionChainEvent>(Collections.Events);
        }

        public IMongoCollection<SubscriptionDocument> GetSubscriptionsCollection()
        {
            return _client.GetDatabase(_dbname).GetCollection<SubscriptionDocument>(Collections.Subscriptions);
        }
    }
}
