using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMFGC_Server {
    public class Output {
        private readonly string LogDirPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");

        private static Output _outputSingleton;
        private static Output OutputSingleton {
            get {
                if (_outputSingleton == null) {
                    _outputSingleton = new Output();
                }
                return _outputSingleton;
            }
        }

        public StreamWriter SW { get; set; }

        public Output() {
            EnsureLogDirectoryExists();
            InstantiateStreamWriter();
        }

        ~Output() {
            if (SW != null) {
                try {
                    SW.Dispose();
                }
                catch (ObjectDisposedException) { } // object already disposed - ignore exception
            }
        }

        public static void WriteLine(string str) {
            Console.WriteLine(str);
            OutputSingleton.SW.WriteLine(str);
        }

        public static void Write(string str) {
            Console.Write(str);
            OutputSingleton.SW.Write(str);
        }

        private void InstantiateStreamWriter() {
            string filePath = Path.Combine(LogDirPath, "latest") + ".log";
            try {
                SW = new StreamWriter(filePath);
                SW.AutoFlush = true;
            }
            catch (UnauthorizedAccessException ex) {
                throw new ApplicationException(string.Format("Access denied. Could not instantiate StreamWriter using path: {0}.", filePath), ex);
            }
        }

        private void EnsureLogDirectoryExists() {
            if (!Directory.Exists(LogDirPath)) {
                try {
                    Directory.CreateDirectory(LogDirPath);
                }
                catch (UnauthorizedAccessException ex) {
                    throw new ApplicationException(string.Format("Access denied. Could not create log directory using path: {0}.", LogDirPath), ex);
                }
            }
        }
    }
}
