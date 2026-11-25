import { useRouter } from '@tanstack/react-router';
import { useEffect, useState } from 'react';
import { api } from '@/lib/api';

/** Whether someone is logged in; "loading" until the first answer. */
export type Session = 'loading' | 'logged-in' | 'logged-out';

/**
 * Asks the API who is logged in (`GET /api/identity/me`), at first and again
 * after every navigation, so the header follows a login or a logout. Between
 * navigations it keeps the last answer, so the header doesn't flicker.
 */
export function useSession(): Session {
  const router = useRouter();
  const [session, setSession] = useState<Session>('loading');

  useEffect(() => {
    let active = true;
    async function check() {
      let next: Session;
      try {
        const { data, response } = await api.GET('/api/identity/me');
        next = response.ok && data ? 'logged-in' : 'logged-out';
      } catch {
        next = 'logged-out';
      }
      if (active) {
        setSession(next);
      }
    }
    void check();
    const unsubscribe = router.subscribe('onResolved', () => void check());
    return () => {
      active = false;
      unsubscribe();
    };
  }, [router]);

  return session;
}
