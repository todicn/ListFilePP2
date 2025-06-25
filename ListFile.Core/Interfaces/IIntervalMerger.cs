namespace ListFile.Core.Interfaces;

/// <summary>
/// Defines the contract for reading lines from files.
/// </summary>
public interface IFileReader
{
    /// <summary>
    /// Reads the last N lines from a file asynchronously.
    /// </summary>
    /// <param name="filePath">The path to the file to read.</param>
    /// <param name="lineCount">The number of lines to read from the end of the file. Default is 10.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the last N lines from the file.</returns>
    /// <exception cref="ArgumentNullException">Thrown when filePath is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    /// <exception cref="ArgumentException">Thrown when lineCount is less than 1.</exception>
    Task<IEnumerable<IFileLine>> ReadLastLinesAsync(string filePath, int lineCount = 10);

    /// <summary>
    /// Reads the last N lines from a file synchronously.
    /// </summary>
    /// <param name="filePath">The path to the file to read.</param>
    /// <param name="lineCount">The number of lines to read from the end of the file. Default is 10.</param>
    /// <returns>The last N lines from the file.</returns>
    /// <exception cref="ArgumentNullException">Thrown when filePath is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    /// <exception cref="ArgumentException">Thrown when lineCount is less than 1.</exception>
    IEnumerable<IFileLine> ReadLastLines(string filePath, int lineCount = 10);
} 