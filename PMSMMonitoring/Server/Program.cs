using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Server.Services;
using System.ServiceModel;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            ServiceHost host = new ServiceHost(typeof(MotorService));

            try
            {
                host.Open();
                Console.WriteLine("=================================");
                Console.WriteLine("  PMSM Motor Monitoring Server  ");
                Console.WriteLine("=================================");
                Console.WriteLine("Servis je pokrenut na: net.tcp://localhost:9000/MotorService");
                Console.WriteLine("Pritisnite ENTER za gasenje servisa...");
                Console.ReadLine();
                host.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greska pri pokretanju servisa: {ex.Message}");
                host.Abort();
            }
        }
    }
}
