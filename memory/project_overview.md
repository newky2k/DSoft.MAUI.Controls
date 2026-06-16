---
name: Project overview
description: DSoft.Maui.Controls library conventions, patterns, and controls added so far
type: project
---

100% MAUI controls library (no native/platform code in controls). Targets .NET 9 and 10, Android/iOS/macOS/Windows.

**Why:** Distributed as the `DSoft.Maui.Controls` NuGet package.

**How to apply:** All new controls must be pure MAUI primitives. Use `#region` blocks, `BindableProperty` for every public property, `OnXxxChanged` callback naming, and add a demo page to `MauiSampleApp`.

## Controls added in this session

- `SpinnerPickerView` — drum-roll/wheel picker. PanGestureRecognizer + TranslationY + spring snap animation. Per-label opacity/scale transforms. Two 1px BoxView selector lines. SelectedIndex/SelectedItem two-way bindable with _suppressCallbacks guard. INotifyCollectionChanged supported.
