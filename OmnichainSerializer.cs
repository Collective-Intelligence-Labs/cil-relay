using Example.Protobuf;
using Google.Protobuf;

namespace OmniChain
{
    public class OmniChainSerializer
    {
        public static IMessage DeserializeWithMessageType(byte[] data)
{
    if (data == null || data.Length < 2)
    {
        throw new ArgumentException("Invalid data");
    }

    OmniChainMessageType messageType = (OmniChainMessageType)data[0];
    byte[] messageBytes = new byte[data.Length - 1];
    Buffer.BlockCopy(data, 1, messageBytes, 0, messageBytes.Length);

    IMessage message;

    switch (messageType)
    {
        case OmniChainMessageType.ItemIssued:
            message = new ItemMinted();
            break;
        case OmniChainMessageType.ItemTransfered:
            message = new ItemTransfered();
            break;
        default:
            throw new ArgumentException("Invalid message type");
    }

    message.MergeFrom(messageBytes);
    return message;
}
    }

    enum OmniChainMessageType
    {
        ItemIssued = 1,
        ItemTransfered = 2,
    }
}