using System;
using System.IO;

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

        public static UnauthorizedIOAccessException Convert(UnauthorizedAccessException ex)
        {
            return new UnauthorizedIOAccessException(ex.Message, ex);
        }
    }
}