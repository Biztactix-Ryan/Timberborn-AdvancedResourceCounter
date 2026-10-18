using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Timberborn.BaseComponentSystem;
using Timberborn.DropdownSystem;
using Timberborn.EntitySystem;
using Timberborn.Goods;
using Timberborn.GoodsUI;
using UnityEngine;

namespace AdvancedResourceCounter {
  internal class AdvancedResourceCounterGoodsDropdownProvider : BaseComponent, IAwakableComponent,
                                                                IInitializableEntity,
                                                                IExtendedTooltipDropdownProvider {

    private readonly IGoodService _goodService;
    private readonly GoodDescriber _goodDescriber;
    private AdvancedResourceCounter _counter;

    public IReadOnlyList<string> Items { get; private set; }

    public AdvancedResourceCounterGoodsDropdownProvider(IGoodService goodService,
                                                        GoodDescriber goodDescriber) {
      _goodService = goodService;
      _goodDescriber = goodDescriber;
    }

    public void Awake() {
      _counter = GetComponent<AdvancedResourceCounter>();
    }

    public void InitializeEntity() {
      Items = _goodService.Goods.OrderBy(good => FormatDisplayText(good, false)).ToImmutableArray();
    }

    public string GetValue() => _counter.GoodId;

    public void SetValue(string goodId) => _counter.SetGoodId(goodId);

    public string FormatDisplayText(string goodId, bool selected) =>
        _goodService.GetGood(goodId).DisplayName.Value;

    public Sprite GetIcon(string goodId) => _goodDescriber.GetIcon(goodId);

    public ImmutableArray<string> GetItemClasses(string value) => ImmutableArray<string>.Empty;

    public string GetDropdownTooltip(string value) => FormatDisplayText(value, false);

  }
}
