namespace Cila
{
    public class OmniChainRelaySettings
    {
        public string RelayId { get; set; }

        public List<ExecutionChainSettings> Chains { get; set; }

        public string MongoDBConnectionString { get; set; }

        public string SingletonAggregateID { get; set; }

        public OmniChainRelaySettings()
        {
            Chains = new List<ExecutionChainSettings>();
        }
    }

    public class ExecutionChainSettings
    {
        public string Rpc { get; set; }

        public string PrivateKey { get; set; }

        public string Contract { get; set; }

        public string AbiFile { get; set; }

        public string ChainId { get; set; }

        public ExecutionChainType ChainType { get; set; }

        private string _abi;
        public string Abi
        {
            get
            {
                _abi = _abi ?? File.ReadAllText(AbiFile);
                return _abi;
            }
        }
    }

    public enum ExecutionChainType
    {
        Ethereum
    }
}