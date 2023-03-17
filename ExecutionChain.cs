using Cila.OmniChain;

namespace Cila
{
    public interface IExecutionChain
    {
        string ID {get;}

        //List<IAggregateState> Aggregates {get;set;}
    
        void Update();
        IEnumerable<DomainEvent> GetNewEvents(int length);
        void PushNewEvents(IEnumerable<DomainEvent> newEvents);

        int Length {get;}
    }

    public class ExecutionChain : IExecutionChain
    {
        public string ID { get; set; }
        public int Length { get => _events.Count; }
        internal IChainClient ChainService { get => chainService; set => chainService = value; }

        private SortedList<int,DomainEvent> _events = new SortedList<int, DomainEvent>();
        private IChainClient chainService;

        public ExecutionChain()
        {
        }

        public IEnumerable<DomainEvent> GetNewEvents(int length)
        {
            if (length >= Length)
            {
                yield break;
            }
            for (int i = length - 1 ; i < Length; i++)
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
                _events.Add((int)e.EventNumber, e);
            }
        }
    }
}