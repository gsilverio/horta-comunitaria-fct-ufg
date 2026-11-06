import { type HTMLAttributes, type ReactNode } from 'react';
import { type Column } from '@tanstack/react-table';
import {
  ArrowDown,
  ArrowLeft,
  ArrowLeftToLine,
  ArrowRight,
  ArrowRightToLine,
  ArrowUp,
  Check,
  ChevronsUpDown,
  PinOff,
  Settings2,
} from 'lucide-react';
import { useDataGrid } from './data-grid-context';
import { cn } from '../libs/utils';
import { DropdownMenu, DropdownMenuCheckboxItem, DropdownMenuContent, DropdownMenuItem, DropdownMenuLabel, DropdownMenuPortal, DropdownMenuSeparator, DropdownMenuSub, DropdownMenuSubContent, DropdownMenuSubTrigger, DropdownMenuTrigger } from '../ui/dropdown-menu';

interface DataGridColumnHeaderProps<TData, TValue>
  extends HTMLAttributes<HTMLDivElement> {
  column: Column<TData, TValue>;
  title?: string;
  icon?: ReactNode;
  pinnable?: boolean;
  filter?: ReactNode;
  visibility?: boolean;
}

function DataGridColumnHeader<TData extends object, TValue>({
  column,
  title = '',
  icon,
  className,
  filter,
  visibility = false,
}: DataGridColumnHeaderProps<TData, TValue>) {
  "use no memo";
  const { isLoading, table, props, recordCount } = useDataGrid<TData>();

  const moveColumn = (direction: 'left' | 'right') => {
    const currentOrder = [...table.getState().columnOrder];
    const currentIndex = currentOrder.indexOf(column.id);

    if (direction === 'left' && currentIndex > 0) {
      const newOrder = [...currentOrder];
      const [moved] = newOrder.splice(currentIndex, 1);
      newOrder.splice(currentIndex - 1, 0, moved);
      table.setColumnOrder(newOrder);
    }

    if (direction === 'right' && currentIndex < currentOrder.length - 1) {
      const newOrder = [...currentOrder];
      const [moved] = newOrder.splice(currentIndex, 1);
      newOrder.splice(currentIndex + 1, 0, moved);
      table.setColumnOrder(newOrder);
    }
  };

  const canMove = (direction: 'left' | 'right') => {
    const currentOrder = table.getState().columnOrder;
    const currentIndex = currentOrder.indexOf(column.id);
    return direction === 'left'
      ? currentIndex > 0
      : currentIndex < currentOrder.length - 1;
  };

  const SortIcon = () => {
    if (!column.getCanSort()) return null;
    return column.getIsSorted() === 'desc' ? (
      <ArrowDown className="size-3.5 shrink-0 opacity-60" />
    ) : column.getIsSorted() === 'asc' ? (
      <ArrowUp className="size-3.5 shrink-0 opacity-60" />
    ) : (
      <ChevronsUpDown className="size-3.5 shrink-0 opacity-60" />
    );
  };

  const HeaderTrigger = () => (
    <button
      type="button"
      disabled={isLoading || recordCount === 0}
      className={cn(
        'group flex h-full w-full min-w-0 items-center justify-center rounded-md',
        'text-horta-ink/80 font-semibold text-[0.8125rem] leading-tight',
        'hover:bg-horta-canvas/60 data-[state=open]:bg-white',
        'px-1 py-0.5',
        className,
      )}
    >
      <span className="inline-flex min-w-0 max-w-full items-center justify-center gap-1">
        {icon}
        <span className="min-w-0 text-center whitespace-normal break-words [overflow-wrap:anywhere] leading-tight">
          {title}
        </span>
        <SortIcon />
      </span>
    </button>
  );

  const headerPin = () => (
    <button
      type="button"
      className="-me-1 size-7 rounded-md hover:bg-horta-canvas/60 flex items-center justify-center"
      onClick={(e) => {
        e.preventDefault();
        e.stopPropagation();
        column.pin(false);
      }}
      aria-label={`Unpin ${title} column`}
      title={`Unpin ${title} column`}
    >
      <PinOff className="size-3.5 opacity-50" aria-hidden="true" />
    </button>
  );

  const hasMenu =
    !!filter ||
    column.getCanSort() ||
    (props.tableLayout?.columnsPinnable && column.getCanPin()) ||
    props.tableLayout?.columnsMovable ||
    (props.tableLayout?.columnsVisibility && visibility);

  if (!hasMenu) {
    return (
      <div
        className={cn(
          'inline-flex h-full w-full min-w-0 items-center justify-center gap-1 text-center text-horta-ink/80 font-normal text-[0.8125rem] leading-tight',
          className,
        )}
      >
        {icon}
        <span className="min-w-0 whitespace-normal break-words [overflow-wrap:anywhere]">
          {title}
        </span>
      </div>
    );
  }

  return (
    <div className="flex h-full w-full min-w-0 items-center justify-center gap-1">
      <DropdownMenu>
        <DropdownMenuTrigger asChild>{HeaderTrigger()}</DropdownMenuTrigger>

        {/* FIX: bg + z-index explícito (resolve transparência) */}
        <DropdownMenuPortal>
          <DropdownMenuContent
            className="w-44 bg-[#e3e3e3] text-horta-ink border shadow-md z-50"
            align="start"
          >
            {filter && <DropdownMenuLabel>{filter}</DropdownMenuLabel>}

            {filter &&
              (column.getCanSort() || column.getCanPin() || visibility) && (
                <DropdownMenuSeparator />
              )}

            {column.getCanSort() && (
              <>
                <DropdownMenuItem
                  onClick={() => {
                    if (column.getIsSorted() === 'asc') column.clearSorting();
                    else column.toggleSorting(false);
                  }}
                >
                  <ArrowUp className="size-3.5" />
                  <span className="grow">Asc</span>
                  {column.getIsSorted() === 'asc' && (
                    <Check className="size-4 opacity-100 text-horta-forest" />
                  )}
                </DropdownMenuItem>

                <DropdownMenuItem
                  onClick={() => {
                    if (column.getIsSorted() === 'desc') column.clearSorting();
                    else column.toggleSorting(true);
                  }}
                >
                  <ArrowDown className="size-3.5" />
                  <span className="grow">Desc</span>
                  {column.getIsSorted() === 'desc' && (
                    <Check className="size-4 opacity-100 text-horta-forest" />
                  )}
                </DropdownMenuItem>
              </>
            )}

            {(filter || column.getCanSort()) &&
              (column.getCanSort() || column.getCanPin() || visibility) && (
                <DropdownMenuSeparator />
              )}

            {props.tableLayout?.columnsPinnable && column.getCanPin() && (
              <>
                <DropdownMenuItem
                  onClick={() =>
                    column.pin(column.getIsPinned() === 'left' ? false : 'left')
                  }
                >
                  <ArrowLeftToLine className="size-3.5" />
                  <span className="grow">Pin to left</span>
                  {column.getIsPinned() === 'left' && (
                    <Check className="size-4 opacity-100 text-horta-forest" />
                  )}
                </DropdownMenuItem>

                <DropdownMenuItem
                  onClick={() =>
                    column.pin(
                      column.getIsPinned() === 'right' ? false : 'right',
                    )
                  }
                >
                  <ArrowRightToLine className="size-3.5" />
                  <span className="grow">Pin to right</span>
                  {column.getIsPinned() === 'right' && (
                    <Check className="size-4 opacity-100 text-horta-forest" />
                  )}
                </DropdownMenuItem>
              </>
            )}

            {props.tableLayout?.columnsMovable && (
              <>
                <DropdownMenuSeparator />
                <DropdownMenuItem
                  onClick={() => moveColumn('left')}
                  disabled={!canMove('left') || column.getIsPinned() !== false}
                >
                  <ArrowLeft className="size-3.5" />
                  <span>Move to Left</span>
                </DropdownMenuItem>
                <DropdownMenuItem
                  onClick={() => moveColumn('right')}
                  disabled={!canMove('right') || column.getIsPinned() !== false}
                >
                  <ArrowRight className="size-3.5" />
                  <span>Move to Right</span>
                </DropdownMenuItem>
              </>
            )}

            {props.tableLayout?.columnsVisibility && visibility && (
              <>
                <DropdownMenuSeparator />
                <DropdownMenuSub>
                  <DropdownMenuSubTrigger>
                    <Settings2 className="size-3.5" />
                    <span>Columns</span>
                  </DropdownMenuSubTrigger>
                  <DropdownMenuPortal>
                    <DropdownMenuSubContent className="bg-white text-horta-ink border shadow-md z-50">
                      {table
                        .getAllColumns()
                        .filter(
                          (col) =>
                            typeof col.accessorFn !== 'undefined' &&
                            col.getCanHide(),
                        )
                        .map((col) => (
                          <DropdownMenuCheckboxItem
                            key={col.id}
                            checked={col.getIsVisible()}
                            onSelect={(event) => event.preventDefault()}
                            onCheckedChange={(value) =>
                              col.toggleVisibility(!!value)
                            }
                            className="capitalize"
                          >
                            {col.columnDef.meta?.headerTitle || col.id}
                          </DropdownMenuCheckboxItem>
                        ))}
                    </DropdownMenuSubContent>
                  </DropdownMenuPortal>
                </DropdownMenuSub>
              </>
            )}
          </DropdownMenuContent>
        </DropdownMenuPortal>
      </DropdownMenu>

      {props.tableLayout?.columnsPinnable &&
        column.getCanPin() &&
        column.getIsPinned() &&
        headerPin()}
    </div>
  );
}

export { DataGridColumnHeader, type DataGridColumnHeaderProps };
