using ListFile.Core.Interfaces;

namespace ListFile.Core.Implementations;

/// <summary>
/// Represents a line from a file with its line number and content.
/// </summary>
public class FileLine : IFileLine
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileLine"/> class.
    /// </summary>
    /// <param name="lineNumber">The line number (1-based indexing).</param>
    /// <param name="content">The content of the line.</param>
    /// <exception cref="ArgumentException">Thrown when lineNumber is less than 1.</exception>
    /// <exception cref="ArgumentNullException">Thrown when content is null.</exception>
    public FileLine(int lineNumber, string content)
    {
        if (lineNumber < 1)
        {
            throw new ArgumentException("Line number must be greater than 0.", nameof(lineNumber));
        }

        LineNumber = lineNumber;
        Content = content ?? throw new ArgumentNullException(nameof(content));
    }

    /// <summary>
    /// Gets the line number (1-based indexing).
    /// </summary>
    public int LineNumber { get; }

    /// <summary>
    /// Gets the content of the line.
    /// </summary>
    public string Content { get; }

    /// <summary>
    /// Returns a string representation of the file line.
    /// </summary>
    /// <returns>A string representation of the file line in the format "LineNumber: Content".</returns>
    public override string ToString()
    {
        return $"{LineNumber}: {Content}";
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current file line.
    /// </summary>
    /// <param name="obj">The object to compare with the current file line.</param>
    /// <returns>true if the specified object is equal to the current file line; otherwise, false.</returns>
    public override bool Equals(object? obj)
    {
        return obj is FileLine fileLine && 
               LineNumber == fileLine.LineNumber && 
               Content == fileLine.Content;
    }

    /// <summary>
    /// Serves as the default hash function.
    /// </summary>
    /// <returns>A hash code for the current file line.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(LineNumber, Content);
    }
} 