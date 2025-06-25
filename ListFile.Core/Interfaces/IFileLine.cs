namespace ListFile.Core.Interfaces;

/// <summary>
/// Represents a line from a file with its line number and content.
/// </summary>
public interface IFileLine
{
    /// <summary>
    /// Gets the line number (1-based indexing).
    /// </summary>
    int LineNumber { get; }

    /// <summary>
    /// Gets the content of the line.
    /// </summary>
    string Content { get; }
} 