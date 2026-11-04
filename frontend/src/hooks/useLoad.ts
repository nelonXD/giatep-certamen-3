import { useEffect, useState } from 'react';
import { request } from '../api/client';
export function useLoad<T>(path: string, reloadKey?: string) {
  const key = reloadKey === undefined ? path : `${path}#${reloadKey}`;
  const [data, setData] = useState<T | null>(null), [error, setError] = useState(''), [loading, setLoading] = useState(true), [revision, setRevision] = useState(0), [loadedPath, setLoadedPath] = useState('');
  useEffect(() => { let active = true; const controller = new AbortController(); setLoading(true); setError(''); request<T>(path, { signal: controller.signal }).then(d => { if (active) setData(d); }).catch(e => { if (active) { setError(e.message); setData(null); } }).finally(() => { if (active) { setLoadedPath(key); setLoading(false); } }); return () => { active = false; controller.abort(); }; }, [path, revision, key]);
  return { data: loadedPath === key ? data : null, error: loadedPath === key ? error : '', loading: loading || loadedPath !== key, refresh: () => setRevision(n => n + 1) };
}
