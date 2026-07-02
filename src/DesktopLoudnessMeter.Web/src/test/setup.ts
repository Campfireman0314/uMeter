import "@testing-library/jest-dom/vitest";

if (!HTMLCanvasElement.prototype.getContext.toString().includes("[native code]")) {
  HTMLCanvasElement.prototype.getContext = function getContext(contextId: string) {
    if (contextId !== "2d") {
      return null;
    }

    return {
      beginPath() {},
      clearRect() {},
      fill() {},
      fillRect() {},
      lineTo() {},
      moveTo() {},
      stroke() {},
      arc() {},
      set fillStyle(_value: string) {},
      set lineWidth(_value: number) {},
      set strokeStyle(_value: string) {},
    } as unknown as CanvasRenderingContext2D;
  } as typeof HTMLCanvasElement.prototype.getContext;
}
