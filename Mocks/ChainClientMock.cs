 
 using Cila.OmniChain;
 /*
 namespace Cila.Mocks
 {
 public class ChainClientMock : IChainClient
    {
        private List<DomainEvent> _events;

        public ChainClientMock(ulong number)
        {
            _events = new List<DomainEvent>();
            for (ulong i = 0; i < number; i++)
            {
                _events.Add(new DomainEvent{
                EvntIdx = i,
                EvntType = DomainEventType.NftTransfered,
                EvntPayload =  Google.Protobuf.ByteString.CopyFrom(new byte[]{1,1,1,1,1})
            });
            }
        }

        public IEnumerable<DomainEvent> Pull(ulong position)
        {
            for (ulong i = position; i < _events.Count; i++)
            {
                yield return _events[i];
            }
        }

        public void Push(ulong position, IEnumerable<DomainEvent> events)
        {
            _events.RemoveRange(position,_events.Count - position);
            _events.AddRange(events.ToArray());
        }
    }


 }

 */