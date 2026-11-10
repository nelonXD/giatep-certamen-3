import { useState } from 'react';

// Only view preferences are saved, scoped to the user and record in this tab.
export function useSessionView<T extends string | boolean>(key: string, initial: T, valid: (value: unknown) => value is T) {
  const [value, update] = useState<T>(() => {
    try { const stored: unknown = JSON.parse(sessionStorage.getItem(key) || 'null'); return valid(stored) ? stored : initial; }
    catch { return initial; }
  });
  function setValue(next: T) {
    update(next);
    try { sessionStorage.setItem(key, JSON.stringify(next)); } catch { /* Storage may be disabled. */ }
  }
  return [value, setValue] as const;
}
