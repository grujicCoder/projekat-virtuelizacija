using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Contracts;
using Common.Exceptions;
using Common.Models;
using System.ServiceModel;

namespace Server.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class MotorService : IMotorService
    {
        private List<MotorSample> _samples = new List<MotorSample>();
        private SessionMeta _currentMeta = null;
        private bool _sessionActive = false;

        public string StartSession(SessionMeta meta)
        {
            if (meta == null)
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault("Meta zaglavlje je null.", "meta"));

            _currentMeta = meta;
            _samples = new List<MotorSample>();
            _sessionActive = true;

            Console.WriteLine("[SERVER] Sesija zapoceta.");
            Console.WriteLine($"  Profile_Id : {meta.Profile_Id}");
            Console.WriteLine($"  Ambient    : {meta.Ambient}");
            Console.WriteLine($"  Torque     : {meta.Torque}");

            return "ACK - Sesija uspesno zapoceta. Status: IN_PROGRESS";
        }

        public string PushSample(MotorSample sample)
        {
            if (!_sessionActive)
                throw new FaultException<ValidationFault>(
                    new ValidationFault("Sesija nije aktivna.", "session", "aktivna sesija"));

            if (sample.Motor_Speed < 0)
                throw new FaultException<ValidationFault>(
                    new ValidationFault("Motor_Speed ne sme biti negativan.",
                        "Motor_Speed", "> 0"));

            _samples.Add(sample);

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

            Console.WriteLine($"[SERVER] Sesija zavrsena. Ukupno uzoraka: {_samples.Count}");

            return $"ACK - Sesija zavrsena. Primljeno uzoraka: {_samples.Count}. Status: COMPLETED";
        }
    }
}
