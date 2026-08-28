using System;
using System.Threading.Tasks;
using Yogurting.Core.Logging;
using Yogurting.Core.Models;
using Yogurting.Core.Network;

namespace Yogurting.Server.Handlers.Npc
{
    /// <summary>
    /// Legacy NPC handler stub. All interactive dialogues, cut-in portraits, shop catalogs, and salon menus 
    /// are now handled by <see cref="Yogurting.Server.Handlers.Field.NpcAndDialogueHandlers"/>.
    /// </summary>
    public sealed class NpcAndShopHandlers
    {
        public NpcAndShopHandlers(Yogurting.Data.Loaders.GameDatabase? gameDb = null)
        {
        }
    }
}
