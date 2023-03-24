using Cila.OmniChain;

namespace Cila
{
    public interface IExecutionChain
    {
        string ID {get;}

        //List<IAggregateState> Aggregates {get;set;}
    
        void Update();
        IEnumerable<DomainEvent> GetNewEvents(ulong length);
        void PushNewEvents(IEnumerable<DomainEvent> newEvents);

        ulong Length {get;}
    }

    public class ExecutionChain : IExecutionChain
    {
        public string ID { get; set; }
        public ulong Length { get => (ulong)_events.Count; }
        internal IChainClient ChainService { get => chainService; set => chainService = value; }

        private SortedList<ulong,DomainEvent> _events = new SortedList<ulong, DomainEvent>();
        private IChainClient chainService;

        public ExecutionChain()
        {
        }

        public IEnumerable<DomainEvent> GetNewEvents(ulong length)
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
            var newEvents = ChainService.Pull(Length);
            AddNewEvents(newEvents);
        }

        public void PushNewEvents(IEnumerable<DomainEvent> newEvents)
        {
            AddNewEvents(newEvents);
        }

        private void AddNewEvents(IEnumerable<DomainEvent> newEvents)
        {
            if (newEvents == null)
            {
                return;
            }
            foreach (var e in newEvents)
            {
                if (e.EvntIdx < (ulong)_events.Count)
                {
                    continue;
                }
                _events.Add(e.EvntIdx, e);
            }
        }
    }
}