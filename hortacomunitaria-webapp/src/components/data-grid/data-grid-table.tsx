import * as React from "react";
import { type CSSProperties, Fragment, type ReactNode } from "react";
import {
  type Cell,
  type Column,
  flexRender,
  type Header,
  type HeaderGroup,
  type Row,
} from "@tanstack/react-table";
import { cva } from "class-variance-authority";
import { useDataGrid } from "./data-grid-context";
import { cn } from "../libs/utils";
import { Checkbox } from "../ui/checkbox";
import { PlantLoader } from "../PlantLoader";
const headerCellSpacingVariants = cva("", {
  variants: {
    size: {
      dense: "px-2.5 h-8",
      default: "px-4",
    },
  },
  defaultVariants: {
    size: "default",
  },
});

const bodyCellSpacingVariants = cva("", {
  variants: {
    size: {
      dense: "px-2.5 py-1",
      default: "px-4 py-3",
    },
  },
  defaultVariants: {
    size: "default",
  },
});

function getPinningStyles<TData extends object>(column: Column<TData>): CSSProperties {
  const isPinned = column.getIsPinned();

  return {
    left: isPinned === "left" ? `${column.getStart("left")}px` : undefined,

    right: isPinned === "right" ? `${column.getAfter("right")}px` : undefined,

    position: isPinned ? "sticky" : "relative",
    width: column.getSize(),
    zIndex: isPinned ? 2 : 0,
  };
}

function DataGridTableBase({ children }: { children: ReactNode }) {
  "use no memo";
  const { props, table } = useDataGrid();

  return (
    <table
      data-slot="data-grid-table"
      className={cn(
        "w-full align-middle caption-bottom text-left rtl:text-right text-horta-ink font-normal text-sm",
        !props.tableLayout?.columnsDraggable &&
          "border-separate border-spacing-0",
        props.tableLayout?.width === "fixed" ? "table-fixed" : "table-auto",
        props.tableClassNames?.base,
      )}
      style={
        props.tableLayout?.fitContentWidth
          ? {
              width: `${table.getTotalSize()}px`,
              minWidth: "100%",
            }
          : undefined
      }
    >
      {children}
    </table>
  );
}

function DataGridTableHead({ children }: { children: ReactNode }) {
  "use no memo";
  const { props } = useDataGrid();

  return (
    <thead
      className={cn(
        props.tableClassNames?.header,
        props.tableLayout?.headerSticky && props.tableClassNames?.headerSticky,
      )}
    >
      {children}
    </thead>
  );
}

function DataGridTableHeadRow<TData extends object>({
  children,
  headerGroup,
}: {
  children: ReactNode;
  headerGroup: HeaderGroup<TData>;
}) {
  "use no memo";
  const { props } = useDataGrid<TData>();

  return (
    <tr
      key={headerGroup.id}
      className={cn(
        "bg-horta-canvas/40",
        props.tableLayout?.headerBorder && "[&>th]:border-b",
        props.tableLayout?.cellBorder && "[&_>:last-child]:border-e-0",
        props.tableLayout?.stripped && "bg-transparent",
        props.tableLayout?.headerBackground === false && "bg-transparent",
        props.tableClassNames?.headerRow,
      )}
    >
      {children}
    </tr>
  );
}

function DataGridTableHeadRowCell<TData extends object>({
  children,
  header,
  dndRef,
  dndStyle,
}: {
  children: ReactNode;
  header: Header<TData, unknown>;
  dndRef?: React.Ref<HTMLTableCellElement>;
  dndStyle?: CSSProperties;
}) {
  "use no memo";
  const { props } = useDataGrid<TData>();

  const { column } = header;
  const isPinned = column.getIsPinned();
  const isLastLeftPinned =
    isPinned === "left" && column.getIsLastColumn("left");
  const isFirstRightPinned =
    isPinned === "right" && column.getIsFirstColumn("right");
  const headerCellSpacing = headerCellSpacingVariants({
    size: props.tableLayout?.dense ? "dense" : "default",
  });

  return (
    <th
      key={header.id}
      ref={dndRef}
      colSpan={header.colSpan}
      style={{
        ...(props.tableLayout?.width === "fixed" && {
          width: `${header.getSize()}px`,
        }),
        ...(props.tableLayout?.columnsPinnable &&
          column.getCanPin() &&
          getPinningStyles(column)),
        ...(dndStyle ? dndStyle : null),
      }}
      data-pinned={isPinned || undefined}
      data-last-col={
        isLastLeftPinned ? "left" : isFirstRightPinned ? "right" : undefined
      }
      className={cn(
        "relative h-10 text-left rtl:text-right align-middle font-normal text-horta-ink/80 [&:has([role=checkbox])]:pe-0",
        headerCellSpacing,
        column.id === "select" && "!px-0",
        props.tableLayout?.cellBorder && "border-e",
        props.tableLayout?.columnsResizable &&
          column.getCanResize() &&
          "overflow-visible pe-3",
        props.tableLayout?.columnsPinnable &&
          column.getCanPin() &&
          "[&:not([data-pinned]):has(+[data-pinned])_div.cursor-col-resize:last-child]:opacity-0 [&[data-last-col=left]_div.cursor-col-resize:last-child]:opacity-0 [&[data-pinned=right]:last-child_div.cursor-col-resize:last-child]:opacity-0",
        header.column.columnDef.meta?.headerClassName,
        column.getIndex() === 0 ||
          column.getIndex() === header.headerGroup.headers.length - 1
          ? props.tableClassNames?.edgeCell
          : "",
      )}
    >
      {children}
    </th>
  );
}

