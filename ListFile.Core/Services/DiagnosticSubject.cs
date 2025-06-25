using ListFile.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace ListFile.Core.Services;

/// <summary>
/// Manages diagnostic observers and notifies them of diagnostic events.
/// </summary>
public class DiagnosticSubject : IDiagnosticSubject
{
    private readonly List<IDiagnosticObserver> observers;
    private readonly object lockObject = new();
    private readonly ILogger<DiagnosticSubject>? logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiagnosticSubject"/> class.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostic subject operations.</param>
    public DiagnosticSubject(ILogger<DiagnosticSubject>? logger = null)
    {
        observers = new List<IDiagnosticObserver>();
        this.logger = logger;
    }

    /// <inheritdoc />
    public void Attach(IDiagnosticObserver observer)
    {
        if (observer == null)
        {
            throw new ArgumentNullException(nameof(observer));
        }

        lock (lockObject)
        {
            if (!observers.Contains(observer))
            {
                observers.Add(observer);
                logger?.LogDebug("Diagnostic observer attached: {ObserverType}", observer.GetType().Name);
            }
        }
    }

    /// <inheritdoc />
    public void Detach(IDiagnosticObserver observer)
    {
        if (observer == null)
        {
            throw new ArgumentNullException(nameof(observer));
        }

        lock (lockObject)
        {
            if (observers.Remove(observer))
            {
                logger?.LogDebug("Diagnostic observer detached: {ObserverType}", observer.GetType().Name);
            }
        }
    }

    /// <inheritdoc />
    public void Notify(IDiagnosticEvent diagnosticEvent)
    {
        if (diagnosticEvent == null)
        {
            throw new ArgumentNullException(nameof(diagnosticEvent));
        }

        List<IDiagnosticObserver> currentObservers;
        lock (lockObject)
        {
            currentObservers = new List<IDiagnosticObserver>(observers);
        }

        foreach (var observer in currentObservers)
        {
            try
            {
                observer.OnDiagnosticEvent(diagnosticEvent);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error notifying diagnostic observer {ObserverType} of event {EventType}", 
                    observer.GetType().Name, diagnosticEvent.EventType);
            }
        }
    }

    /// <summary>
    /// Gets the number of currently attached observers.
    /// </summary>
    public int ObserverCount
    {
        get
        {
            lock (lockObject)
            {
                return observers.Count;
            }
        }
    }
} 