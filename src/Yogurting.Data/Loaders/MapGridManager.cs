using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Yogurting.Core.Logging;

namespace Yogurting.Data.Loaders
{
    public sealed class MapGrid
    {
        public string Code { get; set; } = string.Empty;
        public int Width { get; set; }
        public int Height { get; set; }
        public byte[] Tiles { get; set; } = Array.Empty<byte>();

        public bool IsWalkable(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return false;
            int idx = y * Width + x;
            if (idx < 0 || idx >= Tiles.Length) return false;
            return Tiles[idx] >= 1; // Delphi TMapData.CanMove (_Unit49.pas:26735): >= 1 = Walkable Floor, 0 = Solid Wall / Obstacle
        }
    }

    public sealed class MapGridManager
    {
        private static readonly ConcurrentDictionary<string, MapGrid> _mapsByCode = new(StringComparer.OrdinalIgnoreCase);
        private static readonly ConcurrentDictionary<int, MapGrid> _mapsByFieldId = new();

        public static IReadOnlyDictionary<string, MapGrid> Maps => _mapsByCode;

        public static void Initialize(string mapDbPath, IReadOnlyDictionary<int, GameFieldDef> fields)
        {
            _mapsByCode.Clear();
            _mapsByFieldId.Clear();

            if (!File.Exists(mapDbPath))
            {
                Logger.Warn($"[MapGridManager] map.db not found at '{mapDbPath}'. Wall collision will be disabled.");
                return;
            }

            try
            {
                using var fs = File.OpenRead(mapDbPath);
                using var reader = new BinaryReader(fs);

                byte[] magic = reader.ReadBytes(4);
                uint uncompressedLen = reader.ReadUInt32();

                // Decompress remainder with ZLibStream
                using var zlib = new ZLibStream(fs, CompressionMode.Decompress);
                using var ms = new MemoryStream();
                zlib.CopyTo(ms);
                byte[] decompressed = ms.ToArray();

                using var dReader = new BinaryReader(new MemoryStream(decompressed));
                if (dReader.BaseStream.Length < 4) return;

                int mapCount = dReader.ReadInt32();
                for (int i = 0; i < mapCount && dReader.BaseStream.Position < dReader.BaseStream.Length; i++)
                {
                    int nameLen = dReader.ReadInt32();
                    if (nameLen <= 0 || nameLen > 256) break;

                    byte[] nameBytes = dReader.ReadBytes(nameLen);
                    string code = Encoding.ASCII.GetString(nameBytes).TrimEnd('\0');

                    int width = dReader.ReadInt32();
                    int height = dReader.ReadInt32();
                    int gridSize = width * height;

                    if (gridSize < 0 || dReader.BaseStream.Position + gridSize > dReader.BaseStream.Length)
                    {
                        break;
                    }

                    byte[] tiles = dReader.ReadBytes(gridSize);

                    var grid = new MapGrid
                    {
                        Code = code,
                        Width = width,
                        Height = height,
                        Tiles = tiles
                    };

                    _mapsByCode[code] = grid;
                }

                Logger.Info($"[MapGridManager] Successfully loaded {_mapsByCode.Count} map collision grids from map.db.");

                // Link FieldId -> MapGrid via Field.txt codes
                foreach (var kvp in fields)
                {
                    int fieldId = kvp.Key;
                    string code = kvp.Value.Code;
                    if (!string.IsNullOrEmpty(code) && _mapsByCode.TryGetValue(code, out var grid))
                    {
                        _mapsByFieldId[fieldId] = grid;
                    }
                }

                Logger.Info($"[MapGridManager] Linked {_mapsByFieldId.Count} fields to active collision grids.");
            }
            catch (Exception ex)
            {
                Logger.Error($"[MapGridManager] Error loading map.db: {ex.Message}");
            }
        }

        public static bool IsWalkable(int fieldId, float x, float y)
        {
            if (_mapsByFieldId.TryGetValue(fieldId, out var grid))
            {
                return grid.IsWalkable((int)MathF.Round(x), (int)MathF.Round(y));
            }
            // If no map grid available, allow movement within default boundaries
            return x >= 2.0f && x <= 98.0f && y >= 2.0f && y <= 98.0f;
        }

        public static bool IsWalkable(string mapCode, float x, float y)
        {
            if (_mapsByCode.TryGetValue(mapCode, out var grid))
            {
                return grid.IsWalkable((int)MathF.Round(x), (int)MathF.Round(y));
            }
            return true;
        }

        public static MapGrid? GetGrid(int fieldId)
        {
            _mapsByFieldId.TryGetValue(fieldId, out var grid);
            return grid;
        }
    }
}
