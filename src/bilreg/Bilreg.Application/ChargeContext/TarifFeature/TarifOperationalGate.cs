namespace Bilreg.Application.ChargeContext.TarifFeature;

public sealed class TarifOperationalGate
{
    private readonly object _lock = new();
    private TarifOperation _active = TarifOperation.None;

    public TarifOperation ActiveOperation
    {
        get
        {
            lock (_lock)
                return _active;
        }
    }

    public bool IsOperationInProgress => ActiveOperation != TarifOperation.None;

    public IDisposable Acquire(TarifOperation operation)
    {
        lock (_lock)
        {
            if (_active != TarifOperation.None && _active != operation)
            {
                throw new InvalidOperationException(
                    $"Operasi tarif '{_active}' sedang berjalan; tidak dapat memulai '{operation}'. " +
                    "Tunggu hingga selesai sebelum menjalankan import atau publish.");
            }

            _active = operation;
        }

        return new ReleaseHandle(this, operation);
    }

    private void Release(TarifOperation operation)
    {
        lock (_lock)
        {
            if (_active == operation)
                _active = TarifOperation.None;
        }
    }

    private sealed class ReleaseHandle : IDisposable
    {
        private readonly TarifOperationalGate _gate;
        private readonly TarifOperation _operation;
        private bool _disposed;

        public ReleaseHandle(TarifOperationalGate gate, TarifOperation operation)
        {
            _gate = gate;
            _operation = operation;
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _gate.Release(_operation);
        }
    }
}
