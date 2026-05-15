using Common.Contracts;
using Common.Exceptions;
using Common.Models;
using System;
using System.Collections.Generic;
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

        public string StartSession(SessionMeta meta)
        {
            if (meta == null)
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault("Meta zaglavlje je null.", "meta"));

            _currentMeta = meta;
            _samples = new List<MotorSample>();
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

            return $"ACK - Uzorak #{_samples.Count} primljen. Status: IN_PROGRESS";
        }

        public string EndSession()
        {
            if (!_sessionActive)
                throw new FaultException<ValidationFault>(
                    new ValidationFault("Nema aktivne sesije.", "session", "aktivna sesija"));

            _sessionActive = false;

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
