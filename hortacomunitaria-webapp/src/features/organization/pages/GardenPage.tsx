import { useCallback } from "react";
import { RecordDetail } from "../../../components/RecordDetail";
import { useRecord } from "../../../hooks/useRecord";
import { useRemote } from "../../../hooks/useRemote";
import { navigate, routeHref } from "../../../hooks/useRoute";
import type { RecordRoute } from "../../../types/records";
import type { Garden } from "../types/Garden";
import { gardenApi } from "../services/GardenApi";
import { GardenForm, GardenReview } from "../components/GardenForm";
import { CatalogStatusFilter } from "../components/CatalogStatusFilter";
import {
  CreateLink,
  OrganizationCollection,
  OrganizationHeading,
  OrganizationRow,
  OrganizationTrail,
} from "../components/OrganizationLayout";
import { useOrganizationList } from "../hooks/useOrganizationList";
import { navigationItems } from "../../../components/navigationItems";

export function GardenPage({
  route,
  saved,
}: {
  route: RecordRoute;
  saved: (message: string) => void;
}) {
  if (route.id === "novo")
    return (
      <>
        <OrganizationTrail current="Nova horta" />
        <GardenForm
          garden=""
          saved={(record) => {
            saved("Horta cadastrada.");
            navigate(record.id, "horta", "interativa");
          }}
          cancel={() => navigate("", "horta", "hortas")}
        />
      </>
    );
  if (route.id) return <GardenDetail route={route} saved={saved} />;
  return <GardenList route={route} />;
}
function GardenList({ route }: { route: RecordRoute }) {
  const destination = navigationItems.find(
    (item) => item.id !== "horta" && item.id === route.search.get("nextModule"),
  );
  const { page, isActive, status, revision, retry, changePage, changeStatus } =
    useOrganizationList(route);
  const load = useCallback(
    (signal: AbortSignal) =>
      gardenApi.list({ page, pageSize: 10, isActive }, signal),
    [page, isActive],
  );
  const result = useRemote(`${page}/${status}/${revision}`, load);
  return (
    <>
      <OrganizationTrail />
      <OrganizationHeading
        eyebrow="Seu ponto de partida"
        title="Hortas"
        description={
          destination
            ? `Escolha uma horta para acessar ${destination.label}.`
            : "Escolha uma horta para organizar seus setores e canteiros."
        }
        action={
          <CreateLink href={routeHref("", "horta", "hortas", "novo")}>
            Nova horta
          </CreateLink>
        }
      />
      <div className="organization-path" aria-label="Organização dos cadastros">
        <span className="font-semibold text-horta-forest">1. Horta</span>
        <span aria-hidden="true">→</span>
        <span>2. Setores</span>
        <span aria-hidden="true">→</span>
        <span>3. Canteiros</span>
      </div>
      <OrganizationCollection
        title="Suas hortas"
        {...result}
        retry={retry}
        changePage={changePage}
        emptyTitle="Nenhuma horta por aqui"
        emptyText={
          status === "active"
            ? "Cadastre sua primeira horta. Depois, você poderá adicionar os setores e os canteiros."
            : "Não há hortas nesta situação. Altere a opção Mostrar para consultar as demais."
        }
        filter={<CatalogStatusFilter value={status} change={changeStatus} />}
      >
        {result.data?.items.map((item) => (
          <OrganizationRow
            key={item.id}
            title={item.name}
            description={item.location ?? "Localização não informada"}
            status={!item.isActive ? "Inativa" : undefined}
            href={routeHref(
              item.id,
              destination?.id ?? "horta",
              destination?.items[0].id ?? "interativa",
            )}
            action={destination ? `Abrir ${destination.label}` : "Configurar horta"}
          />
        ))}
      </OrganizationCollection>
    </>
  );
}
function GardenDetail({
  route,
  saved,
}: {
  route: RecordRoute;
  saved: (message: string) => void;
}) {
  const get = useCallback(
    (signal: AbortSignal) => gardenApi.get(route.id, signal),
    [route.id],
  );
  const audit = useCallback(
    (page: number, signal: AbortSignal) =>
      gardenApi.history(route.id, page, signal),
    [route.id],
  );
  const state = useRecord<Garden>(
    route.id,
    get,
    audit,
    (version, reason) => gardenApi.deactivate(route.id, version, reason),
    () => saved("Horta atualizada."),
  );
  const record = state.detail.data;
  const back = () => navigate(route.garden || route.id, "horta", "setores");
  if (route.search.get("edit") === "1" && record)
    return (
      <>
        <OrganizationTrail current="Editar horta" />
        <GardenForm
          garden={route.garden}
          original={record}
          saved={() => {
            saved("Horta atualizada.");
            back();
          }}
          cancel={back}
        />
      </>
    );
  return (
    <>
      <OrganizationTrail current="Dados da horta" />
      <RecordDetail
        compact
        requireActiveGarden={false}
        state={state}
        title={record?.name ?? "Horta"}
        version={record?.version ?? 0}
        active={record?.isActive === true}
        catalog
        back={back}
        edit={() =>
          navigate(
            route.garden || route.id,
            "horta",
            "hortas",
            route.id,
            new URLSearchParams({ edit: "1" }),
          )
        }
        items={
          record
            ? [
                {
                  label: "Dados da horta",
                  value: <GardenReview record={record} />,
                },
              ]
            : []
        }
      />
    </>
  );
}
