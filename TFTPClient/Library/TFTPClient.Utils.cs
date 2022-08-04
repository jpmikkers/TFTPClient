using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace Baksteen.Net.TFTP.Client
{
    public partial class TFTPClient : IDisposable
    {
        /// <summary>
        /// Generic clip method
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value">input value</param>
        /// <param name="minValue">minimum value to clip to</param>
        /// <param name="maxValue">maximum value to clip to</param>
        /// <returns></returns>
        private static T Clip<T>(T value, T minValue, T maxValue) where T : IComparable<T>
        {
            T result;
            if (value.CompareTo(minValue) < 0)
                result = minValue;
            else if (value.CompareTo(maxValue) > 0)
                result = maxValue;
            else
                result = value;
            return result;
        }

        /// <summary>
        /// Converts a string key/value dictionary into a pretty printed string. Example:
        /// 'key1'='value1', 'key2'='value2' ...
        /// </summary>
        /// <param name="options">dictionary to format as a string</param>
        /// <returns>formatted key-value collection string</returns>
        private static string OptionString(Dictionary<string, string> options)
        {
            return options.Select(x => $"'{x.Key}'='{x.Value}'").Aggregate((x, y) => x + ", " + y);
        }

        /// <summary>
        /// Converts an array segment into a hex string, limited to a given number of bytes.
        /// If length of the input segment exceeded that limit, the string is terminated with an ellipsis.
        /// </summary>
        /// <param name="data">array segment to convert into a hex string</param>
        /// <param name="separator">which seperator to use between each hex byte</param>
        /// <param name="limit">maximum number of bytes to convert</param>
        /// <returns>formatted string</returns>
        private static string HexStr(ArraySegment<byte> data, string separator, int limit)
        {
            var sb = new StringBuilder();
            limit = Math.Min(data.Count, limit);

            for (int t = 0; t < limit; t++)
            {
                sb.Append(data.Array[data.Offset + t].ToString("X2"));
                sb.Append(separator);
            }

            if (data.Count > limit)
            {
                sb.Append("..");
            }
            else
            {
                if (sb.Length > separator.Length) sb.Length = sb.Length - separator.Length;
            }

            return sb.ToString();
        }

        #region packet serialization/deserialization
        internal static ushort ReadUInt16(Stream s)
        {
            var br = new BinaryReader(s);
            return (ushort)IPAddress.NetworkToHostOrder((short)br.ReadUInt16());
        }

        internal static void WriteUInt16(Stream s, ushort v)
        {
            var bw = new BinaryWriter(s);
            bw.Write((ushort)IPAddress.HostToNetworkOrder((short)v));
        }

        internal static Dictionary<string, string> ReadOptions(Stream s)
        {
            var options = new Dictionary<string, string>();
            while (s.Position < s.Length)
            {
                string key = ReadZString(s).ToLower();
                string val = ReadZString(s).ToLower();
                options.Add(key, val);
            }
            return options;
        }

        internal static void WriteOptions(Stream s, Dictionary<string, string> options)
        {
            foreach (var option in options)
            {
                WriteZString(s, option.Key);
                WriteZString(s, option.Value);
            }
        }

        internal static string ReadZString(Stream s)
        {
            var sb = new StringBuilder();
            int c = s.ReadByte();
            while (c > 0)
            {
                sb.Append((char)c);
                c = s.ReadByte();
            }
            return sb.ToString();
        }

        internal static void WriteZString(Stream s, string msg)
        {
            var tw = new StreamWriter(s, Encoding.ASCII);
            tw.Write(msg);
            tw.Flush();
            s.WriteByte(0);
        }
        #endregion
    }
}
