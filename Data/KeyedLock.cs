// =====================================================================
//  KeyedLock - قفل لكل مفتاح (Uid مثلاً) لمنع تعارض الكتابة المتزامنة
// =====================================================================
using System.Collections.Concurrent;

namespace BuildingManagementMvc.Data;

/// <summary>
/// Per-key asynchronous mutex. Work that uses the same key runs one at a time; different keys run in parallel.
/// </summary>
public static class KeyedLock
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    /// <summary>
    /// Waits for the lock of a key and returns a handle that releases it when disposed.
    /// </summary>
    public static async Task<IDisposable> AcquireAsync(string key, CancellationToken ct = default)
    {
        var sem = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await sem.WaitAsync(ct);
        return new Releaser(sem);
    }

    private sealed class Releaser : IDisposable
    {
        private readonly SemaphoreSlim _sem;
        public Releaser(SemaphoreSlim sem) => _sem = sem;
        /// <summary>
        /// Releases the lock so the next waiter for the same key can continue.
        /// </summary>
        public void Dispose() => _sem.Release();
    }
}
