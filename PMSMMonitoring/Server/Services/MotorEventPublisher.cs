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

        public event EventHandler<WarningEventArgs> OnVoltageSpikeQ;
        public event EventHandler<WarningEventArgs> OnVoltageSpikeD;
        public event EventHandler<WarningEventArgs> OnSpeedSpike;
        public event EventHandler<WarningEventArgs> OnOutOfBandWarning;

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

        public void RaiseVoltageSpikeQ(string message, string direction)
        {
            OnVoltageSpikeQ?.Invoke(this, new WarningEventArgs(message, $"VoltageSpikeQ - {direction}"));
        }

        public void RaiseVoltageSpikeD(string message, string direction)
        {
            OnVoltageSpikeD?.Invoke(this, new WarningEventArgs(message, $"VoltageSpikeD - {direction}"));
        }

        public void RaiseSpeedSpike(string message, string direction)
        {
            OnSpeedSpike?.Invoke(this, new WarningEventArgs(message, $"SpeedSpike - {direction}"));
        }

        public void RaiseOutOfBandWarning(string message, string direction)
        {
            OnOutOfBandWarning?.Invoke(this, new WarningEventArgs(message, $"OutOfBandWarning - {direction}"));
        }
    }
}
