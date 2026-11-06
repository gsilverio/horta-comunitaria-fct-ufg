import { type ReactNode, useCallback, useEffect, useRef, useState } from "react";
import { type Table } from "@tanstack/react-table";
import { ChevronDown, ChevronUp } from "lucide-react";

import {
  DropdownMenu,
  DropdownMenuCheckboxItem,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuTrigger,
} from "../ui/dropdown-menu";

interface DataGridColumnVisibilityProps<TData> {
  table: Table<TData>;
  trigger: ReactNode;
  onReset?: () => void;
}

function DataGridColumnVisibility<TData extends object>({
  table,
  trigger,
  onReset,
}: DataGridColumnVisibilityProps<TData>) {
  "use no memo";
  const [open, setOpen] = useState(false);
  const [canScrollUp, setCanScrollUp] = useState(false);
  const [canScrollDown, setCanScrollDown] = useState(false);

  const scrollContainerRef = useRef<HTMLDivElement>(null);

  const hideableColumns = table
    .getAllLeafColumns()
    .filter(
      (column) =>
        typeof column.accessorFn !== "undefined" && column.getCanHide(),
    );

  const visibleHideableCount = hideableColumns.filter((column) =>
    column.getIsVisible(),
  ).length;

  const updateScrollIndicators = useCallback(() => {
    const container = scrollContainerRef.current;

    if (!container) {
      setCanScrollUp(false);
      setCanScrollDown(false);
      return;
    }

    const scrollTop = container.scrollTop;
    const maxScrollTop = container.scrollHeight - container.clientHeight;

    setCanScrollUp(scrollTop > 2);
    setCanScrollDown(maxScrollTop - scrollTop > 2);
  }, []);

  useEffect(() => {
    if (!open) {
      return;
    }

    const frameId = window.requestAnimationFrame(() => {
      updateScrollIndicators();
    });

    window.addEventListener("resize", updateScrollIndicators);

    return () => {
      window.cancelAnimationFrame(frameId);
      window.removeEventListener("resize", updateScrollIndicators);
    };
  }, [open, hideableColumns.length, updateScrollIndicators]);

  function handleOpenChange(nextOpen: boolean) {
    setOpen(nextOpen);

    if (nextOpen) {
      window.requestAnimationFrame(updateScrollIndicators);
    }
  }

  function scrollColumns(direction: "up" | "down") {
    const container = scrollContainerRef.current;

    if (!container) {
      return;
    }

    container.scrollBy({
      top: direction === "up" ? -160 : 160,
      behavior: "smooth",
    });
  }

  return (
    <DropdownMenu modal={false} open={open} onOpenChange={handleOpenChange}>
      <DropdownMenuTrigger asChild>{trigger}</DropdownMenuTrigger>

      <DropdownMenuContent
        align="end"
        sideOffset={8}
        className="w-[240px] bg-white p-1"
      >
        <DropdownMenuLabel className="px-2 py-2 text-sm font-medium">
          Colunas visíveis
        </DropdownMenuLabel>

        {canScrollUp && (
          <button
            type="button"
            aria-label="Mostrar colunas anteriores"
            className="
              flex
              h-7
              w-full
              items-center
              justify-center
              rounded-sm
              text-slate-400
              transition-colors
              hover:bg-[#e3e3e3]
              hover:text-slate-700
            "
            onMouseDown={(event) => event.preventDefault()}
            onClick={(event) => {
              event.preventDefault();
              event.stopPropagation();
              scrollColumns("up");
            }}
          >
            <ChevronUp className="h-4 w-4" />
          </button>
        )}

        <div
          ref={scrollContainerRef}
          className="
            max-h-[280px]
            overflow-y-auto
            overscroll-contain
            py-0.5
            [scrollbar-width:none]
            [&::-webkit-scrollbar]:hidden
          "
          onScroll={updateScrollIndicators}
        >
          {hideableColumns.map((column) => {
            const label = String(
              column.columnDef.meta?.headerTitle ?? column.id,
            );

            const isVisible = column.getIsVisible();
            const isLastVisibleColumn = isVisible && visibleHideableCount === 1;

            return (
              <DropdownMenuCheckboxItem
                key={column.id}
                checked={isVisible}
                disabled={isLastVisibleColumn}
                className="
                  min-h-9
                  rounded-sm
                  py-1.5
                  pe-2
                  ps-8
                  text-sm
                  hover:bg-[#e3e3e3]
                "
                onSelect={(event) => {
                  event.preventDefault();
                }}
                onCheckedChange={(checked) => {
                  if (isLastVisibleColumn) {
                    return;
                  }

                  column.toggleVisibility(checked === true);
                }}
              >
                <span className="truncate" title={label}>
                  {label}
                </span>
              </DropdownMenuCheckboxItem>
            );
          })}
        </div>

        {canScrollDown && (
          <button
            type="button"
            aria-label="Mostrar próximas colunas"
            className="
              flex
              h-7
              w-full
              items-center
              justify-center
              rounded-sm
              text-slate-400
              transition-colors
              hover:bg-[#e3e3e3]
              hover:text-slate-700
            "
            onMouseDown={(event) => event.preventDefault()}
            onClick={(event) => {
              event.preventDefault();
              event.stopPropagation();
              scrollColumns("down");
            }}
          >
            <ChevronDown className="h-4 w-4" />
          </button>
        )}

        {onReset && (
          <DropdownMenuItem
            className="
              mt-1
              min-h-9
              justify-center
              rounded-sm
              text-sm
              font-medium
              text-horta-forest
              hover:bg-[#e3e3e3]
              focus:bg-[#e3e3e3]
            "
            onSelect={(event) => {
              event.preventDefault();
              onReset();

              window.requestAnimationFrame(updateScrollIndicators);
            }}
          >
            Restaurar padrão
          </DropdownMenuItem>
        )}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

export { DataGridColumnVisibility };
