import "@testing-library/jest-dom/vitest";
import { act, fireEvent, render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { readFileSync } from "node:fs";
import { join } from "node:path";
import { describe, expect, it, vi } from "vitest";
import { AppShell } from "./AppShell";
import type { HostCommandResultDto, HostSnapshotDto } from "./bridge/hostBridge";
import type { HostBridgeLike } from "./state/hostState";

describe("AppShell", () => {
  it("renders the native main-window layout with status, controls, recent events, and source card", async () => {
    const bridge = new FakeBridge(snapshotWithSources());

    render(<AppShell bridge={bridge} />);

    expect(await screen.findByText("1 source tracked")).toBeInTheDocument();
    expect(screen.getByRole("region", { name: "App chrome" })).toBeInTheDocument();
    expect(screen.getByRole("region", { name: "Status and controls" })).toBeInTheDocument();
    expect(screen.getByRole("region", { name: "Recent events" })).toBeInTheDocument();
    expect(screen.getByRole("region", { name: "Sources" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "uMeter" })).toBeInTheDocument();
    expect(screen.getByText("WASAPI / VST3 source metering")).toBeInTheDocument();
    expect(screen.getByText("33 ms")).toBeInTheDocument();
    expect(screen.getByRole("combobox", { name: "Mode" })).toHaveTextContent("Peak");
    expect(screen.getByRole("combobox", { name: "True Peak oversampling" })).toHaveTextContent("4x");
    expect(screen.getByRole("combobox", { name: "True Peak oversampling" })).toBeDisabled();
    expect(screen.getByRole("checkbox", { name: "Always on top" })).toBeChecked();
    expect(screen.getByRole("checkbox", { name: "Show hidden" })).not.toBeChecked();
    expect(screen.getByRole("button", { name: "Theme: Milk White" })).toHaveAttribute("title", "Theme: Milk White");
    expect(screen.getByRole("button", { name: "Settings" })).toHaveTextContent("\uE713");
    expect(screen.getByRole("button", { name: "Settings" })).toHaveAttribute("title", "Global settings");
    expect(screen.getByRole("button", { name: "Hide to tray" })).toHaveAttribute("title", "Hide to tray");
    expect(screen.getByRole("button", { name: "Close" })).toHaveAttribute("title", "Close");
    expect(screen.getByRole("button", { name: "Reset All" })).toBeInTheDocument();
    expect(within(screen.getByRole("region", { name: "Recent events" })).getByText("Source appeared")).toBeInTheDocument();
    const sources = screen.getByRole("region", { name: "Sources" });
    expect(within(sources).getByText("Lead Bus")).toBeInTheDocument();
    expect(within(sources).getByText("Hidden Reference")).toBeInTheDocument();
    expect(within(sources).getAllByText("Plugin sender")).toHaveLength(2);
    expect(within(getSourceCard("Lead Bus")).getByTitle("Drag to reorder")).toHaveTextContent("::");
    expect(within(sources).getByText("-6.0 dB")).toHaveClass("hot");
    expect(within(sources).getByText("-14.0 dB")).toHaveClass("normal");
    expect(within(getSourceCard("Lead Bus")).getByRole("button", { name: "Move source up" })).toHaveAttribute("title", "Move up");
    expect(within(getSourceCard("Lead Bus")).getByRole("button", { name: "Move source down" })).toHaveTextContent("\uE70D");
    expect(within(sources).getByRole("button", { name: "Restore" })).toBeInTheDocument();
    expect(screen.getByText("Per-process LUFS metrics warm up after active audio starts.")).toBeInTheDocument();
  });

  it("matches native SizeAll cursors on source and detail module drag handles", () => {
    const css = readFileSync(join(process.cwd(), "src", "styles", "app.css"), "utf8");

    expect(css).toMatch(/\.source-drag-handle\s*\{[^}]*cursor:\s*all-scroll;/s);
    expect(css).toMatch(/\.chart-module-drag-handle\s*\{[^}]*cursor:\s*all-scroll;/s);
  });

  it("reserves separate native source-card rows for meter and actions", () => {
    const css = readFileSync(join(process.cwd(), "src", "styles", "app.css"), "utf8");

    expect(css).toMatch(/\.source-card\s*\{[^}]*grid-template-rows:\s*0 auto 8px 8px 8px 24px 0;/s);
    expect(css).toMatch(/\.source-drop-before\s*\{[^}]*grid-template-rows:\s*18px auto 8px 8px 8px 24px 0;/s);
    expect(css).toMatch(/\.source-drop-after\s*\{[^}]*grid-template-rows:\s*0 auto 8px 8px 8px 24px 18px;/s);
    expect(css).toMatch(/\.meter-track\s*\{[^}]*margin:\s*0;/s);
  });

  it("uses native-style real scrolling for recent events", () => {
    const css = readFileSync(join(process.cwd(), "src", "styles", "app.css"), "utf8");

    expect(css).toMatch(/\.recent-card\s*\{[^}]*grid-template-rows:\s*26px 8px minmax\(0,\s*1fr\);/s);
    expect(css).toMatch(/\.recent-list\s*\{[^}]*overflow-y:\s*auto;/s);
    expect(css).not.toMatch(/\.recent-card::before/);
    expect(css).not.toMatch(/\.recent-card::after/);
  });

  it("matches native pointer cursors for disabled controls", () => {
    const css = readFileSync(join(process.cwd(), "src", "styles", "app.css"), "utf8");

    expect(css).toMatch(/button:disabled\s*\{[^}]*cursor:\s*pointer;/s);
    expect(css).toMatch(/\.native-combo-button:disabled\s*\{[^}]*cursor:\s*pointer;/s);
    expect(css).toMatch(/input\[type="checkbox"\]:disabled\s*\{[^}]*cursor:\s*pointer;/s);
    expect(css).toMatch(/\.chart-collision-source-button:disabled\s*\{[^}]*cursor:\s*pointer;/s);
    expect(css).not.toContain("cursor: not-allowed");
  });

  it("matches native cross cursor on the loudness clock chart", () => {
    const css = readFileSync(join(process.cwd(), "src", "styles", "app.css"), "utf8");

    expect(css).toMatch(/\.chart-module-loudnessClock\s+\.chart-canvas-wrap\s+canvas\s*\{[^}]*cursor:\s*crosshair;/s);
  });

  it("emphasizes critical native source readouts", async () => {
    const snapshot = snapshotWithSources();
    snapshot.sources = [
      {
        ...snapshot.sources[0],
        sourceKey: "plugin:critical",
        displayName: "Critical",
        effectiveDisplayName: "Critical",
        displayTitle: "Critical",
        sourceIdentityText: "plugin:critical",
        peakDb: -0.4,
        peakDbText: "-0.4 dB",
      },
      {
        ...snapshot.sources[0],
        sourceKey: "plugin:over",
        displayName: "Over",
        effectiveDisplayName: "Over",
        displayTitle: "Over",
        sourceIdentityText: "plugin:over",
        peakDb: 0.3,
        peakDbText: "+0.3 dB",
      },
    ];

    render(<AppShell bridge={new FakeBridge(snapshot)} />);

    expect(await screen.findByText("1 source tracked")).toBeInTheDocument();
    expect(screen.getByText("-0.4 dB")).toHaveClass("critical");
    expect(screen.getByText("+0.3 dB")).toHaveClass("over");
  });

  it("routes native main-window controls through the bridge", async () => {
    const bridge = new FakeBridge(snapshotWithSources());
    const user = userEvent.setup();

    render(<AppShell bridge={bridge} />);

    await screen.findByText("1 source tracked");
    await user.click(screen.getByRole("button", { name: "Settings" }));
    await user.click(screen.getByRole("button", { name: "Hide to tray" }));
    await user.click(screen.getByRole("button", { name: "Close" }));
    await user.click(screen.getByRole("checkbox", { name: "Always on top" }));
    await user.click(screen.getByRole("button", { name: "Theme: Milk White" }));
    await user.click(screen.getByRole("menuitem", { name: "Forest Green" }));
    await user.click(screen.getByRole("button", { name: "Reset All" }));
    await user.click(screen.getByRole("checkbox", { name: "Show hidden" }));
    await chooseNativeComboOption(user, "Mode", "LUFS");
    await user.click(screen.getByRole("checkbox", { name: "Enable true peak" }));
    expect(screen.getByRole("combobox", { name: "Mode" })).toHaveTextContent("LUFS");
    expect(within(getSourceCard("Lead Bus")).getByText("LUFS n/a")).toHaveClass("normal");
    expect(screen.getByRole("checkbox", { name: "Enable true peak" })).toBeChecked();
    expect(screen.getByRole("combobox", { name: "True Peak oversampling" })).toBeEnabled();
    await user.click(screen.getByRole("checkbox", { name: "Eco mode" }));
    await chooseNativeComboOption(user, "True Peak oversampling", "8x");
    const leadCard = getSourceCard("Lead Bus");
    await user.click(within(leadCard).getByRole("button", { name: "Hide" }));
    await user.click(within(leadCard).getByRole("button", { name: "Reset" }));
    await user.click(within(leadCard).getByRole("button", { name: "Pop-up" }));
    await user.click(within(leadCard).getByRole("button", { name: "Details" }));

    expect(bridge.commands.filter(([command]) => command !== "refresh")).toEqual([
      ["openGlobalSettings", undefined],
      ["hideToTray", undefined],
      ["closeWindow", undefined],
      ["setAlwaysOnTop", { isAlwaysOnTop: false }],
      ["setTheme", { themeId: "forest-green" }],
      ["resetAll", undefined],
      ["setShowHiddenSources", { showHiddenSources: true }],
      ["setDisplayMode", { displayMode: "lufs" }],
      ["setTruePeakEnabled", { truePeakEnabled: true }],
      ["setPerformanceMode", { performanceMode: "eco" }],
      [
        "setTruePeakOversampling",
        {
          sourceKey: "plugin:lead",
          sourceKind: "plugin",
          processId: null,
          truePeakOversampling: "eightX",
        },
      ],
      ["hideSource", { sourceKey: "plugin:lead" }],
      [
        "resetSource",
        {
          sourceKey: "plugin:lead",
          sourceKind: "plugin",
          processId: null,
        },
      ],
      ["openPopup", { sourceKey: "plugin:lead" }],
      ["selectDetail", { sourceKey: "plugin:lead" }],
    ]);
  });

  it("closes the native-style theme context menu with Escape and outside clicks", async () => {
    const bridge = new FakeBridge(snapshotWithSources());
    const user = userEvent.setup();

    render(<AppShell bridge={bridge} />);

    await screen.findByText("1 source tracked");
    await user.click(screen.getByRole("button", { name: "Theme: Milk White" }));
    expect(screen.getByRole("menu", { name: "Theme" })).toBeInTheDocument();
    expect(screen.getByRole("menuitem", { name: "Midnight" })).toHaveFocus();
    fireEvent.keyDown(screen.getByRole("menu", { name: "Theme" }), { key: "ArrowUp" });
    expect(screen.getByRole("menuitem", { name: "Pink Dream" })).toHaveFocus();
    fireEvent.keyDown(screen.getByRole("menu", { name: "Theme" }), { key: "ArrowDown" });
    expect(screen.getByRole("menuitem", { name: "Midnight" })).toHaveFocus();

    fireEvent.keyDown(window, { key: "Escape" });
    expect(screen.queryByRole("menu", { name: "Theme" })).not.toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Theme: Milk White" }));
    expect(screen.getByRole("menuitem", { name: "Milk White" })).not.toHaveAttribute("aria-checked");
    expect(screen.queryByRole("menuitemradio", { name: "Milk White" })).not.toBeInTheDocument();
    fireEvent.pointerDown(document.body);
    expect(screen.queryByRole("menu", { name: "Theme" })).not.toBeInTheDocument();
  });

  it("matches native title bar drag and double-click window commands", async () => {
    const bridge = new FakeBridge(snapshotWithSources());

    render(<AppShell bridge={bridge} />);

    await screen.findByText("1 source tracked");
    const chromeTitle = screen.getByText("uMeter").closest(".chrome-title");
    expect(chromeTitle).not.toBeNull();

    await act(async () => {
      fireEvent.mouseDown(chromeTitle!, { button: 0, detail: 1 });
      await Promise.resolve();
    });
    await act(async () => {
      fireEvent.mouseDown(chromeTitle!, { button: 0, detail: 2 });
      await Promise.resolve();
    });
    fireEvent.mouseDown(chromeTitle!, { button: 2, detail: 1 });

    expect(bridge.commands.filter(([command]) => command !== "refresh")).toEqual([
      ["beginWindowDrag", undefined],
      ["toggleWindowState", undefined],
    ]);
  });

  it("keeps native main-control captions separate from checkbox hit targets", async () => {
    const bridge = new FakeBridge(snapshotWithSources());
    const user = userEvent.setup();

    render(<AppShell bridge={bridge} />);

    await screen.findByText("1 source tracked");
    const ecoCheckbox = screen.getByRole("checkbox", { name: "Eco mode" });
    expect(ecoCheckbox).not.toBeChecked();

    await user.click(screen.getByText("Performance"));
    expect(ecoCheckbox).not.toBeChecked();

    await user.click(ecoCheckbox);
    expect(ecoCheckbox).toBeChecked();
    expect(bridge.commands.filter(([command]) => command !== "refresh")).toContainEqual([
      "setPerformanceMode",
      { performanceMode: "eco" },
    ]);
  });

  it("matches native combobox open, keyboard, and selected item states", async () => {
    const bridge = new FakeBridge(snapshotWithSources());
    const user = userEvent.setup();

    render(<AppShell bridge={bridge} />);

    await screen.findByText("1 source tracked");
    const mode = screen.getByRole("combobox", { name: "Mode" });
    await user.click(mode);

    expect(mode).toHaveAttribute("aria-expanded", "true");
    expect(screen.getByRole("listbox", { name: "Mode" })).toBeInTheDocument();
    expect(screen.getByRole("option", { name: "Peak" })).toHaveAttribute("aria-selected", "true");
    await act(async () => {
      fireEvent.keyDown(mode, { key: "ArrowDown" });
      await Promise.resolve();
    });
    await act(async () => {
      fireEvent.keyDown(mode, { key: "Enter" });
      await Promise.resolve();
    });

    expect(mode).toHaveTextContent("LUFS");
    expect(mode).toHaveAttribute("aria-expanded", "false");
    expect(screen.queryByRole("listbox", { name: "Mode" })).not.toBeInTheDocument();
    expect(bridge.commands.filter(([command]) => command === "setDisplayMode")).toContainEqual([
      "setDisplayMode",
      { displayMode: "lufs" },
    ]);
  });

  it("filters recent events with the native main-window options", async () => {
    const bridge = new FakeBridge(snapshotWithRecentEventKinds());
    const user = userEvent.setup();

    render(<AppShell bridge={bridge} />);

    await screen.findByText("3 source tracked");
    const recent = screen.getByRole("region", { name: "Recent events" });
    expect(screen.getByRole("combobox", { name: "Recent event filter" })).toHaveTextContent("All");
    expect(within(recent).getByText("Source appeared")).toBeInTheDocument();
    expect(within(recent).getByText("Loudness jump")).toBeInTheDocument();
    expect(within(recent).getByText("True peak risk")).toBeInTheDocument();
    expect(within(recent).getByText("Reset complete")).toBeInTheDocument();
    expect(within(recent).getByText("Frequency overlap")).toBeInTheDocument();
    expect(within(recent).getByText("Source appeared").closest("li")?.querySelector(".event-dot")).toHaveClass("muted");
    expect(within(recent).getByText("Loudness jump").closest("li")?.querySelector(".event-dot")).toHaveClass("hot");
    expect(within(recent).getByText("True peak risk").closest("li")?.querySelector(".event-dot")).toHaveClass("critical");
    expect(within(recent).getByText("Frequency overlap").closest("li")?.querySelector(".event-dot")).toHaveClass("over");

    await chooseNativeComboOption(user, "Recent event filter", "Loudness");
    expect(within(recent).queryByText("Source appeared")).not.toBeInTheDocument();
    expect(within(recent).getByText("Loudness jump")).toBeInTheDocument();

    await chooseNativeComboOption(user, "Recent event filter", "Peaks");
    expect(within(recent).queryByText("No matching events")).not.toBeInTheDocument();
    expect(within(recent).getByText("True peak risk")).toBeInTheDocument();
    expect(within(recent).queryByText("Loudness jump")).not.toBeInTheDocument();
  });

  it("edits source aliases from the native-style source title", async () => {
    const bridge = new FakeBridge(snapshotWithSources());
    const user = userEvent.setup();

    render(<AppShell bridge={bridge} />);

    await screen.findByText("1 source tracked");
    await user.dblClick(within(getSourceCard("Lead Bus")).getByText("Lead Bus"));
    const aliasInput = screen.getByLabelText("Alias for Lead");
    expect(aliasInput).toHaveValue("Lead Bus");

    await user.clear(aliasInput);
    await user.type(aliasInput, "Lead Mix{Enter}");

    expect(within(getSourceCard("Lead Mix")).getByText("Lead Mix")).toBeInTheDocument();
    expect(bridge.commands.filter(([command]) => command !== "refresh")).toContainEqual([
      "setSourceAlias",
      {
        sourceKey: "plugin:lead",
        sourceKind: "plugin",
        processId: null,
        alias: "Lead Mix",
      },
    ]);
  });

  it("switches to the native-style detail view and routes detail controls", async () => {
    const bridge = new FakeBridge(snapshotWithSources());
    const user = userEvent.setup();

    render(<AppShell bridge={bridge} />);

    await screen.findByText("1 source tracked");
    await user.click(within(getSourceCard("Lead Bus")).getByRole("button", { name: "Details" }));

    expect(screen.getByRole("region", { name: "Detail view" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Save detail layout" })).toHaveTextContent("\uE74E");
    expect(screen.getByRole("button", { name: "Load detail layout" })).toHaveTextContent("\uE8E5");
    expect(screen.getByRole("button", { name: "Waveform settings" })).toHaveTextContent("\uE713");
    expect(screen.getByRole("button", { name: "Waveform settings" })).toHaveAttribute("title", "Waveform settings");
    expect(screen.getByRole("button", { name: "Stereo settings" })).toHaveTextContent("\uE713");
    expect(screen.getByRole("button", { name: "Stereo settings" })).toHaveAttribute("title", "Stereo image scale settings");
    expect(screen.getByRole("button", { name: "Show Stereo" })).toHaveAttribute("title", "Show stereo image");
    expect(screen.getByRole("button", { name: "Show Loudness Clock" })).toHaveAttribute("title", "Show clock");
    expect(screen.getByRole("button", { name: "Show Frequency Collision" })).toHaveAttribute("title", "Show frequency collision");
    expect(screen.getByRole("heading", { name: "Lead Bus" })).toBeInTheDocument();
    expect(screen.getByText("plugin:lead")).toBeInTheDocument();
    expect(screen.getByText("Momentary loudness")).toBeInTheDocument();
    expect(screen.getByLabelText("True-peak level: -0.4dB")).toBeInTheDocument();
    expect(screen.getByText("-0.4dB")).toHaveClass("critical");
    expect(screen.getAllByText("-60")).toHaveLength(5);
    expect(screen.getByText("Waveform")).toBeInTheDocument();
    expect(screen.getByText("True Peak")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Reset" })).toBeInTheDocument();
    expect(screen.queryByRole("checkbox", { name: "Top" })).not.toBeInTheDocument();
    expect(screen.queryByLabelText("Waveform mode")).not.toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Forget" })).toHaveAttribute(
      "title",
      "Forget saved alias and detail settings for this source.",
    );

    await user.click(screen.getByRole("button", { name: "Save detail layout" }));
    await user.click(screen.getByRole("button", { name: "Load detail layout" }));
    await user.click(screen.getByRole("button", { name: "Stereo settings" }));
    await chooseNativeComboOption(user, "Detail true peak oversampling", "8x");
    await user.click(screen.getByRole("button", { name: "Waveform settings" }));
    await chooseNativeComboOption(user, "Waveform mode", "Channels");
    fireEvent.keyDown(window, { key: "Escape" });
    const waveformToggle = screen.getByRole("button", { name: "Show Waveform" });
    expect(waveformToggle).toHaveAttribute("aria-pressed", "true");
    await user.click(waveformToggle);
    expect(screen.getByRole("heading", { name: "Waveform" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Show Waveform" })).toHaveAttribute("aria-pressed", "false");
    expect(screen.getByRole("button", { name: "Waveform settings" })).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Waveform settings" }));
    expect(screen.getByLabelText("Waveform settings panel")).toBeInTheDocument();
    fireEvent.keyDown(window, { key: "Escape" });
    await user.click(screen.getByRole("button", { name: "Show Waveform" }));
    await user.click(screen.getByRole("button", { name: "Reset" }));
    await user.click(screen.getByRole("button", { name: "Forget" }));

    expect(bridge.commands.filter(([command]) => command !== "refresh")).toEqual([
      ["selectDetail", { sourceKey: "plugin:lead" }],
      ["saveDetailLayout", undefined],
      ["loadDetailLayout", undefined],
      ["openStereoSettings", undefined],
      [
        "setTruePeakOversampling",
        {
          sourceKey: "plugin:lead",
          sourceKind: "plugin",
          processId: null,
          truePeakOversampling: "eightX",
        },
      ],
      [
        "setWaveformDisplayMode",
        {
          sourceKey: "plugin:lead",
          sourceKind: "plugin",
          processId: null,
          waveformDisplayMode: "channels",
        },
      ],
      ["setDetailModuleVisibility", { sourceKey: "plugin:lead", moduleKind: "waveform", isVisible: false }],
      ["setDetailModuleVisibility", { sourceKey: "plugin:lead", moduleKind: "waveform", isVisible: true }],
      [
        "resetSource",
        {
          sourceKey: "plugin:lead",
          sourceKind: "plugin",
          processId: null,
        },
      ],
      ["forgetSource", { sourceKey: "plugin:lead" }],
    ]);
    expect(screen.getByRole("region", { name: "Sources" })).toBeInTheDocument();
  });

  it("cancels detail title alias editing with Escape like native", async () => {
    const bridge = new FakeBridge(snapshotWithSources());
    const user = userEvent.setup();

    render(<AppShell bridge={bridge} />);

    await screen.findByText("1 source tracked");
    await user.click(within(getSourceCard("Lead Bus")).getByRole("button", { name: "Details" }));
    await user.dblClick(screen.getByRole("heading", { name: "Lead Bus" }));
    const aliasInput = screen.getByLabelText("Alias for Lead");

    await user.clear(aliasInput);
    await user.type(aliasInput, "Cancelled{Escape}");

    expect(screen.getByRole("heading", { name: "Lead Bus" })).toBeInTheDocument();
    expect(bridge.commands.filter(([command]) => command === "setSourceAlias")).toEqual([]);
  });

  it("routes native spectrum range settings from the detail module popup", async () => {
    const bridge = new FakeBridge(snapshotWithSources());
    const user = userEvent.setup();

    render(<AppShell bridge={bridge} />);

    await screen.findByText("1 source tracked");
    await user.click(within(getSourceCard("Lead Bus")).getByRole("button", { name: "Details" }));
    await user.click(screen.getByRole("button", { name: "Spectrum settings" }));

    const panel = screen.getByLabelText("Spectrum settings panel");
    await editRangeSliderValue(panel, 0, "Floor", "-84");
    await editRangeSliderValue(panel, 1, "Ceiling", "-12");
    await user.click(within(panel).getByRole("button", { name: "Apply" }));

    expect(bridge.commands.filter(([command]) => command !== "refresh")).toContainEqual([
      "setSpectrumDisplayRange",
      {
        sourceKey: "plugin:lead",
        spectrumDisplayFloorDb: -84,
        spectrumDisplayCeilingDb: -12,
      },
    ]);
    expect(within(screen.getByLabelText("Spectrum settings panel")).getByText("-84 to -12 dB")).toBeInTheDocument();
  });

  it("matches native editable slider keyboard and cancel behavior", async () => {
    const bridge = new FakeBridge(snapshotWithSources());
    const user = userEvent.setup();

    render(<AppShell bridge={bridge} />);

    await screen.findByText("1 source tracked");
    await user.click(within(getSourceCard("Lead Bus")).getByRole("button", { name: "Details" }));
    await user.click(screen.getByRole("button", { name: "Clock settings" }));

    const panel = screen.getByLabelText("Clock settings panel");
    const warningField = getSliderField(panel, "Warning");
    fireEvent.keyDown(warningField, { key: "ArrowLeft" });
    expect(within(warningField).getByText("-13 LUFS")).toBeInTheDocument();
    fireEvent.keyDown(warningField, { key: "Enter" });
    const editor = within(warningField).getByLabelText("Warning");
    fireEvent.change(editor, { target: { value: "-33" } });
    fireEvent.keyDown(editor, { key: "Escape" });

    expect(within(warningField).queryByLabelText("Warning")).not.toBeInTheDocument();
    expect(within(warningField).getByText("-13 LUFS")).toBeInTheDocument();
  });

  it("matches native editable slider pointer drag behavior", async () => {
    const bridge = new FakeBridge(snapshotWithSources());
    const user = userEvent.setup();

    render(<AppShell bridge={bridge} />);

    await screen.findByText("1 source tracked");
    await user.click(within(getSourceCard("Lead Bus")).getByRole("button", { name: "Details" }));
    await user.click(screen.getByRole("button", { name: "Clock settings" }));

    const panel = screen.getByLabelText("Clock settings panel");
    const warningField = getSliderField(panel, "Warning");
    const visual = warningField.querySelector<HTMLElement>(".native-slider-visual");
    mockSliderRect(visual);

    act(() => {
      dispatchPointerEvent(visual!, "pointerdown", { pointerId: 1, button: 0, clientX: sliderClientXForPercent(50) });
    });
    expect(within(warningField).getByText("-60 LUFS")).toBeInTheDocument();

    act(() => {
      dispatchPointerEvent(visual!, "pointermove", { pointerId: 1, clientX: sliderClientXForPercent(75) });
      dispatchPointerEvent(visual!, "pointerup", { pointerId: 1, clientX: sliderClientXForPercent(75) });
    });

    expect(within(warningField).getByText("-30 LUFS")).toBeInTheDocument();
  });

  it("matches native range slider nearest-thumb drag behavior", async () => {
    const bridge = new FakeBridge(snapshotWithSources());
    const user = userEvent.setup();

    render(<AppShell bridge={bridge} />);

    await screen.findByText("1 source tracked");
    await user.click(within(getSourceCard("Lead Bus")).getByRole("button", { name: "Details" }));
    await user.click(screen.getByRole("button", { name: "Spectrum settings" }));

    const panel = screen.getByLabelText("Spectrum settings panel");
    const field = panel.querySelectorAll<HTMLElement>(".native-slider-field-range")[0];
    expect(field).toBeTruthy();
    const visual = field.querySelector<HTMLElement>(".native-slider-visual");
    mockSliderRect(visual);

    act(() => {
      dispatchPointerEvent(visual!, "pointerdown", { pointerId: 1, button: 0, clientX: sliderClientXForPercent(18) });
      dispatchPointerEvent(visual!, "pointermove", { pointerId: 1, clientX: sliderClientXForPercent(25) });
      dispatchPointerEvent(visual!, "pointerup", { pointerId: 1, clientX: sliderClientXForPercent(25) });
    });
    expect(within(field).getByText("-150 to -6 dB")).toBeInTheDocument();

    act(() => {
      dispatchPointerEvent(visual!, "pointerdown", { pointerId: 2, button: 0, clientX: sliderClientXForPercent(98) });
      dispatchPointerEvent(visual!, "pointermove", { pointerId: 2, clientX: sliderClientXForPercent(70) });
      dispatchPointerEvent(visual!, "pointerup", { pointerId: 2, clientX: sliderClientXForPercent(70) });
    });
    expect(within(field).getByText("-150 to -60 dB")).toBeInTheDocument();
  });

  it("resizes detail modules through the native-style resize thumb", async () => {
    const bridge = new FakeBridge(snapshotWithSources());
    const user = userEvent.setup();

    render(<AppShell bridge={bridge} />);

    await screen.findByText("1 source tracked");
    await user.click(within(getSourceCard("Lead Bus")).getByRole("button", { name: "Details" }));

    const waveformModule = getChartModule("Waveform");
    const grip = within(waveformModule).getByTitle("Resize module");
    act(() => {
      fireEvent.pointerDown(grip, { pointerId: 1, clientY: 200 });
      fireEvent.pointerMove(grip, { pointerId: 1, clientY: 260 });
    });
    expect(within(waveformModule).getByRole("img", { name: "Waveform chart" })).toHaveStyle({ height: "166px" });
    await act(async () => {
      fireEvent.pointerUp(grip, { pointerId: 1, clientY: 260 });
      await Promise.resolve();
    });

    expect(bridge.commands.filter(([command]) => command !== "refresh")).toContainEqual([
      "setDetailModuleHeight",
      {
        sourceKey: "plugin:lead",
        moduleKind: "waveform",
        moduleHeight: 220,
      },
    ]);
  });

  it("matches native detail module drag reorder hints and drop behavior", async () => {
    const bridge = new FakeBridge(snapshotWithSources());
    const user = userEvent.setup();

    render(<AppShell bridge={bridge} />);

    await screen.findByText("1 source tracked");
    await user.click(within(getSourceCard("Lead Bus")).getByRole("button", { name: "Details" }));

    const waveformModule = getChartModule("Waveform");
    const spectrumModule = getChartModule("Spectrum");
    const dragHandle = within(waveformModule).getByTitle("Drag to reorder");
    vi.spyOn(spectrumModule, "getBoundingClientRect").mockReturnValue({
      left: 0,
      right: 420,
      top: 100,
      bottom: 260,
      width: 420,
      height: 160,
      x: 0,
      y: 100,
      toJSON: () => undefined,
    });

    const dragData = createTestDataTransfer();
    await act(async () => {
      dispatchDragEvent(dragHandle, "dragstart", createDragEventInit({ dataTransfer: dragData }));
      await Promise.resolve();
    });
    await act(async () => {
      dispatchDragEvent(spectrumModule, "dragover", createDragEventInit({ clientY: 230, dataTransfer: dragData }));
      await Promise.resolve();
    });

    expect(spectrumModule).toHaveClass("chart-module-drop-after");
    expect(spectrumModule.querySelector(".chart-module-drop-hint-after")).toBeInTheDocument();

    await act(async () => {
      dispatchDragEvent(spectrumModule, "drop", createDragEventInit({ clientY: 230, dataTransfer: dragData }));
      await Promise.resolve();
    });

    expect(bridge.commands.filter(([command]) => command !== "refresh")).toContainEqual([
      "moveDetailModule",
      {
        sourceKey: "plugin:lead",
        moduleKind: "waveform",
        targetModuleKind: "spectrum",
        insertAfter: true,
      },
    ]);
    expect(getChartModule("Spectrum").compareDocumentPosition(getChartModule("Waveform"))).toBe(
      Node.DOCUMENT_POSITION_FOLLOWING,
    );
  });

  it("matches native spectrum hover frequency readout", async () => {
    const bridge = new FakeBridge(snapshotWithSources());
    const user = userEvent.setup();

    render(<AppShell bridge={bridge} />);

    await screen.findByText("1 source tracked");
    await user.click(within(getSourceCard("Lead Bus")).getByRole("button", { name: "Details" }));

    const spectrumModule = getChartModule("Spectrum");
    const spectrumCanvas = within(spectrumModule).getByRole("img", { name: "Spectrum chart" });
    vi.spyOn(spectrumCanvas, "getBoundingClientRect").mockReturnValue({
      left: 0,
      right: 280,
      top: 0,
      bottom: 100,
      width: 280,
      height: 100,
      x: 0,
      y: 0,
      toJSON: () => undefined,
    });

    fireEvent.mouseMove(spectrumCanvas, { clientX: 80, clientY: 50 });

    expect(spectrumModule.querySelector(".chart-hover-readout")).toHaveTextContent("3.34 kHz");

    fireEvent.mouseLeave(spectrumCanvas);
    expect(spectrumModule.querySelector(".chart-hover-readout")).not.toBeInTheDocument();
  });

  it("matches native spectrum wheel zoom, drag pan, and double-click reset", async () => {
    const bridge = new FakeBridge(snapshotWithSources());
    const user = userEvent.setup();
    const drawnSpectrumYs: number[][] = [];
    const originalGetContext = HTMLCanvasElement.prototype.getContext;
    vi.spyOn(HTMLCanvasElement.prototype, "getContext").mockImplementation(function getContext(
      this: HTMLCanvasElement,
      contextId: string,
    ) {
      if (contextId !== "2d" || this.getAttribute("aria-label") !== "Spectrum chart") {
        return originalGetContext.call(this, contextId as "2d") as CanvasRenderingContext2D | null;
      }

      let currentYs: number[] = [];
      return {
        beginPath() {},
        clearRect() {
          currentYs = [];
          drawnSpectrumYs.push(currentYs);
        },
        fillRect(x: number, y: number, width: number) {
          if (x === 0 && width < 280) {
            currentYs.push(y);
          }
        },
        lineTo() {},
        moveTo() {},
        stroke() {},
        set fillStyle(_value: string) {},
        set lineWidth(_value: number) {},
        set strokeStyle(_value: string) {},
      } as unknown as CanvasRenderingContext2D;
    });

    render(<AppShell bridge={bridge} />);

    await screen.findByText("1 source tracked");
    await user.click(within(getSourceCard("Lead Bus")).getByRole("button", { name: "Details" }));

    const spectrumModule = getChartModule("Spectrum");
    const spectrumCanvas = within(spectrumModule).getByRole("img", { name: "Spectrum chart" });
    Object.defineProperty(spectrumCanvas, "clientWidth", { configurable: true, value: 280 });
    Object.defineProperty(spectrumCanvas, "clientHeight", { configurable: true, value: 100 });
    vi.spyOn(spectrumCanvas, "getBoundingClientRect").mockReturnValue({
      left: 0,
      right: 280,
      top: 0,
      bottom: 100,
      width: 280,
      height: 100,
      x: 0,
      y: 0,
      toJSON: () => undefined,
    });

    await act(async () => {
      fireEvent.wheel(spectrumCanvas, { deltaY: 0, clientY: 50 });
      await Promise.resolve();
    });
    const initialYs = drawnSpectrumYs.at(-1)!;
    const initialSpreadY = spread(initialYs);

    await act(async () => {
      fireEvent.wheel(spectrumCanvas, { deltaY: -120, clientY: 50 });
      await Promise.resolve();
    });
    const zoomedYs = drawnSpectrumYs.at(-1)!;
    const zoomedSpreadY = spread(zoomedYs);
    const zoomedAverageY = average(zoomedYs);

    await act(async () => {
      dispatchPointerEvent(spectrumCanvas, "pointerdown", { pointerId: 1, button: 0, clientY: 50 });
      dispatchPointerEvent(spectrumCanvas, "pointermove", { pointerId: 1, clientY: 70 });
      dispatchPointerEvent(spectrumCanvas, "pointerup", { pointerId: 1, clientY: 70 });
      await Promise.resolve();
    });
    const pannedAverageY = average(drawnSpectrumYs.at(-1)!);

    await act(async () => {
      fireEvent.doubleClick(spectrumCanvas, { clientX: 80, clientY: 50 });
      await Promise.resolve();
    });
    const resetSpreadY = spread(drawnSpectrumYs.at(-1)!);

    expect(zoomedSpreadY).not.toBeCloseTo(initialSpreadY, 1);
    expect(pannedAverageY).not.toBeCloseTo(zoomedAverageY, 1);
    expect(resetSpreadY).toBeCloseTo(initialSpreadY, 1);
  });

  it("matches native waveform wheel zoom and double-click reset", async () => {
    const bridge = new FakeBridge(snapshotWithSources());
    const user = userEvent.setup();
    const drawnLineYs: number[][] = [];
    const originalGetContext = HTMLCanvasElement.prototype.getContext;
    vi.spyOn(HTMLCanvasElement.prototype, "getContext").mockImplementation(function getContext(
      this: HTMLCanvasElement,
      contextId: string,
    ) {
      if (contextId !== "2d" || this.getAttribute("aria-label") !== "Waveform chart") {
        return originalGetContext.call(this, contextId as "2d") as CanvasRenderingContext2D | null;
      }

      const currentLineYs: number[] = [];
      return {
        beginPath() {
          currentLineYs.length = 0;
        },
        clearRect() {},
        fillRect() {},
        lineTo(_x: number, y: number) {
          currentLineYs.push(y);
        },
        moveTo() {},
        stroke() {
          if (currentLineYs.length > 0) {
            drawnLineYs.push([...currentLineYs]);
          }
        },
        set fillStyle(_value: string) {},
        set lineWidth(_value: number) {},
        set strokeStyle(_value: string) {},
      } as unknown as CanvasRenderingContext2D;
    });

    render(<AppShell bridge={bridge} />);

    await screen.findByText("1 source tracked");
    await user.click(within(getSourceCard("Lead Bus")).getByRole("button", { name: "Details" }));

    const waveformCanvas = within(getChartModule("Waveform")).getByRole("img", { name: "Waveform chart" });
    Object.defineProperty(waveformCanvas, "clientWidth", { configurable: true, value: 280 });
    Object.defineProperty(waveformCanvas, "clientHeight", { configurable: true, value: 100 });
    vi.spyOn(waveformCanvas, "getBoundingClientRect").mockReturnValue({
      left: 0,
      right: 280,
      top: 0,
      bottom: 100,
      width: 280,
      height: 100,
      x: 0,
      y: 0,
      toJSON: () => undefined,
    });
    await act(async () => {
      fireEvent.wheel(waveformCanvas, { deltaY: 0, clientY: 50 });
      await Promise.resolve();
    });
    const initialMaxY = Math.max(...drawnLineYs.at(-1)!);

    await act(async () => {
      fireEvent.wheel(waveformCanvas, { deltaY: -120, clientY: 50 });
      await Promise.resolve();
    });
    const zoomedMaxY = Math.max(...drawnLineYs.at(-1)!);

    await act(async () => {
      dispatchPointerEvent(waveformCanvas, "pointerdown", { pointerId: 1, button: 0, clientY: 50 });
      dispatchPointerEvent(waveformCanvas, "pointermove", { pointerId: 1, clientY: 70 });
      dispatchPointerEvent(waveformCanvas, "pointerup", { pointerId: 1, clientY: 70 });
      await Promise.resolve();
    });
    const pannedMaxY = Math.max(...drawnLineYs.at(-1)!);

    await act(async () => {
      fireEvent.doubleClick(waveformCanvas);
      await Promise.resolve();
    });
    const resetMaxY = Math.max(...drawnLineYs.at(-1)!);

    expect(zoomedMaxY).toBeGreaterThan(initialMaxY);
    expect(pannedMaxY).not.toBeCloseTo(zoomedMaxY, 1);
    expect(resetMaxY).toBeCloseTo(initialMaxY, 1);
  });

  it("routes native loudness clock settings from the detail module popup", async () => {
    const bridge = new FakeBridge(snapshotWithSources());
    const user = userEvent.setup();

    render(<AppShell bridge={bridge} />);

    await screen.findByText("1 source tracked");
    await user.click(within(getSourceCard("Lead Bus")).getByRole("button", { name: "Details" }));
    await user.click(screen.getByRole("button", { name: "Clock settings" }));

    const panel = screen.getByLabelText("Clock settings panel");
    await editRangeSliderValue(panel, 0, "Min", "-54");
    await editRangeSliderValue(panel, 1, "Max", "-6");
    await editSingleSliderValue(panel, "Warning", "-18");
    await editSingleSliderValue(panel, "Cycle", "90");
    await user.click(within(panel).getByRole("button", { name: "Apply" }));

    expect(bridge.commands.filter(([command]) => command !== "refresh")).toContainEqual([
      "setLoudnessClockSettings",
      {
        sourceKey: "plugin:lead",
        loudnessClockMinimumDb: -54,
        loudnessClockMaximumDb: -6,
        loudnessClockWarningDb: -18,
        loudnessClockCycleSeconds: 90,
      },
    ]);
    expect(within(screen.getByLabelText("Clock settings panel")).getByText("90 s")).toBeInTheDocument();
  });

  it("routes native collision source and settings controls from the detail module", async () => {
    const bridge = new FakeBridge(snapshotWithSources());
    const user = userEvent.setup();

    render(<AppShell bridge={bridge} />);

    await screen.findByText("1 source tracked");
    await user.click(within(getSourceCard("Lead Bus")).getByRole("button", { name: "Details" }));

    await user.click(screen.getByRole("button", { name: "Collision source" }));
    const collisionMenu = screen.getByRole("menu", { name: "Collision source menu" });
    expect(within(collisionMenu).getByRole("menuitem", { name: "Global" })).toHaveFocus();
    fireEvent.keyDown(collisionMenu, { key: "ArrowUp" });
    expect(within(collisionMenu).getByRole("menuitem", { name: "Hidden Reference" })).toHaveFocus();
    fireEvent.keyDown(collisionMenu, { key: "ArrowDown" });
    expect(within(collisionMenu).getByRole("menuitem", { name: "Global" })).toHaveFocus();
    await user.click(
      within(screen.getByRole("menu", { name: "Collision source menu" })).getByRole("menuitem", {
        name: "Hidden Reference",
      }),
    );
    await user.click(screen.getByRole("button", { name: "Collision settings" }));

    const panel = screen.getByLabelText("Collision settings panel");
    await chooseNativeComboOption(user, "Collision scale", "Mel", panel);
    await editSingleSliderValue(panel, "Level floor", "-84");
    await editSingleSliderValue(panel, "Prominence", "12");
    await editSingleSliderValue(panel, "Match tolerance", "30");
    await user.click(within(panel).getByRole("button", { name: "Apply" }));

    expect(bridge.commands.filter(([command]) => command !== "refresh")).toContainEqual([
      "setCollisionSettings",
      {
        sourceKey: "plugin:lead",
        collisionLevelFloorDb: -84,
        collisionProminenceThresholdDb: 12,
        collisionRelativeThreshold: 0.3,
        collisionFrequencyScale: "mel",
        inlineCollisionSourceKey: "plugin:hidden",
      },
    ]);
    expect(screen.getByRole("button", { name: "Collision source" })).toHaveTextContent("Hidden Reference");
  });

  it("dismisses native-style detail chart popups with Escape, outside clicks, and sibling popup buttons", async () => {
    const bridge = new FakeBridge(snapshotWithSources());
    const user = userEvent.setup();

    render(<AppShell bridge={bridge} />);

    await screen.findByText("1 source tracked");
    await user.click(within(getSourceCard("Lead Bus")).getByRole("button", { name: "Details" }));

    await user.click(screen.getByRole("button", { name: "Spectrum settings" }));
    expect(screen.getByLabelText("Spectrum settings panel")).toBeInTheDocument();
    fireEvent.keyDown(window, { key: "Escape" });
    expect(screen.queryByLabelText("Spectrum settings panel")).not.toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Collision source" }));
    expect(screen.getByLabelText("Collision source menu")).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Collision settings" }));
    expect(screen.queryByLabelText("Collision source menu")).not.toBeInTheDocument();
    expect(screen.getByLabelText("Collision settings panel")).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Collision source" }));
    expect(screen.queryByLabelText("Collision settings panel")).not.toBeInTheDocument();
    expect(screen.getByLabelText("Collision source menu")).toBeInTheDocument();

    fireEvent.pointerDown(document.body);
    expect(screen.queryByLabelText("Collision source menu")).not.toBeInTheDocument();
  });

  it("shows the native missing-instance switch control in detail view", async () => {
    const bridge = new FakeBridge(snapshotWithMissingDetailSource());
    const user = userEvent.setup();

    render(<AppShell bridge={bridge} />);

    expect(await screen.findByText("Instance not found")).toBeInTheDocument();
    expect(screen.queryByRole("combobox", { name: "Switch detail source" })).not.toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Switch detail source" }));
    expect(screen.getByRole("menu", { name: "Switch detail source menu" })).toBeInTheDocument();
    const switchMenu = screen.getByRole("menu", { name: "Switch detail source menu" });
    expect(within(switchMenu).getByRole("menuitem", { name: "Lead Bus" })).toHaveFocus();
    fireEvent.keyDown(switchMenu, { key: "ArrowDown" });
    expect(within(switchMenu).getByRole("menuitem", { name: "Lead Bus" })).toHaveFocus();
    fireEvent.keyDown(window, { key: "Escape" });
    expect(screen.queryByRole("menu", { name: "Switch detail source menu" })).not.toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Switch detail source" }));
    expect(screen.getByRole("menu", { name: "Switch detail source menu" })).toBeInTheDocument();
    fireEvent.pointerDown(document.body);
    expect(screen.queryByRole("menu", { name: "Switch detail source menu" })).not.toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Switch detail source" }));
    await user.click(within(screen.getByRole("menu", { name: "Switch detail source menu" })).getByRole("menuitem", { name: "Lead Bus" }));

    expect(screen.getByRole("heading", { name: "Lead Bus" })).toBeInTheDocument();
    expect(screen.queryByText("Instance not found")).not.toBeInTheDocument();
    expect(bridge.commands.filter(([command]) => command !== "refresh")).toContainEqual([
      "selectDetail",
      { sourceKey: "plugin:lead" },
    ]);
  });

  it("routes native source restore and move controls through the bridge", async () => {
    const bridge = new FakeBridge(snapshotWithSources());
    const user = userEvent.setup();

    render(<AppShell bridge={bridge} />);

    await screen.findByText("1 source tracked");
    const leadCard = getSourceCard("Lead Bus");
    await user.click(within(leadCard).getByRole("button", { name: "Move source down" }));
    await user.click(screen.getByRole("button", { name: "Restore" }));

    expect(bridge.commands.filter(([command]) => command !== "refresh")).toEqual([
      ["moveSource", { sourceKey: "plugin:lead", moveDirection: "down" }],
      ["restoreSource", { sourceKey: "plugin:hidden" }],
    ]);
    expect(screen.getAllByRole("button", { name: "Hide" }).length).toBeGreaterThanOrEqual(1);
  });

  it("matches native source drag reorder hints and drop behavior", async () => {
    const bridge = new FakeBridge(snapshotWithSources());

    render(<AppShell bridge={bridge} />);

    await screen.findByText("1 source tracked");
    const leadCard = getSourceCard("Lead Bus");
    const hiddenCard = getSourceCard("Hidden Reference");
    const dragHandle = within(leadCard).getByTitle("Drag to reorder");
    vi.spyOn(hiddenCard, "getBoundingClientRect").mockReturnValue({
      left: 0,
      right: 320,
      top: 100,
      bottom: 200,
      width: 320,
      height: 100,
      x: 0,
      y: 100,
      toJSON: () => undefined,
    });

    const dragData = createTestDataTransfer();
    await act(async () => {
      dispatchDragEvent(dragHandle, "dragstart", createDragEventInit({ dataTransfer: dragData }));
      await Promise.resolve();
    });
    await act(async () => {
      dispatchDragEvent(hiddenCard, "dragover", createDragEventInit({ clientY: 175, dataTransfer: dragData }));
      await Promise.resolve();
    });

    expect(hiddenCard).toHaveClass("source-drop-after");
    expect(hiddenCard.querySelector(".source-drop-hint-after")).toBeInTheDocument();

    await act(async () => {
      dispatchDragEvent(hiddenCard, "drop", createDragEventInit({ clientY: 175, dataTransfer: dragData }));
      await Promise.resolve();
    });

    expect(bridge.commands.filter(([command]) => command !== "refresh")).toContainEqual([
      "moveSource",
      { sourceKey: "plugin:lead", moveDirection: "down" },
    ]);
    expect(getSourceCard("Hidden Reference").compareDocumentPosition(getSourceCard("Lead Bus"))).toBe(
      Node.DOCUMENT_POSITION_FOLLOWING,
    );
  });

  it("automatically refreshes the host snapshot so newly active apps appear", async () => {
    vi.useFakeTimers();
    try {
      const bridge = new FakeBridge(snapshotWithoutSources());
      bridge.queueRefresh(snapshotWithSources());

      render(<AppShell bridge={bridge} />);

      await act(async () => {});
      expect(within(screen.getByRole("region", { name: "Sources" })).getByText("No active audio sources")).toBeInTheDocument();
      expect(within(screen.getByRole("region", { name: "Recent events" })).getByText("No recent events")).toBeInTheDocument();

      await act(async () => {
        await vi.advanceTimersByTimeAsync(33);
      });

      expect(within(screen.getByRole("region", { name: "Sources" })).getByText("Lead Bus")).toBeInTheDocument();
      expect(bridge.commands).toContainEqual(["refresh", undefined]);
    } finally {
      vi.clearAllTimers();
      vi.useRealTimers();
    }
  });

  it("matches the native refresh cadence and switches to eco cadence when enabled", async () => {
    vi.useFakeTimers();
    try {
      const bridge = new FakeBridge(snapshotWithSources());

      render(<AppShell bridge={bridge} />);

      await act(async () => {});
      expect(screen.getByText("33 ms")).toBeInTheDocument();
      await act(async () => {
        await vi.advanceTimersByTimeAsync(33);
      });
      expect(bridge.commands).toContainEqual(["refresh", undefined]);

      bridge.commands.length = 0;
      fireEvent.click(screen.getByRole("checkbox", { name: "Eco mode" }));
      expect(screen.getByText("100 ms")).toBeInTheDocument();
      expect(bridge.commands.filter(([command]) => command !== "refresh")).toContainEqual([
        "setPerformanceMode",
        { performanceMode: "eco" },
      ]);

      bridge.commands.length = 0;
      await act(async () => {});
      await act(async () => {
        await vi.advanceTimersByTimeAsync(99);
      });
      expect(bridge.commands).not.toContainEqual(["refresh", undefined]);

      await act(async () => {
        await vi.advanceTimersByTimeAsync(1);
      });
      expect(bridge.commands).toContainEqual(["refresh", undefined]);
    } finally {
      vi.useRealTimers();
    }
  });

  it("shows bridge errors while preserving the last safe snapshot", async () => {
    const bridge = new FakeBridge(snapshotWithSources());
    const user = userEvent.setup();

    render(<AppShell bridge={bridge} />);
    await screen.findByText("1 source tracked");
    bridge.error = new Error("Unknown command.");
    await user.click(screen.getByRole("button", { name: "Reset All" }));

    expect(screen.getByText("Unknown command.")).toBeInTheDocument();
    expect(within(screen.getByRole("region", { name: "Sources" })).getByText("Lead Bus")).toBeInTheDocument();
  });

  it("falls back to the default web theme for null or unknown theme ids", async () => {
    const unknownThemeSnapshot = snapshotWithSources();
    unknownThemeSnapshot.settings = {
      ...unknownThemeSnapshot.settings,
      themeId: "does-not-exist",
    };
    const nullThemeSnapshot = snapshotWithSources();
    nullThemeSnapshot.settings = {
      ...nullThemeSnapshot.settings,
      themeId: null,
    };

    const { unmount } = render(<AppShell bridge={new FakeBridge(unknownThemeSnapshot)} />);
    expect(await screen.findByRole("main")).toHaveAttribute("data-theme", "midnight");
    unmount();

    render(<AppShell bridge={new FakeBridge(nullThemeSnapshot)} />);
    expect(await screen.findByRole("main")).toHaveAttribute("data-theme", "midnight");
  });
});

function getSourceCard(name: string): HTMLElement {
  const sources = screen.getByRole("region", { name: "Sources" });
  const card = within(sources).getByText(name).closest("article");
  expect(card).not.toBeNull();
  return card!;
}

function getChartModule(name: string): HTMLElement {
  const heading = screen.getByRole("heading", { name });
  const module = heading.closest("[data-testid='chart-module']");
  expect(module).not.toBeNull();
  return module as HTMLElement;
}

async function chooseNativeComboOption(
  user: ReturnType<typeof userEvent.setup>,
  name: string,
  optionName: string,
  scope?: HTMLElement,
): Promise<void> {
  const queries = scope ? within(scope) : screen;
  await user.click(queries.getByRole("combobox", { name }));
  await user.click(screen.getByRole("option", { name: optionName }));
}

async function editSingleSliderValue(panel: HTMLElement, label: string, value: string): Promise<void> {
  const field = getSliderField(panel, label);
  fireEvent.keyDown(field, { key: "Enter" });
  const editor = within(field).getByLabelText(label);
  fireEvent.change(editor, { target: { value } });
  fireEvent.keyDown(editor, { key: "Enter" });
}

async function editRangeSliderValue(panel: HTMLElement, thumbIndex: 0 | 1, label: string, value: string): Promise<void> {
  const field = panel.querySelectorAll<HTMLElement>(".native-slider-field-range")[0];
  expect(field).toBeTruthy();
  const visual = field.querySelector<HTMLElement>(".native-slider-visual");
  mockSliderRect(visual);
  fireEvent.doubleClick(field, { clientX: thumbIndex === 0 ? 120 : 270 });
  const editor = within(field).getByLabelText(label);
  fireEvent.change(editor, { target: { value } });
  fireEvent.keyDown(editor, { key: "Enter" });
}

function mockSliderRect(visual: HTMLElement | null): void {
  expect(visual).toBeTruthy();
  vi.spyOn(visual!, "getBoundingClientRect").mockReturnValue({
    left: 0,
    right: 280,
    top: 0,
    bottom: 30,
    width: 280,
    height: 30,
    x: 0,
    y: 0,
    toJSON: () => undefined,
  });
}

function sliderClientXForPercent(percent: number): number {
  return 14 + 252 * (percent / 100);
}

function average(values: number[]): number {
  return values.reduce((total, value) => total + value, 0) / Math.max(1, values.length);
}

function spread(values: number[]): number {
  return Math.max(...values) - Math.min(...values);
}

function createTestDataTransfer(): DataTransfer {
  const data = new Map<string, string>();
  return {
    dropEffect: "none",
    effectAllowed: "all",
    clearData: () => data.clear(),
    files: [] as unknown as FileList,
    getData: (format: string) => data.get(format) ?? "",
    items: [] as unknown as DataTransferItemList,
    setData: (format: string, value: string) => data.set(format, value),
    types: [] as unknown as readonly string[],
    setDragImage: () => undefined,
  } as DataTransfer;
}

function createDragEventInit(
  init: MouseEventInit & { dataTransfer?: DataTransfer } = {},
): MouseEventInit & { dataTransfer: DataTransfer } {
  const { dataTransfer, ...eventInit } = init;
  return {
    bubbles: true,
    cancelable: true,
    ...eventInit,
    dataTransfer: dataTransfer ?? createTestDataTransfer(),
  };
}

function dispatchDragEvent(
  element: HTMLElement,
  type: string,
  init: MouseEventInit & { dataTransfer: DataTransfer },
): void {
  const event = new MouseEvent(type, { bubbles: true, cancelable: true, ...init });
  Object.defineProperty(event, "dataTransfer", { value: init.dataTransfer });
  element.dispatchEvent(event);
}

function dispatchPointerEvent(
  element: HTMLElement,
  type: string,
  init: MouseEventInit & { pointerId: number },
): void {
  const event = new MouseEvent(type, { bubbles: true, cancelable: true, ...init }) as MouseEvent & {
    pointerId: number;
  };
  Object.defineProperty(event, "pointerId", { value: init.pointerId });
  element.dispatchEvent(event);
}

function getSliderField(panel: HTMLElement, label: string): HTMLElement {
  const field = within(panel).getByText(label).closest(".native-slider-field");
  expect(field).not.toBeNull();
  return field as HTMLElement;
}

class FakeBridge implements HostBridgeLike {
  public readonly commands: Array<[string, unknown]> = [];
  public error: Error | null = null;
  private readonly refreshSnapshots: HostSnapshotDto[] = [];

  public constructor(private snapshot: HostSnapshotDto) {}

  public async requestSnapshot(): Promise<HostSnapshotDto> {
    return this.snapshot;
  }

  public queueRefresh(snapshot: HostSnapshotDto): void {
    this.refreshSnapshots.push(snapshot);
  }

  public async sendCommand(command: string, payload?: unknown): Promise<HostCommandResultDto> {
    this.commands.push([command, payload]);
    if (this.error) {
      throw this.error;
    }

    if (command === "refresh" && this.refreshSnapshots.length > 0) {
      this.snapshot = this.refreshSnapshots.shift()!;
    }

    if (command === "setShowHiddenSources" && isShowHiddenPayload(payload)) {
      this.snapshot = {
        ...this.snapshot,
        settings: {
          ...this.snapshot.settings,
          showHiddenSources: payload.showHiddenSources,
        },
      };
    }

    if (command === "setTheme" && isThemePayload(payload)) {
      this.snapshot = {
        ...this.snapshot,
        settings: {
          ...this.snapshot.settings,
          themeId: payload.themeId,
        },
      };
    }

    if (command === "setTruePeakOversampling" && isTruePeakPayload(payload)) {
      this.updateSource((source) =>
        source.sourceKey === payload.sourceKey
          ? { ...source, truePeakOversampling: payload.truePeakOversampling }
          : source,
      );
    }

    if (command === "hideSource" && isSourcePayload(payload)) {
      this.updateSource((source) => (source.sourceKey === payload.sourceKey ? { ...source, isHidden: true } : source));
    }

    if (command === "restoreSource" && isSourcePayload(payload)) {
      this.updateSource((source) => (source.sourceKey === payload.sourceKey ? { ...source, isHidden: false } : source));
    }

    if (command === "setSourceAlias" && isAliasPayload(payload)) {
      this.updateSource((source) => {
        if (source.sourceKey !== payload.sourceKey) {
          return source;
        }

        const alias = normalizeAlias(payload.alias);
        const effectiveDisplayName = alias ?? source.displayName;
        return {
          ...source,
          alias,
          effectiveDisplayName,
          displayTitle: effectiveDisplayName,
        };
      });
    }

    if (command === "moveSource" && isMoveSourcePayload(payload)) {
      const index = this.snapshot.sources.findIndex((source) => source.sourceKey === payload.sourceKey);
      const targetIndex = index + (payload.moveDirection === "up" ? -1 : 1);
      if (index >= 0 && targetIndex >= 0 && targetIndex < this.snapshot.sources.length) {
        const sources = [...this.snapshot.sources];
        const [source] = sources.splice(index, 1);
        sources.splice(targetIndex, 0, source);
        this.snapshot = {
          ...this.snapshot,
          sources: sources.map((source, orderIndex) => ({ ...source, orderIndex })),
        };
      }
    }

    if (command === "selectDetail" && isSourcePayload(payload)) {
      const source = this.snapshot.sources.find((item) => item.sourceKey === payload.sourceKey);
      this.snapshot = {
        ...this.snapshot,
        selectedDetail: source
          ? {
              source,
              modules: [
                { kind: "waveform", title: "Waveform", orderIndex: 0, isVisible: true, height: 140 },
                { kind: "spectrum", title: "Spectrum", orderIndex: 1, isVisible: true, height: 140 },
                { kind: "stereo", title: "Stereo", orderIndex: 2, isVisible: true, height: 140 },
                { kind: "loudnessClock", title: "Loudness Clock", orderIndex: 3, isVisible: true, height: 140 },
                { kind: "collision", title: "Frequency Collision", orderIndex: 4, isVisible: true, height: 140 },
              ],
              isWaveformVisible: true,
              isSpectrumVisible: true,
              isStereoVisible: true,
              isLoudnessClockVisible: true,
              isCollisionVisible: true,
              collision: {
                comparisonSourceKey: null,
                comparisonDisplayName: null,
                levelFloorDb: -96,
                prominenceThresholdDb: 9,
                relativeThreshold: 0.25,
                frequencyScale: "logarithmic",
                bands: [
                  { startRatio: 0.2, endRatio: 0.36, intensity: 0.8 },
                  { startRatio: 0.58, endRatio: 0.68, intensity: 0.55 },
                ],
              },
            }
          : null,
      };
    }

    if (command === "backToGlobal") {
      this.snapshot = {
        ...this.snapshot,
        selectedDetail: null,
      };
    }

    if (command === "setWaveformDisplayMode" && isWaveformModePayload(payload)) {
      this.updateSource((source) =>
        source.sourceKey === payload.sourceKey
          ? { ...source, waveformDisplayMode: payload.waveformDisplayMode }
          : source,
      );
    }

    if (command === "setSpectrumDisplayRange" && isSpectrumRangePayload(payload)) {
      this.updateSourceSettings(payload.sourceKey, (source) => ({
        ...source,
        spectrumDisplayFloorDb: payload.spectrumDisplayFloorDb,
        spectrumDisplayCeilingDb: payload.spectrumDisplayCeilingDb,
      }));
    }

    if (command === "setLoudnessClockSettings" && isLoudnessClockSettingsPayload(payload)) {
      this.updateSourceSettings(payload.sourceKey, (source) => ({
        ...source,
        loudnessClockMinimumDb: payload.loudnessClockMinimumDb,
        loudnessClockMaximumDb: payload.loudnessClockMaximumDb,
        loudnessClockWarningDb: payload.loudnessClockWarningDb,
        loudnessClockCycleSeconds: payload.loudnessClockCycleSeconds,
      }));
    }

    if (command === "setCollisionSettings" && isCollisionSettingsPayload(payload)) {
      this.updateSourceSettings(payload.sourceKey, (source) => ({
        ...source,
        collisionLevelFloorDb: payload.collisionLevelFloorDb,
        collisionProminenceThresholdDb: payload.collisionProminenceThresholdDb,
        collisionRelativeThreshold: payload.collisionRelativeThreshold,
        collisionFrequencyScale: payload.collisionFrequencyScale,
        inlineCollisionSourceKey: payload.inlineCollisionSourceKey,
      }));
      this.snapshot = {
        ...this.snapshot,
        selectedDetail: this.snapshot.selectedDetail
          ? {
              ...this.snapshot.selectedDetail,
              collision: this.snapshot.selectedDetail.collision
                ? {
                    ...this.snapshot.selectedDetail.collision,
                    comparisonSourceKey: payload.inlineCollisionSourceKey,
                    comparisonDisplayName:
                      this.snapshot.sources.find((source) => source.sourceKey === payload.inlineCollisionSourceKey)
                        ?.displayTitle ?? null,
                    levelFloorDb: payload.collisionLevelFloorDb,
                    prominenceThresholdDb: payload.collisionProminenceThresholdDb,
                    relativeThreshold: payload.collisionRelativeThreshold,
                    frequencyScale: payload.collisionFrequencyScale,
                  }
                : null,
            }
          : null,
      };
    }

    if (command === "setDetailModuleVisibility" && isModuleVisibilityPayload(payload)) {
      this.snapshot = {
        ...this.snapshot,
        selectedDetail: this.snapshot.selectedDetail
          ? {
              ...this.snapshot.selectedDetail,
              modules: this.snapshot.selectedDetail.modules.map((module) =>
                module.kind === payload.moduleKind ? { ...module, isVisible: payload.isVisible } : module,
              ),
            }
          : null,
      };
    }

    if (command === "setDetailModuleHeight" && isModuleHeightPayload(payload)) {
      this.snapshot = {
        ...this.snapshot,
        selectedDetail: this.snapshot.selectedDetail
          ? {
              ...this.snapshot.selectedDetail,
              modules: this.snapshot.selectedDetail.modules.map((module) =>
                module.kind === payload.moduleKind
                  ? { ...module, height: normalizeModuleHeight(module.kind, payload.moduleHeight) }
                  : module,
              ),
            }
          : null,
      };
    }

    if (command === "moveDetailModule" && isMoveDetailModulePayload(payload)) {
      this.snapshot = {
        ...this.snapshot,
        selectedDetail: this.snapshot.selectedDetail
          ? {
              ...this.snapshot.selectedDetail,
              modules: moveModuleToDropPosition(
                this.snapshot.selectedDetail.modules,
                payload.moduleKind,
                payload.targetModuleKind,
                payload.insertAfter,
              ),
            }
          : null,
      };
    }

    if (command === "forgetSource" && isSourcePayload(payload)) {
      this.snapshot = {
        ...this.snapshot,
        selectedDetail: null,
        settings: {
          ...this.snapshot.settings,
          sources: this.snapshot.settings.sources.filter((source) => source.sourceKey !== payload.sourceKey),
        },
      };
    }

    return {
      commandKind: command,
      success: true,
      snapshot: this.snapshot,
      error: null,
    };
  }

  private updateSource(update: (source: HostSnapshotDto["sources"][number]) => HostSnapshotDto["sources"][number]) {
    const sources = this.snapshot.sources.map((source) => update(source));
    const selected = this.snapshot.selectedDetail;
    this.snapshot = {
      ...this.snapshot,
      sources,
      selectedDetail: selected
        ? {
            ...selected,
            source: sources.find((source) => source.sourceKey === selected.source.sourceKey) ?? selected.source,
          }
        : null,
    };
  }

  private updateSourceSettings(
    sourceKey: string,
    update: (source: HostSnapshotDto["settings"]["sources"][number]) => HostSnapshotDto["settings"]["sources"][number],
  ) {
    const existing = this.snapshot.settings.sources.find((source) => source.sourceKey === sourceKey) ?? {
      sourceKey,
      alias: null,
      isHidden: false,
    };
    this.snapshot = {
      ...this.snapshot,
      settings: {
        ...this.snapshot.settings,
        sources: [
          ...this.snapshot.settings.sources.filter((source) => source.sourceKey !== sourceKey),
          update(existing),
        ],
      },
    };
  }
}

function isSourcePayload(payload: unknown): payload is { sourceKey: string } {
  return typeof payload === "object" && payload !== null && "sourceKey" in payload;
}

function isShowHiddenPayload(payload: unknown): payload is { showHiddenSources: boolean } {
  return typeof payload === "object" && payload !== null && "showHiddenSources" in payload;
}

function isTruePeakPayload(payload: unknown): payload is { sourceKey: string; truePeakOversampling: string } {
  return typeof payload === "object" && payload !== null && "sourceKey" in payload && "truePeakOversampling" in payload;
}

function isThemePayload(payload: unknown): payload is { themeId: string | null } {
  return typeof payload === "object" && payload !== null && "themeId" in payload;
}

function isWaveformModePayload(payload: unknown): payload is { sourceKey: string; waveformDisplayMode: string } {
  return typeof payload === "object" && payload !== null && "sourceKey" in payload && "waveformDisplayMode" in payload;
}

function isSpectrumRangePayload(
  payload: unknown,
): payload is { sourceKey: string; spectrumDisplayFloorDb: number; spectrumDisplayCeilingDb: number } {
  return (
    typeof payload === "object" &&
    payload !== null &&
    "sourceKey" in payload &&
    "spectrumDisplayFloorDb" in payload &&
    "spectrumDisplayCeilingDb" in payload
  );
}

function isLoudnessClockSettingsPayload(
  payload: unknown,
): payload is {
  sourceKey: string;
  loudnessClockMinimumDb: number;
  loudnessClockMaximumDb: number;
  loudnessClockWarningDb: number;
  loudnessClockCycleSeconds: number;
} {
  return (
    typeof payload === "object" &&
    payload !== null &&
    "sourceKey" in payload &&
    "loudnessClockMinimumDb" in payload &&
    "loudnessClockMaximumDb" in payload &&
    "loudnessClockWarningDb" in payload &&
    "loudnessClockCycleSeconds" in payload
  );
}

function isCollisionSettingsPayload(payload: unknown): payload is {
  sourceKey: string;
  collisionLevelFloorDb: number;
  collisionProminenceThresholdDb: number;
  collisionRelativeThreshold: number;
  collisionFrequencyScale: string;
  inlineCollisionSourceKey: string | null;
} {
  return (
    typeof payload === "object" &&
    payload !== null &&
    "sourceKey" in payload &&
    "collisionLevelFloorDb" in payload &&
    "collisionProminenceThresholdDb" in payload &&
    "collisionRelativeThreshold" in payload &&
    "collisionFrequencyScale" in payload &&
    "inlineCollisionSourceKey" in payload
  );
}

function isModuleVisibilityPayload(payload: unknown): payload is { moduleKind: string; isVisible: boolean } {
  return typeof payload === "object" && payload !== null && "moduleKind" in payload && "isVisible" in payload;
}

function isModuleHeightPayload(payload: unknown): payload is { moduleKind: string; moduleHeight: number } {
  return (
    typeof payload === "object" &&
    payload !== null &&
    "moduleKind" in payload &&
    "moduleHeight" in payload &&
    typeof payload.moduleHeight === "number"
  );
}

function isMoveDetailModulePayload(
  payload: unknown,
): payload is { moduleKind: string; targetModuleKind: string; insertAfter: boolean } {
  return (
    typeof payload === "object" &&
    payload !== null &&
    "moduleKind" in payload &&
    "targetModuleKind" in payload &&
    "insertAfter" in payload &&
    typeof payload.insertAfter === "boolean"
  );
}

function isMoveSourcePayload(payload: unknown): payload is { sourceKey: string; moveDirection: "up" | "down" } {
  return typeof payload === "object" && payload !== null && "sourceKey" in payload && "moveDirection" in payload;
}

function isAliasPayload(payload: unknown): payload is { sourceKey: string; alias: string | null } {
  return typeof payload === "object" && payload !== null && "sourceKey" in payload && "alias" in payload;
}

function normalizeAlias(alias: string | null | undefined): string | null {
  const next = typeof alias === "string" ? alias.trim() : "";
  return next.length ? next : null;
}

function normalizeModuleHeight(kind: string, height: number): number {
  if (kind === "collision") {
    return 112;
  }

  const minimum = kind === "stereo" ? 320 : 160;
  return Math.max(minimum, Math.min(1200, Math.round(height)));
}

function moveModuleToDropPosition(
  modules: NonNullable<HostSnapshotDto["selectedDetail"]>["modules"],
  moduleKind: string,
  targetModuleKind: string,
  insertAfter: boolean,
): NonNullable<HostSnapshotDto["selectedDetail"]>["modules"] {
  const nextModules = [...modules];
  const sourceIndex = nextModules.findIndex((module) => module.kind === moduleKind);
  if (sourceIndex < 0 || moduleKind === targetModuleKind) {
    return modules;
  }

  const [source] = nextModules.splice(sourceIndex, 1);
  const targetIndex = nextModules.findIndex((module) => module.kind === targetModuleKind);
  if (targetIndex < 0) {
    return modules;
  }

  nextModules.splice(targetIndex + (insertAfter ? 1 : 0), 0, source);
  return nextModules.map((module, orderIndex) => ({ ...module, orderIndex }));
}

function snapshotWithSources(): HostSnapshotDto {
  return {
    sources: [
      {
        sourceKey: "plugin:lead",
        sourceKind: "plugin",
        sourceKindLabel: "Plugin sender",
        processId: null,
        displayName: "Lead",
        alias: "Lead Bus",
        effectiveDisplayName: "Lead Bus",
        displayTitle: "Lead Bus",
        sourceIdentityText: "plugin:lead",
        isHidden: false,
        orderIndex: 0,
        isInstanceMissing: false,
        peak: 0.5,
        peakDb: -6,
        peakDbText: "-6.0 dB",
        state: "Active",
        truePeakOversampling: "fourX",
        waveformDisplayMode: "overall",
        spectrumDisplayMode: "overall",
        truePeakDb: -0.4,
        waveformSamples: [-0.4, 0.2, 0.7, -0.1],
        channelWaveformSamples: [
          [-0.2, 0.1, 0.6],
          [0.3, -0.4, 0.2],
        ],
        spectrumBuckets: [0.1, 0.6, 0.92, 0.4],
        channelSpectrumBuckets: [
          [0.1, 0.4, 0.8],
          [0.2, 0.7, 0.5],
        ],
        stereoImagePoints: [{ angle: 0, radius: 0.7 }],
        stereoImageX: 0.2,
        stereoImageY: -0.1,
        plotFrameVersion: 1,
      },
      {
        sourceKey: "plugin:hidden",
        sourceKind: "plugin",
        sourceKindLabel: "Plugin sender",
        processId: null,
        displayName: "Hidden Reference",
        alias: null,
        effectiveDisplayName: "Hidden Reference",
        displayTitle: "Hidden Reference",
        sourceIdentityText: "plugin:hidden",
        isHidden: true,
        orderIndex: 1,
        isInstanceMissing: false,
        peak: 0.2,
        peakDb: -14,
        peakDbText: "-14.0 dB",
        state: "Hidden",
        truePeakOversampling: "fourX",
        waveformDisplayMode: "overall",
        spectrumDisplayMode: "overall",
        plotFrameVersion: 1,
      },
    ],
    selectedDetail: null,
    settings: {
      schemaVersion: 1,
      themeId: "milk-white",
      maxRecentEventEntries: 6,
      eventOutputFolder: null,
      loadDefaultDetailLayoutFromFile: false,
      defaultDetailLayoutFile: null,
      showHiddenSources: false,
      sources: [],
    },
    recentEvents: [
      {
        timeText: "12:34:56",
        kind: "source",
        title: "Source appeared",
        detail: "Lead Bus",
        severity: "normal",
      },
    ],
    statusText: "1 source tracked",
    commands: {
      canBackToGlobal: true,
      canResetAll: true,
      canForgetSelectedSource: true,
      canMoveSelectedSourceUp: true,
      canMoveSelectedSourceDown: true,
    },
  };
}

function snapshotWithoutSources(): HostSnapshotDto {
  return {
    ...snapshotWithSources(),
    sources: [],
    selectedDetail: null,
    recentEvents: [],
    statusText: "No active audio sources",
  };
}

function snapshotWithMissingDetailSource(): HostSnapshotDto {
  const snapshot = snapshotWithSources();
  const missingSource = {
    ...snapshot.sources[1],
    sourceKey: "plugin:missing",
    displayName: "Missing Reference",
    alias: null,
    effectiveDisplayName: "Missing Reference",
    displayTitle: "Missing Reference",
    sourceIdentityText: "plugin:missing",
    isHidden: false,
    isInstanceMissing: true,
    state: "Missing",
  };

  return {
    ...snapshot,
    sources: [snapshot.sources[0], missingSource],
    selectedDetail: {
      source: missingSource,
      modules: [
        { kind: "waveform", title: "Waveform", orderIndex: 0, isVisible: true, height: 140 },
        { kind: "spectrum", title: "Spectrum", orderIndex: 1, isVisible: true, height: 140 },
        { kind: "stereo", title: "Stereo", orderIndex: 2, isVisible: false, height: 140 },
        { kind: "loudnessClock", title: "Loudness Clock", orderIndex: 3, isVisible: true, height: 140 },
      ],
      isWaveformVisible: true,
      isSpectrumVisible: true,
      isStereoVisible: false,
      isLoudnessClockVisible: true,
      isCollisionVisible: false,
      collision: null,
    },
  };
}

function snapshotWithRecentEventKinds(): HostSnapshotDto {
  return {
    ...snapshotWithSources(),
    recentEvents: [
      {
        timeText: "12:34:56",
        kind: "source",
        title: "Source appeared",
        detail: "Lead Bus",
        severity: "muted",
      },
      {
        timeText: "12:35:01",
        kind: "loudness",
        title: "Loudness jump",
        detail: "Lead Bus",
        severity: "hot",
      },
      {
        timeText: "12:35:04",
        kind: "truePeak",
        title: "True peak risk",
        detail: "Lead Bus reached -0.8 dBTP",
        severity: "critical",
      },
      {
        timeText: "12:35:05",
        kind: "source",
        title: "Reset complete",
        detail: "Lead Bus",
        severity: "normal",
      },
      {
        timeText: "12:35:07",
        kind: "collision",
        title: "Frequency overlap",
        detail: "Lead Bus / Hidden Reference",
        severity: "over",
      },
    ],
    statusText: "3 source tracked",
  };
}
