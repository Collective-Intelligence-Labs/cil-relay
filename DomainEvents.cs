using Nethereum.ABI.FunctionEncoding.Attributes;
using System.Numerics;


namespace Cila 
{
    public enum DomainEventType {
        NFTMinted = 0,
        NFTTransfered = 1
    }

    [FunctionOutput]
    public class DomainEvent
    {
        [Parameter("uint64", "evnt_idx", 1)]
        public ulong EventNumber { get; set; }

        [Parameter("DomainEventType", "evnt_type", 2)]
        public byte EventType {get;set;}

        [Parameter("bytes", "evnt_payload", 3)]
        public byte[] Payload {get;set;}
    }
}
