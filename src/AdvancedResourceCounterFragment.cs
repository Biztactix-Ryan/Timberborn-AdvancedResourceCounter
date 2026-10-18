using System;
using Timberborn.AutomationBuildings;
using Timberborn.BaseComponentSystem;
using Timberborn.CoreUI;
using Timberborn.DropdownSystem;
using Timberborn.EntityPanelSystem;
using Timberborn.Localization;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedResourceCounter {
  /// <summary>
  /// Reuses the vanilla Resource Counter panel layout (loaded twice so we get a second threshold
  /// field and slider with identical styling), replacing the comparison dropdown with ON/OFF labels.
  /// </summary>
  internal class AdvancedResourceCounterFragment : IEntityPanelFragment {

    private static readonly string VanillaView = "Game/EntityPanel/ResourceCounterFragment";
    private static readonly string PercentModeLocKey = "Building.ResourceCounter.Mode.Percent";
    private static readonly string QuantityModeLocKey = "Building.ResourceCounter.Mode.Quantity";
    private static readonly string MeasurementLocKey = "Automation.Measurement";
    private static readonly string OnBelowLocKey = "AdvancedResourceCounter.OnBelow";
    private static readonly string OffAtLocKey = "AdvancedResourceCounter.OffAt";
    private static readonly string OnBelowPercentLocKey = "AdvancedResourceCounter.OnBelowPercent";
    private static readonly string OffAtPercentLocKey = "AdvancedResourceCounter.OffAtPercent";
    private static readonly string StateOnLocKey = "AdvancedResourceCounter.StateOn";
    private static readonly string StateOffLocKey = "AdvancedResourceCounter.StateOff";
    private static readonly float PercentStep = 0.01f;

    private readonly VisualElementLoader _visualElementLoader;
    private readonly DropdownItemsSetter _dropdownItemsSetter;
    private readonly RadioToggleFactory _radioToggleFactory;
    private readonly ILoc _loc;

    private VisualElement _root;
    private RadioToggle _modeRadioToggle;
    private Dropdown _goodDropdown;
    private Toggle _includeInputsToggle;
    private Label _measurement;
    private VisualElement _lowRow;
    private VisualElement _highRow;
    private IntegerField _lowThreshold;
    private IntegerField _highThreshold;
    private Label _lowFillRateLabel;
    private Label _highFillRateLabel;
    private PreciseSlider _lowFillRateSlider;
    private PreciseSlider _highFillRateSlider;
    private Label _state;
    private AdvancedResourceCounter _counter;

    public AdvancedResourceCounterFragment(VisualElementLoader visualElementLoader,
                                           DropdownItemsSetter dropdownItemsSetter,
                                           RadioToggleFactory radioToggleFactory,
                                           ILoc loc) {
      _visualElementLoader = visualElementLoader;
      _dropdownItemsSetter = dropdownItemsSetter;
      _radioToggleFactory = radioToggleFactory;
      _loc = loc;
    }

    public VisualElement InitializeFragment() {
      _root = _visualElementLoader.LoadVisualElement(VanillaView);
      var donor = _visualElementLoader.LoadVisualElement(VanillaView);
      var bottom = _root.Q<VisualElement>("BottomSection");

      _modeRadioToggle = _radioToggleFactory.CreateLocalizable(
          new[] { PercentModeLocKey, QuantityModeLocKey },
          _root.Q<VisualElement>("ModeRadioToggleContainer"));
      _modeRadioToggle.RadioButtonSelected += (_, mode) =>
          _counter.SetMode((ResourceCounterMode) mode);

      _goodDropdown = _root.Q<Dropdown>("Good");
      _measurement = _root.Q<Label>("Measurement");
      _includeInputsToggle = _root.Q<Toggle>("Toggle");
      _includeInputsToggle.RegisterValueChangedCallback(evt => _counter.SetIncludeInputs(evt.newValue));

      _lowRow = _root.Q<VisualElement>("ComparisonWrapper");
      _lowThreshold = SetUpThresholdRow(_lowRow, OnBelowLocKey, OnLowThresholdChanged);
      _lowFillRateLabel = _root.Q<Label>("FillRateLabel");
      _lowFillRateSlider = SetUpSlider(_root.Q<PreciseSlider>("FillRateSlider"), OnLowFillRateChanged);

      _highRow = donor.Q<VisualElement>("ComparisonWrapper");
      _highThreshold = SetUpThresholdRow(_highRow, OffAtLocKey, OnHighThresholdChanged);
      _highFillRateLabel = donor.Q<Label>("FillRateLabel");
      _highFillRateSlider = SetUpSlider(donor.Q<PreciseSlider>("FillRateSlider"), OnHighFillRateChanged);
      bottom.Add(_highRow);
      bottom.Add(_highFillRateLabel);
      bottom.Add(_highFillRateSlider);

      _state = new Label();
      _state.AddToClassList("game-text-normal");
      _state.style.unityTextAlign = TextAnchor.MiddleCenter;
      _state.style.marginTop = 4;
      bottom.Add(_state);

      _root.ToggleDisplayStyle(false);
      return _root;
    }

    public void ShowFragment(BaseComponent entity) {
      _counter = entity.GetComponent<AdvancedResourceCounter>();
      if (!_counter) {
        return;
      }
      _dropdownItemsSetter.SetItems(
          _goodDropdown, _counter.GetComponent<AdvancedResourceCounterGoodsDropdownProvider>());
      _includeInputsToggle.SetValueWithoutNotify(_counter.IncludeInputs);
      RefreshThresholds();
      _root.ToggleDisplayStyle(true);
    }

    public void ClearFragment() {
      _counter = null;
      _root.ToggleDisplayStyle(false);
    }

    public void UpdateFragment() {
      if (!_counter) {
        return;
      }
      var quantity = _counter.Mode == ResourceCounterMode.StockLevel;
      _modeRadioToggle.Update((int) _counter.Mode);
      _measurement.text = quantity
          ? _loc.T(MeasurementLocKey, _counter.SampledResourceCount.ToString())
          : _loc.T(MeasurementLocKey, FormatPercent(_counter.SampledFillRate));
      _lowFillRateSlider.SetMarker(_counter.SampledFillRate);
      _highFillRateSlider.SetMarker(_counter.SampledFillRate);
      _lowFillRateLabel.text = _loc.T(OnBelowPercentLocKey, FormatPercent(_counter.LowFillRate));
      _highFillRateLabel.text = _loc.T(OffAtPercentLocKey, FormatPercent(_counter.HighFillRate));
      _state.text = _loc.T(_counter.Latched ? StateOnLocKey : StateOffLocKey);

      _includeInputsToggle.ToggleDisplayStyle(quantity);
      _lowRow.ToggleDisplayStyle(quantity);
      _highRow.ToggleDisplayStyle(quantity);
      _lowFillRateLabel.ToggleDisplayStyle(!quantity);
      _lowFillRateSlider.ToggleDisplayStyle(!quantity);
      _highFillRateLabel.ToggleDisplayStyle(!quantity);
      _highFillRateSlider.ToggleDisplayStyle(!quantity);
    }

    private IntegerField SetUpThresholdRow(VisualElement row, string labelLocKey,
                                           Action<int> onChanged) {
      var comparisonDropdown = row.Q<Dropdown>("ComparisonMode");
      var label = new Label(_loc.T(labelLocKey));
      label.AddToClassList("game-text-normal");
      label.style.unityTextAlign = TextAnchor.MiddleLeft;
      label.style.width = 70;
      label.style.marginRight = 8;
      row.Insert(row.IndexOf(comparisonDropdown), label);
      comparisonDropdown.RemoveFromHierarchy();

      var field = row.Q<IntegerField>("Threshold");
      field.isDelayed = true;
      field.RegisterValueChangedCallback(evt => onChanged(evt.newValue));
      return field;
    }

    private static PreciseSlider SetUpSlider(PreciseSlider slider, Action<float> onChanged) {
      slider.SetStepWithoutNotify(PercentStep);
      slider.SetValueChangedCallback(onChanged);
      return slider;
    }

    private void OnLowThresholdChanged(int value) {
      _counter.SetLowThreshold(value);
      RefreshThresholds();
    }

    private void OnHighThresholdChanged(int value) {
      _counter.SetHighThreshold(value);
      RefreshThresholds();
    }

    private void OnLowFillRateChanged(float value) {
      _counter.SetLowFillRate(value);
      _highFillRateSlider.UpdateValuesWithoutNotify(_counter.HighFillRate, 1f);
    }

    private void OnHighFillRateChanged(float value) {
      _counter.SetHighFillRate(value);
      _lowFillRateSlider.UpdateValuesWithoutNotify(_counter.LowFillRate, 1f);
    }

    // Pushes the counter's (possibly clamped) thresholds back into the inputs.
    private void RefreshThresholds() {
      _lowThreshold.SetValueWithoutNotify(_counter.LowThreshold);
      _highThreshold.SetValueWithoutNotify(_counter.HighThreshold);
      _lowFillRateSlider.UpdateValuesWithoutNotify(_counter.LowFillRate, 1f);
      _highFillRateSlider.UpdateValuesWithoutNotify(_counter.HighFillRate, 1f);
    }

    private static string FormatPercent(float value) => $"{Mathf.RoundToInt(value * 100)}%";

  }
}
