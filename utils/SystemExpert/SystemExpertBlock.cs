using SystemExpert.Screens;
using Sphere10.Framework.Windows.Forms;

namespace SystemExpert;

public class SystemExpertBlock : WinFormsApplicationBlock {

	public static WinFormsApplicationBlock Build() {
		return new WinFormsApplicationBlockBuilder()
			.WithName("System Expert")
			.WithDefaultScreen<SystemInfoScreen>(title: "System Info")
			.AddMenu(Menu => Menu
				.WithText("Tools")
				.ConfigureItem(Item => Item.AsScreenItem().WithText("System Info").WithScreen<SystemInfoScreen>().AsSingleInstance().WithTitle("System Info"))
				.ConfigureItem(Item => Item.AsScreenItem().WithText("Processes").WithScreen<ProcessesScreen>().AsSingleInstance().WithTitle("Processes"))
				.ConfigureItem(Item => Item.AsScreenItem().WithText("Services").WithScreen<ServicesScreen>().AsSingleInstance().WithTitle("Services"))
				.ConfigureItem(Item => Item.AsScreenItem().WithText("Network").WithScreen<NetworkScreen>().AsSingleInstance().WithTitle("Network"))
				.ConfigureItem(Item => Item.AsScreenItem().WithText("Event Log").WithScreen<EventLogScreen>().AsSingleInstance().WithTitle("Event Log"))
				.ConfigureItem(Item => Item.AsScreenItem().WithText("Environment").WithScreen<EnvironmentScreen>().AsSingleInstance().WithTitle("Environment"))
			)
			.Build();
	}
}

