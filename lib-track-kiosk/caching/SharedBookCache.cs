using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace lib_track_kiosk.caching
{
    /// <summary>
    /// A simple process-wide shared cache for BookInfo lists and cover images.
    /// - Keeps a single cached copy of fetched books so multiple UC instances reuse it.
    /// - Keeps a bounded in-memory image cache (master images). Returns clones for UI use.
    /// - Evicts oldest images when capacity exceeded.
    /// </summary>
    public static class SharedBookCache
    {
        // Books cache
        private static List<object> _booksRaw; // typed as object to avoid dependency on BookInfo type at compile-time in some projects; cast when used
        private static readonly object _booksLock = new object();

        // Image cache
        private static readonly ConcurrentDictionary<string, Image> _images = new ConcurrentDictionary<string, Image>(StringComparer.OrdinalIgnoreCase);
        private static readonly ConcurrentQueue<string> _imageQueue = new ConcurrentQueue<string>();
        private static readonly SemaphoreSlim _downloadSemaphore = new SemaphoreSlim(6);
        private static readonly HttpClient _httpClient = new HttpClient() { Timeout = TimeSpan.FromSeconds(10) };

        // Max number of master images to keep in memory (change if you need larger/smaller)
        private static int _maxImages = 200;

        /// <summary>
        /// Returns a cached list of BookInfo (shallow copy). If not present, calls the fetcher to populate cache.
        /// Usage: await SharedBookCache.GetOrFetchBooksAsync(() => GetAllBooks.GetAllAsync());
        /// </summary>
        public static async Task<List<T>> GetOrFetchBooksAsync<T>(Func<Task<List<T>>> fetcher)
            where T : class
        {
            if (fetcher == null) throw new ArgumentNullException(nameof(fetcher));

            // Fast path: cached
            lock (_booksLock)
            {
                if (_booksRaw != null)
                {
                    // Return a shallow copy to avoid callers mutating shared list
                    return _booksRaw.Cast<T>().ToList();
                }
            }

            // Fetch outside lock
            var fetched = await fetcher().ConfigureAwait(false);
            if (fetched == null) fetched = new List<T>();

            lock (_booksLock)
            {
                if (_booksRaw == null)
                {
                    _booksRaw = fetched.Cast<object>().ToList();
                }

                // Return a shallow copy
                return _booksRaw.Cast<T>().ToList();
            }
        }

        /// <summary>
        /// Forcibly replaces the books cache (useful if you want to refresh).
        /// </summary>
        public static void SetBooks<T>(List<T> books) where T : class
        {
            lock (_booksLock)
            {
                _booksRaw = (books ?? new List<T>()).Cast<object>().ToList();
            }
        }

        /// <summary>
        /// Clears the cached books (so next request will re-fetch).
        /// </summary>
        public static void ClearBooks()
        {
            lock (_booksLock)
            {
                _booksRaw = null;
            }
        }

        /// <summary>
        /// Returns a cloned Image for UI assignment. Downloads and caches master image if missing.
        /// </summary>
        public static async Task<Image> GetOrDownloadImageAsync(string url, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            url = url.Trim();

            // cached master image exists -> return clone
            if (_images.TryGetValue(url, out var master) && master != null)
            {
                try { return (Image)master.Clone(); } catch { return master; }
            }

            // throttle downloads
            await _downloadSemaphore.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                // double-check cache
                if (_images.TryGetValue(url, out var master2) && master2 != null)
                {
                    try { return (Image)master2.Clone(); } catch { return master2; }
                }

                // download
                using (var resp = await _httpClient.GetAsync(url, ct).ConfigureAwait(false))
                {
                    if (!resp.IsSuccessStatusCode) return null;
                    var bytes = await resp.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
                    if (bytes == null || bytes.Length == 0) return null;

                    using (var ms = new MemoryStream(bytes))
                    {
                        Image img;
                        try
                        {
                            img = Image.FromStream(ms);
                        }
                        catch
                        {
                            return null;
                        }

                        Image store;
                        try
                        {
                            // create independent bitmap so underlying stream won't keep locked resources
                            store = new Bitmap(img);
                            img.Dispose();
                        }
                        catch
                        {
                            store = img;
                        }

                        // add to cache
                        if (_images.TryAdd(url, store))
                        {
                            _imageQueue.Enqueue(url);
                            // Evict if over capacity
                            while (_imageQueue.Count > _maxImages && _imageQueue.TryDequeue(out var oldKey))
                            {
                                if (_images.TryRemove(oldKey, out var oldImg))
                                {
                                    try { oldImg.Dispose(); } catch { }
                                }
                            }
                        }
                        else
                        {
                            // someone else added; dispose our local store
                            try { store.Dispose(); } catch { }
                        }

                        // return a clone for caller
                        if (_images.TryGetValue(url, out var cachedMaster) && cachedMaster != null)
                        {
                            try { return (Image)cachedMaster.Clone(); } catch { return cachedMaster; }
                        }
                        // fallback
                        return store;
                    }
                }
            }
            finally
            {
                _downloadSemaphore.Release();
            }
        }

        /// <summary>
        /// Clears the image cache (disposes images).
        /// </summary>
        public static void ClearImages()
        {
            foreach (var kv in _images)
            {
                try { kv.Value?.Dispose(); } catch { }
            }
            _images.Clear();

            while (_imageQueue.TryDequeue(out _)) { }
        }

        /// <summary>
        /// Sets the maximum number of images to hold in memory.
        /// </summary>
        public static void SetMaxImages(int max)
        {
            if (max <= 0) throw new ArgumentOutOfRangeException(nameof(max));
            _maxImages = max;
        }
    }
}