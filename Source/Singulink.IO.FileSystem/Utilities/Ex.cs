namespace Singulink.IO.Utilities;

internal static class Ex
{
    public static DirectoryNotFoundException NotFound(IAbsoluteDirectoryPath dir)
    {
        return new DirectoryNotFoundException($"Could not find directory '{dir.PathDisplay}'.");
    }

    public static FileNotFoundException NotFound(IAbsoluteFilePath file)
    {
        return new FileNotFoundException($"Could not find file '{file.PathDisplay}'.", file.PathDisplay);
    }

    public static IOException FileIsDir(IAbsolutePath file)
    {
        return new IOException($"The path '{file.PathDisplay}' points to a directory, not a file.");
    }

    public static IOException DirIsFile(IAbsolutePath dir)
    {
        return new IOException($"The path '{dir.PathDisplay}' points to a file, not a directory.");
    }

    public static UnauthorizedIOAccessException Convert(UnauthorizedAccessException ex)
    {
        return new UnauthorizedIOAccessException(ex.Message, ex);
    }
}
