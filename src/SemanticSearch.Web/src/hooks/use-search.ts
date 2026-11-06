import { useState, useEffect, useRef } from 'react';

export interface SearchResult {
  id: string;
  filename: string;
  folderPath: string | null;
  author: string | null;
  createdAt: string | null;
  nasPath: string;
  score: number;
}

export interface UseSearchResult {
  query: string;
  setQuery: (q: string) => void;
  results: SearchResult[];
  loading: boolean;
  error: string | null;
  searched: boolean;
}

const DEBOUNCE_MS = 400;
const API_BASE = '/api/search';

export function useSearch(): UseSearchResult {
  const [query, setQuery] = useState('');
  const [results, setResults] = useState<SearchResult[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [searched, setSearched] = useState(false);
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    if (timerRef.current) clearTimeout(timerRef.current);

    if (!query.trim()) {
      setResults([]);
      setSearched(false);
      return;
    }

    timerRef.current = setTimeout(async () => {
      setLoading(true);
      setError(null);
      try {
        const res = await fetch(`${API_BASE}?query=${encodeURIComponent(query)}&top=10`);
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        const data: SearchResult[] = await res.json();
        setResults(data);
        setSearched(true);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Search failed');
        setResults([]);
      } finally {
        setLoading(false);
      }
    }, DEBOUNCE_MS);

    return () => {
      if (timerRef.current) clearTimeout(timerRef.current);
    };
  }, [query]);

  return { query, setQuery, results, loading, error, searched };
}
