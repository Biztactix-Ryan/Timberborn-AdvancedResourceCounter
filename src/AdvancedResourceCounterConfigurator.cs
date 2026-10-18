using Bindito.Core;
using Timberborn.Automation;
using Timberborn.EntityPanelSystem;
using Timberborn.TemplateInstantiation;

namespace AdvancedResourceCounter {
  [Context("Game")]
  public class AdvancedResourceCounterConfigurator : Configurator {

    protected override void Configure() {
      Bind<AdvancedResourceCounter>().AsTransient();
      Bind<AdvancedResourceCounterBannerSetter>().AsTransient();
      Bind<AdvancedResourceCounterGoodsDropdownProvider>().AsTransient();
      Bind<AdvancedResourceCounterFragment>().AsSingleton();
      MultiBind<TemplateModule>().ToProvider(ProvideTemplateModule).AsSingleton();
      MultiBind<EntityPanelModule>().ToProvider<EntityPanelModuleProvider>().AsSingleton();
    }

    private static TemplateModule ProvideTemplateModule() {
      var builder = new TemplateModule.Builder();
      builder.AddDecorator<AdvancedResourceCounterSpec, AdvancedResourceCounter>();
      builder.AddDecorator<AdvancedResourceCounter, AutomatorIlluminator>();
      builder.AddDecorator<AdvancedResourceCounter, AdvancedResourceCounterBannerSetter>();
      builder.AddDecorator<AdvancedResourceCounter, AdvancedResourceCounterGoodsDropdownProvider>();
      return builder.Build();
    }

    private class EntityPanelModuleProvider : IProvider<EntityPanelModule> {

      private readonly AdvancedResourceCounterFragment _fragment;

      public EntityPanelModuleProvider(AdvancedResourceCounterFragment fragment) {
        _fragment = fragment;
      }

      public EntityPanelModule Get() {
        var builder = new EntityPanelModule.Builder();
        builder.AddMiddleFragment(_fragment);
        return builder.Build();
      }

    }

  }
}
