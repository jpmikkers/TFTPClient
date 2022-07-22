/*

Copyright (c) 2022 Jean-Paul Mikkers

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.

*/
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace GitHub.JPMikkers.TFTP.Client
{
    public partial class TFTPClient : IDisposable
    {
        private const int DefaultBlockSize = 512;
        private const int MinBlockSize = 8;
        private const int MaxBlockSize = 65464;

        public delegate void OnTraceDelegate(string msg);
        public delegate void OnProgressDelegate(string file, long sent, long target);

        private enum Instruction
        {
            Drop,       // drop the last incoming packet, resume waiting for a new one
            Retry,      // resend the last outgoing packet
            SendNew,    // send a new outgoing packet
            SendFinal,  // send one last outgoing packet before stopping
            Done        // completed
        }

        private readonly IPEndPoint _serverEndPoint;
        private IPEndPoint _peerEndPoint;

        private bool _isUpload;
        private string _filename;
        private Stream _stream;
        private readonly Settings _settings;
        private TFTPPacket _request;
        private int _blockSize;
        private int _timeout;
        private CancellationToken _userCancellationToken;

        private long _transferred;
        private long _transferSize;
        private long _lastProgressTime;
        private readonly Stopwatch _progressStopwatch = new();

        const uint IOC_IN = 0x80000000;
        const uint IOC_VENDOR = 0x18000000;
        const uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;
        internal const int MaxTFTPPacketSize = MaxBlockSize + 4;

        private Socket _socket;
        private readonly byte[] _receiveBuffer;

        /// <summary>
        /// During downloads, blocknumber is the number of the block that we expect to receive, and which we will ACK.
        /// During uploads, blocknumber is the number of the block that we sent and expect an ACK for.
        /// </summary>
        private ushort _blockNumber;
        private Dictionary<string, string> _requestedOptions = new();
        private bool _init;

        private void Trace(Func<string> constructMsg)
        {
            if (_settings.OnTrace != null)
            {
                _settings.OnTrace(this, new TraceEventArgs { Message = constructMsg() });
            }
        }

        private void Progress(bool force)
        {
            if (_settings.OnProgress != null)
            {
                if (force || (_progressStopwatch.ElapsedMilliseconds - _lastProgressTime) >= (long)_settings.ProgressInterval.TotalMilliseconds)
                {
                    _lastProgressTime = _progressStopwatch.ElapsedMilliseconds;
                    _settings.OnProgress(this, new ProgressEventArgs { 
                        Filename = _filename, 
                        IsUpload = _isUpload, 
                        Transferred = _transferred, 
                        TransferSize = _transferSize 
                    });
                }
            }
        }

        private async Task SendPacket(TFTPPacket msg)
        {
            Trace(() => $"-> [{msg.EndPoint}] {msg}");
            var ms = new MemoryStream();
            msg.Serialize(ms);
            byte[] buffer = ms.ToArray();
            await _socket.SendToAsync(new ReadOnlyMemory<byte>(buffer), SocketFlags.None, msg.EndPoint, _userCancellationToken);
        }

        private async Task<TFTPPacket> ReceivePacket(TimeSpan timeout)
        {
            if (timeout.TotalMilliseconds <= 0) return null;

            using (var timeoutCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_userCancellationToken))
            {
                try
                {
                    timeoutCancellationTokenSource.CancelAfter(timeout);
                    var rp = await _socket.ReceiveFromAsync(new Memory<byte>(_receiveBuffer), SocketFlags.None, new IPEndPoint(_serverEndPoint.Address, _serverEndPoint.Port), timeoutCancellationTokenSource.Token);
                    var result = TFTPPacket.Deserialize(new MemoryStream(_receiveBuffer, 0, rp.ReceivedBytes, false, true));
                    result.EndPoint = (IPEndPoint)rp.RemoteEndPoint;
                    Trace(() => $"<- [{result.EndPoint}] {result}");
                    return result;
                }
                catch (OperationCanceledException) when (timeoutCancellationTokenSource.IsCancellationRequested && !_userCancellationToken.IsCancellationRequested)
                {
                    Trace(() => $"receive timeout");
                    return null;
                }
            }
        }

        /// <summary>
        /// This method performs the TFTP request/response cycle in a loop, where each response is interpreted by a lambda function.
        /// The result of this lambda function determines the following step in the cycle:
        /// 
        /// Drop        : drop the last incoming packet, resume waiting for a new one. Drop does not reset the packet timeout.
        /// Retry       : resend the last outgoing packet. This can only be done up to the configured maximum number of retries.
        /// SendNew     : send a new outgoing packet.
        /// SendFinal   : send one last outgoing packet before stopping
        /// Done        : completed
        /// </summary>
        /// <param name="step">the packet interpretation function</param>
        private async Task PumpPackets(Func<TFTPPacket, Instruction> step)
        {
            int retry = 0;
            var stopWatch = new System.Diagnostics.Stopwatch();
            Instruction instruction;

            do
            {
                await SendPacket(_request);
                stopWatch.Restart();

                do
                {
                    var response = await ReceivePacket(TimeSpan.FromMilliseconds(Math.Max(0, (_timeout * 1000) - stopWatch.ElapsedMilliseconds)));
                    instruction = step(response);
                } while (instruction == Instruction.Drop);

                if (instruction == Instruction.Retry)
                {
                    if (++retry > _settings.Retries)
                    {
                        throw new TFTPException($"Remote side didn't respond after {_settings.Retries} retries");
                    }
                    else
                    {
                        Trace(() => $"No response, retry {retry} of {_settings.Retries}");
                    }
                }
                else
                {
                    retry = 0;
                }
            } while (instruction != Instruction.SendFinal && instruction != Instruction.Done);

            if (instruction == Instruction.SendFinal)
            {
                await SendPacket(_request);
            }
        }

        /// <summary>
        /// This method does the first level of response interpretation (see: PumpPackets). It takes care of the following tasks:
        /// - on timeout receiving the response : retry sending the request
        /// - drop responses that aren't from the expected sender
        /// - everything else: forward them to the next level of response interpretation by way of calling the lambda function.
        /// </summary>
        /// <param name="packet">the incoming response packet, or null on timeout</param>
        /// <param name="step">the nested packet interpretation function</param>
        /// <returns></returns>
        private Instruction FilterPacket(TFTPPacket packet, Func<TFTPPacket, Instruction> step)
        {
            // on timeout receiving the response : retry sending the request
            if (packet == null) return Instruction.Retry;

            /// packet isn't coming from the expected address: drop
            if (!packet.EndPoint.Address.Equals(_peerEndPoint.Address))
            {
                Trace(() => $"Got response from {packet.EndPoint}, but {_peerEndPoint} expected, dropping packet");
                return Instruction.Drop;
            }

            if (!_init)
            {
                /// packet isn't coming from the expected port: drop
                if (packet.EndPoint.Port != _peerEndPoint.Port)
                {
                    Trace(() => $"Got response from {packet.EndPoint}, but {_peerEndPoint} expected, dropping packet");
                    return Instruction.Drop;
                }
            }

            // packet checks out ok, let the nested function interpret it
            var result = step(packet);

            // if the nested function accepted the packet, it's safe to assume that the packet endpoint is 
            // the peer endpoint that we should check for from now on.
            if (result != Instruction.Drop && result != Instruction.Retry)
            {
                _peerEndPoint = packet.EndPoint;
                _init = false;
            }

            return result;
        }

        public TFTPClient(IPEndPoint serverEndPoint, Settings settings)
        {
            settings ??= new Settings();
            _serverEndPoint = serverEndPoint;
            _settings = settings;
            _blockSize = DefaultBlockSize;
            bool ipv6 = (serverEndPoint.AddressFamily == AddressFamily.InterNetworkV6);
            _socket = new Socket(serverEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            SetSocketBufferSize();
            if (!ipv6) _socket.DontFragment = settings.DontFragment;
            if (settings.Ttl >= 0) _socket.Ttl = settings.Ttl;
            _socket.Bind(new IPEndPoint(ipv6 ? IPAddress.IPv6Any : IPAddress.Any, 0));

            if (OperatingSystem.IsWindows())
            {
                // IOControl() is only available on windows. This call prevents the UDP socket from
                // being closed as a result of ICMP Port Unreachable messages.
                // see: https://stackoverflow.com/questions/15228272/what-would-cause-a-connectionreset-on-an-udp-socket
                _socket.IOControl((IOControlCode)SIO_UDP_CONNRESET, new byte[] { 0, 0, 0, 0 }, null);
            }

            _socket.SendTimeout = 10000;        // this only affects synchronous Send
            _socket.ReceiveTimeout = 10000;     // this only affects synchronous Receive
            _receiveBuffer = new byte[MaxTFTPPacketSize];
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_socket != null)
                {
                    DelayedDisposer.QueueDelayedDispose(_socket, 500);
                    _socket = null;
                }
            }
        }

        public async Task DownloadAsync(string filename, Stream stream, CancellationToken cancellationToken = default)
        {
            _isUpload = false;
            Init(filename, stream);
            _requestedOptions.Add(Option_TransferSize, "0");
            _blockNumber = 1;
            _userCancellationToken = cancellationToken;
            _request = new TFTPPacket_ReadRequest { 
                EndPoint = _serverEndPoint, 
                Filename = filename, 
                Options = _requestedOptions 
            };
            Progress(true);
            await PumpPackets(p => FilterPacket(p, DoDownload));
            Progress(true);
            Trace(() => "Download complete");
        }

        public async Task UploadAsync(string filename, Stream stream, CancellationToken cancellationToken = default)
        {
            _isUpload = true;
            Init(filename, stream);

            try
            {
                _transferSize = stream.Length;

                if (_transferSize >= 0)
                {
                    _requestedOptions.Add(Option_TransferSize, _transferSize.ToString(CultureInfo.InvariantCulture));
                }
                else
                {
                    _transferSize = -1;
                }
            }
            catch
            {
                _transferSize = -1;
            }

            _blockNumber = 0;
            _userCancellationToken = cancellationToken;
            _request = new TFTPPacket_WriteRequest { 
                EndPoint = _serverEndPoint, 
                Filename = filename, 
                Options = _requestedOptions 
            };
            Progress(true);
            await PumpPackets(p => FilterPacket(p, DoUpload));
            Progress(true);
            Trace(() => "Upload complete");
        }

        public void Download(string filename, Stream stream)
        {
            Task.Run(async () => { await DownloadAsync(filename, stream); }).Wait();
        }

        public void Upload(string filename, Stream stream)
        {
            Task.Run(async () => { await UploadAsync(filename, stream); }).Wait();
        }

        private Instruction DoDownload(TFTPPacket packet)
        {
            Instruction result = Instruction.Drop;

            switch (packet.Code)
            {
                case Opcode.OptionsAck:
                    if (_init)
                    {
                        HandleOptionsAck((TFTPPacket_OptionsAck)packet);
                        // If the transfer was initiated with a Read Request, then an ACK (with the data block number set to 0) is sent by the client to confirm 
                        // the values in the server's OACK packet.
                        _request = new TFTPPacket_Ack { 
                            EndPoint = packet.EndPoint, 
                            BlockNumber = 0 
                        };
                        result = Instruction.SendNew;
                        Progress(true);
                    }
                    else
                    {
                        // received another optionsack, probably the server resent it because our ack(0) was lost. drop it.
                        result = Instruction.Drop;
                    }
                    break;

                case Opcode.Data:
                    {
                        var responseData = (TFTPPacket_Data)packet;
                        // did we receive the expected blocknumber ?
                        if (responseData.BlockNumber == _blockNumber)
                        {
                            _request = new TFTPPacket_Ack { 
                                EndPoint = packet.EndPoint, 
                                BlockNumber = _blockNumber 
                            };
                            _stream.Write(responseData.Data.Array, responseData.Data.Offset, responseData.Data.Count);
                            _blockNumber++;
                            result = (responseData.Data.Count < _blockSize) ? Instruction.SendFinal : Instruction.SendNew;
                            _transferred += responseData.Data.Count;
                            Progress(false);
                        }
                        else
                        {
                            // not the correct block, drop it (maybe the next one will be ok)
                            result = Instruction.Drop;
                        }
                    }
                    break;

                case Opcode.Error:
                    HandleError((TFTPPacket_Error)packet);
                    break;

                default:
                    throw new TFTPException("Illegal server response");
            }
            return result;
        }

        private Instruction DoUpload(TFTPPacket packet)
        {
            Instruction result = Instruction.Drop;

            switch (packet.Code)
            {
                case Opcode.OptionsAck:
                    if (_init)
                    {
                        HandleOptionsAck((TFTPPacket_OptionsAck)packet);
                        Progress(true);
                        // If the transfer was initiated with a Write Request, then the client begins the transfer with the first DATA packet (blocknr=1), using the negotiated values.  
                        // If the client rejects the OACK, then it sends an ERROR packet, with error code 8, to the server and the transfer is terminated.
                        _blockNumber++;
                        _request = new TFTPPacket_Data { 
                            EndPoint = packet.EndPoint, 
                            BlockNumber = _blockNumber, 
                            Data = ReadData(_stream, _blockSize) 
                        };
                        result = Instruction.SendNew;
                        _transferred += ((TFTPPacket_Data)_request).Data.Count;
                    }
                    else
                    {
                        // received another optionsack, probably the server resent it because our first datapacket was lost. drop it.
                        result = Instruction.Drop;
                    }
                    break;

                case Opcode.Ack:
                    {
                        var responseData = (TFTPPacket_Ack)packet;
                        _peerEndPoint = packet.EndPoint;
                        // did we receive the expected blocknumber ?
                        if (responseData.BlockNumber == _blockNumber)
                        {
                            Progress(false);
                            // was the outstanding request a data packet, and the last one?
                            if (_request is TFTPPacket_Data && ((TFTPPacket_Data)_request).Data.Count < _blockSize)
                            {
                                // that was the ack for the last packet, we're done
                                result = Instruction.Done;
                            }
                            else
                            {
                                _blockNumber++;
                                _request = new TFTPPacket_Data { 
                                    EndPoint = packet.EndPoint, 
                                    BlockNumber = _blockNumber, 
                                    Data = ReadData(_stream, _blockSize) 
                                };
                                result = Instruction.SendNew;
                                _transferred += ((TFTPPacket_Data)_request).Data.Count;
                            }
                        }
                        else
                        {
                            // not the correct block, drop it (maybe the next one will be ok)
                            result = Instruction.Drop;
                        }
                    }
                    break;

                case Opcode.Error:
                    HandleError((TFTPPacket_Error)packet);
                    break;

                default:
                    throw new TFTPException("Illegal server response");
            }
            return result;
        }

        private void Init(string filename, Stream stream)
        {
            this._filename = filename;
            this._stream = stream;
            _blockSize = DefaultBlockSize;
            _timeout = (int)Math.Ceiling(_settings.ResponseTimeout.TotalSeconds);
            _init = true;
            _requestedOptions = new Dictionary<string, string>();

            if (_settings.BlockSize != DefaultBlockSize)
            {
                // limit blocksize to allowed range
                _requestedOptions.Add(Option_BlockSize, Clip(_settings.BlockSize, MinBlockSize, MaxBlockSize).ToString());
            }

            _requestedOptions.Add(Option_Timeout, Clip(_timeout, 1, 255).ToString());

            _lastProgressTime = 0;
            _transferred = 0;
            _transferSize = -1;
            _progressStopwatch.Restart();

            SetSocketBufferSize();

            _peerEndPoint = new IPEndPoint(_serverEndPoint.Address, _serverEndPoint.Port);
        }

        private void SetSocketBufferSize()
        {
            int multiple = 8;

            while (multiple > 0)
            {
                try
                {
                    var socketBufferSize = multiple * (_blockSize + 4);
                    Trace(() => $"Setting socket buffers to {socketBufferSize}");
                    _socket.SendBufferSize = socketBufferSize;
                    _socket.ReceiveBufferSize = socketBufferSize;
                    break;
                }
                catch
                {
                    Trace(() => "Failed to modify socket buffer size");
                    multiple /= 2;
                }
            }
        }

        private void HandleOptionsAck(TFTPPacket_OptionsAck response)
        {
            if (response.Options.ContainsKey(Option_BlockSize))
            {
                _blockSize = int.Parse(response.Options[Option_BlockSize], CultureInfo.InvariantCulture);
                SetSocketBufferSize();
            }

            if (response.Options.ContainsKey(Option_Timeout))
            {
                _timeout = int.Parse(response.Options[Option_Timeout], CultureInfo.InvariantCulture);
            }

            if (response.Options.ContainsKey(Option_TransferSize))
            {
                _transferSize = long.Parse(response.Options[Option_TransferSize], CultureInfo.InvariantCulture);
            }
        }

        private static void HandleError(TFTPPacket_Error p)
        {
            throw new TFTPException($"Server error {p.ErrorCode} : {p.ErrorMessage}");
        }

        private static ArraySegment<byte> ReadData(Stream s, int len)
        {
            var buf = new byte[len];
            int done = s.Read(buf, 0, len);
            return new ArraySegment<byte>(buf, 0, done);
        }
    }
}
