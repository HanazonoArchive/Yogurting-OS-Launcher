using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Yogurting.Core.Models;

namespace Yogurting.Server.World
{
    /// <summary>
    /// Master coordinator for all campus and dungeon zones across the Yogurting game world.
    /// </summary>
    public sealed class WorldManager
    {
        private readonly ConcurrentDictionary<int, FieldInstance> _fields = new();

        public WorldManager()
        {
            // Pre-seed core academy zones
            GetOrCreateField(1, "Estiva Academy (Lobby)");
            GetOrCreateField(386, "Estiva Outdoors (Campus Gate)");
            GetOrCreateField(387, "Estiva 1F (Main Building)");
            GetOrCreateField(90, "So-il Athletic Ground");
            GetOrCreateField(91, "So-il 1F (Main Building)");
        }

        public FieldInstance GetOrCreateField(int fieldId, string name = "")
        {
            return _fields.GetOrAdd(fieldId, id => new FieldInstance(id, name));
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
