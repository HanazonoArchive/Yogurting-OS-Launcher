using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading.Tasks;
using Yogurting.Core.Logging;

namespace Yogurting.Core.Network
{
    /// <summary>
    /// Delegate representing an asynchronous packet handler function.
    /// </summary>
    public delegate Task PacketHandlerDelegate<TContext>(TContext context, byte[] packetData);

    /// <summary>
    /// High-performance, reflection-driven packet dispatching engine.
    /// Maps <see cref="PacketOpcode"/> to strongly-typed async handler delegates.
    /// </summary>
    public sealed class PacketDispatcher<TContext>
    {
        private readonly ConcurrentDictionary<ushort, PacketHandlerDelegate<TContext>> _handlers = new();

        /// <summary>
        /// Registers a single handler for an opcode.
        /// </summary>
        public void Register(PacketOpcode opcode, PacketHandlerDelegate<TContext> handler)
        {
            _handlers[(ushort)opcode] = handler;
        }

        /// <summary>
        /// Automatically registers all methods decorated with <see cref="PacketHandlerAttribute"/> on a target instance.
        /// Supports (TContext, byte[]), (TContext, PacketReader), and parameterless (TContext) signatures.
        /// </summary>
        public void RegisterHandlers(object handlerInstance)
        {
            var type = handlerInstance.GetType();
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var method in methods)
            {
                var attributes = method.GetCustomAttributes<PacketHandlerAttribute>();
                foreach (var attr in attributes)
                {
                    try
                    {
                        var parameters = method.GetParameters();

                        if (parameters.Length == 2 && parameters[0].ParameterType == typeof(TContext))
                        {
                            if (parameters[1].ParameterType == typeof(byte[]))
                            {
                                if (method.ReturnType == typeof(Task))
                                {
                                    var handlerDelegate = (PacketHandlerDelegate<TContext>)Delegate.CreateDelegate(
                                        typeof(PacketHandlerDelegate<TContext>), handlerInstance, method);
                                    _handlers[(ushort)attr.Opcode] = handlerDelegate;
                                }
                                else if (method.ReturnType == typeof(void))
                                {
                                    var action = (Action<TContext, byte[]>)Delegate.CreateDelegate(
                                        typeof(Action<TContext, byte[]>), handlerInstance, method);
                                    _handlers[(ushort)attr.Opcode] = (context, data) =>
                                    {
                                        action(context, data);
                                        return Task.CompletedTask;
                                    };
                                }
                            }
                            else if (parameters[1].ParameterType == typeof(PacketReader))
                            {
                                if (method.ReturnType == typeof(Task))
                                {
                                    var readerMethod = (Func<TContext, PacketReader, Task>)Delegate.CreateDelegate(
                                        typeof(Func<TContext, PacketReader, Task>), handlerInstance, method);

                                    _handlers[(ushort)attr.Opcode] = (context, packetData) =>
                                    {
                                        var reader = (packetData != null && packetData.Length > 6)
                                            ? new PacketReader(packetData, 6, packetData.Length - 6)
                                            : new PacketReader(Array.Empty<byte>());
                                        return readerMethod(context, reader);
                                    };
                                }
                                else if (method.ReturnType == typeof(void))
                                {
                                    var action = (Action<TContext, PacketReader>)Delegate.CreateDelegate(
                                        typeof(Action<TContext, PacketReader>), handlerInstance, method);

                                    _handlers[(ushort)attr.Opcode] = (context, packetData) =>
                                    {
                                        var reader = (packetData != null && packetData.Length > 6)
                                            ? new PacketReader(packetData, 6, packetData.Length - 6)
                                            : new PacketReader(Array.Empty<byte>());
                                        action(context, reader);
                                        return Task.CompletedTask;
                                    };
                                }
                            }
                        }
                        else if (parameters.Length == 1 && parameters[0].ParameterType == typeof(TContext))
                        {
                            if (method.ReturnType == typeof(Task))
                            {
                                var singleParamMethod = (Func<TContext, Task>)Delegate.CreateDelegate(
                                    typeof(Func<TContext, Task>), handlerInstance, method);
                                _handlers[(ushort)attr.Opcode] = (context, _) => singleParamMethod(context);
                            }
                            else if (method.ReturnType == typeof(void))
                            {
                                var action = (Action<TContext>)Delegate.CreateDelegate(
                                    typeof(Action<TContext>), handlerInstance, method);
                                _handlers[(ushort)attr.Opcode] = (context, _) =>
                                {
                                    action(context);
                                    return Task.CompletedTask;
                                };
                            }
                        }
                        else
                        {
                            Logger.Error($"[PacketDispatcher] Incompatible handler signature on {type.Name}.{method.Name}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"[PacketDispatcher] Failed to bind handler for {attr.Opcode} on {type.Name}.{method.Name}: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Dispatches an incoming raw packet payload to its registered handler.
        /// </summary>
        public async Task<bool> DispatchAsync(TContext context, ushort opcode, byte[] packetData)
        {
            if (_handlers.TryGetValue(opcode, out var handler))
            {
                try
                {
                    await handler(context, packetData);
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Error($"[PacketDispatcher] Error executing handler for Opcode 0x{opcode:04X} ({opcode}): {ex}");
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true if a handler is registered for the specified opcode.
        /// </summary>
        public bool HasHandler(ushort opcode) => _handlers.ContainsKey(opcode);
    }
}
