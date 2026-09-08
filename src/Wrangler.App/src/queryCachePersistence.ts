import { dehydrate, hydrate, type DehydratedState, type Query, type QueryClient } from "@tanstack/react-query";
import type { StorageLike } from "./routes/settings/-hooks/repositoryFeatures";

/**
 * Snapshots the query cache into localStorage and restores it before the first
 * render.
 *
 * QUERY_DEFAULTS already keeps data alive for a whole day, so navigating around
 * the app never re-shows a spinner — but that cache is memory only. A reload, a
 * PWA relaunch, or the browser discarding a background tab throws it away, and
 * the dashboard is back to a spinner and a cold fetch of every selected
 * workflow. Persisting it means the last rendered dashboard is on screen
 * immediately on the next visit, with the refetch happening behind it.
 */

export const QUERY_CACHE_STORAGE_KEY = "wrangler.queryCache";

/** Bump when the persisted shape changes; older snapshots are then discarded. */
export const QUERY_CACHE_VERSION = 1;

/**
 * Beyond this a snapshot is dropped rather than restored. Matches the gcTime in
 * QUERY_DEFAULTS: showing a day-old build status briefly is the point, showing
 * a week-old one is just misleading.
 */
export const QUERY_CACHE_MAX_AGE = 24 * 60 * 60 * 1000;

/**
 * localStorage is a ~5MB budget shared with the app's settings (which matter
 * far more than this cache), so an oversized snapshot is skipped rather than
 * risking a quota error that could take a setting write down with it.
 */
export const QUERY_CACHE_MAX_CHARS = 2_000_000;

/** Coalesces the burst of cache events a fetch or a stream push produces. */
const PERSIST_DEBOUNCE_MS = 1000;

/**
 * Root query keys worth restoring: the list pages whose data costs a GitHub
 * call per selected workflow/repo and which the SSE stream keeps current.
 * Everything else (the signed-in user, user search, values read straight from
 * localStorage) is either cheap, ephemeral, or already synchronous.
 */
export const PERSISTED_QUERY_KEYS = [
  "getWorkflows",
  "getWorkflowRuns",
  "pullRequests",
  "attention",
  "gates",
] as const;

interface QueryCacheSnapshot {
  version: number;
  savedAt: number;
  state: DehydratedState;
}

const isPersistable = (query: Query): boolean => {
  if (query.state.status !== "success") return false;
  const root = query.queryKey[0];
  return typeof root === "string" && (PERSISTED_QUERY_KEYS as readonly string[]).includes(root);
};

/**
 * Writes the persistable part of the cache to storage. Returns whether anything
 * was stored — a failure is never fatal, it only costs the next visit a spinner.
 */
export const saveQueryCache = (queryClient: QueryClient, storage: StorageLike): boolean => {
  const state = dehydrate(queryClient, {
    shouldDehydrateQuery: isPersistable,
    shouldDehydrateMutation: () => false,
  });

  if (state.queries.length === 0) {
    // Nothing worth restoring (e.g. the user cleared every repo): drop the old
    // snapshot rather than leave it to be restored on top of the new reality.
    storage.removeItem(QUERY_CACHE_STORAGE_KEY);
    return false;
  }

  const snapshot: QueryCacheSnapshot = { version: QUERY_CACHE_VERSION, savedAt: Date.now(), state };

  let json: string;
  try {
    json = JSON.stringify(snapshot);
  } catch {
    return false;
  }

  if (json.length > QUERY_CACHE_MAX_CHARS) return false;

  try {
    storage.setItem(QUERY_CACHE_STORAGE_KEY, json);
    return true;
  } catch {
    // Quota exceeded or storage disabled. Clear the key so a stale snapshot
    // isn't restored later as if it were the current state.
    try {
      storage.removeItem(QUERY_CACHE_STORAGE_KEY);
    } catch {
      // Storage is unavailable entirely; nothing further to do.
    }
    return false;
  }
};

