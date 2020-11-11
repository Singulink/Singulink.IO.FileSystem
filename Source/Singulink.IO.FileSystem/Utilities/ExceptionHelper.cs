using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Singulink.IO.Utilities
{
    internal static class ExceptionHelper
    {
        public static DirectoryNotFoundException GetNotFoundException(IAbsoluteDirectoryPath path)
        {
            return new DirectoryNotFoundException($"Could not find a part of the path '{path.PathDisplay}'.");
        }

        public static FileNotFoundException GetNotFoundException(IAbsoluteFilePath path)
        {
            return new FileNotFoundException($"Could not find file '{path.PathDisplay}'.", path.PathDisplay);
        }
    }
}
