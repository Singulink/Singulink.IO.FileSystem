using System;

namespace Singulink.IO
{
    internal static partial class Interop
    {
        internal static partial class Windows
        {
            /// <summary>
            /// Disposable guard object that safely disables the normal media insertion prompt for removable media (floppies, cds, memory cards, etc.)
            /// </summary>
            /// <remarks>
            /// <para>Note that removable media file systems lazily load. After starting the OS they won't be loaded until you have media in the drive- and as
            /// such the prompt won't happen. You have to have had media in at least once to get the file system to load and then have removed it.</para>
            /// </remarks>
            internal struct MediaInsertionPromptGuard : IDisposable
            {
                private bool _disableSuccess;
                private uint _oldMode;

                public static MediaInsertionPromptGuard Enter()
                {
                    MediaInsertionPromptGuard prompt = default;
                    prompt._disableSuccess = WindowsNative.SetThreadErrorMode(WindowsNative.SEM_FAILCRITICALERRORS, out prompt._oldMode);
                    return prompt;
                }

                public void Dispose()
                {
                    if (_disableSuccess)
                        WindowsNative.SetThreadErrorMode(_oldMode, out _);
                }
            }
        }
    }
}