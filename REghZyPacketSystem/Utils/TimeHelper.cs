using System;
using System.Diagnostics;

namespace REghZyPacketSystem.Utils {
    public static class TimeHelper {
        public static long SystemMillis() {
            // return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return Stopwatch.GetTimestamp() / TimeSpan.TicksPerMillisecond;
        }
    }
}
