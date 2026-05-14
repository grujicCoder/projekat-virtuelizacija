using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Models;

namespace Client.Helpers
{
    public class DisposalTest
    {
        public static void RunTest(string csvPath)
        {
            Console.WriteLine("\n========================================");
            Console.WriteLine("  TEST: Dispose pattern demonstracija  ");
            Console.WriteLine("========================================");

            Console.WriteLine("\n[TEST 1] Normalno citanje i zatvaranje resursa:");
            try
            {
                using (CsvReader reader = new CsvReader(csvPath, "test_log.log"))
                {
                    List<MotorSample> samples = reader.ReadSamples(5);
                    Console.WriteLine($"[TEST 1] Procitano {samples.Count} uzoraka.");
                }
                Console.WriteLine("[TEST 1] Resursi uspesno oslobodjeni nakon normalnog rada.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TEST 1] Greska: {ex.Message}");
            }

            Console.WriteLine("\n[TEST 2] Citanje sa simuliranim izuzetkom:");
            try
            {
                using (CsvReader reader = new CsvReader(csvPath, "test_log.log"))
                {
                    List<MotorSample> samples = reader.ReadSamples(5);
                    Console.WriteLine($"[TEST 2] Procitano {samples.Count} uzoraka.");
                    throw new Exception("Simuliran prekid veze usred prenosa!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TEST 2] Uhvacen izuzetak: {ex.Message}");
                Console.WriteLine("[TEST 2] Resursi su oslobodjeni i pored izuzetka!");
            }

            Console.WriteLine("\n[TEST 3] Pokusaj duplog Dispose poziva:");
            CsvReader disposedReader = new CsvReader(csvPath, "test_log.log");
            disposedReader.Dispose();
            Console.WriteLine("[TEST 3] Prvi Dispose pozvan.");
            disposedReader.Dispose();
            Console.WriteLine("[TEST 3] Drugi Dispose pozvan - nema greske!");

            Console.WriteLine("\n========================================");
            Console.WriteLine("  TEST ZAVRSEN                         ");
            Console.WriteLine("========================================\n");
        }
    }
}
