import { type ReactNode } from "react";
import { type Table } from "@tanstack/react-table";
import {
  DropdownMenu,
  DropdownMenuCheckboxItem,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "../ui/dropdown-menu";

function DataGridColumnVisibility<TData extends object>({
  table,
  trigger,
  onReset,
}: {
  table: Table<TData>;
  trigger: ReactNode;
  onReset?: () => void;
}) {
  "use no memo";
  const hideableColumns = table
    .getAllLeafColumns()
    .filter((column) => column.getCanHide());

  const visibleHideableCount = hideableColumns.filter((column) =>
    column.getIsVisible(),
  ).length;

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>{trigger}</DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="min-w-[150px] bg-white">
        <DropdownMenuLabel className="font-medium">
          Alternar Colunas
        </DropdownMenuLabel>
        {table
          .getAllColumns()
          .filter(
            (column) =>
              typeof column.accessorFn !== "undefined" && column.getCanHide(),
          )
          .map((column) => {
            return (
              <DropdownMenuCheckboxItem
                key={column.id}
                className=""
                checked={column.getIsVisible()}
                onSelect={(event) => event.preventDefault()}
                onCheckedChange={(value) => column.toggleVisibility(!!value)}
                disabled={column.getIsVisible() && visibleHideableCount === 1}
              >
                {column.columnDef.meta?.headerTitle || column.id}
              </DropdownMenuCheckboxItem>
            );
          })}
        {onReset && (
          <>
            <DropdownMenuSeparator />

            <DropdownMenuItem
              onSelect={(event) => {
                event.preventDefault();
                onReset();
              }}
            >
              Restaurar padrão
            </DropdownMenuItem>
          </>
        )}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

export { DataGridColumnVisibility };
