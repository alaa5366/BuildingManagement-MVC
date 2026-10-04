// =====================================================================
//  KeyedLock - قفل لكل مفتاح (Uid مثلاً) لمنع تعارض الكتابة المتزامنة
// =====================================================================
using System.Collections.Concurrent;

namespace BuildingManagementMvc.Data;

public static class KeyedLock
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

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
        public void Dispose() => _sem.Release();
    }
}