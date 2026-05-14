using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Models;
using System.Globalization;
using System.IO;

namespace Client.Helpers
{
    public class CsvReader : IDisposable
    {
        private readonly string _csvPath;
        private readonly string _logPath;
        private StreamReader _reader;
        private StreamWriter _logWriter;
        private bool _disposed = false;

        public CsvReader(string csvPath, string logPath)
        {
            _csvPath = csvPath;
            _logPath = logPath;
        }

        private void InitializeStreams()
        {
            _reader = new StreamReader(_csvPath);
            _logWriter = new StreamWriter(_logPath, append: true);
        }

        public List<MotorSample> ReadSamples(int maxRows)
        {
            InitializeStreams();
            List<MotorSample> samples = new List<MotorSample>(maxRows);

            try
            {
                string header = _reader.ReadLine();
                if (header == null)
                {
                    Console.WriteLine("[CLIENT] CSV fajl je prazan!");
                    return samples;
                }

                Console.WriteLine($"[CLIENT] Header: {header}");
                Console.WriteLine($"[CLIENT] Citanje maksimalno {maxRows} redova...");

                int rowNumber = 0;
                int validCount = 0;
                int invalidCount = 0;
                string line;

                while ((line = _reader.ReadLine()) != null && validCount < maxRows)
                {
                    rowNumber++;
                    MotorSample sample = TryParseLine(line, rowNumber);

                    if (sample != null)
                    {
                        samples.Add(sample);
                        validCount++;
                    }
                    else
                    {
                        invalidCount++;
                        LogInvalidRow(rowNumber, line);
                    }
                }

                Console.WriteLine($"[CLIENT] Ucitano validnih redova  : {validCount}");
                Console.WriteLine($"[CLIENT] Nevalidnih redova (log)  : {invalidCount}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CLIENT] Greska pri citanju CSV: {ex.Message}");
            }

            return samples;
        }

        private MotorSample TryParseLine(string line, int rowNumber)
        {
            try
            {
                string[] parts = line.Split(',');

                if (parts.Length < 13)
                {
                    return null;
                }

                double u_q = double.Parse(parts[0].Trim(), CultureInfo.InvariantCulture);
                double u_d = double.Parse(parts[3].Trim(), CultureInfo.InvariantCulture);
                double motor_speed = double.Parse(parts[5].Trim(), CultureInfo.InvariantCulture);
                double ambient = double.Parse(parts[10].Trim(), CultureInfo.InvariantCulture);
                double torque = double.Parse(parts[11].Trim(), CultureInfo.InvariantCulture);
                int profile_id = int.Parse(parts[12].Trim(), CultureInfo.InvariantCulture);

                if (motor_speed < 0)
                    return null;

                return new MotorSample
                {
                    U_q = u_q,
                    U_d = u_d,
                    Motor_Speed = motor_speed,
                    Ambient = ambient,
                    Torque = torque,
                    Profile_Id = profile_id
                };
            }
            catch
            {
                return null;
            }
        }

        private void LogInvalidRow(int rowNumber, string line)
        {
            string logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] " +
                             $"Red #{rowNumber} je nevalidan: {line}";
            _logWriter.WriteLine(logLine);
            _logWriter.Flush();
            Console.WriteLine($"[CLIENT] Nevalidan red #{rowNumber} - zapisano u log.");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _reader?.Close();
                    _logWriter?.Close();
                    Console.WriteLine("[CLIENT] CsvReader resursi oslobodjeni.");
                }
                _disposed = true;
            }
        }

        ~CsvReader()
        {
            Dispose(false);
        }
    }
}
