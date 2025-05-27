using System;
using System.IO;

namespace Singulink.IO.Utilities;

internal static class Ex
{
    public static DirectoryNotFoundException NotFound(IAbsoluteDirectoryPath path)
    {
        return new DirectoryNotFoundException($"Could not find a part of the path '{path.PathDisplay}'.");
    }

    public static FileNotFoundException NotFound(IAbsoluteFilePath path)
    {
        return new FileNotFoundException($"Could not find file '{path.PathDisplay}'.", path.PathDisplay);
    }

    public static IOException FileIsDir(IAbsoluteFilePath path)
    {
        return new IOException($"The path '{path.PathDisplay}' points to a directory.");
    }

    public static IOException DirIsFile(IAbsoluteDirectoryPath path)
    {
        return new IOException($"The path '{path.PathDisplay}' points to a file.");
    }

    public static UnauthorizedIOAccessException Convert(UnauthorizedAccessException ex)
    {
        return new UnauthorizedIOAccessException(ex.Message, ex);
    }
}