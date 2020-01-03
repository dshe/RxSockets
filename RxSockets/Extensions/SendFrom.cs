namespace RxSockets
{
    public static class SendToEx
    {
        public static void SendFrom(this byte[] buffer, IRxSocketClient rxsocket) => rxsocket.Send(buffer);
    }
}
