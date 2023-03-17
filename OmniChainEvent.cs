using Nethereum.ABI.FunctionEncoding.Attributes;


namespace Cila 
{
    public enum DomainEventType {
        NFTMinted = 0,
        NFTTransfered = 1
    }

    [FunctionOutput]
    public class OmniChainEvent
    {
        [Parameter("uint256", "idx", 1)]
        public int EventNumber { get; set; }

        [Parameter("uint8", "t", 2)]
        public byte EventType {get;set;}

        [Parameter("bytes", "payload", 3)]
        public byte[] Payload {get;set;}

      
    }
}
