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
            foreach (var item in config.Chains)
            {
                var chain1 = new ExecutionChain();
                chain1.ID = "Id" + new Random().Next();
                chain1.ChainService = new EthChainClient(item.Rpc,item.Contract,item.PrivateKey, item.Abi);
                //chain1.ChainService = new ChainClientMock(random.Next(10));
                Console.WriteLine("Creating chain with RPC: {0}, Private Key: {2}, Contract: {1}", item.Rpc,item.Contract,item.PrivateKey);
                _chains.Add(chain1);
            }
        }
        public static bool Mocker = true;
        public void SyncAllChains()
        {
            //fetch the latest state for each chains
            Console.WriteLine("Current active chains: {0}", _chains.Count);
            foreach (var chain in _chains)
            {
                chain.Update();
            }
            Console.WriteLine("All chains updated");
            var leaderEventNumber = _chains.Max(x=> x.Length);
             Console.WriteLine("Leader chain event number: {0}", leaderEventNumber);
            var leader = _chains.Where(x=> x.Length == leaderEventNumber).FirstOrDefault();
            if (leader == null)
            {
                return;
            }
            foreach (var chain in _chains)
            {
                if (chain.ID == leader.ID)
                {
                    continue;
                }
                var newEvents = leader.GetNewEvents(chain.Length); 
                chain.PushNewEvents(newEvents);
            }
        }
    }
}