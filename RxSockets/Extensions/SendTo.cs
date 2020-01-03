namespace RxSockets
{
    public static class SendToEx
    {
        public static void SendTo(this byte[] buffer, IRxSocketClient rxsocket) => rxsocket.Send(buffer);
    }
}
