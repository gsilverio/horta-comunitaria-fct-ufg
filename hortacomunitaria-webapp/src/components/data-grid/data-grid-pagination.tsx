import { type ReactNode } from 'react';
import { ChevronLeftIcon, ChevronRightIcon } from 'lucide-react';

import { useDataGrid } from './data-grid-context';
import { Button } from '../ui/button';
import { cn } from '../libs/utils';
import { Skeleton } from '../ui/skeleton';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../ui/select';


interface DataGridPaginationProps {
  sizes?: number[];
  sizesInfo?: string;
  sizesLabel?: string;
  sizesDescription?: string;
  sizesSkeleton?: ReactNode;
  more?: boolean;
  moreLimit?: number;
  info?: string;
  infoSkeleton?: ReactNode;
  className?: string;
}

function DataGridPagination(props: DataGridPaginationProps) {
  "use no memo";
  const { table, recordCount, isLoading } = useDataGrid();

  const defaultProps: Partial<DataGridPaginationProps> = {
    sizes: [5, 10, 25, 50, 100, 500, 1000],
    sizesLabel: 'Show',
    sizesDescription: 'per page',
    sizesSkeleton: <Skeleton className="h-8 w-44" />,
    moreLimit: 5,
    more: false,
    info: `{from} - {to} de {count}`,
    infoSkeleton: <Skeleton className="h-8 w-60" />,
  };

  const mergedProps: DataGridPaginationProps = { ...defaultProps, ...props };

  const btnBaseClasses = 'size-7 p-0 text-sm';
  const btnArrowClasses = btnBaseClasses + ' rtl:transform rtl:rotate-180';
  const pageIndex = table.getState().pagination.pageIndex;
  const pageSize = table.getState().pagination.pageSize;
  const from = recordCount === 0 ? 0 : pageIndex * pageSize + 1;
  const to = Math.min((pageIndex + 1) * pageSize, recordCount);
  const pageCount = table.getPageCount();

  // Replace placeholders in paginationInfo
  const paginationInfo = mergedProps?.info
    ? mergedProps.info
      .replace('{from}', from.toString())
      .replace('{to}', to.toString())
      .replace('{count}', recordCount.toString())
    : `${from} - ${to} of ${recordCount}`;

  // Pagination limit logic
  const paginationMoreLimit = mergedProps?.moreLimit || 5;

  // Determine the start and end of the pagination group
  const currentGroupStart =
    Math.floor(pageIndex / paginationMoreLimit) * paginationMoreLimit;
  const currentGroupEnd = Math.min(
    currentGroupStart + paginationMoreLimit,
    pageCount,
  );

  // Render page buttons based on the current group
  const renderPageButtons = () => {
    const buttons = [];
    for (let i = currentGroupStart; i < currentGroupEnd; i++) {
      buttons.push(
        <Button
          key={i}
          aria-label={`Página ${i + 1}`}
          aria-current={pageIndex === i ? "page" : undefined}
          disabled={isLoading}
          size="sm"
          variant="ghost"
          className={cn(btnBaseClasses, 'text-horta-muted ', {
            'bg-horta-forest text-white rounded-full hover:bg-horta-forest': pageIndex === i,
          })}
          onClick={() => {
            if (pageIndex !== i) {
              table.setPageIndex(i);
            }
          }}
        >
          {i + 1}
        </Button>,
      );
    }
    return buttons;
  };

  // Render a "previous" ellipsis button if there are previous pages to show
  const renderEllipsisPrevButton = () => {
    if (currentGroupStart > 0) {
      return (
        <Button
          size="sm"
          className={btnBaseClasses}
          variant="ghost"
          onClick={() => table.setPageIndex(currentGroupStart - 1)}
        >
          ...
        </Button>
      );
    }
    return null;
  };

  // Render a "next" ellipsis button if there are more pages to show after the current group
  const renderEllipsisNextButton = () => {
    if (currentGroupEnd < pageCount) {
      return (
        <Button
          className={btnBaseClasses}
          variant="ghost"
          size="sm"
          onClick={() => table.setPageIndex(currentGroupEnd)}
        >
          ...
        </Button>
      );
    }
    return null;
  };

  return (
    <div
      data-slot="data-grid-pagination"
      className={cn(
        'flex flex-wrap flex-col sm:flex-row justify-between items-center gap-2.5 py-2.5 sm:py-0 grow',
        mergedProps?.className,
      )}
    >

      <div className={cn("flex flex-wrap items-center space-x-2.5 pb-2.5 sm:pb-0 order-2 sm:order-1", isLoading && "opacity-50 pointer-events-none")}>


        <>
          <div className="text-sm text-horta-muted">Itens por página</div>
          <Select
            value={`${pageSize}`}
            disabled={isLoading} // <--- Adicionado disabled
            indicatorPosition="right"
            onValueChange={(value) => {
              const newPageSize = Number(value);
              table.setPageSize(newPageSize);
            }}
          >
            <SelectTrigger aria-label="Itens por página" className="w-fit" size="sm">
              <SelectValue placeholder={`${pageSize}`} />
            </SelectTrigger>
            <SelectContent side="top" className="min-w-[50px] bg-white">
              {mergedProps?.sizes?.map((size: number) => (
                <SelectItem key={size} value={`${size}`}>
                  {size}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </>
      </div>


      <div className={cn("flex flex-col sm:flex-row justify-center sm:justify-end items-center gap-2.5 pt-2.5 sm:pt-0 order-1 sm:order-2", isLoading && "opacity-50 pointer-events-none")}>


        <>
          <div className="text-sm text-horta-muted text-nowrap order-2 sm:order-1">
            {paginationInfo}
          </div>
          {pageCount > 1 && (
            <div className="flex items-center space-x-1 order-1 sm:order-2">
              <Button
                size="sm"
                variant="ghost"
                className={btnArrowClasses}
                onClick={() => table.previousPage()}
                disabled={!table.getCanPreviousPage() || isLoading} // <--- Adicionado || isLoading
              >
                <span className="sr-only">Página anterior</span>
                <ChevronLeftIcon className="size-4" />
              </Button>

              {renderEllipsisPrevButton()}



              {renderPageButtons()}

              {renderEllipsisNextButton()}

              <Button
                size="sm"
                variant="ghost"
                className={btnArrowClasses}
                onClick={() => table.nextPage()}
                disabled={!table.getCanNextPage() || isLoading} // <--- Adicionado || isLoading
              >
                <span className="sr-only">Próxima página</span>
                <ChevronRightIcon className="size-4" />
              </Button>
            </div>
          )}
        </>
      </div>
    </div >
  );
}

export { DataGridPagination, type DataGridPaginationProps };
