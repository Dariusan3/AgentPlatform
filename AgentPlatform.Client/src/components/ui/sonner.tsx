import { Toaster as Sonner } from 'sonner'

/**
 * `unstyled` scoate complet stilurile proprii ale lui Sonner si ne lasa
 * aspectul pe clasele noastre. Fara el, tema lui (light, implicita) pune un
 * fundal alb care invinge `bg-surface`.
 *
 * Animatiile si pozitionarea rămân ale lui Sonner.
 */
export function Toaster() {
  return (
    <Sonner
      theme="dark"
      position="top-center"
      duration={5000}
      gap={10}
      offset={24}
      toastOptions={{
        unstyled: true,
        classNames: {
          toast:
            'rounded-card border-line bg-surface text-fg flex w-full items-start gap-3 border p-4 shadow-[0_24px_60px_-24px_rgba(0,0,0,0.9)]',
          title: 'text-[13.5px] leading-snug font-medium',
          description: 'text-muted mt-1 text-[12.5px] leading-relaxed',
          icon: 'mt-px shrink-0 [&_svg]:size-4',
          error: 'border-red-500/45 [&_[data-icon]]:text-red-400',
          success: 'border-amber/45 [&_[data-icon]]:text-amber',
        },
      }}
    />
  )
}