function DataGridTableHeadRowCellResize<TData extends object>({
  header,
}: {
  header: Header<TData, unknown>;
}) {
  "use no memo";
  const { column } = header;

  return (
    <div
      {...{
        onDoubleClick: () => column.resetSize(),
        onMouseDown: header.getResizeHandler(),
        onTouchStart: header.getResizeHandler(),
        className: `absolute top-0 h-full w-4 cursor-col-resize
          select-none touch-none
          -end-2 z-20 flex justify-center
          before:absolute before:w-px before:inset-y-0 before:bg-horta-line before:-translate-x-px`,
      }}
    />
  );
}

function DataGridTableRowSpacer() {
  "use no memo";
  return <tbody aria-hidden="true" className="h-2"></tbody>;
}

function DataGridTableBody({ children }: { children: ReactNode }) {
  "use no memo";
  const { props } = useDataGrid();

  return (
    <tbody
      className={cn(
        "[&_tr:last-child]:border-0 ",
        props.tableLayout?.rowRounded &&
          "[&_td:first-child]:rounded-s-lg [&_td:last-child]:rounded-e-lg",
        props.tableClassNames?.body,
      )}
    >
      {children}
    </tbody>
  );
}

function DataGridTableBodyRowSkeleton({ children }: { children: ReactNode }) {
  "use no memo";
  const { table, props } = useDataGrid();
  const hasSelectionColumn = table
    .getVisibleLeafColumns()
    .some((column) => column.id === "select");

  return (
    <tr
      className={cn(
        "hover:bg-horta-canvas/40 data-[state=selected]:bg-horta-canvas/50",
        props.onRowClick && "cursor-pointer",
        !props.tableLayout?.stripped &&
          props.tableLayout?.rowBorder &&
          "border-b border-horta-line [&:not(:last-child)>td]:border-b",
        props.tableLayout?.cellBorder && "[&_>:last-child]:border-e-0",
        props.tableLayout?.stripped &&
          "odd:bg-horta-canvas/90 hover:bg-transparent odd:hover:bg-horta-canvas",
        (table.options.enableRowSelection || hasSelectionColumn) &&
          "[&_>:first-child]:relative",
        props.tableClassNames?.bodyRow,
      )}
    >
      {children}
    </tr>
  );
}

function DataGridTableBodyRowSkeletonCell<TData extends object>({
  children,
  column,
}: {
  children: ReactNode;
  column: Column<TData>;
}) {
  "use no memo";
  const { props, table } = useDataGrid<TData>();

  const isPinned = column.getIsPinned();

  const bodyCellSpacing = bodyCellSpacingVariants({
    size: props.tableLayout?.dense ? "dense" : "default",
  });

  return (
    <td
      style={{
        ...(props.tableLayout?.columnsPinnable &&
          column.getCanPin() &&
          getPinningStyles(column)),
      }}
      data-pinned={isPinned || undefined}
      className={cn(
        "align-middle",
        bodyCellSpacing,

        isPinned && `bg-[#ececec] border-b border-white`,

        props.tableLayout?.cellBorder && "border-e",

        props.tableLayout?.columnsResizable &&
          column.getCanResize() &&
          "truncate",

        column.columnDef.meta?.cellClassName,

        column.getIndex() === 0 ||
          column.getIndex() === table.getVisibleFlatColumns().length - 1
          ? props.tableClassNames?.edgeCell
          : "",
      )}
    >
      {children}
    </td>
  );
}

