

using System.Security.Cryptography;
using System.Text;
using Cila.OmniChain;
using Google.Protobuf.WellKnownTypes;
using static Confluent.Kafka.ConfigPropertyNames;

namespace Cila
{
    public class EventsDispatcher
    {
        private readonly SubscriptionsService _subscriptionsService;
        private readonly OmniChainRelaySettings _settings;
        private readonly KafkaProducer _producer;

        public EventsDispatcher(SubscriptionsService subscriptionsService, OmniChainRelaySettings settings, KafkaProducer kafkaProducer)
        {
            this._subscriptionsService = subscriptionsService;
            this._settings = settings;
            this._producer = kafkaProducer;
        }

        public void Dispatch(string originChainId, string aggregateId, string relayId, IEnumerable<ExecutionChainEvent> events, UInt32 startIndex)
        {
            var otherChains = _subscriptionsService.GetAllExceptOrigin(aggregateId, originChainId);
            //var clients = GetSubscriptions(aggregateId, originChainId);
            foreach (var chain in otherChains)
            {
                var client = CreateChainClientInstance(chain.ChainId, GetChainClientType(chain.ChainId));
                var chainId = client.GetChainId();

                ProduceTransmittedEvent(events, aggregateId, relayId, chainId);
                
                var tx = client.PushAsync(aggregateId, startIndex, events.Select(x => x.Serialized)).GetAwaiter().GetResult();

                var operationId = aggregateId + events.Max(x => x.Version);
                ProduceSyncTransactionExecutedEvent(chainId, aggregateId, operationId, relayId, tx);     
            }
        }

        private IEnumerable<IChainClient> GetSubscriptions(string aggregateId, string originChainId)
        {
            var chains = _subscriptionsService.GetAllExceptOrigin(aggregateId, originChainId);
            foreach( var chain in chains)
            {
                IChainClient chainClient = CreateChainClientInstance(chain.ChainId, GetChainClientType(chain.ChainId));
                yield return chainClient;
            }
        }

        private IChainClient CreateChainClientInstance(string chainId, System.Type type)
        {
            var chainSettings = _settings.Chains.Where(x=>x.ChainId == chainId).FirstOrDefault();
            if (chainSettings == null)
            {
                throw new ArgumentNullException("Chain settings might be set for the chain ID " + chainId);
            }
            return new EthChainClient(chainSettings.Rpc,chainSettings.Contract, chainSettings.PrivateKey, chainSettings.Abi, _settings.SingletonAggregateID);
        }

        private System.Type GetChainClientType(string chainId)
        {
            return typeof(EthChainClient);
        }

        private async Task ProduceTransmittedEvent(IEnumerable<ExecutionChainEvent> events, string aggregateId, string relayId, string chainId)
        {
            try
            {
                var operationId = aggregateId + events.Max(x => x.Version);
                var infEvent = new InfrastructureEvent
                {
                    Id = Guid.NewGuid().ToString(),
                    EvntType = InfrastructureEventType.RelayEventsTransmiitedEvent,
                    AggregatorId = aggregateId,
                    OperationId = operationId,
                    RelayId = relayId,
                    ChainId = chainId,
                    Timestamp = Timestamp.FromDateTime(DateTime.UtcNow)
                };
                foreach (var e in events)
                {
                    infEvent.Events.Add(new DomainEventDto
                    {
                        Id = e.Version.ToString(),
                        Timespan = Timestamp.FromDateTime(DateTime.UtcNow),
                    });
                }
                await _producer.ProduceAsync("infr", infEvent);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: {0}", ex.Message);
                throw;
            }
        }

        private async Task ProduceSyncTransactionExecutedEvent(string chainId, string aggregateId, string operationId, string relayId, string txHash)
        {
            var infEvent = new InfrastructureEvent
            {
                Id = Guid.NewGuid().ToString(),
                EvntType = InfrastructureEventType.TransactionExecutedEvent,
                AggregatorId = aggregateId,
                RelayId = relayId,
                ChainId = chainId,
                OperationId = operationId,
                // TODO: add trx hash
                CoreId = txHash,
                Timestamp = Timestamp.FromDateTime(DateTime.UtcNow)
            };
            await _producer.ProduceAsync("infr", infEvent);
        }

        private static byte[] GetHash(string inputString)
        {
            using (HashAlgorithm algorithm = SHA256.Create())
                return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
        }

        private static string GetHashString(string inputString)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in GetHash(inputString))
                sb.Append(b.ToString("X2"));

            return sb.ToString();
        }
    }
}