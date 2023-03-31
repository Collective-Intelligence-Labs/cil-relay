using Cila.OmniChain;


namespace Cila
{
    public interface IExecutionChain
    {
        string ID {get;}

        //List<IAggregateState> Aggregates {get;set;}
    
        void Update();
        IEnumerable<ExecutionChainEvent> GetNewEvents(ulong length);
        void PushNewEvents(IEnumerable<ExecutionChainEvent> newEvents);

        ulong Length {get;}
    }

    public class ExecutionChainEvent
    {
        public DomainEvent Event {get;set;}
        
        public byte[] Original {get;set;}

    }

    public class ExecutionChain : IExecutionChain
    {
        public string ID { get; set; }
        public ulong Length { get => (ulong)_events.Count; }
        internal IChainClient ChainService { get => chainService; set => chainService = value; }

        private SortedList<ulong,ExecutionChainEvent> _events = new SortedList<ulong, ExecutionChainEvent>();

        private SortedList<ulong,byte[]> _originalEvents = new SortedList<ulong, byte[]>();

        private IChainClient chainService;

        private string _singletonAggregateID;

        public ExecutionChain(string singletonAggregateID)
        {
            this._singletonAggregateID = singletonAggregateID;
        }

        public IEnumerable<ExecutionChainEvent> GetNewEvents(ulong length)
        {
            if (length >= Length)
            {
                yield break;
            }
            for (ulong i = length ; i < Length; i++)
            {
                yield return _events[i];
            } 
        }

        public void Update()
        {
            var newEvents = ChainService.Pull(_singletonAggregateID, Length);
            AddNewEvents(newEvents.Select(x=> new ExecutionChainEvent(){Event = OmniChainSerializer.DeserializeDomainEvent(x), Original = x}));
        }

        public void PushNewEvents(IEnumerable<ExecutionChainEvent> newEvents)
        {
            chainService.Push(_singletonAggregateID,Length,newEvents.Select(x=> x.Original));
            AddNewEvents(newEvents);
        }

        private void AddNewEvents(IEnumerable<ExecutionChainEvent> newEvents)
        {
            if (newEvents == null)
            {
                return;
            }
            foreach (var e in newEvents)
            {
                if (e.Event.EvntIdx < (ulong)_events.Count)
                {
                    continue;
                }
                _events.Add(e.Event.EvntIdx, e);
            }
        }
    }
}