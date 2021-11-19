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

        private IPEndPoint m_ServerEndPoint;
        private IPEndPoint m_PeerEndPoint;

        private bool m_IsUpload;
        private string m_Filename;
        private Stream m_Stream;
        private Settings m_Settings;
        private TFTPPacket m_Request;
        private int m_BlockSize;
        private int m_Timeout;

        private long m_Transferred;
        private long m_TransferSize;
        private long m_LastProgressTime;
        private System.Diagnostics.Stopwatch m_ProgressStopwatch = new System.Diagnostics.Stopwatch();

        const uint IOC_IN = 0x80000000;
        const uint IOC_VENDOR = 0x18000000;
        const uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;
        internal const int MaxTFTPPacketSize = MaxBlockSize + 4;

        private Socket m_Socket;
        private byte[] m_ReceiveBuffer;

        /// <summary>
        /// During downloads, blocknumber is the number of the block that we expect to receive, and which we will ACK.
        /// During uploads, blocknumber is the number of the block that we sent and expect an ACK for.
        /// </summary>
        private ushort m_BlockNumber;
        private Dictionary<string, string> m_RequestedOptions = new Dictionary<string, string>();
        private bool m_Init;

        private void Trace(Func<string> constructMsg)
        {
            if (m_Settings.OnTrace != null)
            {
                m_Settings.OnTrace(this, new TraceEventArgs { Message = constructMsg() });
            }
        }

        private void Progress(bool force)
        {
            if (m_Settings.OnProgress != null)
            {
                if (force || (m_ProgressStopwatch.ElapsedMilliseconds - m_LastProgressTime) >= (long)m_Settings.ProgressInterval.TotalMilliseconds)
                {
                    m_LastProgressTime = m_ProgressStopwatch.ElapsedMilliseconds;
                    m_Settings.OnProgress(this, new ProgressEventArgs { Filename = m_Filename, IsUpload = m_IsUpload, Transferred = m_Transferred, TransferSize = m_TransferSize });
                }
            }
        }

        private void SendPacket(TFTPPacket msg)
        {
            Trace(() => $"-> [{msg.EndPoint}] {msg.ToString()}");
            var ms = new MemoryStream();
            msg.Serialize(ms);
            byte[] buffer = ms.ToArray();
            m_Socket.SendTo(buffer, 0, buffer.Length, SocketFlags.None, msg.EndPoint);
        }

        private TFTPPacket ReceivePacket(int timeout)
        {
            if (timeout <= 0) return null;

            try
            {
                m_Socket.ReceiveTimeout = timeout;
                EndPoint responseEndPoint = new IPEndPoint(m_ServerEndPoint.Address, m_ServerEndPoint.Port);
                int len = m_Socket.ReceiveFrom(m_ReceiveBuffer, ref responseEndPoint);
                var result = TFTPPacket.Deserialize(new MemoryStream(m_ReceiveBuffer, 0, len, false, true));
                result.EndPoint = (IPEndPoint)responseEndPoint;
                Trace(() => $"<- [{result.EndPoint}] {result.ToString()}");
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
                SendPacket(m_Request);
                stopWatch.Restart();

                do
                {
                    var response = ReceivePacket((int)Math.Max(0, (m_Timeout * 1000) - stopWatch.ElapsedMilliseconds));
                    instruction = step(response);
                } while (instruction == Instruction.Drop);

                if (instruction == Instruction.Retry)
                {
                    if (++retry > m_Settings.Retries)
                    {
                        throw new TFTPException($"Remote side didn't respond after {m_Settings.Retries} retries");
                    }
                    else
                    {
                        Trace(() => $"No response, retry {retry} of {m_Settings.Retries}");
                    }
                }
                else
                {
                    retry = 0;
                }
            } while (instruction != Instruction.SendFinal && instruction != Instruction.Done);

            if (instruction == Instruction.SendFinal)
            {
                SendPacket(m_Request);
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
            if (!packet.EndPoint.Address.Equals(m_PeerEndPoint.Address))
            {
                Trace(() => $"Got response from {packet.EndPoint}, but {m_PeerEndPoint} expected, dropping packet");
                return Instruction.Drop;
            }

            if (!m_Init)
            {
                /// packet isn't coming from the expected port: drop
                if (packet.EndPoint.Port != m_PeerEndPoint.Port)
                {
                    Trace(() => $"Got response from {packet.EndPoint}, but {m_PeerEndPoint} expected, dropping packet");
                    return Instruction.Drop;
                }
            }

            // packet checks out ok, let the nested function interpret it
            var result = step(packet);

            // if the nested function accepted the packet, it's safe to assume that the packet endpoint is 
            // the peer endpoint that we should check for from now on.
            if (result != Instruction.Drop && result != Instruction.Retry)
            {
                m_PeerEndPoint = packet.EndPoint;
                m_Init = false;
            }

            return result;
        }

        public TFTPClient(IPEndPoint serverEndPoint, Settings settings)
        {
            settings = settings ?? new Settings();
            m_ServerEndPoint = serverEndPoint;
            m_Settings = settings;
            m_BlockSize = DefaultBlockSize;
            bool ipv6 = (serverEndPoint.AddressFamily == AddressFamily.InterNetworkV6);
            m_Socket = new Socket(serverEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            SetSocketBufferSize();
            if (!ipv6) m_Socket.DontFragment = settings.DontFragment;
            if (settings.Ttl >= 0) m_Socket.Ttl = settings.Ttl;
            m_Socket.Bind(new IPEndPoint(ipv6 ? IPAddress.IPv6Any : IPAddress.Any, 0));
            m_Socket.IOControl((IOControlCode)SIO_UDP_CONNRESET, new byte[] { 0, 0, 0, 0 }, null);
            m_Socket.SendTimeout = 10000;
            m_Socket.ReceiveTimeout = 10000;
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
                if (m_Socket != null)
                {
                    DelayedDisposer.QueueDelayedDispose(m_Socket, 500);
                    m_Socket = null;
                }
            }
        }

        public void Download(string filename, Stream stream)
        {
            m_IsUpload = false;
            Init(filename, stream);
            m_RequestedOptions.Add(Option_TransferSize, "0");
            m_BlockNumber = 1;
            m_Request = new TFTPPacket_ReadRequest() { EndPoint = m_ServerEndPoint, Filename = filename, Options = m_RequestedOptions };
            Progress(true);
            PumpPackets(p => FilterPacket(p, DoDownload));
            Progress(true);
            Trace(() => "Download complete");
        }

        public void Upload(string filename, Stream stream)
        {
            m_IsUpload = true;
            Init(filename, stream);

            try
            {
                m_TransferSize = stream.Length;

                if (m_TransferSize >= 0)
                {
                    m_RequestedOptions.Add(Option_TransferSize, m_TransferSize.ToString(CultureInfo.InvariantCulture));
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

            m_BlockNumber = 0;
            m_Request = new TFTPPacket_WriteRequest() { EndPoint = m_ServerEndPoint, Filename = filename, Options = m_RequestedOptions };
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
                    if (m_Init)
                    {
                        HandleOptionsAck((TFTPPacket_OptionsAck)packet);
                        // If the transfer was initiated with a Read Request, then an ACK (with the data block number set to 0) is sent by the client to confirm 
                        // the values in the server's OACK packet.
                        m_Request = new TFTPPacket_Ack() { EndPoint = packet.EndPoint, BlockNumber = 0 };
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
                        if (responseData.BlockNumber == m_BlockNumber)
                        {
                            m_Request = new TFTPPacket_Ack { EndPoint = packet.EndPoint, BlockNumber = m_BlockNumber };
                            m_Stream.Write(responseData.Data.Array, responseData.Data.Offset, responseData.Data.Count);
                            m_BlockNumber++;
                            result = (responseData.Data.Count < m_BlockSize) ? Instruction.SendFinal : Instruction.SendNew;
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
                    if (m_Init)
                    {
                        HandleOptionsAck((TFTPPacket_OptionsAck)packet);
                        Progress(true);
                        // If the transfer was initiated with a Write Request, then the client begins the transfer with the first DATA packet (blocknr=1), using the negotiated values.  
                        // If the client rejects the OACK, then it sends an ERROR packet, with error code 8, to the server and the transfer is terminated.
                        m_BlockNumber++;
                        m_Request = new TFTPPacket_Data() { EndPoint = packet.EndPoint, BlockNumber = m_BlockNumber, Data = ReadData(m_Stream, m_BlockSize) };
                        result = Instruction.SendNew;
                        m_Transferred += ((TFTPPacket_Data)m_Request).Data.Count;
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
                        m_PeerEndPoint = packet.EndPoint;
                        // did we receive the expected blocknumber ?
                        if (responseData.BlockNumber == m_BlockNumber)
                        {
                            Progress(false);
                            // was the outstanding request a data packet, and the last one?
                            if (m_Request is TFTPPacket_Data && ((TFTPPacket_Data)m_Request).Data.Count < m_BlockSize)
                            {
                                // that was the ack for the last packet, we're done
                                result = Instruction.Done;
                            }
                            else
                            {
                                m_BlockNumber++;
                                m_Request = new TFTPPacket_Data() { EndPoint = packet.EndPoint, BlockNumber = m_BlockNumber, Data = ReadData(m_Stream, m_BlockSize) };
                                result = Instruction.SendNew;
                                m_Transferred += ((TFTPPacket_Data)m_Request).Data.Count;
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
            this.m_Filename = filename;
            this.m_Stream = stream;
            m_BlockSize = DefaultBlockSize;
            m_Timeout = (int)Math.Ceiling(m_Settings.ResponseTimeout.TotalSeconds);
            m_Init = true;
            m_RequestedOptions = new Dictionary<string, string>();

            if (m_Settings.BlockSize != DefaultBlockSize)
            {
                // limit blocksize to allowed range
                m_RequestedOptions.Add(Option_BlockSize, Clip(m_Settings.BlockSize, MinBlockSize, MaxBlockSize).ToString());
            }

            m_RequestedOptions.Add(Option_Timeout, Clip(m_Timeout, 1, 255).ToString());

            m_LastProgressTime = 0;
            m_Transferred = 0;
            m_TransferSize = -1;
            m_ProgressStopwatch.Restart();

            SetSocketBufferSize();

            m_PeerEndPoint = new IPEndPoint(m_ServerEndPoint.Address, m_ServerEndPoint.Port);
        }

        private void SetSocketBufferSize()
        {
            int multiple = 8;

            while (multiple > 0)
            {
                try
                {
                    var socketBufferSize = multiple * (m_BlockSize + 4);
                    Trace(() => $"Setting socket buffers to {socketBufferSize}");
                    m_Socket.SendBufferSize = socketBufferSize;
                    m_Socket.ReceiveBufferSize = socketBufferSize;
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
                m_BlockSize = int.Parse(response.Options[Option_BlockSize], CultureInfo.InvariantCulture);
                SetSocketBufferSize();
            }

            if (response.Options.ContainsKey(Option_Timeout))
            {
                m_Timeout = int.Parse(response.Options[Option_Timeout], CultureInfo.InvariantCulture);
            }

            if (response.Options.ContainsKey(Option_TransferSize))
            {
                m_TransferSize = long.Parse(response.Options[Option_TransferSize], CultureInfo.InvariantCulture);
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

        public static void Download(IPEndPoint serverEndPoint, string localFilename, string remoteFilename, Settings settings = null)
        {
            using (Stream localStream = File.Create(localFilename))
            {
                Download(serverEndPoint, localStream, remoteFilename, settings);
            }
        }

        public static void Download(IPEndPoint serverEndPoint, Stream localStream, string remoteFilename, Settings settings = null)
        {
            using (var session = new TFTPClient(serverEndPoint, settings))
            {
                session.Download(remoteFilename, localStream);
            }
        }

        public static void Upload(IPEndPoint serverEndPoint, string localFilename, string remoteFilename, Settings settings = null)
        {
            using (Stream localStream = File.OpenRead(localFilename))
            {
                Upload(serverEndPoint, localStream, remoteFilename, settings);
            }
        }

        public static void Upload(IPEndPoint serverEndPoint, Stream localStream, string remoteFilename, Settings settings = null)
        {
            using (var session = new TFTPClient(serverEndPoint, settings))
            {
                session.Upload(remoteFilename, localStream);
            }
        }
    }
}
