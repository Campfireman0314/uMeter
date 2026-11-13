## ADDED Requirements

### Requirement: Keyboard-operated measurement controls

Both custom slider types SHALL support keyboard adjustment and Enter/F2 numeric entry with Escape cancellation. The two-endpoint range control SHALL expose an active endpoint, support Space endpoint switching, and preserve minimum separation. Tab SHALL remain unhandled so normal WPF focus navigation can proceed. Numeric-editor key bubbling SHALL NOT move slider values while typing.

#### Scenario: Adjust the selected range endpoint
- **WHEN** the range slider receives Space followed by an arrow key
- **THEN** the active endpoint switches and only that endpoint is adjusted by the configured step

#### Scenario: Direct entry and cancellation
- **WHEN** Enter or F2 opens numeric entry and Escape is pressed after changing the editor text
- **THEN** the editor closes without committing the text or changing the existing value

#### Scenario: Editor navigation
- **WHEN** navigation keys arrive while the numeric editor is open
- **THEN** they do not adjust the slider through its parent key handler
- **AND** Tab is not consumed by the slider's adjustment logic

### Requirement: Windows UI Automation range access

The single-value slider SHALL expose the RangeValue pattern. The range slider SHALL expose one independently named RangeValue provider per logical endpoint, including enabled state and permissible values. Automation focus on an endpoint SHALL focus the interactive slider and select that endpoint. Provider writes SHALL preserve two-way bindings and reject disabled or out-of-range updates; real value changes SHALL raise the appropriate notification paths.

#### Scenario: Automation focuses the upper endpoint
- **WHEN** the upper endpoint peer receives a focus request
- **THEN** the owning slider takes keyboard focus and activates the upper endpoint
- **AND** subsequent arrow input adjusts that endpoint

#### Scenario: Disabled control
- **WHEN** a provider receives a value write while its slider is disabled
- **THEN** it reports the disabled state and rejects the write without changing the value

### Requirement: Valid fractional bounds

For finite ordered bounds with room for the configured separation, slider coercion SHALL keep values within their advertised provider ranges after quantization and dynamic bound updates. Exact limits SHALL remain reachable even when they do not fall on a step, and writing a provider's own current endpoint value SHALL be idempotent.

#### Scenario: Fractional maximum shrinks below the current range
- **GIVEN** a range slider with Minimum=-120, Maximum=0, Step=1, LowerValue=-60, and UpperValue=-10
- **WHEN** Maximum changes to -80.4
- **THEN** UpperValue becomes -80.4 and LowerValue is at most -81.4
- **AND** both providers advertise ordered ranges containing their values

#### Scenario: Fractional single-value limits
- **WHEN** the single-value slider has fractional limits such as Minimum=0.4 and Maximum=100.6 with Step=1
- **THEN** provider writes and Home/End can reach those exact limits without rounding outside them

### Requirement: Meaningful names and visible focus

Core themed controls and custom sliders SHALL expose an explicit focus indication using existing theme resources and an applicable system high-contrast fallback. Core icon commands, diagram-setting controls, compact metric selectors, and sliders in inline and popup detail views SHALL have meaningful accessible names that follow existing live localization. This SHALL NOT imply verified real-screen-reader or mixed-DPI appearance.

#### Scenario: Keyboard focus on a measurement control
- **WHEN** a custom slider or supported themed core control receives keyboard focus
- **THEN** its explicit focus indication becomes visible without changing its layout contract

#### Scenario: Detail language changes
- **WHEN** an existing supported language is applied to inline or popup detail controls
- **THEN** accessible names use the corresponding catalog text while bound names remain controlled by their bindings

### Requirement: Honest accessibility acceptance

Automated control/provider, pixel, and XAML checks SHALL be distinguished from human and real-device acceptance. Narrator workflows, client-side event delivery, complete real-window traversal, DPI appearance, and live audio smoke checks SHALL remain open until actually observed.

#### Scenario: Automated verification completes
- **WHEN** all scoped automated checks and independent reviews pass
- **THEN** engineering completion can be recorded without checking off unobserved human or real-device acceptance gates
