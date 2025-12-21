<div class="article">

# Exception Handling

## Improvements

There are a few important improvements to exception handling that `Singulink.IO.FileSystem` provides over the `System.IO` APIs:

### Cross-Platform Consistency

The types of exceptions thrown on Unix and Windows are not consistent in `System.IO` in many instances, which this library attempts to remedy. If a `FileNotFoundException` is thrown on Windows then you can expect the same to happen on Unix as well.

### UnauthorizedIOAccessException

This library eliminates any instances of `System.UnauthorizedAccessException` being thrown, instead replacing it with a new `UnauthorizedIOAccessException` that inherits from `System.IOException`, greatly improving the way exceptions can be handled by your code.

### Separation of Concerns

With `System.IO`, exception handling is clunky because I/O operations could throw any number of exceptions with no common base type other than the overly general `Exception` type: `ArgumentException`, `IOException` (and subtypes), `UnauthorizedAccessException`, etc. You either have to put `Exception` handling blocks everywhere but that could hide issues in your code, or you have to add multiple exception handling blocks around everything which is tedious.

With this library, parsing is separated from I/O, so you wrap path parsing operations with `ArgumentException` handling blocks, and you wrap I/O operations with `IOException` handling blocks since all the exceptions that can be thrown inherit from that type. Simple, tidy, concise, and easy to follow exception handling best practices.

</div>