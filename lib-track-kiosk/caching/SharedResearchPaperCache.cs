using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace lib_track_kiosk.caching
{
    /// <summary>
    /// A small process-wide shared cache for research paper lists.
    /// - Keeps a single cached copy of fetched research-paper lists so multiple UC instances reuse it.
    /// - Thread-safe and returns shallow copies to callers to avoid accidental mutations of the canonical cache.
    /// - Supports forcing a replacement or clearing the cache.
    /// </summary>
    public static class SharedResearchPaperCache
    {
        // Underlying storage (boxed as object to avoid tight coupling to a specific DTO type)
        private static List<object> _papersRaw;
        private static readonly object _papersLock = new object();

        /// <summary>
        /// Get cached papers or fetch them using the provided fetcher if missing.
        /// Returns a shallow copy list of T to callers.
        /// Example usage:
        ///   var items = await SharedResearchPaperCache.GetOrFetchAsync(() => GetAllResearchPapers.GetAllAsync());
        /// </summary>
        /// <typeparam name="T">The item type returned by the fetcher (e.g. AllResearchPaperInfo).</typeparam>
        /// <param name="fetcher">Function that fetches the list when cache empty.</param>
        /// <returns>Shallow copy of cached items (List&lt;T&gt;).</returns>
        public static async Task<List<T>> GetOrFetchAsync<T>(Func<Task<List<T>>> fetcher) where T : class
        {
            if (fetcher == null) throw new ArgumentNullException(nameof(fetcher));

            // Fast path: return cached shallow copy if present
            lock (_papersLock)
            {
                if (_papersRaw != null)
                {
                    // return shallow copy to avoid caller mutating shared list
                    return _papersRaw.Cast<T>().ToList();
                }
            }

            // Not cached: fetch outside lock
            var fetched = await fetcher().ConfigureAwait(false) ?? new List<T>();

            // store into cache if still empty
            lock (_papersLock)
            {
                if (_papersRaw == null)
                {
                    _papersRaw = fetched.Cast<object>().ToList();
                }

                return _papersRaw.Cast<T>().ToList();
            }
        }

        /// <summary>
        /// Forcibly replaces cache content with the provided list.
        /// </summary>
        public static void SetPapers<T>(List<T> papers) where T : class
        {
            lock (_papersLock)
            {
                _papersRaw = (papers ?? new List<T>()).Cast<object>().ToList();
            }
        }

        /// <summary>
        /// Clears the cached papers so next call will re-fetch.
        /// </summary>
        public static void Clear()
        {
            lock (_papersLock)
            {
                _papersRaw = null;
            }
        }

        /// <summary>
        /// Returns whether a cached value currently exists.
        /// </summary>
        public static bool HasValue()
        {
            lock (_papersLock)
            {
                return _papersRaw != null && _papersRaw.Count > 0;
            }
        }

        /// <summary>
        /// Returns the cached count (0 if none).
        /// </summary>
        public static int Count
        {
            get
            {
                lock (_papersLock)
                {
                    return _papersRaw?.Count ?? 0;
                }
            }
        }
    }
}