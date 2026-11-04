import { useRef } from "react";
import type { ReactNode } from "react";
import { Navigation, SectionNavigation } from "./Navigation";
import { Icon } from "./Icon";
import { routeHref } from "../hooks/useRoute";
import type { RecordRoute } from "../types/records";

function Brand() {
  return (
    <a
      className="brand integrated-brand"
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
  children,
}: {
  route: RecordRoute;
  gardenSelect?: ReactNode;
  children: ReactNode;
}) {
  const menu = useRef<HTMLDialogElement>(null);
  const organization = route.module === "horta";
  return (
    <div
      className={organization ? "app-shell organization-shell" : "app-shell"}
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
          <Brand />
          <div className="sidebar-context">{gardenSelect}</div>
          <Navigation route={route} />
        </aside>
        <dialog
          ref={menu}
          className="navigation-dialog"
          aria-label="Menu de navegação"
        >
          <div className="flex items-center justify-between gap-4">
            <Brand />
            <button
              className="secondary"
              aria-label="Fechar menu"
              onClick={() => menu.current?.close()}
            >
              <Icon name="close" />
            </button>
          </div>
          <div className="my-5" onChange={() => menu.current?.close()}>
            {gardenSelect}
          </div>
          <Navigation route={route} onNavigate={() => menu.current?.close()} />
        </dialog>
        <div className="workspace-content">
          <div className="flex items-center justify-between gap-4 px-5 pt-5 lg:hidden">
            <Brand />
            <button
              className="secondary p-3"
              aria-label="Abrir menu de navegação"
              onClick={() => menu.current?.showModal()}
            >
              <Icon name="menu" />
            </button>
          </div>
          {route.garden && (
            <>
              <div className={route.module === "horta" ? "hidden" : "workspace-toolbar"}>
                <div className="flex flex-wrap items-center gap-x-5 gap-y-2">
                  {route.module !== "horta" && <a className="font-semibold underline" href={routeHref(route.garden, "horta", "interativa")}>Configurar horta</a>}
                </div>
              </div>
              <SectionNavigation route={route} />
            </>
          )}
          <main id="main-content" tabIndex={-1}>
            {children}
          </main>
          <footer className="workspace-footer">
            <span>Horta Comunitária FCT/UFG</span>
          </footer>
        </div>
      </div>
    </div>
  );
}
