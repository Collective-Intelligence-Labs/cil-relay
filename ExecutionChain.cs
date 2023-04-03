using Cila.OmniChain;
using MongoDB.Bson;
using Nethereum.Util;
using Nethereum.Util.HashProviders;

namespace Cila
{
    public interface IExecutionChain
    {
        string ID {get;}
        void Update();
    }

    public class ExecutionChainEvent
    {
        public string Id {get;set;}

        public string OriginChainId {get; set;}

        public string AggregateId {get;set;}
        
        public byte[] Serialized {get;set;}

        public byte[] Hash {get;set;}

        public int BlockNumber {get;set;}

        public ulong Version {get;set;}

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

        public ExecutionChain(string singletonAggregateID, EventStore eventStore, EventsDispatcher eventsDispatcher)
        {
            this._singletonAggregateID = singletonAggregateID;
            _eventStore = eventStore;
            _eventsDispatcher = eventsDispatcher;
        }

        public void Update()
        {
            var hashProvider = new Sha3KeccackHashProvider();
            var newEvents = ChainService.PullNewEvents(_lastBlock);
            var aggregates = newEvents.Select(x => 
            { 
                var domainEvent = OmniChainSerializer.DeserializeDomainEvent(x);
                return new ExecutionChainEvent(){
                    Id = ObjectId.GenerateNewId().ToString(),
                Serialized = x,
                AggregateId = _singletonAggregateID,
                OriginChainId = ID,
                Hash = hashProvider.ComputeHash(domainEvent.EvntPayload.ToByteArray()), //replace with retrieving it from the chain and validating
                Version = domainEvent.EvntIdx
                };
            }).GroupBy(x=> x.AggregateId);

            foreach (var aggregate in aggregates){
                var newVersion = aggregate.Max(x=> x.Version);
                var startIndex = aggregate.Min(x=> x.Version);
                var currentVersion = _eventStore.GetLatestVersion(aggregate.Key);
                if (currentVersion == null || currentVersion < newVersion)
                {
                    // selects new events if current Version null then all events
                    var events = currentVersion == null ? aggregate : aggregate.Where(x=> x.Version > currentVersion);
                    _eventsDispatcher.Dispatch(ID, aggregate.Key , events, (UInt32)startIndex );
                    _eventStore.AppendEvents(aggregate.Key, events);
                }
            }
        }
    }
}