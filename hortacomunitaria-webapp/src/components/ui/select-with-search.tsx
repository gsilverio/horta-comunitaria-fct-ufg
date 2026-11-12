import { Check, ChevronsUpDown, Search } from "lucide-react";
import { PlantLoader } from "../PlantLoader";

import { cn } from "../libs/utils";
import { Input } from "./input";
import { Popover, PopoverContent, PopoverTrigger } from "./popover";
import { type ReactNode, useEffect, useMemo, useRef, useState, type WheelEvent } from "react";

export interface SelectWithSearchOption<TData = unknown> {
  value: string;
  label: ReactNode;
  description?: ReactNode;
  searchText?: string;
  disabled?: boolean;
  data?: TData;
}

interface SelectWithSearchProps<TData = unknown> {
  label?: string;
  value?: string;
  options: SelectWithSearchOption<TData>[];
  placeholder?: string;
  searchPlaceholder?: string;
  emptyMessage?: string;
  loading?: boolean;
  disabled?: boolean;
  helperText?: string | null;
  error?: string | null;
  /** Reserva altura fixa para helper/erro, evitando deslocar o alinhamento. */
  reserveMessageSpace?: boolean;
  className?: string;
  triggerClassName?: string;
  contentClassName?: string;
  autoSelectSingleOption?: boolean;
  lockWhenSingleOption?: boolean;
  singleOptionHelperText?: string | null;
  onValueChange: (value: string, option: SelectWithSearchOption<TData>) => void;
  renderOption?: (
    option: SelectWithSearchOption<TData>,
    selected: boolean,
  ) => ReactNode;
  renderValue?: (option: SelectWithSearchOption<TData>) => ReactNode;
}

function normalizeSearch(value: unknown) {
  return String(value ?? "")
    .toLowerCase()
    .normalize("NFD")
    .replace(/\p{Diacritic}/gu, "");
}

