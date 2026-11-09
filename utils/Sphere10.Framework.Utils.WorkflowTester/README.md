# Workflow Tester

A Windows Forms tool for trying WorkflowCore workflows interactively, including step failures, suspension and resumption.

The form starts a WorkflowCore host with three registered workflows and writes their progress to its output box and to the console. **Test1** starts `Workflow1`, whose three steps are `StepBodyAsync` classes. **Test2** starts `Workflow2`, whose three steps are inline asynchronous delegates. **Subscription** starts a loop that waits, prints a charge warning and waits again. At the end of each pass the loop continues unless **Cancel Subscriptions** is checked.

The **Workflow Error Source** group makes step 1, 2 or 3 throw an `InvalidOperationException`. Every workflow suspends on an error, and **Resume Workflow** asks for a workflow ID and resumes it. **2 second delay at step** adds a two-second wait to each step of the test workflows. **View Workflows** prints the state of every instance started in this session. The host uses WorkflowCore's default in-memory persistence, so workflow state ends with the process. A commented-out line in `ConfigureServices` shows a SQL Server persistence alternative, which the project's package references support.

## Build and run

The project targets `net10.0-windows` and references `Sphere10.Framework`, `Sphere10.Framework.Application`, `Sphere10.Framework.Windows` and `Sphere10.Framework.Windows.Forms`. Neither `src/Sphere10.Framework (Win).sln` nor `src/Sphere10.Framework (CrossPlatform).sln` includes it, so no solution build checks that it still compiles. To try it, run the project file directly from the repository root:

```powershell
dotnet run --project utils/Sphere10.Framework.Utils.WorkflowTester -c Release -p:BuildRevision=0
```

<!-- domains:begin -->
## Domains

Every folder of `Sphere10.Framework.Utils.WorkflowTester` is a domain with its own README, which lists all of its public types.

| Domain | What it provides | Source |
|---|---|---|

### Project root

Types declared in the project folder itself:

| Type | Kind | Purpose |
|---|---|---|
| [`WorkflowTestForm`](WorkflowTestForm.cs) | class | Form that hosts an `IWorkflowHost` with a subscription workflow and two step workflows, lets the user choose a failing step and writes step output to a text box. Its sample workflows are nested types. |
<!-- domains:end -->

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`WorkflowTestForm`](WorkflowTestForm.cs) | partial class | Main window that hosts WorkflowCore, starts and resumes workflows, selects the failing step and prints workflow state. |
| [`WorkflowTestForm.SubscriptionBag`](WorkflowTestForm.cs) | nested class | Data of the subscription workflow, with its warning and duration seconds, active flag and charge count. |
| [`WorkflowTestForm.SubscriptionWorkflow`](WorkflowTestForm.cs) | nested class | `Subscription` workflow that loops through a warning delay and a charge-or-cancel decision while the subscription is active. |
| [`WorkflowTestForm.TestWorkflow1`](WorkflowTestForm.cs) | nested class | `Workflow1`, which runs its nested `Step1`, `Step2` and `Step3` step-body classes in order. |
| [`WorkflowTestForm.TestWorkflow2`](WorkflowTestForm.cs) | nested class | `Workflow2`, which runs three inline asynchronous steps in order. |
