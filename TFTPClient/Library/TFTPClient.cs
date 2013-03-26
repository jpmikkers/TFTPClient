/*

Copyright (c) 2013 Jean-Paul Mikkers

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

namespace CodePlex.JPMikkers.TFTP.Client
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

        private IPEndPoint serverEndPoint;
        private IPEndPoint peerEndPoint;

        private string filename;
        private Stream stream;
        private Settings settings;
        private TFTPPacket request;
        private int blockSize;
        private int timeout;

        private long m_Transferred;
        private long m_TransferSize;
        private long m_LastProgressTime;
        private System.Diagnostics.Stopwatch m_ProgressStopwatch = new System.Diagnostics.Stopwatch();

        const uint IOC_IN = 0x80000000;
        const uint IOC_VENDOR = 0x18000000;
        const uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;
        internal const int MaxTFTPPacketSize = MaxBlockSize + 4;

        private Socket socket;
        private byte[] m_ReceiveBuffer;

        /// <summary>
        /// During downloads, blocknumber is the number of the block that we expect to receive, and which we will ACK.
        /// During uploads, blocknumber is the number of the block that we sent and expect an ACK for.
        /// </summary>
        private ushort blockNumber;
        private Dictionary<string, string> requestedOptions = new Dictionary<string, string>();
        private bool init;

        private void Trace(Func<string> constructMsg)
        {
            if (settings.OnTrace != null)
            {
                settings.OnTrace(constructMsg());
            }
        }

        private void Progress(bool force)
        {
            if (settings.OnProgress != null)
            {
                if (force || (m_ProgressStopwatch.ElapsedMilliseconds - m_LastProgressTime) >= (long)settings.ProgressInterval.TotalMilliseconds)
                {
                    m_LastProgressTime = m_ProgressStopwatch.ElapsedMilliseconds;
                    settings.OnProgress(filename, m_Transferred, m_TransferSize);
                }
            }
        }

        private void SendPacket(TFTPPacket msg)
        {
            Trace(() => string.Format("-> [{0}] {1}", msg.EndPoint, msg.ToString()));
            var ms = new MemoryStream();
            msg.Serialize(ms);
            byte[] buffer = ms.ToArray();
            socket.SendTo(buffer, 0, buffer.Length, SocketFlags.None, msg.EndPoint);
        }

        private TFTPPacket ReceivePacket(int timeout)
        {
            if (timeout <= 0) return null;

            try
            {
                socket.ReceiveTimeout = timeout;
                EndPoint responseEndPoint = new IPEndPoint(serverEndPoint.Address, serverEndPoint.Port);
                int len = socket.ReceiveFrom(m_ReceiveBuffer, ref responseEndPoint);
                var result = TFTPPacket.Deserialize(new MemoryStream(m_ReceiveBuffer, 0, len, false, true));
                result.EndPoint = (IPEndPoint)responseEndPoint;
                Trace(() => string.Format("<- [{0}] {1}", result.EndPoint, result.ToString()));
                return result;
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode == SocketError.TimedOut)
                {
                    return null;
                }
                else
                {
                    throw;
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
        private void PumpPackets(Func<TFTPPacket, Instruction> step)
        {
            int retry = 0;
            var stopWatch = new System.Diagnostics.Stopwatch();
            Instruction instruction;

            do
            {
                SendPacket(request);
                stopWatch.Restart();

                do
                {
                    var response = ReceivePacket((int)Math.Max(0, (timeout * 1000) - stopWatch.ElapsedMilliseconds));
                    instruction = step(response);
                } while (instruction == Instruction.Drop);

                if (instruction == Instruction.Retry)
                {
                    if (++retry > settings.Retries)
                    {
                        throw new TFTPException(string.Format("Remote side didn't respond after {0} retries", settings.Retries));
                    }
                    else
                    {
                        Trace(() => string.Format("No response, retry {0} of {1}", retry, settings.Retries));
                    }
                }
                else
                {
                    retry = 0;
                }
            } while (instruction != Instruction.SendFinal && instruction != Instruction.Done);

            if (instruction == Instruction.SendFinal)
            {
                SendPacket(request);
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
            if (!packet.EndPoint.Address.Equals(peerEndPoint.Address))
            {
                Trace(() => string.Format("Got response from {0}, but {1} expected, dropping packet", packet.EndPoint, peerEndPoint));
                return Instruction.Drop;
            }

            if (!init)
            {
                /// packet isn't coming from the expected port: drop
                if (packet.EndPoint.Port != peerEndPoint.Port)
                {
                    Trace(() => string.Format("Got response from {0}, but {1} expected, dropping packet", packet.EndPoint, peerEndPoint));
                    return Instruction.Drop;
                }
            }

            // packet checks out ok, let the nested function interpret it
            var result = step(packet);

            // if the nested function accepted the packet, it's safe to assume that the packet endpoint is 
            // the peer endpoint that we should check for from now on.
            if (result != Instruction.Drop && result != Instruction.Retry)
            {
                peerEndPoint = packet.EndPoint;
                init = false;
            }

            return result;
        }

        public TFTPClient(IPEndPoint serverEndPoint, Settings settings)
        {
            this.serverEndPoint = serverEndPoint;
            this.settings = settings ?? new Settings();
            bool ipv6 = (serverEndPoint.AddressFamily == AddressFamily.InterNetworkV6);
            socket = new Socket(serverEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            socket.SendBufferSize = 65536;
            socket.ReceiveBufferSize = 65536;
            if (!ipv6) socket.DontFragment = settings.DontFragment;
            if (settings.Ttl >= 0) socket.Ttl = settings.Ttl;
            socket.Bind(new IPEndPoint(ipv6 ? IPAddress.IPv6Any : IPAddress.Any, 0));
            socket.IOControl((IOControlCode)SIO_UDP_CONNRESET, new byte[] { 0, 0, 0, 0 }, null);
            socket.SendTimeout = 10000;
            socket.ReceiveTimeout = 10000;
            m_ReceiveBuffer = new byte[MaxTFTPPacketSize];
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
                if (socket != null)
                {
                    DelayedDisposer.QueueDelayedDispose(socket, 500);
                    socket = null;
                }
            }
        }

        public void Download(string filename, Stream stream)
        {
            Init(filename, stream);
            requestedOptions.Add(Option_TransferSize, "0");
            blockNumber = 1;
            request = new TFTPPacket_ReadRequest() { EndPoint = serverEndPoint, Filename = filename, Options = requestedOptions };
            Progress(true);
            PumpPackets(p => FilterPacket(p, DoDownload));
            Progress(true);
            Trace(() => "Download complete");
        }

        public void Upload(string filename, Stream stream)
        {
            Init(filename, stream);

            try
            {
                m_TransferSize = stream.Length;

                if (m_TransferSize >= 0)
                {
                    requestedOptions.Add(Option_TransferSize, m_TransferSize.ToString(CultureInfo.InvariantCulture));
                }
                else
                {
                    m_TransferSize = -1;
                }
            }
            catch
            {
                m_TransferSize = -1;
            }

            blockNumber = 0;
            request = new TFTPPacket_WriteRequest() { EndPoint = serverEndPoint, Filename = filename, Options = requestedOptions };
            Progress(true);
            PumpPackets(p => FilterPacket(p, DoUpload));
            Progress(true);
            Trace(() => "Upload complete");
        }

        private Instruction DoDownload(TFTPPacket packet)
        {
            Instruction result = Instruction.Drop;

            switch (packet.Code)
            {
                case Opcode.OptionsAck:
                    if (init)
                    {
                        HandleOptionsAck((TFTPPacket_OptionsAck)packet);
                        // If the transfer was initiated with a Read Request, then an ACK (with the data block number set to 0) is sent by the client to confirm 
                        // the values in the server's OACK packet.
                        request = new TFTPPacket_Ack() { EndPoint = packet.EndPoint, BlockNumber = 0 };
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
                        if (responseData.BlockNumber == blockNumber)
                        {
                            request = new TFTPPacket_Ack { EndPoint = packet.EndPoint, BlockNumber = blockNumber };
                            stream.Write(responseData.Data.Array, responseData.Data.Offset, responseData.Data.Count);
                            blockNumber++;
                            result = (responseData.Data.Count < blockSize) ? Instruction.SendFinal : Instruction.SendNew;
                            m_Transferred += responseData.Data.Count;
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
                    if (init)
                    {
                        HandleOptionsAck((TFTPPacket_OptionsAck)packet);
                        Progress(true);
                        // If the transfer was initiated with a Write Request, then the client begins the transfer with the first DATA packet (blocknr=1), using the negotiated values.  
                        // If the client rejects the OACK, then it sends an ERROR packet, with error code 8, to the server and the transfer is terminated.
                        blockNumber++;
                        request = new TFTPPacket_Data() { EndPoint = packet.EndPoint, BlockNumber = blockNumber, Data = ReadData(stream, blockSize) };
                        result = Instruction.SendNew;
                        m_Transferred += ((TFTPPacket_Data)request).Data.Count;
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
                        peerEndPoint = packet.EndPoint;
                        // did we receive the expected blocknumber ?
                        if (responseData.BlockNumber == blockNumber)
                        {
                            Progress(false);
                            // was the outstanding request a data packet, and the last one?
                            if (request is TFTPPacket_Data && ((TFTPPacket_Data)request).Data.Count < blockSize)
                            {
                                // that was the ack for the last packet, we're done
                                result = Instruction.Done;
                            }
                            else
                            {
                                blockNumber++;
                                request = new TFTPPacket_Data() { EndPoint = packet.EndPoint, BlockNumber = blockNumber, Data = ReadData(stream, blockSize) };
                                result = Instruction.SendNew;
                                m_Transferred += ((TFTPPacket_Data)request).Data.Count;
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
            this.filename = filename;
            this.stream = stream;
            blockSize = DefaultBlockSize;
            timeout = (int)Math.Ceiling(settings.ResponseTimeout.TotalSeconds);
            init = true;
            requestedOptions = new Dictionary<string, string>();

            if (settings.BlockSize != DefaultBlockSize)
            {
                // limit blocksize to allowed range
                requestedOptions.Add(Option_BlockSize, Clip(settings.BlockSize,MinBlockSize,MaxBlockSize).ToString());
            }

            requestedOptions.Add(Option_Timeout, Clip(timeout,1,255).ToString());

            m_LastProgressTime = 0;
            m_Transferred = 0;
            m_TransferSize = -1;
            m_ProgressStopwatch.Restart();

            peerEndPoint = new IPEndPoint(serverEndPoint.Address, serverEndPoint.Port);
        }

        private void HandleOptionsAck(TFTPPacket_OptionsAck response)
        {
            if (response.Options.ContainsKey(Option_BlockSize))
            {
                blockSize = int.Parse(response.Options[Option_BlockSize], CultureInfo.InvariantCulture);
            }

            if (response.Options.ContainsKey(Option_Timeout))
            {
                timeout = int.Parse(response.Options[Option_Timeout], CultureInfo.InvariantCulture);
            }

            if (response.Options.ContainsKey(Option_TransferSize))
            {
                m_TransferSize = long.Parse(response.Options[Option_TransferSize], CultureInfo.InvariantCulture);
            }
        }

        private static void HandleError(TFTPPacket_Error p)
        {
            throw new TFTPException(string.Format("Server error {0} : {1}", p.ErrorCode, p.ErrorMessage));
        }

        private static ArraySegment<byte> ReadData(Stream s, int len)
        {
            var buf = new byte[len];
            int done = s.Read(buf, 0, len);
            return new ArraySegment<byte>(buf, 0, done);
        }

        public static void Download(IPEndPoint serverEndPoint, string localFilename, string remoteFilename, Settings settings=null)
        {
            using (Stream localStream = File.Create(localFilename))
            {
                Download(serverEndPoint, localStream, remoteFilename, settings);
            }
        }

        public static void Download(IPEndPoint serverEndPoint, Stream localStream, string remoteFilename, Settings settings=null)
        {
            using (var session = new TFTPClient(serverEndPoint, settings))
            {
                session.Download(remoteFilename, localStream);
            }
        }

        public static void Upload(IPEndPoint serverEndPoint, string localFilename, string remoteFilename, Settings settings=null)
        {
            using (Stream localStream = File.OpenRead(localFilename))
            {
                Upload(serverEndPoint, localStream, remoteFilename, settings);
            }
        }

        public static void Upload(IPEndPoint serverEndPoint, Stream localStream, string remoteFilename, Settings settings=null)
        {
            using (var session = new TFTPClient(serverEndPoint, settings))
            {
                session.Upload(remoteFilename, localStream);
            }
        }
    }
}
