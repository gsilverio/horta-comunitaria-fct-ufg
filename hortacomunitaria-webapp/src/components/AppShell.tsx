import { useRef } from "react";
import type { ReactNode } from "react";
import { Navigation } from "./Navigation";
import { currentLocation } from "./navigationItems";
import { Icon } from "./Icon";
import { routeHref } from "../hooks/useRoute";
import type { RecordRoute } from "../types/records";

function Brand() {
  return (
    <a
      className="brand"
      href={routeHref("", "horta", "hortas")}
      aria-label="Horta Comunitária, todas as hortas"
    >
      <svg viewBox="0 0 32 32" aria-hidden="true">
        <path d="M16 27V15M16 19C6 21 3 13 5 6c8 0 12 5 11 13ZM16 15C15 7 21 3 28 4c0 7-4 12-12 11Z" />
      </svg>
      <span>
        Horta Comunitária<small>FCT / UFG · APOEMA</small>
      </span>
    </a>
  );
}
export function AppShell({
  route,
  gardenSelect,
  gardenName,
  children,
}: {
  route: RecordRoute;
  gardenSelect?: ReactNode;
  gardenName?: string;
  children: ReactNode;
}) {
  const menu = useRef<HTMLDialogElement>(null);
  const organization = route.module === "horta";
  const wide = organization && route.resource === "interativa";
  const { area, page } = currentLocation(route);
  return (
    <div
      className={`app-shell ${organization ? "organization-shell" : ""} ${wide ? "workspace-wide" : ""}`}
    >
      <a
        className="skip-link"
        href="#main-content"
        onClick={(event) => {
          event.preventDefault();
          document.getElementById("main-content")?.focus();
        }}
      >
        Pular para o conteúdo
      </a>
      <div className="workspace-layout">
        <aside className="workspace-sidebar" aria-label="Navegação principal">
          <div className="sidebar-brand">
            <Brand />
          </div>
          <Navigation route={route} />
        </aside>
        <dialog
          ref={menu}
          id="workspace-navigation"
          className="navigation-dialog"
          aria-label="Menu de navegação"
          onClick={(event) => {
            if (event.target === event.currentTarget) menu.current?.close();
          }}
        >
          <div className="sidebar-brand justify-between gap-3 pr-2">
            <Brand />
            <button
              className="secondary shrink-0 border-0 bg-transparent px-3"
              aria-label="Fechar menu"
              onClick={() => menu.current?.close()}
            >
              <Icon name="close" />
            </button>
          </div>
          <Navigation route={route} onNavigate={() => menu.current?.close()} />
        </dialog>
        <div className="workspace-content">
          <header className="workspace-header">
            <button
              type="button"
              className="secondary shrink-0 px-3 lg:hidden"
              aria-label="Abrir menu de navegação"
              aria-controls="workspace-navigation"
              aria-haspopup="dialog"
              onClick={() => menu.current?.showModal()}
            >
              <Icon name="menu" />
            </button>
            <p className="workspace-location">
              {gardenName && !gardenSelect && <span>{gardenName}</span>}
              {gardenName && !gardenSelect && area && <Icon name="chevron" />}
              {area && (page ? <span>{area}</span> : <strong>{area}</strong>)}
              {page && <Icon name="chevron" />}
              {page && <strong>{page}</strong>}
            </p>
            <div className="ml-auto flex min-w-0 flex-1 items-center justify-end gap-3 sm:gap-5">
              {gardenSelect && <div className="min-w-0 flex-1 sm:max-w-72">{gardenSelect}</div>}
              <div className="flex shrink-0 items-center justify-end gap-2.5" role="group" aria-label="Identificação do operador demonstrativo">
                <div className="hidden min-w-0 text-right sm:block">
                  <p className="m-0 truncate text-[13px] leading-4 font-semibold text-horta-ink">Operador de demonstração</p>
                  <p className="m-0 text-xs leading-4 text-horta-muted">Modo demonstrativo</p>
                </div>
                <span className="flex size-9 shrink-0 items-center justify-center rounded-full border border-horta-line bg-horta-soft text-xs font-semibold text-horta-forest" aria-hidden="true">OD</span>
              </div>
            </div>
          </header>
          <main id="main-content" tabIndex={-1}>
            <div className="main-inner">{children}</div>
          </main>
          <footer className="workspace-footer">
            <span>Horta Comunitária FCT/UFG</span>
          </footer>
        </div>
      </div>
    </div>
  );
}
