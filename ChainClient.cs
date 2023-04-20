
using Nethereum.Web3;
using Nethereum.Contracts;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.RPC.Eth.DTOs;
using System.Numerics;
using Nethereum.Contracts.ContractHandlers;
using Nethereum.JsonRpc.Client;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3.Accounts;
using System;

using Cila;
using Google.Protobuf;
using Org.BouncyCastle.Ocsp;

namespace Cila.OmniChain
{
    public class LoggingInterceptor : RequestInterceptor
    {
        public override Task InterceptSendRequestAsync(Func<string, string, object[], Task> interceptedSendRequestAsync, string method, string route = null, params object[] paramList)
        {
            Console.WriteLine($"Method: {method}");
            Console.WriteLine($"Params: {string.Join(", ", paramList)}");
            return interceptedSendRequestAsync(method, route, paramList);
        }
    }

    interface IChainClient
    {
        void Push(string aggregateId, UInt32 position, IEnumerable<byte[]> events);
        Task<string> PushAsync(string aggregateId, UInt32 position, IEnumerable<byte[]> events);
        IEnumerable<byte[]> Pull(string aggregateId, UInt32 position);
        IEnumerable<byte[]> PullNewEvents(UInt32 position);
        Task<IEnumerable<byte[]>> PullAsync(string aggregateId, UInt32 position);
        Task<string> GetRelayPermission();
    }

    [Function("pull")]
    public class PullFuncation : FunctionMessage
    {
        [Parameter("string", "aggregateId", 1)]
        public string AggregateId { get; set; }

        [Parameter("uint", "startIndex", 2)]
        public UInt32 StartIndex { get; set; }

        [Parameter("uint", "limit", 3)]
        public UInt32 Limit { get; set; }
    }

    [Function("pullBytes")]
    public class PullBytesFuncation : FunctionMessage
    {
        [Parameter("string", "aggregateId", 1)]
        public string AggregateId { get; set; }

        [Parameter("uint", "startIndex", 2)]
        public UInt32 StartIndex { get; set; }

        [Parameter("uint", "limit", 3)]
        public UInt32 Limit { get; set; }
    }

    [Function("push")]
    public class PushFuncation : FunctionMessage
    {
        [Parameter("string", "_aggregateId", 1)]
        public string AggregateId { get; set; }

        [Parameter("uint", "_position", 2)]
        public UInt32 Position { get; set; }

        [Parameter("DomainEvent[]", "events", 3)]
        public List<DomainEvent> Events { get; set; }
    }

    [Function("pushBytes")]
    public class PushBytesFunction : FunctionMessage
    {
        [Parameter("string", "aggregateId", 1)]
        public string AggregateId { get; set; }

        [Parameter("uint", "startIndex", 2)]
        public UInt32 Position { get; set; }

        [Parameter("bytes[]", "evnts", 3)]
        public List<byte[]> Events { get; set; }
    }

    public class EthChainClient : IChainClient
    {
        private Web3 _web3;
        private ContractHandler _handler;
        private string _privateKey;
        private readonly string _singletonAggregateID;
        private readonly Account _account;

        public EthChainClient(string rpc, string contract, string privateKey, string abi, string singletonAggregateID)
        {

            _privateKey = privateKey;
            _singletonAggregateID = singletonAggregateID;
            _account = new Nethereum.Web3.Accounts.Account(privateKey);
            Console.WriteLine("The account {0} is used to connect to contract {1}", _account.Address, contract);
            _web3 = new Web3(_account, rpc, log: new EthLogger());
            _handler = _web3.Eth.GetContractHandler(contract);
        }

        public const UInt32 MAX_LIMIT = 1000000;

        public async Task<IEnumerable<byte[]>> PullAsync(string aggregateId, UInt32 position)
        {
            Console.WriteLine("Chain Service Pull execution started from position: {0}, aggregate: {1}", position, aggregateId);
            var handler = _handler.GetFunction<PullBytesFuncation>();

            var request = new PullBytesFuncation
            {
                StartIndex = position,
                Limit = MAX_LIMIT,
                AggregateId = aggregateId
            };
            var result = await handler.CallAsync<PullEventsDTO>(request);

            Console.WriteLine("Chain Service Pull executed: {0}", result);
            return result.Events;
        }


        public Task<string> GetRelayPermission()
        {
            return _handler.GetFunction<ReadRelay>().CallAsync<string>();
        }

        public async Task<string> PushAsync(string aggregateId, UInt32 position, IEnumerable<byte[]> events)
        {

            var _queryHandler = _web3.Eth.GetContractQueryHandler<PushBytesFunction>();
            var txHandler = _web3.Eth.GetContractTransactionHandler<PushBytesFunction>();

            var request = new PushBytesFunction
            {
                Events = events.ToList(),
                Position = position,
                AggregateId = aggregateId
            };

            foreach (var ev in request.Events)
            {
                Console.WriteLine("Event: " + Convert.ToHexString(ev));
            }

            var gasEstimate = await txHandler.EstimateGasAsync(_handler.ContractAddress, request);
            request.Gas = new BigInteger(2 * gasEstimate.ToUlong());
            var result = await txHandler.SendRequestAsync(_handler.ContractAddress, request);

            Console.WriteLine("Chain Service Push executed: {0}", result);
            return result;

        }

        public IEnumerable<byte[]> Pull(string aggregateId, UInt32 position)
        {
            return PullAsync(aggregateId, position).GetAwaiter().GetResult();
        }

        public void Push(string aggregateId, UInt32 position, IEnumerable<byte[]> events)
        {
            PushAsync(aggregateId, position, events).GetAwaiter().GetResult();
        }


        public IEnumerable<byte[]> PullNewEvents(uint position)
        {
            return this.Pull(_singletonAggregateID, position);
        }
    }

    [Function("relay", "address")]
    public class ReadRelay
    {
    }

    [FunctionOutput]
    public class PullEventsDTO : IFunctionOutputDTO
    {
        [Parameter("bytes[]", order: 1)]
        public List<byte[]> Events { get; set; }
    }
}