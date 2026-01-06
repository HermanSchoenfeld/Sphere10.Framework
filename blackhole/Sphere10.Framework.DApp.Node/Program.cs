// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

public class Program {
	static void Main(string[] args) {
	}
}


//using Sphere10.Framework;
//using System;
//using System.IO;
//using System.IO.Pipes;
//using System.Linq;
//using System.Net.Sockets;
//using System.Reflection;
//using System.Security.Policy;
//using System.Threading;
//using System.Threading.Tasks;
//using Sphere10.Framework.DApp.Node;
//using Sphere10.Framework.Application;
//using Sphere10.Framework.DApp.Node.UI;
//using Sphere10.Framework.DApp.Node.RPC;
//using Sphere10.Framework.Consensus;
//using Sphere10.Framework.DApp.Core.Mining;
//using Sphere10.Framework.DApp.Core.Consensus.Serializers;
//using Sphere10.Framework.DApp.Node.UI.Components;

//using Sphere10.Framework.CryptoEx;
//using Sphere10.Framework.DApp.Core.Runtime;

//namespace Sphere10.Framework.DApp.Node {

//	class Program {


//		private static CommandLineParameters Arguments = new CommandLineParameters() {
//			Header = new[] {
//				"Sphere10.FrameworkP2P Node {CurrentVersion}",
//				"Copyright (c) Sphere 10 Software 2021 - {CurrentYear}"
//			},

//			Parameters = new CommandLineParameter[] {
//				new("host", "Host read/write ports ", CommandLineParameterOptions.Optional | CommandLineParameterOptions.RequiresValue),
//			},

//			Options = CommandLineArgumentOptions.DoubleDash | CommandLineArgumentOptions.PrintHelpOnH
//		};


//		static async Task Main(string[] args) {
//			var userArgsResult = Arguments.TryParseArguments(args);
//			if (userArgsResult.Failure) {
//				userArgsResult.ErrorMessages.ForEach(Console.WriteLine);
//				return;
//			}
//			var userArgs = userArgsResult.Value;
//			if (userArgs.HelpRequested) {
//				Arguments.PrintHelp();
//				return;
//			}

//			Sphere10Framework.Instance.StartFramework();

//			var stopNodeTokenSource = new CancellationTokenSource();

//			INode hostedNode = default;
//			Task hostedNodeRunner = default;
//			if (userArgs.Arguments.Contains("host")) {
//				var hostParams = userArgs.Arguments["host"].Single();
//				var splits = hostParams.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
//				if (splits.Length != 2 || !int.TryParse(splits[0], out var readPort) || !int.TryParse(splits[1], out var writePort)) {
//					Console.WriteLine("Invalid format for host read/write port");
//					return;
//				}
//				//hostedNode = new Sphere10.Framework.DApp.Core.Runtime.Node()
//				//hostedNodeRunner = hostedNode.Run(stopNodeTokenSource.Token);
//			}

//			Navigator.Start(stopNodeTokenSource.Token);

//			if (hostedNode != default) {
//				await hostedNode.RequestShutdown();
//				await hostedNodeRunner;
//			}
//		}
//	}
//}

