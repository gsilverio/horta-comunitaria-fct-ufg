import { useGardenState } from "../hooks/useGardenState";
import { isValidElement } from "react";
import type { ReactNode } from "react";
import type { DetailItem } from "../types/records";
import { Facts, Field } from "./Fields";
import { Failure } from "./Feedback";
import { AuditHistory } from "./AuditHistory";
import { PageHeader } from "./PageHeader";
import { RecordTabs } from "./RecordTabs";
import type { RecordTab } from "./RecordTabs";
import type { useRecord } from "../hooks/useRecord";
import { useCanManage } from "../features/auth/hooks/useAuth";

/**
 * Detalhe padrão: cabeçalho com a ação principal e abas do mesmo registro.
 * `sections` vêm primeiro (o conteúdo de trabalho); Dados, Histórico e Manutenção são comuns a todos.
 */
export function RecordDetail<T>({
  state,
  title,
  eyebrow,
  description,
  items,
  version,
  active,
  edit,
  back,
  children,
  heading,
  actions,
  banner,
  sections = [],
  catalog = false,
  requireActiveGarden = true,
  compact = false,
  hideBack = false,
  deactivationInitiallyOpen = false,
  attachments,
}: {
  /** Conteúdo da aba Fotos, exibida logo após Dados. */
  attachments?: ReactNode;
  compact?: boolean;
  hideBack?: boolean;
  deactivationInitiallyOpen?: boolean;
  state: ReturnType<typeof useRecord<T>>;
  title: string;
  eyebrow?: ReactNode;
  description?: ReactNode;
  items: DetailItem[];
  version: number;
  active: boolean;
  edit: () => void;
  back: () => void;
  children?: ReactNode;
  heading?: ReactNode;
  actions?: ReactNode;
  banner?: ReactNode;
  sections?: RecordTab[];
  catalog?: boolean;
  requireActiveGarden?: boolean;
}) {
  const garden = useGardenState();
  // Em estrutura e catálogos, editar e inativar são do perfil Adm; nas demais páginas, ambos os perfis alteram.
  const canManage = useCanManage();
  const canChange = canManage && active && (!requireActiveGarden || garden.isActive);
  const statusLabel = catalog ? (active ? "Ativo" : "Inativo") : active ? "Válido" : "Invalidado";
  const editButton = state.detail.data && canChange && (
    <button className="secondary" disabled={state.busy} onClick={edit}>
      {catalog ? "Editar cadastro" : "Corrigir registro"}
    </button>
  );
  const data: ReactNode = <div className="panel">
    {items.filter(item => isValidElement(item.value)).map(item => <div key={item.label}>{item.value}</div>)}
    {items.some(item => !isValidElement(item.value)) && <Facts items={items.filter(item => !isValidElement(item.value))} />}
    {children}
  </div>;
  const history: ReactNode = <div className="panel">
    <p className="hint">Versão atual: {version}</p>
    {state.history.loading && <p role="status">Carregando histórico…</p>}
    {state.history.error && <Failure error={state.history.error} retry={state.refresh} />}
    {state.history.data && <AuditHistory data={state.history.data} change={state.setPage} />}
  </div>;
  const maintenance: ReactNode = <section className="panel maintenance-section">
    <h2>{catalog ? "Inativar cadastro" : "Invalidar registro"}</h2>
    <p className="text-horta-muted">O histórico permanece disponível. Dependentes podem impedir a alteração.</p>
    {state.error && <Failure error={state.error} />}
    <Field name="reason" label="Motivo" required type="textarea" value={state.reason} change={state.setReason} error={state.error} maxLength={500} />
    <div className="mt-3 flex flex-wrap gap-2">
      <button className="danger" disabled={state.busy || state.error?.status === 409} onClick={() => void state.deactivate(version)}>
        Confirmar {catalog ? "inativação" : "invalidação"}
      </button>
      {state.error?.status === 409 && <button className="secondary" onClick={state.refresh}>Recarregar para revisão</button>}
      {state.error?.status === 409 && state.reviewReady && !state.detail.loading && state.detail.data && <button className="secondary" onClick={state.acknowledge}>Revisei o registro atual</button>}
    </div>
  </section>;
  const tabs: RecordTab[] = [
    ...sections,
    { id: "dados", label: "Dados", content: data },
    ...(attachments ? [{ id: "fotos", label: "Fotos", content: attachments }] : []),
    { id: "historico", label: "Histórico", content: history },
    ...(canChange ? [{ id: "manutencao", label: "Manutenção", content: maintenance }] : []),
  ];
  return (
    <>
      {!hideBack && <button className="text-button" onClick={back} disabled={state.busy}>← Voltar</button>}
      {state.notice && <p className="feedback success" role="status">{state.notice}</p>}
      {state.detail.data && heading ? (
        <>
          {heading}
          {(editButton || actions) && <div className="page-actions my-3 sm:justify-start">{actions}{editButton}</div>}
        </>
      ) : (
        <PageHeader
          eyebrow={eyebrow}
          title={title}
          status={state.detail.data && <span className={active ? "badge" : "badge invalid"}>{statusLabel}{!compact && ` · Versão ${version}`}</span>}
          description={description}
          actions={(editButton || actions) && <>{actions}{editButton}</>}
        />
      )}
      {state.detail.loading && <p role="status">Carregando registro…</p>}
      {state.detail.error && <Failure error={state.detail.error} retry={state.refresh} />}
      {state.detail.data && <>
        {!active && <p className="notice">Registro preservado para consulta e histórico.</p>}
        {banner}
        <RecordTabs key={sections.map(section => section.id).join("/")} label={`Seções de ${title}`} tabs={tabs} initial={deactivationInitiallyOpen ? "manutencao" : undefined} />
      </>}
    </>
  );
}
