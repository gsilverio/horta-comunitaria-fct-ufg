import { useCallback } from "react";
import { RecordDetail } from "../../../components/RecordDetail";
import { useRecord } from "../../../hooks/useRecord";
import { useRemote } from "../../../hooks/useRemote";
import { navigate } from "../../../hooks/useRoute";
import type { RecordRoute } from "../../../types/records";
import type { Garden } from "../types/Garden";
import { gardenApi } from "../services/GardenApi";
import { GardenForm, GardenReview } from "../components/GardenForm";
import { CatalogStatusFilter } from "../components/CatalogStatusFilter";
import {
  CreateLink,
  OrganizationHeading,
  OrganizationTrail,
} from "../components/OrganizationLayout";
import { useOrganizationList } from "../hooks/useOrganizationList";
import { GardenDataGrid } from "../components/GardenDataGrid";
import { SectorCollection } from "./SectorCollection";
import { routeHref } from "../../../hooks/useRoute";
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
        <OrganizationTrail current="Nova horta" withGarden={false} />
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
  const destination = navigationItems.find(item => item.id !== 'horta' && item.id === route.search.get('nextModule'));
  const { page, pageSize, isActive, status, revision, retry, changePage, changePageSize, changeStatus } = useOrganizationList(route);
  const load = useCallback((signal: AbortSignal) => gardenApi.list({ page, pageSize, isActive }, signal), [page, pageSize, isActive]);
  const result = useRemote(page + '/' + pageSize + '/' + status + '/' + revision, load);
  return <>
    <OrganizationHeading title="Hortas" description={destination ? 'Escolha uma horta para acessar ' + destination.label + '.' : 'Gerencie os dados, a situação e a configuração das suas hortas.'} action={<CreateLink href={routeHref('', 'horta', 'hortas', 'novo')}>Nova horta</CreateLink>} />
    <GardenDataGrid {...result} page={page} pageSize={pageSize} changePage={changePage} changePageSize={changePageSize} retry={retry} filter={<CatalogStatusFilter value={status} change={changeStatus} />} destination={destination ? { id: destination.id, label: destination.label, resource: destination.items[0].id } : undefined} />
  </>;
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
  const back = () => navigate('', 'horta', 'hortas');
  const openDetail = () => navigate(route.garden || route.id, 'horta', 'hortas', route.id);
  if (route.search.get("edit") === "1" && record)
    return (
      <>
        <OrganizationTrail current="Editar horta" />
        <GardenForm
          garden={route.garden}
          original={record}
          saved={() => {
            saved("Horta atualizada.");
            openDetail();
          }}
          cancel={openDetail}
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
        deactivationInitiallyOpen={route.search.get('inactivate') === '1'}
        hideBack
        eyebrow="Horta"
        actions={record?.isActive && route.garden === record.id ? <><a className="action-link secondary" href={routeHref(record.id, 'horta', 'interativa')}>Abrir mapa</a><CreateLink href={routeHref(record.id, 'horta', 'setores', 'novo')}>Novo setor</CreateLink></> : undefined}
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
        sections={record && route.garden === record.id ? [{ id: "setores", label: "Setores", content: <SectorCollection route={route} /> }] : []}
      />
    </>
  );
}
