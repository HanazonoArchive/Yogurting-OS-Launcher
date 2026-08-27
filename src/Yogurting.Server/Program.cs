using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Yogurting.Core.Logging;
using Yogurting.Core.Models;
using Yogurting.Core.Network;
using Yogurting.Data.Loaders;
using Yogurting.Data.Repositories;
using Yogurting.Server.Handlers;
using Yogurting.Server.World;

namespace Yogurting.Server
{
    public class ServerConfig
    {
        public ServerInfo Server { get; set; } = new();
        public NetworkInfo Network { get; set; } = new();
        public PathInfo Paths { get; set; } = new();
    }

    public class ServerInfo
    {
        public string Name { get; set; } = "Yogurting Online English Revival";
        public string Motd { get; set; } = "Welcome to Yogurting Modern English Server! Enjoy school life!";
        public string Language { get; set; } = "en";
        public int MaxPlayers { get; set; } = 500;
    }

    public class NetworkInfo
    {
        public string BindAddress { get; set; } = "0.0.0.0";
        public int LoginPort { get; set; } = 10000;
        public int FieldPort { get; set; } = 10002;
        public int EpisodePort { get; set; } = 10003;
        public int CommPort { get; set; } = 10004;
    }

    public class PathInfo
    {
        public string DbDirectory { get; set; } = "data/db";
        public string ScoreDirectory { get; set; } = "data/score";
        public string SaveDirectory { get; set; } = "data/save";
    }

    internal static class Program
    {
        private static void Main(string[] args)
        {
            // Register Code Pages Provider for CP932 / Shift-JIS support
            try
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            }
            catch { }

