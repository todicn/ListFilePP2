using System.Text;
using ListFile.Core.Configuration;
using ListFile.Core.Interfaces;

namespace ListFile.Core.Implementations;

/// <summary>
/// Strategy for reading small files by loading all lines into memory.
/// This strategy is efficient for small files but uses more memory.
/// </summary>
public class SmallFileReadingStrategy : IFileReadingStrategy
{
    /// <summary>
    /// Determines if this strategy can handle the given file.
    /// Small files are those at or below the configured threshold.
    /// </summary>
    /// <param name="fileInfo">Information about the file to read.</param>
    /// <param name="options">Configuration options for file reading.</param>
    /// <returns>True if the file is small enough for this strategy.</returns>
    public bool CanHandle(FileInfo fileInfo, FileReaderOptions options)
    {
        return fileInfo.Length <= options.SmallFileThresholdBytes;
    }

    /// <summary>
    /// Reads the last N lines from a small file by reading all lines into memory.
    /// This approach is simple and fast for small files.
    /// </summary>
    /// <param name="filePath">The path to the file to read.</param>
    /// <param name="lineCount">The number of lines to read from the end of the file.</param>
    /// <param name="options">Configuration options for file reading.</param>
    /// <returns>The last N lines from the file.</returns>
    public IEnumerable<IFileLine> ReadLastLines(string filePath, int lineCount, FileReaderOptions options)
    {
        var encoding = GetEncoding(options.DefaultEncoding);
        var allLines = File.ReadAllLines(filePath, encoding);
        var startLine = Math.Max(0, allLines.Length - lineCount);
        var result = new List<IFileLine>();

        for (int i = startLine; i < allLines.Length; i++)
        {
            result.Add(new FileLine(i + 1, allLines[i]));
        }

        return result;
    }

    /// <summary>
    /// Gets the encoding from the configuration string.
    /// </summary>
    /// <param name="encodingName">The name of the encoding.</param>
    /// <returns>The encoding instance.</returns>
    private static Encoding GetEncoding(string encodingName)
    {
        return encodingName.ToUpperInvariant() switch
        {
            "UTF-8" => Encoding.UTF8,
            "UTF-16" => Encoding.Unicode,
            "UTF-32" => Encoding.UTF32,
            "ASCII" => Encoding.ASCII,
            _ => Encoding.UTF8
        };
    }
} 