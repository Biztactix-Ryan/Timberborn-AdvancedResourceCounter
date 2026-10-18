using System;
using Timberborn.Automation;
using Timberborn.AutomationBuildings;
using Timberborn.BaseComponentSystem;
using Timberborn.DuplicationSystem;
using Timberborn.GameDistricts;
using Timberborn.Goods;
using Timberborn.Persistence;
using Timberborn.ResourceCountingSystem;
using Timberborn.WorldPersistence;

namespace AdvancedResourceCounter {
  /// <summary>
  /// Resource counter with hysteresis: output turns ON when the measured value drops below the low
  /// threshold and stays ON until it reaches the high threshold. Equivalent to two resource counters
  /// feeding a Set/Reset memory.
  /// </summary>
  public class AdvancedResourceCounter : BaseComponent, IAwakableComponent, IPersistentEntity,
                                         IDuplicable<AdvancedResourceCounter>, ISamplingTransmitter {

    private static readonly ComponentKey ComponentKey = new("AdvancedResourceCounter");
    private static readonly PropertyKey<string> GoodIdKey = new("GoodId");
    private static readonly PropertyKey<ResourceCounterMode> ModeKey = new("Mode");
    private static readonly PropertyKey<bool> IncludeInputsKey = new("IncludeInputs");
    private static readonly PropertyKey<int> LowThresholdKey = new("LowThreshold");
    private static readonly PropertyKey<int> HighThresholdKey = new("HighThreshold");
    private static readonly PropertyKey<float> LowFillRateKey = new("LowFillRate");
    private static readonly PropertyKey<float> HighFillRateKey = new("HighFillRate");
    private static readonly PropertyKey<bool> LatchedKey = new("Latched");

    private readonly IGoodService _goodService;
    private readonly ResourceCountingService _resourceCountingService;
    private Automator _automator;
    private DistrictBuilding _districtBuilding;

    public string GoodId { get; private set; }
    public ResourceCounterMode Mode { get; private set; } = ResourceCounterMode.StockLevel;
    public bool IncludeInputs { get; private set; }
    public int LowThreshold { get; private set; } = 50;
    public int HighThreshold { get; private set; } = 200;
    public float LowFillRate { get; private set; } = 0.2f;
    public float HighFillRate { get; private set; } = 0.8f;
    public bool Latched { get; private set; }
    public int SampledResourceCount { get; private set; }
    public float SampledFillRate { get; private set; }

    public event EventHandler<string> GoodChanged;

    public AdvancedResourceCounter(IGoodService goodService,
                                   ResourceCountingService resourceCountingService) {
      _goodService = goodService;
      _resourceCountingService = resourceCountingService;
    }

    public void Awake() {
      GoodId = _goodService.Goods[0];
      _automator = GetComponent<Automator>();
      _districtBuilding = GetComponent<DistrictBuilding>();
      _districtBuilding.ReassignedInstantDistrict += OnReassignedDistrict;
      _districtBuilding.ReassignedConstructionDistrict += OnReassignedDistrict;
    }

    public void Save(IEntitySaver entitySaver) {
      var saver = entitySaver.GetComponent(ComponentKey);
      saver.Set(GoodIdKey, GoodId);
      saver.Set(ModeKey, Mode);
      saver.Set(IncludeInputsKey, IncludeInputs);
      saver.Set(LowThresholdKey, LowThreshold);
      saver.Set(HighThresholdKey, HighThreshold);
      saver.Set(LowFillRateKey, LowFillRate);
      saver.Set(HighFillRateKey, HighFillRate);
      saver.Set(LatchedKey, Latched);
    }

    public void Load(IEntityLoader entityLoader) {
      if (!entityLoader.TryGetComponent(ComponentKey, out var loader)) {
        return;
      }
      if (loader.Has(GoodIdKey)) {
        GoodId = loader.Get(GoodIdKey);
      }
      if (loader.Has(ModeKey)) {
        Mode = loader.Get(ModeKey);
      }
      if (loader.Has(IncludeInputsKey)) {
        IncludeInputs = loader.Get(IncludeInputsKey);
      }
      if (loader.Has(LowThresholdKey)) {
        LowThreshold = loader.Get(LowThresholdKey);
      }
      if (loader.Has(HighThresholdKey)) {
        HighThreshold = loader.Get(HighThresholdKey);
      }
      if (loader.Has(LowFillRateKey)) {
        LowFillRate = loader.Get(LowFillRateKey);
      }
      if (loader.Has(HighFillRateKey)) {
        HighFillRate = loader.Get(HighFillRateKey);
      }
      if (loader.Has(LatchedKey)) {
        Latched = loader.Get(LatchedKey);
      }
    }

    public void DuplicateFrom(AdvancedResourceCounter source) {
      Mode = source.Mode;
      GoodId = source.GoodId;
      GoodChanged?.Invoke(this, GoodId);
      IncludeInputs = source.IncludeInputs;
      LowThreshold = source.LowThreshold;
      HighThreshold = source.HighThreshold;
      LowFillRate = source.LowFillRate;
      HighFillRate = source.HighFillRate;
      Sample();
    }

    public void SetGoodId(string goodId) {
      GoodId = goodId;
      GoodChanged?.Invoke(this, goodId);
      Sample();
    }

    public void SetMode(ResourceCounterMode mode) {
      Mode = mode;
      Sample();
    }

    public void SetIncludeInputs(bool includeInputs) {
      IncludeInputs = includeInputs;
      Sample();
    }

    // Thresholds keep Low <= High: moving one past the other drags the other along.
    public void SetLowThreshold(int value) {
      LowThreshold = Math.Max(0, value);
      HighThreshold = Math.Max(HighThreshold, LowThreshold);
      UpdateOutputState();
    }

    public void SetHighThreshold(int value) {
      HighThreshold = Math.Max(0, value);
      LowThreshold = Math.Min(LowThreshold, HighThreshold);
      UpdateOutputState();
    }

    public void SetLowFillRate(float value) {
      LowFillRate = Math.Clamp(value, 0f, 1f);
      HighFillRate = Math.Max(HighFillRate, LowFillRate);
      UpdateOutputState();
    }

    public void SetHighFillRate(float value) {
      HighFillRate = Math.Clamp(value, 0f, 1f);
      LowFillRate = Math.Min(LowFillRate, HighFillRate);
      UpdateOutputState();
    }

    public void Sample() {
      var district = _districtBuilding.GetInstantOrConstructionDistrict();
      var count = district
          ? _resourceCountingService.GetDistrictResourceCounter(district).GetResourceCount(GoodId)
          : ResourceCount.Empty;
      SampledFillRate = count.FillRate;
      SampledResourceCount = IncludeInputs ? count.AllStock : count.AvailableStock;
      UpdateOutputState();
    }

    private void OnReassignedDistrict(object sender, EventArgs e) {
      Sample();
    }

    private void UpdateOutputState() {
      if (!_districtBuilding.GetInstantOrConstructionDistrict()) {
        _automator.SetState(false);
        return;
      }
      var (low, high) = Mode switch {
          ResourceCounterMode.StockLevel => (SampledResourceCount < LowThreshold,
                                             SampledResourceCount >= HighThreshold),
          ResourceCounterMode.FillRate => (SampledFillRate < LowFillRate,
                                           SampledFillRate >= HighFillRate),
          _ => throw new ArgumentOutOfRangeException()
      };
      if (high) {
        Latched = false;
      } else if (low) {
        Latched = true;
      }
      _automator.SetState(Latched);
    }

  }
}
