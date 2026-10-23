using Jellyfin.Plugin.MyAnimeList.Configuration;
using LiteDB;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Jellyfin.Plugin.MyAnimeList.Providers.MyAnimeList.APIs.JikanSystem
{
    public class LiteDbCacheStore : IDisposable, ICacheStore
    {
        private readonly LiteDatabase _db;
        private DateTime _lastFileCleanup = DateTime.UtcNow;
        private static readonly TimeSpan FileCleanupInterval = TimeSpan.FromHours(1);

        private readonly ConcurrentDictionary<string, CacheItem> _memory = new();
        private readonly ConcurrentDictionary<string, bool> _indexed = new();
        private readonly ConcurrentDictionary<string, ILiteCollection<CacheRecord>> _collections = new();
        private readonly CacheExpiryScheduler _expiryScheduler;
        private readonly Task _workerTask;
        private const int CacheSchemaVersion = 17;
        private static readonly PluginConfiguration DefaultConfig = new();
        private static PluginConfiguration _config => Plugin.Instance?.Configuration ?? DefaultConfig;
        private static bool DisableLocalCache => _config.DisableLocalCache;

        private class CacheItem
        {
            public object Value;
            public long ExpiryTicks;
        }

        private class CacheRecord
        {
            [BsonId]
            public string Key { get; set; }

            public string Data { get; set; }

            public long ExpiryTicks { get; set; }
        }

        private class WriteItem
        {
            public string Type;
            public string Id;
            public object Value;
            public long ExpiryTicks;
        }

        public LiteDbCacheStore(string path)
        {
            _expiryScheduler = new CacheExpiryScheduler(CleanupMemory);

            if (DisableLocalCache)
                return;

            var dbName = $"cache_v{CacheSchemaVersion}";
            var dbPath = Path.Combine(path, dbName + ".db");

            try
            {
                Directory.CreateDirectory(path);

                // Remove caches from older schema versions, but keep this version's
                // database and its "-log.db" journal.
                foreach (var file in Directory.GetFiles(path))
                {
                    if (!Path.GetFileName(file).StartsWith(dbName, StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch
                        {
                        }
                    }
                }
            }
            catch
            {
                // No usable cache folder: keep going with the in-memory cache only.
                return;
            }

            _db = OpenDatabase(path, dbName, dbPath);

            if (_db is not null)
                _workerTask = Task.Run(ProcessQueue);
        }

        // Only this process uses the cache, so open it in direct mode. Shared mode
        // needs a system-wide named mutex ("Global\...") that Jellyfin can't create
        // in Docker or as a non-root user, which stopped the plugin from loading.
        private static LiteDatabase OpenDatabase(string path, string dbName, string dbPath)
        {
            var connection = $"Filename={dbPath};Connection=direct;";

            try
            {
                return new LiteDatabase(connection);
            }
            catch
            {
                // A damaged cache file: delete it and start a new one.
            }

            try
            {
                foreach (var file in Directory.GetFiles(path, dbName + "*"))
                    File.Delete(file);

                return new LiteDatabase(connection);
            }
            catch
            {
                // Still can't open it: keep going with the in-memory cache only.
                return null;
            }
        }


        private void CleanupMemory()
        {
            var now = DateTime.UtcNow.Ticks;

            foreach (var kv in _memory)
            {
                if (kv.Value.ExpiryTicks < now)
                    _memory.TryRemove(kv.Key, out _);
            }
        }

        private readonly ConcurrentQueue<WriteItem> _writeQueue = new();
        private readonly SemaphoreSlim _signal = new(0);
        private readonly CancellationTokenSource _cts = new();

        private async Task ProcessQueue()
        {
            if (DisableLocalCache || _db is null) return;

            try
            {
                while (!_cts.IsCancellationRequested)
                {
                    await _signal.WaitAsync(_cts.Token).ConfigureAwait(false);

                    if (_cts.IsCancellationRequested)
                        break;

                    await Task.Delay(TimeSpan.FromSeconds(15), _cts.Token).ConfigureAwait(false);

                    var batchMap = new Dictionary<(string type, string id), WriteItem>();
                    while (_writeQueue.TryDequeue(out var item))
                    {
                        batchMap[(item.Type, item.Id)] = item;
                    }

                    var batch = batchMap.Values.ToList();

                    if (batch.Count == 0)
                        continue;

                    try
                    {
                        WriteBatch(batch);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        // A failed write loses some cache entries, but must not
                        // stop this loop, or nothing would be saved again.
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // normal shutdown
            }
        }

        private void WriteBatch(List<WriteItem> batch)
        {
            foreach (var group in batch.GroupBy(x => x.Type))
            {
                var col = _collections.GetOrAdd(group.Key, k => _db.GetCollection<CacheRecord>(k));

                if (_indexed.TryAdd(group.Key, true))
                {
                    col.EnsureIndex(x => x.ExpiryTicks);
                }

                var records = group.Select(x => new CacheRecord
                {
                    Key = x.Id,
                    Data = JsonSerializer.Serialize(x.Value),
                    ExpiryTicks = x.ExpiryTicks
                });

                col.Upsert(records);
            }

            if (DateTime.UtcNow - _lastFileCleanup > FileCleanupInterval)
            {
                var now = DateTime.UtcNow.Ticks;

                foreach (var name in _db.GetCollectionNames())
                {
                    var col = _db.GetCollection<CacheRecord>(name);
                    col.EnsureIndex(x => x.ExpiryTicks);
                    try
                    {
                        col.DeleteMany(x => x.ExpiryTicks < now);
                    }
                    catch { }
                }

                _lastFileCleanup = DateTime.UtcNow;
            }
        }

        private static (string type, string id) ParseKey(string key)
        {
            var idx = key.IndexOf(':');
            if (idx < 0)
                return ("cache", key);

            return (
                key.Substring(0, idx),
                key.Substring(idx + 1)
            );
        }

        public T Get<T>(string key)
        {
            var now = DateTime.UtcNow.Ticks;

            if (_memory.TryGetValue(key, out var mem))
            {
                if (mem != default && mem.ExpiryTicks > now)
                    return (T)mem.Value;

                _memory.TryRemove(key, out _);
            }

            if (DisableLocalCache || _db is null)
            {
                return default;
            }

            var (type, id) = ParseKey(key);
            var col = _collections.GetOrAdd(type, k => _db.GetCollection<CacheRecord>(k));

            try
            {
                var record = col.FindById(id);

                if (record is null ||
                    record.ExpiryTicks < now ||
                    record.Data is null)
                {
                    col.Delete(id);
                    return default;
                }

                var value = JsonSerializer.Deserialize<T>(record.Data);

                if (value is null)
                {
                    col.Delete(id);
                    return default;
                }

                _memory[key] = new CacheItem
                {
                    Value = value,
                    ExpiryTicks = record.ExpiryTicks
                };

                return value;
            }
            catch
            {
                try
                {
                    col.Delete(id);
                }
                catch
                {
                }

                return default;
            }
        }

        private int _disposed;

        public void Put<T>(string key, T value, DateTime expiry)
        {
            if (Volatile.Read(ref _disposed) != 0)
                return;

            if (value is null)
                return;

            var expiryTicks = expiry.ToUniversalTime().Ticks;

            _memory[key] = new CacheItem
            {
                Value = value,
                ExpiryTicks = expiryTicks
            };

            _expiryScheduler.Schedule(expiryTicks);

            if (DisableLocalCache || _db is null)
            {
                return;
            }

            if (Volatile.Read(ref _disposed) != 0)
                return;

            var (type, id) = ParseKey(key);

            _writeQueue.Enqueue(new WriteItem
            {
                Type = type,
                Id = id,
                Value = value,
                ExpiryTicks = expiryTicks
            });

            try
            {
                _signal.Release();
            }
            catch (ObjectDisposedException)
            {

            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            _cts.Cancel();

            // Wake ProcessQueue if it is waiting on the semaphore.
            try
            {
                _signal.Release();
            }
            catch (ObjectDisposedException)
            {
            }

            try
            {
                _workerTask?.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException) when (_cts.IsCancellationRequested)
            {
                // Expected during shutdown.
            }
            catch
            {
                // Do not prevent Jellyfin/plugin shutdown.
            }

            _expiryScheduler.Dispose();
            _signal.Dispose();
            _cts.Dispose();
            _db?.Dispose();
        }
    }
}