/**
 * Restores a snapshot into `queryClient`. Call before rendering so the first
 * paint already has data.
 *
 * Restored queries are marked stale so each refetches as soon as its page
 * mounts: the SSE stream was not running while the app was closed and the
 * broadcaster keeps no buffer, so anything that happened in between was missed.
 * Without that, a snapshot younger than the query's staleTime would sit there
 * unrefreshed. Hence: previous state instantly, live data as soon as GitHub
 * answers.
 */
export const restoreQueryCache = (
  queryClient: QueryClient,
  storage: StorageLike,
  now: number = Date.now(),
): boolean => {
  let raw: string | null;
  try {
    raw = storage.getItem(QUERY_CACHE_STORAGE_KEY);
  } catch {
    return false;
  }
  if (!raw) return false;

  const discard = () => {
    try {
      storage.removeItem(QUERY_CACHE_STORAGE_KEY);
    } catch {
      // Ignore: an unreadable storage can't be cleaned up either.
    }
  };

  let snapshot: QueryCacheSnapshot;
  try {
    snapshot = JSON.parse(raw) as QueryCacheSnapshot;
  } catch {
    discard();
    return false;
  }

  const usable =
    !!snapshot &&
    snapshot.version === QUERY_CACHE_VERSION &&
    typeof snapshot.savedAt === "number" &&
    !!snapshot.state &&
    Array.isArray(snapshot.state.queries) &&
    now - snapshot.savedAt <= QUERY_CACHE_MAX_AGE;

  if (!usable) {
    discard();
    return false;
  }

  hydrate(queryClient, snapshot.state);

  for (const queryKey of PERSISTED_QUERY_KEYS) {
    queryClient.invalidateQueries({ queryKey: [queryKey], refetchType: "none" });
  }

  return true;
};

/**
 * Drops the snapshot. Used on logout: queryClient.clear() empties the in-memory
 * cache, but the page navigates away before the debounced write can mirror
 * that, which would leave the previous session's data to be restored for
 * whoever signs in next.
 */
export const clearQueryCacheSnapshot = (storage: StorageLike): void => {
  try {
    storage.removeItem(QUERY_CACHE_STORAGE_KEY);
  } catch {
    // Storage unavailable; nothing to clear.
  }
};

export interface QueryCachePersistence {
  /** Write the current cache immediately, outside the debounce. */
  flush: () => void;
  /** Stop persisting (cancels any pending write). */
  stop: () => void;
}

/**
 * Keeps the stored snapshot in step with the cache: a trailing-debounced write
 * on cache changes, plus an immediate one as the page goes away — a mobile
 * browser can kill a backgrounded tab without another turn of the event loop,
 * and a reload can beat the debounce. Those are exactly the cases this whole
 * module exists for.
 */
export const startPersistingQueryCache = (
  queryClient: QueryClient,
  storage: StorageLike,
  debounceMs: number = PERSIST_DEBOUNCE_MS,
): QueryCachePersistence => {
  let timer: ReturnType<typeof setTimeout> | undefined;

  const flush = () => {
    if (timer) {
      clearTimeout(timer);
      timer = undefined;
    }
    saveQueryCache(queryClient, storage);
  };

  const schedule = () => {
    if (timer) return; // Trailing debounce: the pending write picks up later changes too.
    timer = setTimeout(() => {
      timer = undefined;
      saveQueryCache(queryClient, storage);
    }, debounceMs);
  };

  const unsubscribe = queryClient.getQueryCache().subscribe(schedule);

  const onHide = () => {
    if (document.visibilityState === "hidden") flush();
  };

  // A reload typically fires pagehide without a visibilitychange, so both.
  const hasDocument = typeof document !== "undefined";
  if (hasDocument) {
    document.addEventListener("visibilitychange", onHide);
    window.addEventListener("pagehide", flush);
  }

  return {
    flush,
    stop: () => {
      unsubscribe();
      if (hasDocument) {
        document.removeEventListener("visibilitychange", onHide);
        window.removeEventListener("pagehide", flush);
      }
      if (timer) {
        clearTimeout(timer);
        timer = undefined;
      }
    },
  };
};
