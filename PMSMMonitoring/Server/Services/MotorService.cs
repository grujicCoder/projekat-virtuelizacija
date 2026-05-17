using Common.Contracts;
using Common.Exceptions;
using Common.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Server.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class MotorService : IMotorService
    {
        private List<MotorSample> _samples = new List<MotorSample>();
        private SessionMeta _currentMeta = null;
        private bool _sessionActive = false;

        private string _sessionFilePath = null;
        private string _rejectsFilePath = null;
        private StreamWriter _sessionWriter = null;
        private StreamWriter _rejectsWriter = null;

        private readonly MotorEventPublisher _publisher = new MotorEventPublisher();

        private double _prevUq = double.NaN;
        private double _prevUd = double.NaN;
        private double _prevSpeed = double.NaN;
        private double _speedSum = 0;
        private int _speedCount = 0;

        private double _udThreshold;
        private double _uqThreshold;
        private double _speedThreshold;
        public MotorService()
        {
            _publisher.OnTransferStarted += (sender, e) =>
                Console.WriteLine($"[EVENT] Transfer Started: {e.Message} u {e.Timestamp:HH:mm:ss}");

            _publisher.OnSampleReceived += (sender, e) =>
                Console.WriteLine($"[EVENT] Sample #{e.SampleNumber} Received: {e.Message}");

            _publisher.OnTransferCompleted += (sender, e) =>
                Console.WriteLine($"[EVENT] Transfer Completed: {e.Message} u {e.Timestamp:HH:mm:ss}");

            _publisher.OnWarningRaised += (sender, e) =>
                Console.WriteLine($"[EVENT][WARNING] {e.WarningType}: {e.Message} u {e.Timestamp:HH:mm:ss}");

            _udThreshold = double.Parse(
                ConfigurationManager.AppSettings["Ud_threshold"] ?? "5.0",
                System.Globalization.CultureInfo.InvariantCulture);
            _uqThreshold = double.Parse(
                ConfigurationManager.AppSettings["Uq_threshold"] ?? "5.0",
                System.Globalization.CultureInfo.InvariantCulture);
            _speedThreshold = double.Parse(
                ConfigurationManager.AppSettings["Speed_threshold"] ?? "50.0",
                System.Globalization.CultureInfo.InvariantCulture);

            _publisher.OnVoltageSpikeQ += (sender, e) =>
                Console.WriteLine($"[EVENT][UPOZORENJE] {e.WarningType}: {e.Message}");

            _publisher.OnVoltageSpikeD += (sender, e) =>
                Console.WriteLine($"[EVENT][UPOZORENJE] {e.WarningType}: {e.Message}");

            _publisher.OnSpeedSpike += (sender, e) =>
                Console.WriteLine($"[EVENT][UPOZORENJE] {e.WarningType}: {e.Message}");

            _publisher.OnOutOfBandWarning += (sender, e) =>
                Console.WriteLine($"[EVENT][UPOZORENJE] {e.WarningType}: {e.Message}");
        }

        public string StartSession(SessionMeta meta)
        {
            if (meta == null)
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault("Meta zaglavlje je null.", "meta"));

            _currentMeta = meta;
            _samples = new List<MotorSample>();
            _prevUq = double.NaN;
            _prevUd = double.NaN;
            _prevSpeed = double.NaN;
            _speedSum = 0;
            _speedCount = 0;
            _sessionActive = true;

            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string sessionFolder = Path.Combine(basePath, "Sessions");

            if (!Directory.Exists(sessionFolder))
                Directory.CreateDirectory(sessionFolder);

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            _sessionFilePath = Path.Combine(sessionFolder, $"measurements{timestamp}.csv");
            _rejectsFilePath = Path.Combine(sessionFolder, $"rejects{timestamp}.csv");

            // kreiranje StreamWriter-a za sesiju
            _sessionWriter = new StreamWriter(_sessionFilePath, append: false);
            _sessionWriter.WriteLine("U_q,U_d,Motor_Speed,Profile_Id,Ambient,Torque");
            _sessionWriter.Flush();

            // kreiranje StreamWriter-a za odbacena merenja
            _rejectsWriter = new StreamWriter(_rejectsFilePath, append: false);
            _rejectsWriter.WriteLine("U_q,U_d,Motor_Speed,Profile_Id,Ambient,Torque,Razlog");
            _rejectsWriter.Flush();

            Console.WriteLine("[SERVER] Sesija zapoceta.");
            Console.WriteLine($"  Profile_Id : {meta.Profile_Id}");
            Console.WriteLine($"  Ambient    : {meta.Ambient}");
            Console.WriteLine($"  Torque     : {meta.Torque}");
            Console.WriteLine($"  Fajl sesije: {_sessionFilePath}");

            _publisher.RaiseTransferStarted($"Sesija zapoceta za Profile_Id: {meta.Profile_Id}");

            return "ACK - Sesija uspesno zapoceta. Status: IN_PROGRESS";
        }

        public string PushSample(MotorSample sample)
        {
            if (!_sessionActive)
                throw new FaultException<ValidationFault>(
                    new ValidationFault("Sesija nije aktivna.", "session", "aktivna sesija"));

            // validacija
            if (sample.Motor_Speed < 0)
            {
                _rejectsWriter?.WriteLine(
                    $"{sample.U_q},{sample.U_d},{sample.Motor_Speed}," +
                    $"{sample.Profile_Id},{sample.Ambient},{sample.Torque}," +
                    $"Motor_Speed negativan");
                _rejectsWriter?.Flush();

                throw new FaultException<ValidationFault>(
                    new ValidationFault("Motor_Speed ne sme biti negativan.",
                        "Motor_Speed", "> 0"));
            }

            _samples.Add(sample);

            // upisivanje u measurements_session.csv
            _sessionWriter?.WriteLine(
                $"{sample.U_q},{sample.U_d},{sample.Motor_Speed}," +
                $"{sample.Profile_Id},{sample.Ambient},{sample.Torque}");
            _sessionWriter?.Flush();

            Console.WriteLine($"[SERVER] Primljen uzorak #{_samples.Count} " +
                $"| Speed: {sample.Motor_Speed:F2} " +
                $"| Uq: {sample.U_q:F2} " +
                $"| Ud: {sample.U_d:F2}");

            _publisher.RaiseSampleReceived($"Uzorak primljen", _samples.Count);

            if (!double.IsNaN(_prevUq))
            {
                double deltaUq = sample.U_q - _prevUq;
                if (Math.Abs(deltaUq) > _uqThreshold)
                {
                    string direction = deltaUq > 0 ? "iznad ocekivanog" : "ispod ocekivanog";
                    _publisher.RaiseVoltageSpikeQ(
                        $"DeltaUq={deltaUq:F4} > prag={_uqThreshold}", direction);
                }
            }

            if (!double.IsNaN(_prevUd))
            {
                double deltaUd = sample.U_d - _prevUd;
                if (Math.Abs(deltaUd) > _udThreshold)
                {
                    string direction = deltaUd > 0 ? "iznad ocekivanog" : "ispod ocekivanog";
                    _publisher.RaiseVoltageSpikeD(
                        $"DeltaUd={deltaUd:F4} > prag={_udThreshold}", direction);
                }
            }

            _prevUq = sample.U_q;
            _prevUd = sample.U_d;

            if (!double.IsNaN(_prevSpeed))
            {
                double deltaSpeed = sample.Motor_Speed - _prevSpeed;
                if (Math.Abs(deltaSpeed) > _speedThreshold)
                {
                    string direction = deltaSpeed > 0 ? "iznad ocekivanog" : "ispod ocekivanog";
                    _publisher.RaiseSpeedSpike(
                        $"DeltaSpeed={deltaSpeed:F4} > prag={_speedThreshold}", direction);
                }
            }

            _prevSpeed = sample.Motor_Speed;
            _speedSum += sample.Motor_Speed;
            _speedCount++;
            double speedMean = _speedSum / _speedCount;

            if (_speedCount > 1)
            {
                double lowerBound = 0.75 * speedMean;
                double upperBound = 1.25 * speedMean;

                if (sample.Motor_Speed < lowerBound)
                {
                    _publisher.RaiseOutOfBandWarning(
                        $"Speed={sample.Motor_Speed:F4} ispod 75% proseka ({speedMean:F4})",
                        "ispod ocekivane vrednosti");
                }
                else if (sample.Motor_Speed > upperBound)
                {
                    _publisher.RaiseOutOfBandWarning(
                        $"Speed={sample.Motor_Speed:F4} iznad 125% proseka ({speedMean:F4})",
                        "iznad ocekivane vrednosti");
                }
            }

            return $"ACK - Uzorak #{_samples.Count} primljen. Status: IN_PROGRESS";
        }

        public string EndSession()
        {
            if (!_sessionActive)
                throw new FaultException<ValidationFault>(
                    new ValidationFault("Nema aktivne sesije.", "session", "aktivna sesija"));

            _sessionActive = false;

            _publisher.RaiseTransferCompleted($"Sesija zavrsena. Primljeno {_samples.Count} uzoraka.");

            // zatvaranje StreamWriter-a
            _sessionWriter?.Close();
            _sessionWriter = null;
            _rejectsWriter?.Close();
            _rejectsWriter = null;

            Console.WriteLine($"[SERVER] Sesija zavrsena. Ukupno uzoraka: {_samples.Count}");
            Console.WriteLine($"[SERVER] Podaci snimljeni u: {_sessionFilePath}");

            return $"ACK - Sesija zavrsena. Primljeno uzoraka: {_samples.Count}. Status: COMPLETED";
        }

        public string GetTransferStatus()
        {
            if (_sessionActive)
                return $"STATUS: Prenos u toku... Primljeno uzoraka: {_samples.Count}";
            else
                return $"STATUS: Prenos zavrsen. Ukupno uzoraka: {_samples.Count}";
        }
    }
}
