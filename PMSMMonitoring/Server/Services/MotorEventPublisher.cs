using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Events;

namespace Server.Services
{
    public class MotorEventPublisher
    {
        public event EventHandler<TransferEventArgs> OnTransferStarted;
        public event EventHandler<SampleEventArgs> OnSampleReceived;
        public event EventHandler<TransferEventArgs> OnTransferCompleted;
        public event EventHandler<WarningEventArgs> OnWarningRaised;

        public void RaiseTransferStarted(string message)
        {
            OnTransferStarted?.Invoke(this, new TransferEventArgs(message));
        }

        public void RaiseSampleReceived(string message, int sampleNumber)
        {
            OnSampleReceived?.Invoke(this, new SampleEventArgs(message, sampleNumber));
        }

        public void RaiseTransferCompleted(string message)
        {
            OnTransferCompleted?.Invoke(this, new TransferEventArgs(message));
        }

        public void RaiseWarning(string message, string warningType)
        {
            OnWarningRaised?.Invoke(this, new WarningEventArgs(message, warningType));
        }
    }
}
