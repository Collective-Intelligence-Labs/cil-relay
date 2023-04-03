using Cila;
using Cila.Database;
using MongoDB.Driver;

public class EventStore
{
    private readonly IMongoCollection<ExecutionChainEvent> _events;

    public EventStore(MongoDatabase database)
    {
        _events = database.GetEventsCollection();;

        // Create an index on the AggregateId field
        var indexKeysDefinition = Builders<ExecutionChainEvent>.IndexKeys.Ascending(e => e.AggregateId);
        var indexModel = new CreateIndexModel<ExecutionChainEvent>(indexKeysDefinition);
        _events.Indexes.CreateOne(indexModel);
    }

    public async Task<IEnumerable<ExecutionChainEvent>> GetEvents(string aggregateId)
    {
        // Find all events for the specified aggregateId, sorted by version
        var filter = Builders<ExecutionChainEvent>.Filter.Eq(e => e.AggregateId, aggregateId);
        var sort = Builders<ExecutionChainEvent>.Sort.Ascending(e => e.Version);
        var events = await _events.Find(filter).Sort(sort).ToListAsync();
        return events;
    }

    public ulong? GetLatestVersion(string aggregateId)
    {
        // Find the latest version for the specified aggregateId
        var filter = Builders<ExecutionChainEvent>.Filter.Eq(e => e.AggregateId, aggregateId);
        var sort = Builders<ExecutionChainEvent>.Sort.Descending(e => e.Version);
        var latestVersion = _events.Find(filter).Sort(sort).FirstOrDefault();

        return latestVersion?.Version ?? null;
    }

    public async Task AppendEvents(string aggregateId, IEnumerable<ExecutionChainEvent> events)
    {
        await _events.InsertManyAsync(events);
    }
}