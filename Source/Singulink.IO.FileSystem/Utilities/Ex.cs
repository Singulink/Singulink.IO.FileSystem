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

    public static IOException FileIsDir(IAbsoluteFilePath file)
    {
        return new IOException($"The dir '{file.PathDisplay}' points to a directory.");
    }

    public static IOException DirIsFile(IAbsoluteDirectoryPath dir)
    {
        return new IOException($"The dir '{dir.PathDisplay}' points to a file.");
    }

    public static UnauthorizedIOAccessException Convert(UnauthorizedAccessException ex)
    {
        return new UnauthorizedIOAccessException(ex.Message, ex);
    }
}
