using ListFile.Core.Interfaces;

namespace ListFile.Core.Interfaces;

/// <summary>
/// Defines functionality for monitoring file changes and reading content when files are modified.
/// </summary>
public interface IFileMonitor : IDisposable
{
    /// <summary>
    /// Event that is raised when a monitored file changes and new lines are read.
    /// </summary>
    event EventHandler<FileChangedEventArgs> FileChanged;

    /// <summary>
    /// Starts monitoring the specified file for changes.
    /// </summary>
    /// <param name="filePath">The path to the file to monitor.</param>
    /// <param name="lineCount">The number of lines to read when the file changes. Default is 10.</param>
    /// <exception cref="ArgumentNullException">Thrown when filePath is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    void StartMonitoring(string filePath, int lineCount = 10);

    /// <summary>
    /// Stops monitoring the currently monitored file.
    /// </summary>
    void StopMonitoring();

    /// <summary>
    /// Gets a value indicating whether the monitor is currently active.
    /// </summary>
    bool IsMonitoring { get; }

    /// <summary>
    /// Gets the path of the currently monitored file, or null if no file is being monitored.
    /// </summary>
    string? MonitoredFilePath { get; }
}

/// <summary>
/// Event arguments for file change notifications.
/// </summary>
public class FileChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileChangedEventArgs"/> class.
    /// </summary>
    /// <param name="filePath">The path of the file that changed.</param>
    /// <param name="lines">The lines read from the file after the change.</param>
    /// <param name="changeType">The type of change that occurred.</param>
    public FileChangedEventArgs(string filePath, IEnumerable<IFileLine> lines, FileChangeType changeType)
    {
        FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        Lines = lines ?? throw new ArgumentNullException(nameof(lines));
        ChangeType = changeType;
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the path of the file that changed.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Gets the lines read from the file after the change.
    /// </summary>
    public IEnumerable<IFileLine> Lines { get; }

    /// <summary>
    /// Gets the type of change that occurred.
    /// </summary>
    public FileChangeType ChangeType { get; }

    /// <summary>
    /// Gets the timestamp when the change was detected.
    /// </summary>
    public DateTime Timestamp { get; }
}

/// <summary>
/// Represents the type of file change that occurred.
/// </summary>
public enum FileChangeType
{
    /// <summary>
    /// The file was modified (content changed).
    /// </summary>
    Modified,

    /// <summary>
    /// The file was created.
    /// </summary>
    Created,

    /// <summary>
    /// The file was deleted.
    /// </summary>
    Deleted
} 