using Nethereum.ABI.FunctionEncoding.Attributes;
using System.Numerics;


namespace Cila 
{
    //public enum DomainEventType {
    //    NFTMinted = 0,
    //    NFTTransfered = 1
    //}

    ////[FunctionOutput]
    //public class DomainEvent
    //{
    //    //[Parameter("uint256", "idx", 1)]
    //    public BigInteger EventNumber { get; set; }

    //    //[Parameter("uint8", "t", 2)]
    //    public byte EventType {get;set;}

    //    //[Parameter("bytes", "payload", 3)]
    //    public byte[] Payload {get;set;}
    //}

    [FunctionOutput]
    public class PullDto
    {
        [Parameter("DomainEvent[]", 1)]
        public DomainEvent[] Events { get; set; }
    }
}
