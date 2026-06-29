---
status: active
created_at: 2026-06-27
created_from: bugfix:2026-06-27-fixed-collision-height
last_verified_at: 2026-06-27
last_verified_by: opsx-bugfix
verification_basis: build-and-test
applies_to:
  - src/DesktopLoudnessMeter.App/MainWindow.xaml
  - src/DesktopLoudnessMeter.App/DetailWindow.xaml
  - src/DesktopLoudnessMeter.App/ViewModels/DetailModuleViewModel.cs
superseded_by:
---

# Visible-Only Layout Bindings Can Hide Controls

**Tags:** [wpf, layout, xaml]

## Symptom

A collapsed detail graph can unexpectedly hide its whole module header instead of leaving the toggle/header available.

## Root Cause

The module exposes display-only height values that intentionally become `NaN` or `0` when the graph body is hidden. Binding a container's `MaxHeight` to one of those visible-only values clamps the entire container, including the header, to zero.

## Fix

Use a fixed cap for fixed-height modules and reserve visible-only bindings for the body height/minimum behavior they were designed to control.

## Key Takeaway

Before binding WPF container constraints to visibility-sensitive view-model properties, check what the value becomes in the hidden state; a zero `MaxHeight` can hide controls that should remain reachable.

## Source

working tree: fixed-height collision analyzer polish (2026-06-27)
