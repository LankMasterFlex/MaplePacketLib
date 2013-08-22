namespace MaplePacketLib
{
    public interface IMapleClient
    {
        void OnConnect(bool success, object state);
        void OnHandshake(short major, string minor,byte locale);
        void OnPacket(byte[] packet);
        void OnDisconnected();
    }
}
