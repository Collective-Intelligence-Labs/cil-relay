namespace Cila
{
    public class OmniChainRelaySettings
    {
        public string RelayId {get;set;}

        public List<ExecutionChainSettings> Chains {get;set;}

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
    }
}