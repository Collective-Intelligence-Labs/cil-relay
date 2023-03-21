
using Nethereum.Web3;
using Nethereum.Contracts;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.RPC.Eth.DTOs;
using System.Numerics;
using Nethereum.Contracts.ContractHandlers;
using Nethereum.JsonRpc.Client;
using Nethereum.Hex.HexTypes;
using OmniChain;

namespace Cila.OmniChain
{
    public class LoggingInterceptor : RequestInterceptor
    {
        public override Task InterceptSendRequestAsync(Func<string, string, object[], Task> interceptedSendRequestAsync, string method, string route = null, params object[] paramList)
        {
            Console.WriteLine($"Method: {method}");
            Console.WriteLine($"Params: {string.Join(", ", paramList)}");
            return interceptedSendRequestAsync(method,route,paramList);
        }
    }

    interface IChainClient
    {
        

        void Push(int position, IEnumerable<DomainEvent> events);
        IEnumerable<DomainEvent> Pull(int position);
    }

    public class ChainClientMock : IChainClient
    {
        private List<DomainEvent> _events;

        public ChainClientMock(ulong number)
        {
            _events = new List<DomainEvent>();
            for (ulong i = 0; i < number; i++)
            {
                _events.Add(new DomainEvent{
                EventNumber = i,
                EventType = 1,
                Payload = new byte[]{1,1,1,1,1}
            });
            }
        }

        public IEnumerable<DomainEvent> Pull(int position)
        {
            for (int i = position; i < _events.Count; i++)
            {
                yield return _events[i];
            }
        }

        public void Push(int position, IEnumerable<DomainEvent> events)
        {
            _events.RemoveRange(position,_events.Count - position);
            _events.AddRange(events.ToArray());
        }
    }

    [Function("pull")]
    public class PullFuncation: FunctionMessage
    {
        [Parameter("address", "aggregateId", 1)]
        public string AggregateId {get;set;}

        [Parameter("uint", "startIndex", 2)]
        public int StartIndex {get;set;}

        [Parameter("uint", "limit", 3)]
        public int Limit {get;set;}
    }

    [Function("pullBytes")]
    public class PullBytesFuncation: FunctionMessage
    {
        [Parameter("address", "aggregateId", 1)]
        public string AggregateId {get;set;}

        [Parameter("uint", "startIndex", 2)]
        public int StartIndex {get;set;}

        [Parameter("uint", "limit", 3)]
        public int Limit {get;set;}
    }

    [Function("push")]
    public class PushFuncation: FunctionMessage
    {
        [Parameter("address", "_aggregateId", 1)]
        public string AggregateId { get; set; }

        [Parameter("uint", "_position", 2)]
        public int Position {get;set;}

        [Parameter("DomainEvent[]", "events", 3)]
        public List<DomainEvent> Events {get;set;}
    }

    public class EthChainClient : IChainClient
    {
        private Web3 _web3;
        private ContractHandler _handler;
        private Event<OmnichainEvent> _eventHandler;
        private NewFilterInput _filterInput;
        private string _privateKey;

        private Contract _contract;

        public EthChainClient(string rpc, string contract, string privateKey, string abi)
        {
            
            _privateKey = privateKey;
            var account = new Nethereum.Web3.Accounts.Account(privateKey);
            _web3 = new Web3(account, rpc);
            _web3.Client.OverridingRequestInterceptor = new LoggingInterceptor();
            _handler = _web3.Eth.GetContractHandler(contract);
            _contract = _web3.Eth.GetContract(abi, contract);
            _eventHandler = _handler.GetEvent<OmnichainEvent>();

            var block = _web3.Eth.Blocks.GetBlockNumber.SendRequestAsync().Result;
            _filterInput = _eventHandler.CreateFilterInput(new BlockParameter(block.ToUlong() - 1024), BlockParameter.CreateLatest());
            //
        }

        public const int MAX_LIMIT = 1000000;
        public const string AGGREGATE_ID = "0x72d6b903899ED707306B7B1B5DD3D3b42195870c";


           

        public async Task<IEnumerable<DomainEvent>> Pull(int position)
        {
             Console.WriteLine("Chain Service Pull execution started from position: {0}, aggregate: {1}", position, AGGREGATE_ID);
             var handler = _handler.GetFunction<PullBytesFuncation>();
             var request = new PullBytesFuncation{
                StartIndex = position,
                    Limit = MAX_LIMIT,
                    AggregateId = AGGREGATE_ID
                };
                var result =  await handler.CallAsync<PullEventsDTO>(request);
                Console.WriteLine("Chain Service Pull executed: {0}", result);
                //return result.Events;   
                return Enumerable.Empty<DomainEvent>();
        }

        public async Task<IEnumerable<DomainEvent>> Pull3(int position)
        {
            var logs = await _eventHandler.GetAllChangesAsync(_filterInput);
            var list = new List<DomainEvent>();
                foreach (var log in logs)
                {
                    Console.WriteLine($"Event Value: {log.Event.Version}, Sender: {log.Event.Type}");
                    //list.Add(OmniChainSerializer.DeserializeWithMessageType(log.Event.Payload));
                    list.Add(new DomainEvent
                    {
                        Payload = log.Event.Payload,
                        EventNumber = log.Event.Version,
                        EventType = log.Event.Type
                    });
                    
                    
                }
                _filterInput = _eventHandler.CreateFilterInput(BlockParameter.CreateLatest(),BlockParameter.CreateLatest());
                return list;
        }

        public async Task ObserveEventAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Wait for a short period before checking for new events again.
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }

        public async Task<string> Push(int position, IEnumerable<DomainEvent> events)
        {
            var handler = _handler.GetFunction<PushFuncation>();
            var request = new PushFuncation{
                Events = events.ToList(),
                Position = position,
                AggregateId = "0"
            };
            var result = await handler.CallAsync<string>(request);
            Console.WriteLine("Chain Service Push} executed: {0}", result);
            return result;
        }

        public DomainEvent Deserizlize(byte[] data)
        {
            return new DomainEvent();
        }

        IEnumerable<DomainEvent> IChainClient.Pull(int position)
        {
            return Pull(position).GetAwaiter().GetResult();
        }

        void IChainClient.Push(int position, IEnumerable<DomainEvent> events)
        {
            Push(position,events).GetAwaiter().GetResult();
        }
    }

    [FunctionOutput]
    public class PullEventsDTO: IFunctionOutputDTO
    {
        [Parameter("bytes[]",order:1)]
        public List<byte[]> Events {get;set;}
    }

    [Event("OmnichainEvent")]
    public class OmnichainEvent: IEventDTO
    {
        [Parameter("uint64", "_idx", 1, true)]
        public ulong Version { get; set; }

        [Parameter("uint8", "_type", 2, true)]
        public byte Type { get; set; }

        [Parameter("bytes", "_payload", 3, true)]
        public byte[] Payload { get; set; }
    }
}