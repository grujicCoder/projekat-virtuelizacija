using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Events
{
    public class TransferEventArgs : EventArgs
    {
        public string Message { get; }
        public DateTime Timestamp { get; }

        public TransferEventArgs(string message)
        {
            Message = message;
            Timestamp = DateTime.Now;
        }
    }

    public class SampleEventArgs : EventArgs
    {
        public string Message { get; }
        public int SampleNumber { get; }
        public DateTime Timestamp { get; }

        public SampleEventArgs(string message, int sampleNumber)
        {
            Message = message;
            SampleNumber = sampleNumber;
            Timestamp = DateTime.Now;
        }
    }

    public class WarningEventArgs : EventArgs
    {
        public string Message { get; }
        public string WarningType { get; }
        public DateTime Timestamp { get; }

        public WarningEventArgs(string message, string warningType)
        {
            Message = message;
            WarningType = warningType;
            Timestamp = DateTime.Now;
        }
    }
}
