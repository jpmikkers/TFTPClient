using System;

namespace GitHub.JPMikkers.TFTP.Client
{
    [Serializable()]
    public class TFTPException : Exception
    {
        public TFTPException() : base() { }
        public TFTPException(string message) : base(message) { }
        public TFTPException(string message, System.Exception inner) : base(message, inner) { }
        protected TFTPException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
}
