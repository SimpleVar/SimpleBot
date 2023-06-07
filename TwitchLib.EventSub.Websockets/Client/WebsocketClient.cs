using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TwitchLib.EventSub.Websockets.Core.EventArgs;

#if NET6_0_OR_GREATER
using System.Buffers;
#endif

namespace TwitchLib.EventSub.Websockets.Client
{
  /// <summary>
  /// Websocket client to connect to variable websocket servers
  /// </summary>
  public class WebsocketClient : IDisposable
  {
    /// <summary>
    /// Determines if the Client is still connected based on WebsocketState
    /// </summary>
    public bool IsConnected => _webSocket.State == WebSocketState.Open;
    /// <summary>
    /// Determines if the Client is has encountered an unrecoverable issue based on WebsocketState
    /// </summary>
    public bool IsFaulted => _webSocket.CloseStatus != WebSocketCloseStatus.Empty && _webSocket.CloseStatus != WebSocketCloseStatus.NormalClosure;

    internal event EventHandler<DataReceivedArgs> OnDataReceived;
    internal event EventHandler<ErrorOccuredArgs> OnErrorOccurred;

    private readonly ClientWebSocket _webSocket;
    private readonly ILogger<WebsocketClient> _logger;

    /// <summary>
    /// Constructor to create a new Websocket client with a logger
    /// </summary>
    public WebsocketClient(ILogger<WebsocketClient> logger = null)
    {
      _webSocket = new ClientWebSocket();
      _logger = logger;
    }

    Uri _url;
    /// <summary>
    /// Connects the websocket client to a given Websocket Server
    /// </summary>
    public async Task<bool> ConnectAsync(Uri url)
    {
      if (_webSocket.State == WebSocketState.Open || _webSocket.State == WebSocketState.Connecting)
        return true;

      _url = url;
      await _webSocket.ConnectAsync(_url, CancellationToken.None).ConfigureAwait(true);
      _ = ProcessDataAsync();
      return IsConnected;
    }

    /// <summary>
    /// Disconnect the Websocket client from its currently connected server
    /// </summary>
    public async Task DisconnectAsync()
    {
      if (_webSocket.State == WebSocketState.Open || _webSocket.State == WebSocketState.Connecting)
        await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None).ConfigureAwait(true);
    }

#if NET6_0_OR_GREATER
    /// <summary>
    /// Background operation to process incoming data via the websocket
    /// </summary>
    private async Task ProcessDataAsync()
    {
      const int minimumBufferSize = 256;
      var decoder = Encoding.UTF8.GetDecoder();

      var store = MemoryPool<byte>.Shared.Rent().Memory;
      var buffer = MemoryPool<byte>.Shared.Rent(minimumBufferSize).Memory;

      var payloadSize = 0;

      while (IsConnected)
      {
        try
        {
          ValueWebSocketReceiveResult receiveResult;
          do
          {
            receiveResult = await _webSocket.ReceiveAsync(buffer, CancellationToken.None).ConfigureAwait(true);

            buffer.CopyTo(store[payloadSize..]);

            payloadSize += receiveResult.Count;
          } while (!receiveResult.EndOfMessage);

          switch (receiveResult.MessageType)
          {
            case WebSocketMessageType.Text:
              {
                var intermediate = MemoryPool<char>.Shared.Rent(payloadSize).Memory;

                if (payloadSize == 0)
                  continue;

                decoder.Convert(store.Span[..payloadSize], intermediate.Span, true, out _, out var charsCount, out _);
                var message = intermediate[..charsCount];

                OnDataReceived?.Invoke(this, new DataReceivedArgs { Message = message.Span.ToString() });
                payloadSize = 0;
                break;
              }
            case WebSocketMessageType.Binary:
              break;
            case WebSocketMessageType.Close:
              _logger?.LogCritical($"{(WebSocketCloseStatus)_webSocket.CloseStatus!} - {_webSocket.CloseStatusDescription!}");
              break;
            default:
              throw new ArgumentOutOfRangeException();
          }
        }
        catch (Exception ex)
        {
          OnErrorOccurred?.Invoke(this, new ErrorOccuredArgs { Exception = ex });
          break;
        }
      }
    }
#else
    /// <summary>
    /// Background operation to process incoming data via the websocket
    /// </summary>
    /// <returns>Task representing the background operation</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    private async Task ProcessDataAsync()
    {
      const int minimumBufferSize = 8192;

      var buffer = new ArraySegment<byte>(new byte[minimumBufferSize]);
      var payloadSize = 0;

      while (IsConnected)
      {
        try
        {
          WebSocketReceiveResult receiveResult;
          using var memory = new MemoryStream();

          do
          {
            receiveResult = await _webSocket.ReceiveAsync(buffer, CancellationToken.None).ConfigureAwait(true);

            if (buffer.Array == null)
              continue;

            memory.Write(buffer.Array, buffer.Offset, receiveResult.Count);
            payloadSize += receiveResult.Count;
          } while (!receiveResult.EndOfMessage);

          switch (receiveResult.MessageType)
          {
            case WebSocketMessageType.Text:
              {
                if (payloadSize == 0)
                  continue;

                memory.Seek(0, SeekOrigin.Begin);
                using var reader = new StreamReader(memory, Encoding.UTF8);
                OnDataReceived?.Invoke(this, new DataReceivedArgs { Message = await reader.ReadToEndAsync().ConfigureAwait(true) });
                break;
              }
            case WebSocketMessageType.Binary:
              break;
            case WebSocketMessageType.Close:
              if (_webSocket.CloseStatus != null)
                _logger?.LogCritical($"{(WebSocketCloseStatus)_webSocket.CloseStatus} - {_webSocket.CloseStatusDescription}");
              break;
            default:
              throw new ArgumentOutOfRangeException();
          }
        }
        catch (Exception ex)
        {
          OnErrorOccurred?.Invoke(this, new ErrorOccuredArgs { Exception = ex });
          break;
        }
      }
    }
#endif

    /// <summary>
    /// Cleanup of any unused resources as per IDisposable guidelines
    /// </summary>
    public void Dispose()
    {
      GC.SuppressFinalize(this);
      _webSocket.Dispose();
    }
  }
}
