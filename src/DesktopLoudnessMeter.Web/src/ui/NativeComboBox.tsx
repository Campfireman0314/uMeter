import { useEffect, useId, useMemo, useRef, useState, type KeyboardEvent } from "react";

export interface NativeComboBoxOption {
  value: string;
  label: string;
}

interface NativeComboBoxProps {
  ariaLabel: string;
  value: string;
  options: readonly NativeComboBoxOption[];
  onChange: (value: string) => void;
  className?: string;
  disabled?: boolean;
}

export function NativeComboBox({
  ariaLabel,
  value,
  options,
  onChange,
  className,
  disabled = false,
}: NativeComboBoxProps) {
  const id = useId();
  const rootRef = useRef<HTMLSpanElement | null>(null);
  const [isOpen, setIsOpen] = useState(false);
  const selectedIndex = Math.max(
    0,
    options.findIndex((option) => option.value === value),
  );
  const [highlightedIndex, setHighlightedIndex] = useState(selectedIndex);
  const selectedOption = options[selectedIndex] ?? options[0];
  const activeOptionId = `${id}-option-${highlightedIndex}`;

  useEffect(() => {
    if (isOpen) {
      setHighlightedIndex(selectedIndex);
    }
  }, [isOpen, selectedIndex]);

  useEffect(() => {
    if (!isOpen) {
      return undefined;
    }

    function handlePointerDown(event: PointerEvent): void {
      if (rootRef.current?.contains(event.target as Node)) {
        return;
      }

      setIsOpen(false);
    }

    window.addEventListener("pointerdown", handlePointerDown);
    return () => window.removeEventListener("pointerdown", handlePointerDown);
  }, [isOpen]);

  const optionIds = useMemo(() => options.map((_, index) => `${id}-option-${index}`), [id, options]);

  function selectOption(index: number): void {
    const option = options[index];
    if (!option) {
      return;
    }

    onChange(option.value);
    setIsOpen(false);
  }

  function moveHighlight(delta: number): void {
    if (!options.length) {
      return;
    }

    setHighlightedIndex((current) => (current + delta + options.length) % options.length);
  }

  function handleKeyDown(event: KeyboardEvent<HTMLButtonElement>): void {
    if (event.key === "Escape") {
      if (isOpen) {
        event.preventDefault();
        setIsOpen(false);
      }
      return;
    }

    if (event.key === "Enter" || event.key === " ") {
      event.preventDefault();
      if (isOpen) {
        selectOption(highlightedIndex);
      } else {
        setIsOpen(true);
      }
      return;
    }

    if (event.key === "ArrowDown") {
      event.preventDefault();
      if (!isOpen) {
        setIsOpen(true);
      } else {
        moveHighlight(1);
      }
      return;
    }

    if (event.key === "ArrowUp") {
      event.preventDefault();
      if (!isOpen) {
        setIsOpen(true);
      } else {
        moveHighlight(-1);
      }
      return;
    }

    if (event.key === "Home") {
      event.preventDefault();
      setIsOpen(true);
      setHighlightedIndex(0);
      return;
    }

    if (event.key === "End") {
      event.preventDefault();
      setIsOpen(true);
      setHighlightedIndex(Math.max(0, options.length - 1));
    }
  }

  return (
    <span ref={rootRef} className={`native-combo${isOpen ? " native-combo-open" : ""}${className ? ` ${className}` : ""}`}>
      <button
        type="button"
        className="native-combo-button"
        role="combobox"
        aria-label={ariaLabel}
        aria-expanded={isOpen}
        aria-haspopup="listbox"
        aria-controls={`${id}-listbox`}
        aria-activedescendant={isOpen ? activeOptionId : undefined}
        disabled={disabled}
        onClick={() => setIsOpen((open) => !open)}
        onKeyDown={handleKeyDown}
      >
        <span className="native-combo-value">{selectedOption?.label ?? ""}</span>
        <span className="native-combo-arrow" aria-hidden="true" />
      </button>
      {isOpen ? (
        <span id={`${id}-listbox`} className="native-combo-popup" role="listbox" aria-label={ariaLabel}>
          {options.map((option, index) => (
            <span
              id={optionIds[index]}
              key={option.value}
              className={`native-combo-option${index === highlightedIndex ? " native-combo-option-highlighted" : ""}`}
              role="option"
              aria-selected={option.value === value}
              onMouseEnter={() => setHighlightedIndex(index)}
              onMouseDown={(event) => event.preventDefault()}
              onClick={() => selectOption(index)}
            >
              {option.label}
            </span>
          ))}
        </span>
      ) : null}
    </span>
  );
}
