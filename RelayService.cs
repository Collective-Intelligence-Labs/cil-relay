using Cila.OmniChain;

namespace Cila
{

    public class RelayService
    {
        public string Id { get; private set; }
        
        private List<IExecutionChain> _chains;
        public RelayService(OmniChainRelaySettings config)
        {
            _chains = new List<IExecutionChain>();
            Id = config.RelayId;
            var random = new Random();
            var db = new Database.MongoDatabase(config);
            var subsService = new SubscriptionsService(db);
            var kafkraProducer = new KafkaProducer(Program.ProducerConfig());
            var subs = subsService.GetAllFor(config.SingletonAggregateID).ToList();
            foreach (var chain in config.Chains)
            {
                if (subs.Count(x => x.ChainId == chain.ChainId) == 0)
                {
                    subsService.Create(config.SingletonAggregateID, chain.ChainId);
                }
                var execChain = new ExecutionChain(config.SingletonAggregateID, new EventStore(db), new EventsDispatcher(subsService, config, kafkraProducer), kafkraProducer);
                execChain.ID = chain.ChainId;
                execChain.ChainService = new EthChainClient(chain.Rpc, chain.Contract, chain.PrivateKey, chain.Abi, config.SingletonAggregateID);
                var relay = execChain.ChainService.GetRelayPermission().GetAwaiter().GetResult();
                Console.WriteLine("Creating chain with RPC: {0}, Private Key: {2}, Contract: {1}, Relay: {3}", chain.Rpc, chain.Contract, chain.PrivateKey, relay);
                _chains.Add(execChain);
            }
        }

        public void SyncAllChains()
        {
            //fetch the latest state for each chains
            Console.WriteLine("Active chains: {0}", _chains.Count);
            foreach (var chain in _chains)
            {
                chain.Update(Id);
            }
        }
    }
}