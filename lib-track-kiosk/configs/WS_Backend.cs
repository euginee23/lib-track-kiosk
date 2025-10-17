using System;

namespace lib_track_kiosk.configs
{
    internal static class WS_Backend
    {
        // WEBSOCKET SERVER URL
        public static string ServerUrl { get; } = "ws://localhost:5000";

        //public static string ServerUrl { get; } = "wss://api.libtrack.codehub.site";
    }
}
