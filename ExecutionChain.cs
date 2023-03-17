using Cila.OmniChain;

namespace Cila
{
    public interface IExecutionChain
    {
        string ID {get;}

        //List<IAggregateState> Aggregates {get;set;}
    
        void Update();
        IEnumerable<OmniChainEvent> GetNewEvents(int length);
        void PushNewEvents(IEnumerable<OmniChainEvent> newEvents);

        int Length {get;}
    }

    public class ExecutionChain : IExecutionChain
    {
        public string ID { get; set; }
        public int Length { get => _events.Count; }
        internal IChainClient ChainService { get => chainService; set => chainService = value; }

        private SortedList<int,OmniChainEvent> _events = new SortedList<int, OmniChainEvent>();
        private IChainClient chainService;

        public ExecutionChain()
        {
        }

        public IEnumerable<OmniChainEvent> GetNewEvents(int length)
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

        public void PushNewEvents(IEnumerable<OmniChainEvent> newEvents)
        {
            AddNewEvents(newEvents);
        }

        private void AddNewEvents(IEnumerable<OmniChainEvent> newEvents)
        {
            if (newEvents == null)
            {
                return;
            }
            foreach (var e in newEvents)
            {
                _events.Add(e.EventNumber, e);
            }
        }
    }
}