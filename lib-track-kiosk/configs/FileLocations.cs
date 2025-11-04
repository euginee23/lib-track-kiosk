using System;
using System.IO;

namespace lib_track_kiosk.configs
{
    internal static class FileLocations
    {
        public static string TemplatesDirectory { get; } = @"E:\Library-Tracker\lib-track-kiosk\fingerprint-templates";

        public static string OnScreenKeyboardExecutable { get; } = @"E:\Library-Tracker\lib-track-kiosk\on-screen-kb\FreeVK.exe";

        public static void EnsureTemplatesDirectoryExists()
        {
            try
            {
                if (!Directory.Exists(TemplatesDirectory))
                    Directory.CreateDirectory(TemplatesDirectory);
            }
            catch
            {
                throw;
            }
        }
    }
}