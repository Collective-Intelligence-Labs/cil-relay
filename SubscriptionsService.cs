using Cila.Database;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cila
{

    public class SubscriptionsService
    {
        private readonly IMongoCollection<SubscriptionDocument> _subscriptions;

        public SubscriptionsService(MongoDatabase database)
        {   
            _subscriptions = database.GetSubscriptionsCollection();

             var indexKeysDefinition = Builders<SubscriptionDocument>.IndexKeys.Ascending(e => e.AggregateId);
             var indexModel = new CreateIndexModel<SubscriptionDocument>(indexKeysDefinition);
             _subscriptions.Indexes.CreateOne(indexModel);
        }

        public void Create(string aggregateId, string chainId)
        {
            _subscriptions.InsertOne(new SubscriptionDocument {
                Id = ObjectId.GenerateNewId().ToString(),
                ChainId = chainId,
                AggregateId = aggregateId
            });
        }

        public IEnumerable<SubscriptionDocument> GetAllExceptOrigin(string aggregateId, string originChainId)
        {
             var filter = Builders<SubscriptionDocument>.Filter.And(
                Builders<SubscriptionDocument>.Filter.Eq(e => e.AggregateId, aggregateId),
                Builders<SubscriptionDocument>.Filter.Ne(e => e.ChainId, originChainId)
                );
            return _subscriptions.Find(filter).ToList();
        }


        public IEnumerable<SubscriptionDocument> GetAllFor(string aggregateId)
        {
             var filter = Builders<SubscriptionDocument>.Filter.And(
                Builders<SubscriptionDocument>.Filter.Eq(e => e.AggregateId, aggregateId)
                );
            return _subscriptions.Find(filter).ToList();
        }
    }

}