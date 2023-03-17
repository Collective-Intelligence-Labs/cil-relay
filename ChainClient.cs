
using Nethereum.Web3;
using Nethereum.Contracts;

using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.RPC.Eth.DTOs;
using System.Numerics;
using Nethereum.Contracts.ContractHandlers;
using Nethereum.JsonRpc.Client;

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
        }

        public const int MAX_LIMIT = 1000000;
        public const string AGGREGATE_ID = "0x4215a6F868D07227f1e2827A6613d87A5961B5f6";


        public async Task<IEnumerable<DomainEvent>> Pull(int position)
        {
             Console.WriteLine("Chain Service Pull execution started from position: {0}, aggregate: {1}", position, AGGREGATE_ID);
            var handler = _contract.GetFunction<PullFuncation>();
            var request = new PullFuncation{
                StartIndex = position,
                Limit = MAX_LIMIT,
                AggregateId = AGGREGATE_ID
            };
                var result =  await handler.CallAsync<PullEventsDTO>(request);
                Console.WriteLine("Chain Service Pull executed: {0}", result);
                return result.Events;
                //.Select(x=> {
                  //  var result = new DomainEvent{Payload = x.Item3, EventType = x.Item2};
                   // result.EventNumber = x.Item1; 
                  //  return result;});

            //return eventsDto.Events;
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
        [Parameter("DomainEvent[]",order:1)]
        public DomainEvent[] Events {get;set;}
    }
}