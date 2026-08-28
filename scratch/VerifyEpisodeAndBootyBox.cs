using System;
using System.IO;
using Yogurting.Core.Network;

namespace Yogurting.Server.Scratch
{
    public static class VerifyEpisodeAndBootyBox
    {
        public static void Main()
        {
            Console.WriteLine("=== Testing Episode Info (0x79B5) & Booty Box Assignment (0x79E3) ===");

            // 1. Test 0x79B5
            byte[] epPkt = YogurtingPackets.MakeGameEpisodeInfoNtf(399, 1, "分かれ道", 1, "Player", 0, 1, 1);
            Console.WriteLine($"[PASS] 0x79B5 created: {epPkt.Length} bytes.");

            // 2. Test 0x79E3
            byte[] boxPkt = YogurtingPackets.MakeGameBootyBoxAssignNtf(1, 0);
            Console.WriteLine($"[PASS] 0x79E3 created: {boxPkt.Length} bytes.");

            // 3. Test 0x5276 (Hunt Monster Death & Booty Delivery)
            byte[] huntDead = YogurtingPackets.MakeGameHuntMonDeadNtf(101, 58, 17, 1, 50, 100, 101001, 1);
            Console.WriteLine($"[PASS] 0x5276 created: {huntDead.Length} bytes.");

            Console.WriteLine("=== All Booty Box Tests Completed Successfully ===");
        }
    }
}
