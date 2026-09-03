using Bindito.Core;
using Timberborn.TemplateInstantiation;
using Timberborn.Reproduction;

namespace Calloatti.BreedingPodTweaks
{
  [Context("Game")]
  public class BreedingPodTweaksConfigurator : Configurator
  {
    protected override void Configure()
    {
      Bind<BreedingPodTweaks>().AsTransient();
      MultiBind<TemplateModule>().ToProvider<TemplateModuleProvider>().AsSingleton();
    }

    private class TemplateModuleProvider : IProvider<TemplateModule>
    {
      public TemplateModule Get()
      {
        TemplateModule.Builder builder = new TemplateModule.Builder();
        builder.AddDecorator<BreedingPod, BreedingPodTweaks>();
        return builder.Build();
      }
    }
  }
}