function DataGridTableBodyRow<TData extends object>({
  children,
  row,
  dndRef,
  dndStyle,
}: {
  children: ReactNode;
  row: Row<TData>;
  dndRef?: React.Ref<HTMLTableRowElement>;
  dndStyle?: CSSProperties;
}) {
  "use no memo";
  const { props, table } = useDataGrid<TData>();
  const hasSelectionColumn = table
    .getVisibleLeafColumns()
    .some((column) => column.id === "select");

  return (
    <tr
      ref={dndRef}
      style={{ ...(dndStyle ? dndStyle : null) }}
      data-state={
        table.options.enableRowSelection && row.getIsSelected()
          ? "selected"
          : undefined
      }
      onClick={() => props.onRowClick && props.onRowClick(row.original)}
      className={cn(
        "group hover:bg-horta-canvas/40 data-[state=selected]:bg-horta-canvas/50",
        props.onRowClick && "cursor-pointer",
        !props.tableLayout?.stripped &&
          props.tableLayout?.rowBorder &&
          "border-b border-horta-line [&:not(:last-child)>td]:border-b",
        props.tableLayout?.cellBorder && "[&_>:last-child]:border-e-0",
        props.tableLayout?.stripped &&
          "odd:bg-horta-canvas/90 hover:bg-[#e3e3e3] odd:hover:bg-[#e3e3e3]",
        (table.options.enableRowSelection || hasSelectionColumn) &&
          "[&_>:first-child]:relative",
        props.tableClassNames?.bodyRow,
      )}
    >
      {children}
    </tr>
  );
}

function DataGridTableBodyRowExpandded<TData extends object>({ row }: { row: Row<TData> }) {
  "use no memo";
  const { props, table } = useDataGrid<TData>();

  return (
    <tr
      className={cn(
        props.tableLayout?.rowBorder && "[&:not(:last-child)>td]:border-b",
      )}
    >
      <td colSpan={row.getVisibleCells().length}>
        {table
          .getAllColumns()
          .find((column) => column.columnDef.meta?.expandedContent)
          ?.columnDef.meta?.expandedContent?.(row.original)}
      </td>
    </tr>
  );
}

function DataGridTableBodyRowCell<TData extends object>({
  children,
  cell,
  dndRef,
  dndStyle,
}: {
  children: ReactNode;
  cell: Cell<TData, unknown>;
  dndRef?: React.Ref<HTMLTableCellElement>;
  dndStyle?: CSSProperties;
}) {
  "use no memo";
  const { props } = useDataGrid<TData>();

  const { column, row } = cell;
  const isPinned = column.getIsPinned();
  const isLastLeftPinned =
    isPinned === "left" && column.getIsLastColumn("left");
  const isFirstRightPinned =
    isPinned === "right" && column.getIsFirstColumn("right");
  const bodyCellSpacing = bodyCellSpacingVariants({
    size: props.tableLayout?.dense ? "dense" : "default",
  });

  return (
    <td
      key={cell.id}
      ref={dndRef}
      {...(props.tableLayout?.columnsDraggable && !isPinned ? { cell } : {})}
      style={{
        ...(props.tableLayout?.columnsPinnable &&
          column.getCanPin() &&
          getPinningStyles(column)),
        ...(dndStyle ? dndStyle : null),
      }}
      data-pinned={isPinned || undefined}
      data-last-col={
        isLastLeftPinned ? "left" : isFirstRightPinned ? "right" : undefined
      }
      className={cn(
        "align-middle",
        bodyCellSpacing,

        props.tableLayout?.cellBorder && "border-e",

        props.tableLayout?.columnsResizable &&
          column.getCanResize() &&
          "truncate",

        cell.column.columnDef.meta?.cellClassName,

        isPinned && `bg-[#ececec] border-b border-white`,

        column.getIndex() === 0 ||
          column.getIndex() === row.getVisibleCells().length - 1
          ? props.tableClassNames?.edgeCell
          : "",
      )}
    >
      {children}
    </td>
  );
}

function DataGridTableEmpty() {
  "use no memo";
  const { table, props } = useDataGrid();
  const totalColumns = table.getVisibleFlatColumns().length;

  return (
    <tr>
      <td
        colSpan={totalColumns}
        className="text-center text-horta-muted py-6"
      >
        {props.emptyMessage || "Nenhum registro encontrado"}
      </td>
    </tr>
  );
}

function DataGridTableLoader() {
  "use no memo";
  const { props } = useDataGrid();

  return (
    <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2">
      <div role="status" className="flex items-center rounded-md border bg-white p-2 shadow-xs">
        <PlantLoader className="size-10" />
        <span className="sr-only">{props.loadingMessage || "Carregando…"}</span>
      </div>
    </div>
  );
}

