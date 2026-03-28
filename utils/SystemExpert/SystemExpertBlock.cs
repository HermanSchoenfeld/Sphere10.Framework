using SystemExpert.Screens;
using Sphere10.Framework.Windows.Forms;

namespace SystemExpert;

public class SystemExpertBlock : ApplicationBlock {

	public static ApplicationBlock Build() {
		return new ApplicationBlockBuilder()
			.WithName("System Expert")
			.WithDefaultScreen<ProcessesScreen>()
			.AddMenu(mb => mb
				.WithText("Tools")
				.AddScreenItem<ProcessesScreen>("Processes", null)
			)
			.Build();
	}
}
