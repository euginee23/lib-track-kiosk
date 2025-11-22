using System;
using System.IO;

namespace lib_track_kiosk.configs
{
    internal static class FileLocations
    {
        // Fingerprint templates directory
        //public static string TemplatesDirectory { get; } = @"E:\Library-Tracker\lib-track-kiosk\fingerprint-templates";

        public static string TemplatesDirectory { get; } = @"C:\lib-track-kiosk\fingerprint-templates";

        // Images used by the UI (avatars, icons, etc.)
        //public static string ImagesDirectory { get; } = @"E:\Library-Tracker\lib-track-kiosk\images";

        public static string ImagesDirectory { get; } = @"C:\lib-track-kiosk\images";
        public static string DefaultAvatarPath { get; } = Path.Combine(ImagesDirectory, "avatar-default.png");

        // Sample book covers used by the welcome/demo UI
        //public static string SampleBooksDirectory { get; } = @"E:\Library-Tracker\dev files\input samples\book";

        public static string SampleBooksDirectory { get; } = @"C:\lib-track-kiosk\input samples\book";
        public static string SampleBookCover1 { get; } = Path.Combine(SampleBooksDirectory, "qwe.jpg");
        public static string SampleBookCover2 { get; } = Path.Combine(SampleBooksDirectory, "qwer.jpg");
        public static string SampleBookCover3 { get; } = Path.Combine(SampleBooksDirectory, "qwert.jpg");

        // On-screen keyboard executable
        //public static string OnScreenKeyboardExecutable { get; } = @"E:\Library-Tracker\lib-track-kiosk\on-screen-kb\FreeVK.exe";
        public static string OnScreenKeyboardExecutable { get; } = @"C:\lib-track-kiosk\on-screen-kb\FreeVK.exe";

        // Convenience method to ensure the templates directory exists (call before writing files).
        public static void EnsureTemplatesDirectoryExists()
        {
            try
            {
                if (!Directory.Exists(TemplatesDirectory))
                    Directory.CreateDirectory(TemplatesDirectory);
            }
            catch
            {
                // Let callers handle errors — keep method simple and safe to call.
                throw;
            }
        }
    }
}