            Console.Title = "Yogurting Modern Server Engine (Open-Source Edition)";
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(@"
  __     __                        _   _               
  \ \   / /                       | | (_)              
   \ \_/ /__   __ _ _   _ _ __ ___| |_ _ _ __   __ _ 
    \   / _ \ / _` | | | | '__/ __| __| | '_ \ / _` |
     | | (_) | (_| | |_| | | | (__| |_| | | | | (_| |
     |_|\___/ \__, |\__,_|_|  \___|\__|_|_| |_|\__, |
               __/ |                            __/ |
              |___/                            |___/ 
");
            Console.ResetColor();

            // Locate base directory and config
            string current = AppDomain.CurrentDomain.BaseDirectory;
            string projectRoot = current;
            while (!string.IsNullOrEmpty(current))
            {
                if (File.Exists(Path.Combine(current, "config", "server.json")) || Directory.Exists(Path.Combine(current, "data", "db")))
                {
                    projectRoot = current;
                    break;
                }
                var parent = Directory.GetParent(current);
                if (parent == null) break;
                current = parent.FullName;
            }

            string logDir = Path.Combine(projectRoot, "logs");
            Logger.Initialize(logDir);

            string configPath = Path.Combine(projectRoot, "config", "server.json");
            ServerConfig config = new ServerConfig();

            if (File.Exists(configPath))
            {
                try
                {
                    string json = File.ReadAllText(configPath);
                    config = JsonSerializer.Deserialize<ServerConfig>(json) ?? new ServerConfig();
                    Logger.Info($"[Config] Loaded configuration from '{configPath}'");
                }
                catch (Exception ex)
                {
                    Logger.Warn($"[Config] Warning: Could not parse server.json ({ex.Message}). Using defaults.");
                }
            }

            bool isSnifferMode = false;
            foreach (var arg in args)
            {
                if (arg.Equals("--sniffer", StringComparison.OrdinalIgnoreCase) || arg.Equals("--proxy", StringComparison.OrdinalIgnoreCase))
                {
                    isSnifferMode = true;
                    config.Network.LoginPort = 20000;
                    config.Network.FieldPort = 20002;
                    config.Network.EpisodePort = 20003;
                    config.Network.CommPort = 20004;
                    Logger.Info("[Config] Sniffer / Proxy Mode Enabled: Listening on internal ports (20000, 20002, 20003, 20004)");
                }
                else if (arg.Equals("--shadow", StringComparison.OrdinalIgnoreCase) || arg.Equals("--diff", StringComparison.OrdinalIgnoreCase))
                {
                    config.Network.LoginPort = 30000;
                    config.Network.FieldPort = 30002;
                    config.Network.EpisodePort = 30003;
                    config.Network.CommPort = 30004;
                    Logger.Info("[Config] Shadow / Differential Mode Enabled: Listening on shadow ports (30000, 30002, 30003, 30004)");
                }
            }

            string dbDir = Path.Combine(projectRoot, config.Paths.DbDirectory);
            string saveDir = Path.Combine(projectRoot, config.Paths.SaveDirectory);

            // 1. Initialize Database Engine (UYgDB 1-to-1)
            var gameDb = new GameDatabase();
            if (Directory.Exists(dbDir))
            {
                gameDb.LoadAll(dbDir);
            }
            else
            {
                Logger.Warn($"[Data] Warning: DB Directory not found at '{dbDir}'");
            }

            // 2. Initialize Starter Items & Character Configuration
            string starterConfigPath = Path.Combine(projectRoot, "config", "starter_items.json");
            StarterConfigLoader.Initialize(starterConfigPath);

            // 3. Initialize Account & Player Repository
            var accountRepo = new JsonAccountRepository(saveDir);

            // 4. Initialize World Manager
            var worldManager = new WorldManager();

            // 5. Initialize Server Handlers
            string clientHost = config.Network.BindAddress == "0.0.0.0" ? "127.0.0.1" : config.Network.BindAddress;
            int clientFieldPort = isSnifferMode ? 10002 : config.Network.FieldPort;
            int clientCommPort = isSnifferMode ? 10004 : config.Network.CommPort;
            var loginHandler = new LoginServerHandler(accountRepo, clientHost, clientFieldPort, clientCommPort);
            var fieldHandler = new FieldServerHandler(accountRepo, worldManager, gameDb);
            var episodeHandler = new EpisodeServerHandler(accountRepo, gameDb, clientHost, clientFieldPort);
            var commHandler = new CommServerHandler(accountRepo);

            // 5. Initialize TCP Game Servers
            var loginServer = new AsyncTcpServer("LoginServer", IPAddress.Any, config.Network.LoginPort);
            loginServer.ClientConnected += loginHandler.HandleClientConnectedAsync;
            loginServer.PacketReceived += loginHandler.HandlePacketAsync;

            var fieldServer = new AsyncTcpServer("FieldServer", IPAddress.Any, config.Network.FieldPort);
            fieldServer.ClientConnected += fieldHandler.HandleClientConnectedAsync;
            fieldServer.PacketReceived += fieldHandler.HandlePacketAsync;
            fieldServer.ClientDisconnected += fieldHandler.HandleClientDisconnectedAsync;

            var episodeServer = new AsyncTcpServer("EpisodeServer", IPAddress.Any, config.Network.EpisodePort);
            episodeServer.ClientConnected += episodeHandler.HandleClientConnectedAsync;
            episodeServer.PacketReceived += episodeHandler.HandlePacketAsync;
            episodeServer.ClientDisconnected += episodeHandler.HandleClientDisconnectedAsync;

            var commServer = new AsyncTcpServer("CommServer", IPAddress.Any, config.Network.CommPort);
            commServer.ClientConnected += commHandler.HandleClientConnectedAsync;
            commServer.PacketReceived += commHandler.HandlePacketAsync;
            commServer.ClientDisconnected += commHandler.HandleClientDisconnectedAsync;

            // 6. Start All Services
            Console.ForegroundColor = ConsoleColor.Green;
            loginServer.Start();
            fieldServer.Start();
            episodeServer.Start();
            commServer.Start();
            Console.ResetColor();

            Logger.Info($"[System] '{config.Server.Name}' is ONLINE on all ports (Login: {config.Network.LoginPort}, Field: {config.Network.FieldPort}, Episode: {config.Network.EpisodePort}, Comm: {config.Network.CommPort})!");
            Logger.Info("[System] Type 'help' for available server commands.\n");

            // Interactive Command Loop
            bool running = true;
            while (running)
            {
                Console.Write("Yogurting> ");
                string? input = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(input)) continue;

                string[] tokens = input.Split(' ', 2);
                string command = tokens[0].ToLowerInvariant();
                string arg = tokens.Length > 1 ? tokens[1] : string.Empty;

                switch (command)
                {
                    case "help":
                        Console.WriteLine("Available Server Commands:");
                        Console.WriteLine("  status              - Show server health & active connections");
                        Console.WriteLine("  users               - List all active connected students");
                        Console.WriteLine("  fields              - Show all active campus zones & populations");
                        Console.WriteLine("  broadcast <message> - Broadcast announcement to all campus zones");
                        Console.WriteLine("  reload              - Reload parameter tables from disk");
                        Console.WriteLine("  clear               - Clear console window");
                        Console.WriteLine("  stop / exit         - Gracefully shut down all game servers");
                        break;

                    case "status":
                        Console.WriteLine($"=== SERVER HEALTH & LOAD ===");
                        Console.WriteLine($"[LoginServer]   Port {config.Network.LoginPort} | Active: {loginServer.ActiveConnections}");
                        Console.WriteLine($"[FieldServer]   Port {config.Network.FieldPort} | Active: {fieldServer.ActiveConnections}");
                        Console.WriteLine($"[EpisodeServer] Port {config.Network.EpisodePort} | Active: {episodeServer.ActiveConnections}");
                        Console.WriteLine($"[CommServer]    Port {config.Network.CommPort} | Active: {commServer.ActiveConnections}");
                        Console.WriteLine($"[Database]      Items: {gameDb.Items.Count}, Episodes: {gameDb.Episodes.Count}, NPCs: {gameDb.Npcs.Count}");
                        break;

                    case "users":
                        Console.WriteLine($"=== CONNECTED STUDENTS ({worldManager.TotalOnlinePlayers} Total) ===");
                        foreach (var field in worldManager.AllFields)
                        {
                            foreach (var p in field.Players)
                            {
                                Console.WriteLine($"  - {p.Player.CharacterName} (ID: {p.Player.CharaId}, School: {p.Player.School}, Zone: {field.Name} [{field.FieldId}])");
                            }
                        }
                        break;

                    case "fields":
                        Console.WriteLine($"=== CAMPUS ZONES ===");
                        foreach (var field in worldManager.AllFields)
                        {
                            Console.WriteLine($"  [{field.FieldId}] {field.Name} - Students: {field.Players.Count}");
                        }
                        break;

                    case "broadcast":
                        if (string.IsNullOrWhiteSpace(arg))
                        {
                            Console.WriteLine("Usage: broadcast <message>");
                            break;
                        }
                        byte[] chatPacket = YogurtingPackets.MakeGameChatNtf(0, "SYSTEM", arg, 1);
                        worldManager.BroadcastGlobalAsync(chatPacket).GetAwaiter().GetResult();
                        Logger.Info($"[Broadcast] Announcement sent: '{arg}'");
                        break;

                    case "reload":
                        Logger.Info("[System] Reloading database tables...");
                        if (Directory.Exists(dbDir)) gameDb.LoadAll(dbDir);
                        Logger.Info("[System] Database reloaded successfully!");
                        break;

                    case "clear":
                        Console.Clear();
                        break;

                    case "stop":
                    case "exit":
                    case "quit":
                        Logger.Info("[System] Saving all accounts and shutting down...");
                        accountRepo.SaveAllAsync().GetAwaiter().GetResult();
                        loginServer.Stop();
                        fieldServer.Stop();
                        episodeServer.Stop();
                        commServer.Stop();
                        running = false;
                        break;

                    default:
                        Console.WriteLine($"Unknown command: '{command}'. Type 'help' for a list of commands.");
                        break;
                }
            }

            Logger.Info("[System] Server shutdown complete. Goodbye!");
        }
    }
}
