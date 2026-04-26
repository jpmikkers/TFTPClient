using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Baksteen.Net.TFTP.Client;

public partial class TFTPClient : IDisposable
{
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

        if (data.Array != null)
        {
            for (var t = 0; t < limit; t++)
            {
                sb.Append(data.Array[data.Offset + t].ToString("X2"));
                sb.Append(separator);
            }
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
    internal static ushort ReadUInt16(Stream stream)
    {
        Span<byte> buf = stackalloc byte[sizeof(ushort)];
        stream.ReadExactly(buf);
        return BinaryPrimitives.ReadUInt16BigEndian(buf);
    }

    internal static void WriteUInt16(Stream stream, ushort value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(ushort)];
        BinaryPrimitives.WriteUInt16BigEndian(buffer, value);
        stream.Write(buffer);
    }

    internal static Dictionary<string, string> ReadOptions(Stream stream)
    {
        var options = new Dictionary<string, string>();
        while (stream.Position < stream.Length)
        {
            string key = ReadZString(stream).ToLower();
            string val = ReadZString(stream).ToLower();
            options.Add(key, val);
        }
        return options;
    }

    internal static void WriteOptions(Stream stream, Dictionary<string, string> options)
    {
        foreach (var option in options)
        {
            WriteZString(stream, option.Key);
            WriteZString(stream, option.Value);
        }
    }

    internal static string ReadZString(Stream stream)
    {
        var sb = new StringBuilder();
        int c = stream.ReadByte();
        while (c > 0)
        {
            sb.Append((char)c);
            c = stream.ReadByte();
        }
        return sb.ToString();
    }

    internal static void WriteZString(Stream stream, string msg)
    {
        var buf = Encoding.ASCII.GetBytes(msg);
        stream.Write(buf);
        stream.WriteByte(0);
    }
    #endregion
}
