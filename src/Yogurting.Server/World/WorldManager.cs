using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Yogurting.Core.Models;
using Yogurting.Data.Loaders;

namespace Yogurting.Server.World
{
    /// <summary>
    /// Master coordinator for all campus and dungeon zones across the Yogurting game world.
    /// Fully data-driven by GameDatabase (Field.txt).
    /// </summary>
    public sealed class WorldManager
    {
        private readonly ConcurrentDictionary<int, FieldInstance> _fields = new();
        private readonly GameDatabase? _gameDb;

        public WorldManager(GameDatabase? gameDb = null)
        {
            _gameDb = gameDb;
        }

        public FieldInstance GetOrCreateField(int fieldId, string name = "")
        {
            return _fields.GetOrAdd(fieldId, id =>
            {
                string fieldName = name;
                if (string.IsNullOrEmpty(fieldName) && _gameDb != null && _gameDb.Fields.TryGetValue(id, out var fieldDef))
                {
                    fieldName = fieldDef.Name;
                }
                return new FieldInstance(id, fieldName);
            });
        }

        public void MovePlayer(PlayerSessionState state, int newFieldId)
        {
            // Remove from old field
            if (_fields.TryGetValue(state.Player.FieldId, out var oldField))
            {
                oldField.RemovePlayer(state.Player.CharaId);
            }

            // Add to new field
            state.Player.FieldId = newFieldId;
            var newField = GetOrCreateField(newFieldId);
            newField.AddPlayer(state);
        }

        public void RemovePlayer(PlayerSessionState state)
        {
            if (_fields.TryGetValue(state.Player.FieldId, out var field))
            {
                field.RemovePlayer(state.Player.CharaId);
            }
        }

        public async Task BroadcastGlobalAsync(byte[] packetData)
        {
            foreach (var field in _fields.Values)
            {
                await field.BroadcastAsync(packetData);
            }
        }

        public int TotalOnlinePlayers => _fields.Values.Sum(f => f.Players.Count);

        public IReadOnlyCollection<FieldInstance> AllFields => _fields.Values.ToList();
    }
}
