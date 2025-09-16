namespace Simulation.Core;

public class DisposableAction(Action disposeAction) : IDisposable
{
    private readonly Action _disposeAction = disposeAction ?? throw new ArgumentNullException(nameof(disposeAction));
    private bool _isDisposed = false;

    public void Dispose()
    {
        if (!_isDisposed)
        {
            _disposeAction.Invoke();
            _isDisposed = true;
        }
    }
}