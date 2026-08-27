using System;
using System.IO;
using System.Text;

namespace Yogurting.Core.Logging
{
    public enum LogLevel
    {
        Debug,
        Info,
        Packet,
        Warn,
        Error
    }

    public static class Logger
    {
        private static readonly object _lock = new();
        private static StreamWriter? _fileWriter;
        private static string _logFilePath = string.Empty;

        public static void Initialize(string logDir = "logs")
        {
            try
            {
                Directory.CreateDirectory(logDir);
                _logFilePath = Path.Combine(logDir, $"server_{DateTime.Now:yyyyMMdd_HHmmss}.log");
                _fileWriter = new StreamWriter(new FileStream(_logFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite), Encoding.UTF8)
                {
                    AutoFlush = true
                };
                Info("Logging", $"Server log initialized at '{_logFilePath}'");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Logging] Failed to initialize file logger: {ex.Message}");
            }
        }

        public static void Info(string category, string message) => Log(LogLevel.Info, category, message, ConsoleColor.Cyan);
        public static void Success(string category, string message) => Log(LogLevel.Info, category, message, ConsoleColor.Green);
        public static void Warn(string category, string message) => Log(LogLevel.Warn, category, message, ConsoleColor.Yellow);
        public static void Error(string category, string message, Exception? ex = null) => Log(LogLevel.Error, category, ex != null ? $"{message}\n{ex}" : message, ConsoleColor.Red);
        public static void Debug(string category, string message) => Log(LogLevel.Debug, category, message, ConsoleColor.DarkGray);

        public static void Info(string message) => Log(LogLevel.Info, "Server", message, ConsoleColor.Cyan);
        public static void Success(string message) => Log(LogLevel.Info, "Server", message, ConsoleColor.Green);
        public static void Warn(string message) => Log(LogLevel.Warn, "Server", message, ConsoleColor.Yellow);
        public static void Error(string message, Exception? ex = null) => Log(LogLevel.Error, "Server", ex != null ? $"{message}\n{ex}" : message, ConsoleColor.Red);
        public static void Debug(string message) => Log(LogLevel.Debug, "Server", message, ConsoleColor.DarkGray);

        private static bool IsHighFrequencyPacket(ushort opcode)
        {
            return opcode switch
            {
                0x79D5 or 0x791E or 0x7921 or 0x7922 or 0x7969 or 0x4E26 or 0x7759 => true,
                _ => false
            };
        }

        public static void Packet(string serverName, string direction, ushort opcode, string opcodeName, int length, byte[]? payload = null)
        {
            // Skip noisy console and disk lock overhead for high-frequency movement packets
            if (IsHighFrequencyPacket(opcode))
            {
                return;
            }

            string timeStr = DateTime.Now.ToString("HH:mm:ss.fff");
            string summary = $"[{timeStr}] [{serverName}] {direction} Opcode 0x{opcode:X4} ({opcode,5}) [{opcodeName}] ({length} bytes)";
            
            lock (_lock)
            {
                Console.ForegroundColor = direction.Contains("RECV") ? ConsoleColor.Magenta : ConsoleColor.DarkCyan;
                Console.WriteLine(summary);

                if (payload != null && payload.Length > 0 && payload.Length <= 128)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("    Hex: " + BitConverter.ToString(payload).Replace("-", " "));
                }
                Console.ResetColor();

                _fileWriter?.WriteLine(summary);
                if (payload != null && payload.Length > 0)
                {
                    _fileWriter?.WriteLine("    Hex: " + BitConverter.ToString(payload).Replace("-", " "));
                }
            }
        }

        private static void Log(LogLevel level, string category, string message, ConsoleColor color)
        {
            string timeStr = DateTime.Now.ToString("HH:mm:ss.fff");
            string line = $"[{timeStr}] [{category}] {message}";

            lock (_lock)
            {
                Console.ForegroundColor = color;
                Console.WriteLine(line);
                Console.ResetColor();

                _fileWriter?.WriteLine(line);
            }
        }
    }
}
