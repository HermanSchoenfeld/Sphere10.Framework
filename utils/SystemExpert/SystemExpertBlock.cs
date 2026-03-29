using SystemExpert.Screens;
using Sphere10.Framework.Windows.Forms;

namespace SystemExpert;

public class SystemExpertBlock : ApplicationBlock {

	public static ApplicationBlock Build() {
		return new ApplicationBlockBuilder()
			.WithName("System Expert")
			.WithDefaultScreen<SystemInfoScreen>()
			.AddMenu(mb => mb
				.WithText("Tools")
				.AddScreenItem<SystemInfoScreen>("System Info", null)
				.AddScreenItem<ProcessesScreen>("Processes", null)
				.AddScreenItem<ServicesScreen>("Services", null)
				.AddScreenItem<NetworkScreen>("Network", null)
				.AddScreenItem<EventLogScreen>("Event Log", null)
				.AddScreenItem<EnvironmentScreen>("Environment", null)
			)
			.Build();
	}
}

