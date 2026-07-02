import "@testing-library/jest-dom/vitest";
import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { App } from "./App";

describe("App", () => {
  it("renders the WebView shell workspace", async () => {
    render(<App />);

    expect(screen.getByRole("heading", { name: "uMeter" })).toBeInTheDocument();
    expect(await screen.findByText("1 source tracked")).toBeInTheDocument();
    expect(screen.getByRole("region", { name: "Status and controls" })).toBeInTheDocument();
    expect(screen.getByRole("region", { name: "Recent events" })).toBeInTheDocument();
    expect(screen.getByRole("region", { name: "Sources" })).toBeInTheDocument();
  });
});
