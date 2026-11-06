import { useGardenState } from "../hooks/useGardenState";
import { isValidElement } from "react";
import type { ReactNode } from "react";
import type { DetailItem } from "../types/records";
import { Facts, Field } from "./Fields";
import { Failure } from "./Feedback";
import { AuditHistory } from "./AuditHistory";
import type { useRecord } from "../hooks/useRecord";
export function RecordDetail<T>({
  state,
  title,
  items,
  version,
  active,
  edit,
  back,
  children,
  heading,
  catalog = false,
  requireActiveGarden = true,
  compact = false,
  hideBack = false,
  deactivationInitiallyOpen = false,
}: {
  compact?: boolean;
  hideBack?: boolean;
  deactivationInitiallyOpen?: boolean;
  state: ReturnType<typeof useRecord<T>>;
  title: string;
  items: DetailItem[];
  version: number;
  active: boolean;
  edit: () => void;
  back: () => void;
  children?: ReactNode;
  heading?: ReactNode;
  catalog?: boolean;
  requireActiveGarden?: boolean;
}) {
  const garden = useGardenState();
  return (
    <>
      {!hideBack && (
        <button className="text-button" onClick={back} disabled={state.busy}>
          ← Voltar
        </button>
      )}
      {state.notice && (
        <p className="feedback success" role="status">
          {state.notice}
        </p>
      )}
      <section className="panel">
        {state.detail.data && heading ? heading : <h1 tabIndex={-1}>{title}</h1>}
        {state.detail.loading && <p role="status">Carregando registro…</p>}
        {state.detail.error && (
          <Failure error={state.detail.error} retry={state.refresh} />
        )}
        {state.detail.data && (
          <>
            {!heading && <p className="badge">
              {catalog
                ? active
                  ? "Ativo"
                  : "Inativo"
                : active
                  ? "Válido"
                  : "Invalidado"}
              {!compact && ` · Versão ${version}`}
            </p>}
            {items
              .filter((item) => isValidElement(item.value))
              .map((item) => (
                <div key={item.label} className="mt-5">
                  {item.value}
                </div>
              ))}
            {items.some(item => !isValidElement(item.value)) && <Facts
              items={items.filter((item) => !isValidElement(item.value))}
            />}
            {children}
            {active && (!requireActiveGarden || garden.isActive) && (
              <>
                <div className="mt-5">
                  <button
                    className="secondary"
                    disabled={state.busy}
                    onClick={edit}
                  >
                    {catalog ? "Editar cadastro" : "Corrigir registro"}
                  </button>
                </div>
                <details className="mt-5" open={deactivationInitiallyOpen || undefined}>
                  <summary>
                    {catalog ? "Inativar cadastro" : "Invalidar registro"}
                  </summary>
                  <p>
                    O histórico permanece disponível. Dependentes podem impedir
                    a alteração.
                  </p>
                  {state.error && <Failure error={state.error} />}
                  <Field
                    name="reason"
                    label="Motivo"
                    required
                    type="textarea"
                    value={state.reason}
                    change={state.setReason}
                    error={state.error}
                    maxLength={500}
                  />
                  <button
                    className="danger mt-4"
                    disabled={state.busy || state.error?.status === 409}
                    onClick={() => void state.deactivate(version)}
                  >
                    Confirmar {catalog ? "inativação" : "invalidação"}
                  </button>
                  {state.error?.status === 409 && (
                    <>
                      <button className="secondary" onClick={state.refresh}>
                        Recarregar para revisão
                      </button>
                      {state.reviewReady &&
                        !state.detail.loading &&
                        state.detail.data && (
                          <button
                            className="secondary"
                            onClick={state.acknowledge}
                          >
                            Revisei o registro atual
                          </button>
                        )}
                    </>
                  )}
                </details>
              </>
            )}
            {!active && (
              <p className="notice">
                Registro preservado para consulta e histórico.
              </p>
            )}
          </>
        )}
      </section>
      {compact && (
        <details className="organization-disclosure">
          <summary>Histórico de alterações</summary>
          <p className="hint">Versão {version}</p>
          {state.history.loading && <p role="status">Carregando histórico…</p>}
          {state.history.error && (
            <Failure error={state.history.error} retry={state.refresh} />
          )}
          {state.history.data && (
            <AuditHistory data={state.history.data} change={state.setPage} />
          )}
        </details>
      )}
      {!compact && state.history.loading && (
        <p role="status">Carregando histórico…</p>
      )}
      {!compact && state.history.error && (
        <Failure error={state.history.error} retry={state.refresh} />
      )}
      {!compact && state.history.data && (
        <AuditHistory data={state.history.data} change={state.setPage} />
      )}
    </>
  );
}
