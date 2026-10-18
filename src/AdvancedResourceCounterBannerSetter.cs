using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.Buildings;
using Timberborn.EntitySystem;
using Timberborn.Goods;
using UnityEngine;

namespace AdvancedResourceCounter {
  /// <summary>Shows the selected good's icon on the building's banner, like the vanilla counter.</summary>
  internal class AdvancedResourceCounterBannerSetter : BaseComponent, IAwakableComponent,
                                                       IInitializablePreview, IInitializableEntity {

    private static readonly Color BannerIconColor = new(0.33f, 0.33f, 0.33f);

    private readonly GoodIconVisualizer _goodIconVisualizer;
    private readonly IGoodService _goodService;
    private BlockObject _blockObject;
    private AdvancedResourceCounter _counter;
    private MeshRenderer _meshRenderer;

    public AdvancedResourceCounterBannerSetter(GoodIconVisualizer goodIconVisualizer,
                                               IGoodService goodService) {
      _goodIconVisualizer = goodIconVisualizer;
      _goodService = goodService;
    }

    public void Awake() {
      _blockObject = GetComponent<BlockObject>();
      _counter = GetComponent<AdvancedResourceCounter>();
      _meshRenderer = GetComponent<BuildingModel>().FinishedModel
          .GetComponentInChildren<MeshRenderer>();
    }

    public void InitializePreview() {
      UpdateBanner();
    }

    public void InitializeEntity() {
      _counter.GoodChanged += (_, _) => UpdateBanner();
      UpdateBanner();
    }

    private void UpdateBanner() {
      var goodId = _counter.GoodId;
      if (string.IsNullOrWhiteSpace(goodId)) {
        _goodIconVisualizer.HideColoredIcon(_meshRenderer.material);
        return;
      }
      _goodIconVisualizer.ShowColoredIcon(_meshRenderer.material, _goodService.GetGood(goodId),
                                          _blockObject.FlipMode.IsFlipped, BannerIconColor);
    }

  }
}
