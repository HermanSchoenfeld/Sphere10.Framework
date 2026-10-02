# SystemExpert

A WinForms system monitor built on the WinFormsApplicationBlock screen host. It starts in `ScreenMode.MultiView` and uses the framework's default `MultiInstance` policy for System Info, Processes, Services, Network, Event Log, and Environment. Each menu activation opens an independent tool instance; select an existing tab or detached window to return to that instance.

Build and run on Windows:

```powershell
dotnet run --project utils/SystemExpert/SystemExpert.csproj
```

## Tabs and detached tools

- Open several tools and switch tabs. The main toolbar follows the selected tool. System Info has no screen toolbar or menu.
- Drag a tab to reorder it, or drag it outside the tab strip to detach it. Detached tools retain their own toolbar controls, including refresh intervals and the event-log selector. Tools without menus or toolbars have no empty bars below the caption.
- Detached windows have compact Re-dock, Minimize, Maximize/Restore, and Close icons. Drag their title near the main tabs until **Release to dock** appears, then release. The Re-dock icon or **Ctrl+Shift+D** also returns the tool.
- Use the sidebar icon in the main toolbar, or **Ctrl+Alt+M**, to hide and show navigation without a separate toggle panel.
- Close a tab or detached window to release that tool. Selecting its navigation item creates it again. Initial data binding and System Info's refresh timer run once per screen instance, so moving it between hosts preserves the screen's state.

The application explicitly uses `HighDpiMode.DpiUnaware` for Windows bitmap scaling, consistent with WinForms Tester. The shared build settings and project set `ForceDesignerDpiUnaware=true` for the designer.

## Hosting checks

Open Processes, Services, Network, and System Info together. Set a refresh interval in a grid tool, switch tabs, then detach and re-dock it. Its refresh controls should keep their values and act on the same tool. Detach System Info to check the content-only host, and use the window icons and sidebar toggle in both expanded and collapsed states. Exit from the main window and confirm that all detached tools close with it.

## Future work

Process
 - View All

Services
  View All
  Create Service 

Environment
  Variables
  Startup

Networking
  View TCP/IP Ports,
  SMTP Emailer with memory

Scheduled Tasks
  - Calendar view
  - Editable via Calendar View  

Registry
  - View all   
  - Editable

Logs
  . Auto show all logs
  .
  .

Files
	Monitor a folder for changes
	Search file lock


Grid changes
  - layout bugs
  - image cells
  - delelect bug
