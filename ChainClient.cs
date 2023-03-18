
using Nethereum.Web3;
using Nethereum.Contracts;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.RPC.Eth.DTOs;
using System.Numerics;
using Nethereum.Contracts.ContractHandlers;
using Nethereum.JsonRpc.Client;
using static Nethereum.RPC.Eth.DTOs.BlockParameter;
using Google.Protobuf;
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

        public ChainClientMock(int number)
        {
            _events = new List<DomainEvent>();
            for (int i = 0; i < number; i++)
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
        private Event<EthDomainEvent> _eventHandler;
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
            _eventHandler = _contract.GetEvent<EthDomainEvent>();
            _filterInput = _eventHandler.CreateFilterInput(BlockParameter.CreateEarliest);
            //
        }

        public const int MAX_LIMIT = 1000000;
        public const string AGGREGATE_ID = "0x4215a6F868D07227f1e2827A6613d87A5961B5f6";


        public async Task<IEnumerable<DomainEvent>> Pull(int position)
        {
            var logs = await _eventHandler.GetAllChangesAsync(_filterInput);
            var list = new List<DomainEvent>();
                foreach (var log in logs)
                {
                    Console.WriteLine($"Event Value: {log.Event.Value}, Sender: {log.Event.Sender}");
                    //list.Add(OmniChainSerializer.DeserializeWithMessageType(log.Event.Payload));
                    list.Add(Deserizlize(log.Event.Payload));
                    
                }
                _filterInput = _eventHandler.CreateFilterInput(BlockParameter.CreateLatest);
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
        public byte[][] Events {get;set;}
    }

    [Event("EthDomainEvent")]
    public class EthDomainEvent: IEventDTO
    {
        [Parameter("uint256", "eventNumber", 1, true)]
        public BigInteger Value { get; set; }

        [Parameter("address", "sender", 2, true)]
        public string Sender { get; set; }

        [Parameter("bytes", "payload", 3, true)]
        public byte[] Payload { get; set; }
    }
}