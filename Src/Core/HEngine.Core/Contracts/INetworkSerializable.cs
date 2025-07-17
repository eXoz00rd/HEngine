using HEngine.Core.Network;

namespace HEngine.Core.Contracts;

public interface INetworkSerializable {
    void Serialize(NetworkWriter writer);
    void Deserialize(NetworkReader reader);
}