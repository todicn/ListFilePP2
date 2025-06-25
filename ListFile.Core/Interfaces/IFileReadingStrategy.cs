using ListFile.Core.Configuration;

namespace ListFile.Core.Interfaces;

/// <summary>
/// Defines a strategy for reading the last N lines from a file.
/// </summary>
public interface IFileReadingStrategy
{
    /// <summary>
    /// Determines if this strategy can handle the given file.
    /// </summary>
    /// <param name="fileInfo">Information about the file to read.</param>
    /// <param name="options">Configuration options for file reading.</param>
    /// <returns>True if this strategy can handle the file, false otherwise.</returns>
    bool CanHandle(FileInfo fileInfo, FileReaderOptions options);

    /// <summary>
    /// Reads the last N lines from a file using this strategy.
    /// </summary>
    /// <param name="filePath">The path to the file to read.</param>
    /// <param name="lineCount">The number of lines to read from the end of the file.</param>
    /// <param name="options">Configuration options for file reading.</param>
    /// <returns>The last N lines from the file.</returns>
    IEnumerable<IFileLine> ReadLastLines(string filePath, int lineCount, FileReaderOptions options);
} 