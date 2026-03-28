using Sphere10.Framework.Application;
using Sphere10.Framework.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace SystemExpert;

public class ModuleConfiguration : ModuleConfigurationBase {
	public override void RegisterComponents(IServiceCollection serviceCollection) {
		serviceCollection.AddInitializer<IncrementUsageByOneInitializer>();
		serviceCollection.AddApplicationBlock(SystemExpertBlock.Build());
	}
}