function DataGridTableRowSelect<TData extends object>({
  row,
  selected,
  size,
  disabled,
  onCheckedChange,
  checkboxClassName,
}: {
  row: Row<TData>;
  selected?: boolean;
  size?: "sm" | "md" | "lg";
  disabled?: boolean;
  onCheckedChange?: (checked: boolean) => void;
  checkboxClassName?: string;
}) {
  "use no memo";
  const isSelected = selected ?? row.getIsSelected();

  return (
    <>
      <div
        className={cn(
          "hidden absolute top-0 bottom-0 start-0 w-[2px] bg-horta-forest",
          isSelected && "block",
        )}
      ></div>
      <Checkbox
        checked={isSelected}
        disabled={disabled}
        onCheckedChange={(value) => {
          const checked = value === true;

          if (onCheckedChange) {
            onCheckedChange(checked);
            return;
          }

          row.toggleSelected(checked);
        }}
        aria-label="Select row"
        size={size ?? "sm"}
        className={cn("align-[inherit]", checkboxClassName)}
      />
    </>
  );
}

function DataGridTableRowSelectAll({ size }: { size?: "sm" | "md" | "lg" }) {
  "use no memo";
  const { table, recordCount, isLoading } = useDataGrid();

  return (
    <Checkbox
      checked={
        table.getIsAllPageRowsSelected() ||
        (table.getIsSomePageRowsSelected() && "indeterminate")
      }
      disabled={isLoading || recordCount === 0}
      onCheckedChange={(value) => table.toggleAllPageRowsSelected(!!value)}
      aria-label="Select all"
      size={size}
      className="align-[inherit]"
    />
  );
}

function DataGridTable<TData extends object>() {
  "use no memo";
  const { table, isLoading, props } = useDataGrid<TData>();
  const pagination = table.getState().pagination;

  return (
    <DataGridTableBase>
      <DataGridTableHead>
        {table
          .getHeaderGroups()
          .map((headerGroup: HeaderGroup<TData>, index) => {
            return (
              <DataGridTableHeadRow headerGroup={headerGroup} key={index}>
                {headerGroup.headers.map((header, index) => {
                  const { column } = header;

                  return (
                    <DataGridTableHeadRowCell header={header} key={index}>
                      {header.isPlaceholder ? null : (
                        <div
                          className={cn(
                            props.tableLayout?.columnsResizable &&
                              column.getCanResize()
                              ? "truncate"
                              : "",
                          )}
                        >
                          {flexRender(
                            header.column.columnDef.header,
                            header.getContext(),
                          )}
                        </div>
                      )}
                      {props.tableLayout?.columnsResizable &&
                        column.getCanResize() && (
                          <DataGridTableHeadRowCellResize header={header} />
                        )}
                    </DataGridTableHeadRowCell>
                  );
                })}
              </DataGridTableHeadRow>
            );
          })}
      </DataGridTableHead>

      {(props.tableLayout?.stripped || !props.tableLayout?.rowBorder) && (
        <DataGridTableRowSpacer />
      )}

      <DataGridTableBody>
        {props.loadingMode === "skeleton" &&
        isLoading &&
        pagination?.pageSize ? (
          Array.from({ length: pagination.pageSize }).map((_, rowIndex) => (
            <DataGridTableBodyRowSkeleton key={rowIndex}>
              {table.getVisibleFlatColumns().map((column, colIndex) => {
                return (
                  <DataGridTableBodyRowSkeletonCell
                    column={column}
                    key={colIndex}
                  >
                    {column.columnDef.meta?.skeleton}
                  </DataGridTableBodyRowSkeletonCell>
                );
              })}
            </DataGridTableBodyRowSkeleton>
          ))
        ) : table.getRowModel().rows.length ? (
          table.getRowModel().rows.map((row: Row<TData>, index) => {
            return (
              <Fragment key={row.id}>
                <DataGridTableBodyRow row={row} key={index}>
                  {row
                    .getVisibleCells()
                    .map((cell: Cell<TData, unknown>, colIndex) => {
                      return (
                        <DataGridTableBodyRowCell cell={cell} key={colIndex}>
                          {flexRender(
                            cell.column.columnDef.cell,
                            cell.getContext(),
                          )}
                        </DataGridTableBodyRowCell>
                      );
                    })}
                </DataGridTableBodyRow>
                {row.getIsExpanded() && (
                  <DataGridTableBodyRowExpandded row={row} />
                )}
              </Fragment>
            );
          })
        ) : (
          <DataGridTableEmpty />
        )}
      </DataGridTableBody>
    </DataGridTableBase>
  );
}

export {
  DataGridTable,
  DataGridTableBase,
  DataGridTableBody,
  DataGridTableBodyRow,
  DataGridTableBodyRowCell,
  DataGridTableBodyRowExpandded,
  DataGridTableBodyRowSkeleton,
  DataGridTableBodyRowSkeletonCell,
  DataGridTableEmpty,
  DataGridTableHead,
  DataGridTableHeadRow,
  DataGridTableHeadRowCell,
  DataGridTableHeadRowCellResize,
  DataGridTableLoader,
  DataGridTableRowSelect,
  DataGridTableRowSelectAll,
  DataGridTableRowSpacer,
};
