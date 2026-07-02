import type { KeyboardEvent } from "react";

export function focusFirstNativeMenuItem(menu: HTMLElement | null): void {
  findNativeMenuItems(menu)[0]?.focus();
}

export function handleNativeMenuKeyboard(event: KeyboardEvent<HTMLElement>): void {
  if (!["ArrowDown", "ArrowUp", "Home", "End"].includes(event.key)) {
    return;
  }

  const items = findNativeMenuItems(event.currentTarget);
  if (items.length === 0) {
    return;
  }

  event.preventDefault();
  const currentIndex = items.indexOf(document.activeElement as HTMLElement);
  const lastIndex = items.length - 1;
  let nextIndex = currentIndex;

  if (event.key === "Home") {
    nextIndex = 0;
  } else if (event.key === "End") {
    nextIndex = lastIndex;
  } else if (event.key === "ArrowDown") {
    nextIndex = currentIndex >= 0 ? (currentIndex + 1) % items.length : 0;
  } else if (event.key === "ArrowUp") {
    nextIndex = currentIndex >= 0 ? (currentIndex - 1 + items.length) % items.length : lastIndex;
  }

  items[nextIndex]?.focus();
}

function findNativeMenuItems(menu: HTMLElement | null): HTMLElement[] {
  return Array.from(menu?.querySelectorAll<HTMLElement>('[role="menuitem"]') ?? []).filter(
    (item) => !item.matches(":disabled"),
  );
}
