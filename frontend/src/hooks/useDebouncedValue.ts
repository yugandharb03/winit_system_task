import { useEffect, useState } from 'react';

/**
 * Delays a value so key-press driven searches do not fire a request per keystroke.
 * The task brief asks for search on key press; the debounce keeps that responsive
 * without hammering the API.
 */
export function useDebouncedValue<T>(value: T, delayMs = 300): T {
  const [debounced, setDebounced] = useState(value);

  useEffect(() => {
    const timer = window.setTimeout(() => setDebounced(value), delayMs);
    return () => window.clearTimeout(timer);
  }, [value, delayMs]);

  return debounced;
}
