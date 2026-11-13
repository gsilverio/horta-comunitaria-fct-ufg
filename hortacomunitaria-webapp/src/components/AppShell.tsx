import { useRef } from "react";
import type { ReactNode } from "react";
import { Navigation } from "./Navigation";
import { LocationTrail } from "./PageTrail";
import { Icon } from "./Icon";
import { AccountMenu } from "../features/auth/components/AccountMenu";
import { areaOf, linkTo } from "../routes/routes";
import type { AppRoute } from "../routes/routes";

function Brand() {
  return (
    <a
      className="brand"
      href={linkTo("", "hortas")}
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
  gardenSwitcher,
  gardenName,
  children,
}: {
  route: AppRoute;
  gardenSelect?: ReactNode;
  /** Troca de horta exibida no topo do menu lateral quando há mais de uma horta. */
  gardenSwitcher?: ReactNode;
  gardenName?: string;
  children: ReactNode;
}) {
  const menu = useRef<HTMLDialogElement>(null);
  // Cadastros da horta (e a lista de hortas) têm caminho próprio; o mapa usa a largura inteira.
  const organization = route.scope === "gardens" || areaOf(route.page)?.id === "horta";
  const wide = route.page === "horta/mapa";
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
          <Navigation route={route} gardenSwitcher={gardenSwitcher} />
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
          <Navigation route={route} gardenSwitcher={gardenSwitcher} onNavigate={() => menu.current?.close()} />
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
            {/* Contexto da horta à esquerda e conta à direita, na mesma altura da marca do menu lateral. */}
            <div className="flex min-w-0 flex-1 items-center">
              {gardenSelect && <div className="min-w-0 flex-1 sm:max-w-80">{gardenSelect}</div>}
            </div>
            <div className="flex shrink-0 items-center sm:border-l sm:border-horta-line sm:pl-3">
              <AccountMenu />
            </div>
          </header>
          <main id="main-content" tabIndex={-1}>
            <div className="main-inner">{(!organization || wide) && <LocationTrail route={route} gardenName={gardenName} />}{children}</div>
          </main>
          <footer className="workspace-footer">
            <span>Horta Comunitária FCT/UFG</span>
          </footer>
        </div>
      </div>
    </div>
  );
}
