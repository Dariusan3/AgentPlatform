import { useEffect, useRef, useState } from 'react'

/**
 * Marcheaza un element ca vizibil cand intra in viewport, o singura data.
 * Daca utilizatorul a cerut mai putina animatie, pornim direct in starea finala.
 */
export function useReveal<T extends HTMLElement = HTMLDivElement>(
  rootMargin = '-12% 0px',
) {
  const ref = useRef<T | null>(null)
  const [shown, setShown] = useState(
    () =>
      typeof window !== 'undefined' &&
      window.matchMedia('(prefers-reduced-motion: reduce)').matches,
  )

  useEffect(() => {
    const el = ref.current
    if (!el || shown) return

    const observer = new IntersectionObserver(
      (entries) => {
        if (entries[0]?.isIntersecting) {
          setShown(true)
          observer.disconnect()
        }
      },
      { rootMargin },
    )

    observer.observe(el)
    return () => observer.disconnect()
  }, [rootMargin, shown])

  return { ref, shown }
}
