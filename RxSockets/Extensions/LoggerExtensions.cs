namespace RxSockets;

public static partial class Extension
{
    [LoggerMessage(EventId = 1, EventName = "SendBytes", Level = LogLevel.Trace, Message = "Send: {Name} on {LocalEndPoint} sending {Bytes} bytes to {RemoteEndPoint}.")]
    internal static partial void LogSend(this ILogger logger, string name, EndPoint? localEndPoint, int bytes, EndPoint? remoteEndPoint);

    [LoggerMessage(EventId = 2, EventName = "ReceiveBytes", Level = LogLevel.Trace, Message = "Receive: {Name} on {LocalEndPoint} received {Bytes} bytes from {RemoteEndPoint}.")]
    internal static partial void LogReceive(this ILogger logger, string name, EndPoint? localEndPoint, int bytes, EndPoint? remoteEndPoint);
}

