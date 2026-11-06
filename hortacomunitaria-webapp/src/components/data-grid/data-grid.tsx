import type { ReactNode } from "react";
import type { Table } from "@tanstack/react-table";
import { cn } from "../libs/utils";
import { DataGridContext } from "./data-grid-context";
import type { DataGridProps } from "./data-grid-context";
export type { DataGridProps, DataGridApiFetchParams, DataGridApiResponse, DataGridRequestParams } from "./data-grid-context";

function DataGridProvider<TData extends object>({
  children,
  table,
  ...props
}: DataGridProps<TData> & { table: Table<TData> }) {
  "use no memo";
  return (
    <DataGridContext.Provider
      value={{
        props,
        table,
        recordCount: props.recordCount,
        isLoading: props.isLoading || false,
      }}
    >
      {children}
    </DataGridContext.Provider>
  );
}

function DataGrid<TData extends object>({
  children,
  table,
  ...props
}: DataGridProps<TData>) {
  "use no memo";
  const defaultProps: Partial<DataGridProps<TData>> = {
    loadingMode: "skeleton",
    tableLayout: {
      fitContentWidth: false,
      dense: false,
      cellBorder: false,
      rowBorder: true,
      rowRounded: false,
      stripped: false,
      headerSticky: false,
      headerBackground: true,
      headerBorder: true,
      width: "fixed",
      columnsVisibility: false,
      columnsResizable: false,
      columnsPinnable: false,
      columnsMovable: false,
      columnsDraggable: false,
      rowsDraggable: false,
    },
    tableClassNames: {
      base: "",
      header: "",
      headerRow: "",
      headerSticky: "sticky top-0 z-10 bg-white/90 backdrop-blur-xs",
      body: "",
      bodyRow: "",
      footer: "",
      edgeCell: "",
    },
  };

  const mergedProps: DataGridProps<TData> = {
    ...defaultProps,
    ...props,
    tableLayout: {
      ...defaultProps.tableLayout,
      ...(props.tableLayout || {}),
    },
    tableClassNames: {
      ...defaultProps.tableClassNames,
      ...(props.tableClassNames || {}),
    },
  };

  // Ensure table is provided
  if (!table) {
    throw new Error('DataGrid requires a "table" prop');
  }

  return (
    <DataGridProvider table={table} {...mergedProps}>
      {children}
    </DataGridProvider>
  );
}

function DataGridContainer({
  children,
  className,
  border = true,
}: {
  children: ReactNode;
  className?: string;
  border?: boolean;
}) {
  "use no memo";
  return (
    <div
      data-slot="data-grid"
      className={cn(
        "relative w-full h-[60dvh] overflow-auto rounded-md",
        border && "border border-horta-line",
        className,
      )}
    >
      {children}
    </div>
  );
}

export { DataGridProvider, DataGrid, DataGridContainer };
