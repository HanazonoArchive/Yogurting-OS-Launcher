using System;

namespace Yogurting.Core.Network
{
    /// <summary>
    /// Attribute used to decorate methods that handle specific Yogurting network packet opcodes.
    /// Enables automatic discovery and registration in <see cref="PacketDispatcher"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public sealed class PacketHandlerAttribute : Attribute
    {
        public PacketOpcode Opcode { get; }

        public PacketHandlerAttribute(PacketOpcode opcode)
        {
            Opcode = opcode;
        }

        public PacketHandlerAttribute(ushort opcode)
        {
            Opcode = (PacketOpcode)opcode;
        }
    }
}
