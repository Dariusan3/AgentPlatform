import type { ReactNode } from 'react'

/** Antetul comun al paginilor: titlu, numar, descriere si actiunea principala */
export function PageHeader({
  title,
  count,
  description,
  action,
}: {
  title: string
  count?: number
  description?: string
  action?: ReactNode
}) {
  return (
    <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
      <div className="min-w-0">
        <div className="flex items-center gap-2.5">
          <h2 className="text-[22px] font-medium tracking-[-0.03em]">{title}</h2>
          {count !== undefined && (
            <span className="border-line text-muted tnum rounded-full border px-2 py-0.5 font-mono text-[11px]">
              {count}
            </span>
          )}
        </div>
        {description && (
          <p className="text-muted mt-2 max-w-xl text-[13.5px] leading-relaxed">
            {description}
          </p>
        )}
      </div>
      {action && <div className="shrink-0">{action}</div>}
    </div>
  )
}
