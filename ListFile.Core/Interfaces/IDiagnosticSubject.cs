namespace ListFile.Core.Interfaces;

/// <summary>
/// Defines the contract for a diagnostic subject that can be observed by diagnostic observers.
/// </summary>
public interface IDiagnosticSubject
{
    /// <summary>
    /// Attaches an observer to the subject.
    /// </summary>
    /// <param name="observer">The observer to attach.</param>
    void Attach(IDiagnosticObserver observer);

    /// <summary>
    /// Detaches an observer from the subject.
    /// </summary>
    /// <param name="observer">The observer to detach.</param>
    void Detach(IDiagnosticObserver observer);

    /// <summary>
    /// Notifies all attached observers of a diagnostic event.
    /// </summary>
    /// <param name="diagnosticEvent">The diagnostic event to notify observers about.</param>
    void Notify(IDiagnosticEvent diagnosticEvent);
} 