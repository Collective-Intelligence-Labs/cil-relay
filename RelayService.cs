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
            foreach (var item in config.Chains)
            {
                if (subs.Count(x => x.ChainId == item.ChainId) == 0)
                {
                    subsService.Create(config.SingletonAggregateID, item.ChainId);
                }
                var chain1 = new ExecutionChain(config.SingletonAggregateID, new EventStore(db), new EventsDispatcher(subsService, config), kafkraProducer);
                chain1.ID = item.ChainId;
                chain1.ChainService = new EthChainClient(item.Rpc,item.Contract,item.PrivateKey, item.Abi,config.SingletonAggregateID);
                //chain1.ChainService = new ChainClientMock(random.Next(10));
                var relay = chain1.ChainService.GetRelayPermission().GetAwaiter().GetResult();
                Console.WriteLine("Creating chain with RPC: {0}, Private Key: {2}, Contract: {1}, Relay: {3}", item.Rpc,item.Contract,item.PrivateKey, relay);
                _chains.Add(chain1);
            }
        }

        public void SyncAllChains()
        {
            //fetch the latest state for each chains
            Console.WriteLine("Current active chains: {0}", _chains.Count);
            foreach (var chain in _chains)
            {
                chain.Update();
            }
        }
    }
}