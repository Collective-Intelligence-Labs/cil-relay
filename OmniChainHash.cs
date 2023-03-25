using Nethereum.Util;

namespace Cila.OmniChain
{
    public class HashFunctions 
    {
        public static byte[] CalculateEventHash(byte[] newEvent, byte[] prevEventHash)
        {
            var keccak = new Sha3Keccack();
            var input = newEvent.Concat(prevEventHash).ToArray();
            byte[] hash = keccak.CalculateHash(input); 
            return hash;
        }
    }
}