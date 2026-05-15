using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Client.Helpers;
using Common.Contracts;
using Common.Models;
using System.Configuration;
using System.ServiceModel;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=================================");
            Console.WriteLine("  PMSM Motor Monitoring Client  ");
            Console.WriteLine("=================================");

            string csvPath = ConfigurationManager.AppSettings["CsvPath"];
            int maxRows = int.Parse(ConfigurationManager.AppSettings["MaxRows"]);

            List<MotorSample> samples;
            using (CsvReader csvReader = new CsvReader(csvPath, "invalid_rows.log"))
            {
                samples = csvReader.ReadSamples(maxRows);
            }
            if (samples.Count == 0)
            {
                Console.WriteLine("[CLIENT] Nema validnih uzoraka. Gasenje...");
                Console.ReadLine();
                return;
            }
            DisposalTest.RunTest(csvPath);
            ChannelFactory<IMotorService> factory = null;
            IMotorService service = null;

            try
            {
                factory = new ChannelFactory<IMotorService>("MotorServiceEndpoint");
                service = factory.CreateChannel();

                SessionMeta meta = new SessionMeta
                {
                    Profile_Id = samples[0].Profile_Id,
                    Ambient = samples[0].Ambient,
                    Torque = samples[0].Torque
                };

                string startResponse = service.StartSession(meta);
                Console.WriteLine($"[CLIENT] {startResponse}");

                Console.WriteLine($"\n[CLIENT] Slanje {samples.Count} uzoraka...\n");

                for (int i = 0; i < samples.Count; i++)
                {
                    try
                    {
                        string response = service.PushSample(samples[i]);
                        Console.WriteLine($"[CLIENT] {response}");
                    }
                    catch (FaultException ex)
                    {
                        Console.WriteLine($"[CLIENT] Greska za uzorak #{i + 1}: {ex.Message}");
                    }
                }

                string endResponse = service.EndSession();
                Console.WriteLine($"\n[CLIENT] {endResponse}");

                factory.Close();
            }
            catch (FaultException ex)
            {
                Console.WriteLine($"[CLIENT] FaultException: {ex.Message}");
                factory?.Abort();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CLIENT] Greska: {ex.Message}");
                factory?.Abort();
            }

            Console.WriteLine("\nPritisnite ENTER za izlaz...");
            Console.ReadLine();
        }
    }
}
