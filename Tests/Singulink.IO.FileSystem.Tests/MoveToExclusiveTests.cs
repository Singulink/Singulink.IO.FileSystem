namespace Singulink.IO.FileSystem.Tests;

[PrefixTestClass]
public class MoveToExclusiveTests
{
    private const string TestDirName = "_test_movetoexclusive";

    // The HResult of the IOException that Windows reports a sharing violation with.
    private const int SharingViolation = unchecked((int)0x80070020);

    private static readonly byte[] Content = [1, 2, 3, 4, 5];
    private static readonly byte[] OtherContent = [6, 7, 8];

    private static IAbsoluteFilePath CreateFile(IAbsoluteDirectoryPath dir, string name, byte[]? content = null)
    {
        var file = dir.CombineFile(name);

        using (var stream = file.OpenStream(FileMode.CreateNew, FileAccess.Write))
            stream.Write(content ?? Content);

        return file;
    }

    private static byte[] ReadFile(IAbsoluteFilePath file)
    {
        using var stream = file.OpenStream(FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        byte[] content = new byte[stream.Length];
        stream.ReadExactly(content);

        return content;
    }

    // ---- Moving ----

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Moves(bool overwrite)
    {
        var dir = CreateEmptyTestDirectory(TestDirName);
        var source = CreateFile(dir, "source.file");
        var destination = dir.CombineDirectory("subdir").CombineFile("destination.file");
        destination.ParentDirectory.Create();

        source.MoveToExclusive(destination, overwrite);

        source.Exists.ShouldBeFalse();
        ReadFile(destination).ShouldBe(Content);
    }

    [TestMethod]
    public void DestinationExists_NoOverwrite_ThrowsIOException()
    {
        var dir = CreateEmptyTestDirectory(TestDirName);
        var source = CreateFile(dir, "source.file");
        var destination = CreateFile(dir, "destination.file", OtherContent);

        Should.Throw<IOException>(() => source.MoveToExclusive(destination)).ShouldBeOfType<IOException>();

        ReadFile(source).ShouldBe(Content);
        ReadFile(destination).ShouldBe(OtherContent);
    }

    [TestMethod]
    public void DestinationExists_Overwrite_Replaces()
    {
        var dir = CreateEmptyTestDirectory(TestDirName);
        var source = CreateFile(dir, "source.file");
        var destination = CreateFile(dir, "destination.file", OtherContent);

        source.MoveToExclusive(destination, overwrite: true);

        source.Exists.ShouldBeFalse();
        ReadFile(destination).ShouldBe(Content);
    }

    [TestMethod]
    public void MovedAlready_ThrowsFileNotFound()
    {
        var dir = CreateEmptyTestDirectory(TestDirName);
        var source = CreateFile(dir, "source.file");
        var winner = dir.CombineFile("winner.file");
        var loser = dir.CombineFile("loser.file");

        source.MoveToExclusive(winner);

        Should.Throw<FileNotFoundException>(() => source.MoveToExclusive(loser));
        Should.Throw<FileNotFoundException>(() => source.MoveToExclusive(loser, overwrite: true));

        loser.Exists.ShouldBeFalse();
        ReadFile(winner).ShouldBe(Content);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void MissingSource_ThrowsFileNotFound(bool overwrite)
    {
        var dir = CreateEmptyTestDirectory(TestDirName);
        var source = dir.CombineFile("missing.file");
        var destination = dir.CombineFile("destination.file");

        Should.Throw<FileNotFoundException>(() => source.MoveToExclusive(destination, overwrite))
            .Message.ShouldBe($"Could not find file '{source.PathDisplay}'.");

        destination.Exists.ShouldBeFalse();
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void MissingSourceParent_ThrowsDirectoryNotFound(bool overwrite)
    {
        var dir = CreateEmptyTestDirectory(TestDirName);
        var source = dir.CombineDirectory("missing_dir").CombineFile("missing.file");

        Should.Throw<DirectoryNotFoundException>(() => source.MoveToExclusive(dir.CombineFile("destination.file"), overwrite))
            .Message.ShouldBe($"Could not find directory '{source.ParentDirectory.PathDisplay}'.");
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void MissingDestinationDirectory_ThrowsDirectoryNotFound(bool overwrite)
    {
        var dir = CreateEmptyTestDirectory(TestDirName);
        var source = CreateFile(dir, "source.file");
        var destination = dir.CombineDirectory("missing_dir").CombineFile("destination.file");

        Should.Throw<DirectoryNotFoundException>(() => source.MoveToExclusive(destination, overwrite))
            .Message.ShouldBe($"Could not find directory '{destination.ParentDirectory.PathDisplay}'.");

        ReadFile(source).ShouldBe(Content);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void SourceIsDirectory_ThrowsIOException(bool overwrite)
    {
        var dir = CreateEmptyTestDirectory(TestDirName);
        var sourceDir = dir.CombineDirectory("existing_dir");
        sourceDir.Create();

        // Construct a file path that actually points at an existing directory by trimming the trailing separator.
        var source = FilePath.ParseAbsolute(sourceDir.PathExport.AsSpan().TrimEnd(['\\', '/']));
        var destination = dir.CombineFile("destination.file");

        Should.Throw<IOException>(() => source.MoveToExclusive(destination, overwrite))
            .ShouldBeOfType<IOException>()
            .Message.ShouldBe($"The path '{source.PathDisplay}' points to a directory, not a file.");

        sourceDir.Exists.ShouldBeTrue();
        dir.GetChildEntries().ShouldBe([sourceDir]);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void DestinationIsDirectory_ThrowsIOException(bool overwrite)
    {
        var dir = CreateEmptyTestDirectory(TestDirName);
        var source = CreateFile(dir, "source.file");
        var destinationDir = dir.CombineDirectory("existing_dir");
        destinationDir.Create();

        var destination = FilePath.ParseAbsolute(destinationDir.PathExport.AsSpan().TrimEnd(['\\', '/']));

        Should.Throw<IOException>(() => source.MoveToExclusive(destination, overwrite)).ShouldBeOfType<IOException>();

        ReadFile(source).ShouldBe(Content);
        destinationDir.IsEmpty.ShouldBeTrue();
    }

    [TestMethod]
    public void SymbolicLink_MovesLinkAndNotTarget()
    {
        var dir = CreateEmptyTestDirectory(TestDirName);
        var target = CreateFile(dir, "target.file");
        var link = dir.CombineFile("link.file");
        var movedLink = dir.CombineFile("moved_link.file");

        try
        {
            File.CreateSymbolicLink(link.PathExport, target.PathExport);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException && OperatingSystem.IsWindows())
        {
            // Creating a symbolic link on Windows needs a privilege or developer mode.
            return;
        }

        link.MoveToExclusive(movedLink);

        link.Exists.ShouldBeFalse();
        movedLink.Attributes.HasFlag(FileAttributes.ReparsePoint).ShouldBeTrue();
        ReadFile(movedLink).ShouldBe(Content);
        ReadFile(target).ShouldBe(Content);
    }

    [TestMethod]
    public void NonAsciiAndLongPaths_Move()
    {
        var testDir = CreateEmptyTestDirectory(TestDirName);
        var dir = testDir;

        // Longer than the Windows MAX_PATH (260 characters), than the buffers that the paths are converted to UTF-8 in on Unix (256 bytes) and than the
        // buffer that the new path is passed in on Windows (1024 bytes).
        for (int i = 0; i < 8; i++)
            dir = dir.CombineDirectory($"{i}_{new string('d', 50)}_\u00E9");

        try
        {
            dir.Create();
            var source = CreateFile(dir, "donn\u00E9es \u65E5\u672C\u8A9E.file");
            var destination = dir.CombineFile($"\u0444\u0430\u0439\u043B_{new string('f', 100)}.file");

            destination.PathExport.Length.ShouldBeGreaterThan(512);

            source.MoveToExclusive(destination);
            source.Exists.ShouldBeFalse();
            ReadFile(destination).ShouldBe(Content);

            destination.MoveToExclusive(source, overwrite: true);
            destination.Exists.ShouldBeFalse();
            ReadFile(source).ShouldBe(Content);
        }
        finally
        {
            // Don't leave paths behind that tools without long path support fail on.
            testDir.Delete(recursive: true);
        }
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void OtherVolume_ThrowsIOExceptionAndDoesNotCopy(bool overwrite)
    {
        // Only Linux has another file system at a known place that any user can write to.
        if (!OperatingSystem.IsLinux() || !Directory.Exists("/dev/shm"))
            return;

        var dir = CreateEmptyTestDirectory(TestDirName);
        var source = CreateFile(dir, "source.file");
        var destination = DirectoryPath.ParseAbsolute("/dev/shm/").CombineFile($"_test_movetoexclusive_{Guid.NewGuid():N}.file");

        if (dir.PathDisplay.StartsWith("/dev/shm/", StringComparison.Ordinal))
            return;

        try
        {
            Should.Throw<IOException>(() => source.MoveToExclusive(destination, overwrite)).ShouldBeOfType<IOException>();

            ReadFile(source).ShouldBe(Content);
            destination.Exists.ShouldBeFalse();
        }
        finally
        {
            destination.Delete();
        }
    }

    // ---- Other handles ----

    [TestMethod]
    public void SourceOpenSharingDeletion_MovesAndHandlesStayValid()
    {
        var dir = CreateEmptyTestDirectory(TestDirName);
        var source = CreateFile(dir, "source.file");
        var destination = dir.CombineFile("destination.file");

        const FileShare Share = FileShare.ReadWrite | FileShare.Delete;

        using var reader = source.OpenStream(FileMode.Open, FileAccess.Read, Share);
        using var otherReader = source.OpenStream(FileMode.Open, FileAccess.Read, Share);
        using var writer = source.OpenStream(FileMode.Append, FileAccess.Write, Share);

        source.MoveToExclusive(destination);

        source.Exists.ShouldBeFalse();

        writer.Write(OtherContent);
        writer.Flush();

        byte[] read = new byte[Content.Length + OtherContent.Length];
        reader.ReadExactly(read);
        read.ShouldBe([.. Content, .. OtherContent]);

        ReadFile(destination).ShouldBe([.. Content, .. OtherContent]);
    }

    [TestMethod]
    [DataRow(FileShare.Read)]
    [DataRow(FileShare.ReadWrite)]
    [DataRow(FileShare.None)]
    public void SourceOpenNotSharingDeletion_ThrowsSharingViolationOnWindows(FileShare share)
    {
        var dir = CreateEmptyTestDirectory(TestDirName);
        var source = CreateFile(dir, "source.file");
        var destination = dir.CombineFile("destination.file");

        using (var stream = source.OpenStream(FileMode.Open, FileAccess.Read, share))
        {
            // Open handles never keep a file from being renamed on Unix.
            if (!OperatingSystem.IsWindows())
            {
                source.MoveToExclusive(destination);
                source.Exists.ShouldBeFalse();
                destination.Exists.ShouldBeTrue();
                return;
            }

            Should.Throw<IOException>(() => source.MoveToExclusive(destination))
                .ShouldBeOfType<IOException>().HResult.ShouldBe(SharingViolation);

            Should.Throw<IOException>(() => source.MoveToExclusive(destination, overwrite: true))
                .ShouldBeOfType<IOException>().HResult.ShouldBe(SharingViolation);

            // The file was not moved, which is what tells this apart from having lost to another party.
            source.Exists.ShouldBeTrue();
            destination.Exists.ShouldBeFalse();
        }

        source.MoveToExclusive(destination);
        ReadFile(destination).ShouldBe(Content);
    }

    [TestMethod]
    [DataRow(FileShare.ReadWrite | FileShare.Delete)]
    [DataRow(FileShare.ReadWrite)]
    public void DestinationOpen_Overwrite_ThrowsOnWindows(FileShare share)
    {
        var dir = CreateEmptyTestDirectory(TestDirName);
        var source = CreateFile(dir, "source.file");
        var destination = CreateFile(dir, "destination.file", OtherContent);

        using (var stream = destination.OpenStream(FileMode.Open, FileAccess.Read, share))
        {
            // Windows does not replace a file that is open, as with MoveTo. On Unix the open handle keeps the replaced file.
            if (OperatingSystem.IsWindows())
            {
                Should.Throw<UnauthorizedIOAccessException>(() => source.MoveToExclusive(destination, overwrite: true));

                ReadFile(source).ShouldBe(Content);
                ReadFile(destination).ShouldBe(OtherContent);
                return;
            }

            source.MoveToExclusive(destination, overwrite: true);

            byte[] read = new byte[OtherContent.Length];
            stream.ReadExactly(read);
            read.ShouldBe(OtherContent);
        }

        source.Exists.ShouldBeFalse();
        ReadFile(destination).ShouldBe(Content);
    }

    // ---- Concurrent moves ----

    [TestMethod]
    [DataRow(2, false)]
    [DataRow(2, true)]
    [DataRow(4, false)]
    [DataRow(4, true)]
    public void ConcurrentMovesOfSameFile_AtMostOneSucceeds(int parties, bool overwrite)
    {
        const int Iterations = 200;

        var dir = CreateEmptyTestDirectory(TestDirName);
        var source = dir.CombineFile("source.file");
        var destinations = Enumerable.Range(0, parties).Select(i => dir.CombineFile($"destination{i}.file")).ToArray();

        int totalWins = 0;
        string? failure = null;

        RunRace(parties, Iterations,
            prepare: () => {
                source.OpenStream(FileMode.CreateNew, FileAccess.Write).Dispose();

                // Something to replace, which a party that loses must leave alone.
                if (overwrite)
                {
                    foreach (var destination in destinations)
                        CreateFile(destination.ParentDirectory, destination.Name);
                }
            },
            move: party => source.MoveToExclusive(destinations[party], overwrite),
            verify: results => {
                int wins = results.Count(r => r is null);
                totalWins += wins;

                failure ??= GetLoserFailure(results);

                if (wins > 1)
                    failure ??= $"{wins} parties moved the file.";

                // The file is under the winner's name and nowhere else, or still under its own name if no party won.
                if (source.Exists != (wins is 0))
                    failure ??= $"{wins} parties moved the file and the source {(source.Exists ? "still exists" : "is gone")}.";

                for (int i = 0; i < parties; i++)
                {
                    bool won = results[i] is null;
                    long? length = destinations[i].Exists ? destinations[i].Length : null;
                    long? expectedLength = won ? 0 : overwrite ? Content.Length : null;

                    if (length != expectedLength)
                        failure ??= $"The destination of a party that {(won ? "won" : "lost")} has length {length?.ToString() ?? "(none)"}.";

                    destinations[i].Delete();
                }

                source.Delete();
            });

        failure.ShouldBeNull();
        totalWins.ShouldBeGreaterThan(0);

        // Nothing but the parties has the file open here, so on Unix, where only a party that moved the file can make another one fail, one always wins.
        if (!OperatingSystem.IsWindows())
            totalWins.ShouldBe(Iterations);
    }

    [TestMethod]
    [DataRow(2)]
    [DataRow(4)]
    public void ConcurrentMovesToSameDestination_NoOverwrite_AtMostOneSucceeds(int parties)
    {
        const int Iterations = 100;

        var dir = CreateEmptyTestDirectory(TestDirName);

        // An existing destination is refused as part of the rename on Windows, and on Linux on the file systems that support it. Elsewhere it is checked
        // before the rename, which two parties can both get past.
        if (!OperatingSystem.IsWindows() && !(OperatingSystem.IsLinux() && dir.FileSystem is not ("9p" or "v9fs" or "nfs" or "nfs4" or "fuse" or "fuseblk")))
            return;

        var sources = Enumerable.Range(0, parties).Select(i => dir.CombineFile($"source{i}.file")).ToArray();
        var destination = dir.CombineFile("destination.file");

        int totalWins = 0;
        string? failure = null;

        RunRace(parties, Iterations,
            prepare: () => {
                for (int i = 0; i < parties; i++)
                    CreateFile(dir, sources[i].Name, [(byte)i]);
            },
            move: party => sources[party].MoveToExclusive(destination),
            verify: results => {
                int wins = results.Count(r => r is null);
                totalWins += wins;

                if (wins is not 1)
                    failure ??= $"{wins} parties moved their file to the destination.";

                // The files of the parties that lost are untouched, and the destination is the winner's file.
                for (int i = 0; i < parties; i++)
                {
                    if (results[i] is not null and not IOException)
                        failure ??= $"A party that lost got {results[i]}";

                    if (sources[i].Exists == results[i] is null)
                        failure ??= $"The source of a party that {(results[i] is null ? "won" : "lost")} {(sources[i].Exists ? "still exists" : "is gone")}.";

                    sources[i].Delete();
                }

                if (wins is 1 && ReadFile(destination)[0] != Array.FindIndex(results, r => r is null))
                    failure ??= "The destination is not the file of the party that won.";

                destination.Delete();
            });

        failure.ShouldBeNull();
        totalWins.ShouldBe(Iterations);
    }

    /// <summary>
    /// Has each party call <paramref name="move"/> at the same moment, once per iteration, and gives the exception that each one got (or <see
    /// langword="null"/>) to <paramref name="verify"/>.
    /// </summary>
    private static void RunRace(int parties, int iterations, Action prepare, Action<int> move, Action<Exception?[]> verify)
    {
        var results = new Exception?[parties];
        int iteration = 0;
        int ready = 0;
        int go = 0;
        bool stop = false;

        using var start = new Barrier(parties + 1);
        using var done = new Barrier(parties + 1);

        var threads = Enumerable.Range(0, parties).Select(party => new Thread(() => {
            while (true)
            {
                start.SignalAndWait();

                if (Volatile.Read(ref stop))
                    return;

                // The barrier releases the parties one after the other, so each one says when it runs and then spins until all of them do. The spinning
                // yields but never sleeps, so that the parties still start together where there are fewer processors than parties.
                int current = Volatile.Read(ref iteration);
                Interlocked.Increment(ref ready);
                var spinner = default(SpinWait);

                while (Volatile.Read(ref go) != current)
                    spinner.SpinOnce(sleep1Threshold: -1);

                try
                {
                    move(party);
                    results[party] = null;
                }
                catch (Exception ex)
                {
                    results[party] = ex;
                }

                done.SignalAndWait();
            }
        })).ToArray();

        foreach (var thread in threads)
            thread.Start();

        try
        {
            for (int i = 1; i <= iterations; i++)
            {
                prepare();

                Volatile.Write(ref iteration, i);
                Volatile.Write(ref ready, 0);
                start.SignalAndWait();

                var spinner = default(SpinWait);

                while (Volatile.Read(ref ready) != parties)
                    spinner.SpinOnce(sleep1Threshold: -1);

                Volatile.Write(ref go, i);
                done.SignalAndWait();

                verify(results);
            }
        }
        finally
        {
            Volatile.Write(ref stop, true);
            start.SignalAndWait();

            foreach (var thread in threads)
                thread.Join();
        }
    }

    /// <summary>
    /// Checks that each party that lost was told so in one of the documented ways: the file is not found, or on Windows a sharing violation.
    /// </summary>
    private static string? GetLoserFailure(Exception?[] results)
    {
        foreach (var exception in results)
        {
            if (exception is null or FileNotFoundException)
                continue;

            if (OperatingSystem.IsWindows() && exception.GetType() == typeof(IOException) && exception.HResult is SharingViolation)
                continue;

            return $"A party that lost got {exception}";
        }

        return null;
    }

    // ---- Path format ----

    [TestMethod]
    public void OtherPlatformFormat_Throws()
    {
        var format = OperatingSystem.IsWindows() ? PathFormat.Unix : PathFormat.Windows;
        string otherDir = OperatingSystem.IsWindows() ? "/data/" : @"C:\data\";

        var dir = CreateEmptyTestDirectory(TestDirName);
        var file = CreateFile(dir, "source.file");
        var otherFile = FilePath.ParseAbsolute(otherDir + "test.file", format);

        Should.Throw<InvalidOperationException>(() => otherFile.MoveToExclusive(file));
        Should.Throw<ArgumentException>(() => file.MoveToExclusive(otherFile)).ParamName.ShouldBe("destinationFile");

        ReadFile(file).ShouldBe(Content);
    }
}