export function SelectWithSearch<TData = unknown>({
  label,
  value,
  options,
  placeholder = "Selecione",
  searchPlaceholder = "Buscar...",
  emptyMessage = "Nenhum resultado encontrado.",
  loading = false,
  disabled = false,
  helperText,
  error,
  reserveMessageSpace = false,
  className,
  triggerClassName,
  contentClassName,
  autoSelectSingleOption = false,
  lockWhenSingleOption = false,
  singleOptionHelperText = "Selecionado automaticamente porque existe apenas uma opção disponível.",
  onValueChange,
  renderOption,
  renderValue,
}: SelectWithSearchProps<TData>) {
  const [open, setOpen] = useState(false);
  const [search, setSearch] = useState("");

  const selectedOption = useMemo(() => {
    return options.find((option) => option.value === value) ?? null;
  }, [options, value]);

  const selectableOptions = useMemo(() => {
    return options.filter((option) => !option.disabled);
  }, [options]);

  const shouldLockSingleOption =
    lockWhenSingleOption && !loading && selectableOptions.length === 1;

  const effectiveDisabled = disabled || shouldLockSingleOption;

  const autoSelectedValueRef = useRef<string | null>(null);

  useEffect(() => {
    if (!autoSelectSingleOption || loading || selectableOptions.length !== 1) {
      autoSelectedValueRef.current = null;
      return;
    }

    const onlyOption = selectableOptions[0];

    if (value === onlyOption.value) {
      return;
    }

    if (autoSelectedValueRef.current === onlyOption.value) {
      return;
    }

    autoSelectedValueRef.current = onlyOption.value;
    onValueChange(onlyOption.value, onlyOption);
  }, [
    autoSelectSingleOption,
    loading,
    selectableOptions,
    value,
    onValueChange,
  ]);

  const filteredOptions = useMemo(() => {
    const query = normalizeSearch(search.trim());

    if (!query) {
      return options;
    }

    return options.filter((option) => {
      const searchable = normalizeSearch(
        [
          option.value,
          option.searchText,
          typeof option.label === "string" ? option.label : "",
          typeof option.description === "string" ? option.description : "",
        ]
          .filter(Boolean)
          .join(" "),
      );

      return searchable.includes(query);
    });
  }, [options, search]);

  function handleOpenChange(nextOpen: boolean) {
    setOpen(nextOpen);

    if (!nextOpen) {
      setSearch("");
    }
  }

  function handleSelect(option: SelectWithSearchOption<TData>) {
    if (option.disabled) return;

    onValueChange(option.value, option);
    setOpen(false);
    setSearch("");
  }

  function handleOptionsWheel(event: WheelEvent<HTMLDivElement>) {
    event.stopPropagation();

    const node = event.currentTarget;

    if (!event.defaultPrevented || node.scrollHeight <= node.clientHeight) {
      return;
    }

    node.scrollTop += event.deltaY;
  }

  return (
    <div className={cn("space-y-1.5", className)}>
      {label && (
        <label className="block text-xs font-semibold text-slate-600">
          {label}
        </label>
      )}

      <Popover modal open={open} onOpenChange={handleOpenChange}>
        <PopoverTrigger asChild>
          <button
            type="button"
            role="combobox"
            aria-expanded={open}
            disabled={effectiveDisabled}
            className={cn(
              `
              flex h-10 w-full items-center justify-between gap-1.5 rounded-md
              border border-slate-200 bg-slate-50 px-4 text-left text-sm
              font-normal text-horta-ink shadow-none transition-colors
              hover:bg-white
              focus-visible:border-horta-focus focus-visible:outline-none
              focus-visible:ring-[3px] focus-visible:ring-horta-focus/30
              disabled:cursor-not-allowed disabled:opacity-50
              data-[state=open]:bg-white
            `,
              shouldLockSingleOption &&
                "bg-slate-100 text-slate-700 opacity-100 hover:bg-slate-100 disabled:opacity-100",
              error && "border-red-300 focus-visible:ring-red-300/30",
              triggerClassName,
            )}
          >
            <span
              className={cn(
                "min-w-0 flex-1 truncate",
                !selectedOption && "text-horta-muted",
                shouldLockSingleOption &&
                  selectedOption &&
                  "font-medium text-slate-700",
              )}
            >
              {selectedOption
                ? renderValue
                  ? renderValue(selectedOption)
                  : selectedOption.label
                : loading
                  ? "Carregando..."
                  : placeholder}
            </span>

            <ChevronsUpDown className="h-4 w-4 shrink-0 opacity-60" />
          </button>
        </PopoverTrigger>

        <PopoverContent
          align="start"
          onWheel={(event) => event.stopPropagation()}
          onTouchMove={(event) => event.stopPropagation()}
          className={cn(
            `
              z-[60] w-[var(--radix-popover-trigger-width)] min-w-[260px]
              border-slate-200 bg-white p-2 shadow-md
            `,
            contentClassName,
          )}
        >
          <div className="relative mb-2">
            <Search className="pointer-events-none absolute left-2.5 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />

            <Input
              autoFocus
              value={search}
              disabled={loading}
              placeholder={searchPlaceholder}
              onChange={(event) => setSearch(event.target.value)}
              className="
                h-9 rounded-md border-slate-200 bg-slate-50 pl-8 text-sm
                shadow-none transition-colors hover:bg-white focus:bg-white
                focus-visible:ring-1 focus-visible:ring-horta-forest
              "
            />
          </div>

          <div
            className="max-h-64 overflow-y-auto overscroll-contain pr-1"
            onWheel={handleOptionsWheel}
          >
            {loading && (
              <div className="flex items-center justify-center py-5 text-sm text-slate-500">
                <PlantLoader className="mr-2 size-4" />
                Carregando...
              </div>
            )}

            {!loading && filteredOptions.length === 0 && (
              <div className="py-5 text-center text-sm text-slate-500">
                {emptyMessage}
              </div>
            )}

            {!loading &&
              filteredOptions.map((option) => {
                const selected = option.value === value;

                return (
                  <button
                    key={option.value}
                    type="button"
                    disabled={option.disabled}
                    onClick={() => handleSelect(option)}
                    className={cn(
                      `
                        flex w-full items-center justify-between gap-3 rounded-md
                        px-2.5 py-2 text-left text-sm transition-colors
                        hover:bg-[#e3e3e3]
                        disabled:cursor-not-allowed disabled:opacity-50
                      `,
                      selected && "bg-horta-forest/10",
                    )}
                  >
                    <div className="min-w-0 flex-1">
                      {renderOption ? (
                        renderOption(option, selected)
                      ) : (
                        <div className="flex min-w-0 flex-col">
                          <span className="truncate font-medium text-slate-800">
                            {option.label}
                          </span>

                          {option.description && (
                            <span className="truncate text-xs text-slate-500">
                              {option.description}
                            </span>
                          )}
                        </div>
                      )}
                    </div>

                    {selected && (
                      <Check className="h-4 w-4 shrink-0 text-horta-forest" />
                    )}
                  </button>
                );
              })}
          </div>
        </PopoverContent>
      </Popover>

      {error ? (
        <p className="min-h-4 text-xs leading-4 text-red-600">{error}</p>
      ) : helperText || (shouldLockSingleOption && singleOptionHelperText) ? (
        <p className="min-h-4 text-xs leading-4 text-slate-500">
          {helperText || singleOptionHelperText}
        </p>
      ) : reserveMessageSpace ? (
        <p className="min-h-4 text-xs leading-4" aria-hidden>
          &nbsp;
        </p>
      ) : null}
    </div>
  );
}
