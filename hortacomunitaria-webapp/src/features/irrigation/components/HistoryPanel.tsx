import { Pagination } from "../../../components/Feedback";
import {
  formatInstant,
  formatNumber,
  formatVolume,
  statusLabel,
} from "../formValues";
import type { Page } from "../../../types/api";
import type {
  CatalogItem,
  HistoryEntry,
  Participant,
  Snapshot,
} from "../types/irrigation";

const operationLabel = {
  Created: "Inclusão",
  Corrected: "Correção",
  Invalidated: "Invalidação",
};
function SnapshotFacts({
  value,
  sectors,
  participants,
}: {
  value: Snapshot;
  sectors: CatalogItem[];
  participants: Participant[];
}) {
  return (
    <dl className="facts">
      <div>
        <dt>Volume</dt>
        <dd>{formatVolume(value.volumeLiters)}</dd>
      </div>
      <div>
        <dt>Setor</dt>
        <dd>
          {sectors.find((sector) => sector.id === value.sectorId)?.name ??
            value.sectorId}
        </dd>
      </div>
      <div>
        <dt>Início / fim (São Paulo)</dt>
        <dd>
          {formatInstant(value.startedAt)} /{" "}
          {value.endedAt ? formatInstant(value.endedAt) : "Não informado"}
        </dd>
      </div>
      <div>
        <dt>Duração ativa / vazão</dt>
        <dd>
          {value.activeDurationMinutes === null
            ? "Não informada"
            : `${formatNumber(value.activeDurationMinutes)} min`}{" "}
          /{" "}
          {value.flowRateLitersPerMinute === null
            ? "Não informada"
            : `${formatNumber(value.flowRateLitersPerMinute)} L/min`}
        </dd>
      </div>
      <div>
        <dt>Origem / método</dt>
        <dd>
          {value.volumeSource === "Estimated"
            ? "Estimado"
            : value.volumeSource === "Measured"
              ? "Medido"
              : "Não informada"}{" "}
          / {value.volumeMethod ?? "Não informado"}
        </dd>
      </div>
      <div>
        <dt>Situação / validade / versão</dt>
        <dd>
          {statusLabel(value.status)} ·{" "}
          {value.validity === "Valid" ? "Válido" : "Invalidado"} · v
          {value.version}
        </dd>
      </div>
      <div>
        <dt>Responsável</dt>
        <dd>
          {value.responsibleParticipantId
            ? (participants.find(
                (person) => person.id === value.responsibleParticipantId,
              )?.displayName ?? value.responsibleParticipantId)
            : "Não informado"}
        </dd>
      </div>
      <div>
        <dt>Canteiros / vínculos preservados</dt>
        <dd>
          {value.beds.length} canteiros
          <details>
            <summary>Ver referências históricas</summary>
            {value.beds.map((bed) => (
              <p key={bed.bedId}>
                <code>{bed.bedId}</code>
                <br />
                <code>{bed.assignmentId}</code>
              </p>
            ))}
          </details>
        </dd>
      </div>
      <div>
        <dt>Observações</dt>
        <dd>{value.notes ?? "Não informadas"}</dd>
      </div>
    </dl>
  );
}
export function HistoryPanel({
  data,
  changePage,
  sectors,
  participants,
}: {
  data: Page<HistoryEntry>;
  changePage: (page: number) => void;
  sectors: CatalogItem[];
  participants: Participant[];
}) {
  return (
    <section className="panel" aria-labelledby="history-title">
      <span className="eyebrow">Rastreabilidade</span>
      <h2 id="history-title">Histórico do registro</h2>
      <ol className="timeline">
        {data.items.map((entry) => (
          <li key={entry.id}>
            <div className="history-head">
              <strong>{operationLabel[entry.operation]}</strong>
              <span>{formatInstant(entry.occurredAt)} · São Paulo</span>
            </div>
            <p>
              {entry.actor.displayName}
              {entry.actor.isDemonstration ? " · autoria demonstrativa" : ""}
            </p>
            {entry.reason && (
              <p>
                <strong>Motivo: </strong>
                {entry.reason}
              </p>
            )}
            <details>
              <summary>Conferir valores antes e depois</summary>
              <div className="grid grid-cols-1 gap-5 sm:grid-cols-2">
                <section>
                  <h3>Antes</h3>
                  {entry.before ? (
                    <SnapshotFacts
                      value={entry.before}
                      sectors={sectors}
                      participants={participants}
                    />
                  ) : (
                    <p>Primeiro registro, sem estado anterior.</p>
                  )}
                </section>
                <section>
                  <h3>Depois</h3>
                  <SnapshotFacts
                    value={entry.after}
                    sectors={sectors}
                    participants={participants}
                  />
                </section>
              </div>
            </details>
          </li>
        ))}
      </ol>
      <Pagination
        data={data}
        change={changePage}
        label="Paginação do histórico"
      />
    </section>
  );
}

