using System.Security.Cryptography;
using System.Text;
using Cila.OmniChain;
using Google.Protobuf.WellKnownTypes;
using MongoDB.Bson;
using Nethereum.Contracts;
using Nethereum.Util;
using Nethereum.Util.HashProviders;

namespace Cila
{
    public interface IExecutionChain
    {
        string ID { get; }
        void Update(string relayId);
    }

    public class ExecutionChainEvent
    {
        public string Id { get; set; }

        public string OriginChainId { get; set; }

        public string AggregateId { get; set; }

        public byte[] Serialized { get; set; }

        public byte[] Hash { get; set; }

        public int BlockNumber { get; set; }

        public ulong Version { get; set; }

    }

    public class ExecutionChain : IExecutionChain
    {
        public string ID { get; set; }
        internal IChainClient ChainService { get => chainService; set => chainService = value; }
        private IChainClient chainService;
        private string _singletonAggregateID;
        private readonly EventStore _eventStore;
        private readonly EventsDispatcher _eventsDispatcher;
        private uint _lastBlock = 0;

        private readonly KafkaProducer _producer;

        public ExecutionChain(string singletonAggregateID, EventStore eventStore, EventsDispatcher eventsDispatcher, KafkaProducer producer)
        {
            this._singletonAggregateID = singletonAggregateID;
            _eventStore = eventStore;
            _eventsDispatcher = eventsDispatcher;
            _producer = producer;
        }

        public void Update(string relayId)
        {
            var hashProvider = new Sha3KeccackHashProvider();
            var newEvents = ChainService.PullNewEvents(_lastBlock);
            newEvents = newEvents ?? new List<byte[]>();

            var aggregates = newEvents.Select(x =>
            {
                var domainEvent = OmniChainSerializer.DeserializeDomainEvent(x);
                return new ExecutionChainEvent()
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    Serialized = x,
                    AggregateId = _singletonAggregateID,
                    OriginChainId = ID,
                    Hash = hashProvider.ComputeHash(domainEvent.EvntPayload.ToByteArray()),
                    Version = domainEvent.EvntIdx
                };
            }).OrderBy(x => x.Version).GroupBy(x => x.AggregateId);

            foreach (var aggregate in aggregates)
            {
                var newVersion = aggregate.Max(x => x.Version);
                var currentVersion = _eventStore.GetLatestVersion(aggregate.Key);
                //TODO: Add conflic resolution logic here: we need to add getting also a hash of latest version merkle tree of all events and then checking if there are a different with the once we receive from the chain because we might push additional events that different by hash not by version
                if (currentVersion == null || currentVersion < newVersion)
                {
                    // selects new events if current Version null then all events
                    var events = currentVersion == null ? aggregate : aggregate.Where(x => x.Version > currentVersion);
                    var startIndex = events.Min(x => x.Version);

                    try
                    {
                        _eventsDispatcher.Dispatch(ID, aggregate.Key, relayId, events, (UInt32)startIndex);
                        _eventStore.AppendEvents(aggregate.Key, events);
                    }
                    catch (SmartContractCustomErrorRevertException e)
                    {
                        ProduceErrorEvent(events, aggregate.Key, relayId, e.Message);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }
        }

        private async Task ProduceErrorEvent(IEnumerable<ExecutionChainEvent> events, string aggregateId, string relayId, string errorMessage)
        {
            try
            {
                var operationId = aggregateId + events.Max(x => x.Version);
                var infEvent = new InfrastructureEvent
                {
                    Id = Guid.NewGuid().ToString(),
                    EvntType = InfrastructureEventType.NotSpecifiedEvent,
                    AggregatorId = aggregateId,
                    OperationId = operationId,
                    RelayId = relayId,
                    ErrorMessage = errorMessage,
                    Timestamp = Timestamp.FromDateTime(DateTime.UtcNow)
                };
                foreach (var e in events)
                {
                    infEvent.Events.Add(new DomainEventDto
                    {
                        Id = e.Version.ToString(),
                        Timespan = Timestamp.FromDateTime(DateTime.UtcNow),
                    });
                }
                await _producer.ProduceAsync("infr", infEvent);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: {0}", ex.Message);
                throw;
            }
        }
    }
}