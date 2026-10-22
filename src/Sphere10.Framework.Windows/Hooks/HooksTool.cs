// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;

namespace Sphere10.Framework.Windows;

public static class HooksTool {

	internal const int MaxExceptionsBeforeAbort = 20;

	/// <summary>Checks current physical button state independently of the calling thread's message queue.</summary>
	public static bool IsAnyMouseButtonDown() =>
		WinAPI.USER32.GetAsyncKeyState((int)VirtualKey.LButton) < 0 ||
		WinAPI.USER32.GetAsyncKeyState((int)VirtualKey.RButton) < 0 ||
		WinAPI.USER32.GetAsyncKeyState((int)VirtualKey.MButton) < 0 ||
		WinAPI.USER32.GetAsyncKeyState((int)VirtualKey.XButton1) < 0 ||
		WinAPI.USER32.GetAsyncKeyState((int)VirtualKey.XButton2) < 0;

	public static void SimulateKeyboardDeviceAction(ScanCode scanCode, KeyState keyState) {
		var inputs = new WinAPI.USER32.INPUT[] {
			new WinAPI.USER32.INPUT {
				type = WinAPI.USER32.INPUT_TYPE.INPUT_KEYBOARD,
				di = new WinAPI.USER32.DEVICEINPUTUNION {
					ki = {
						wScan = scanCode,
						dwFlags = WinAPI.USER32.KEYEVENTF.SCANCODE | (keyState == KeyState.Up ? WinAPI.USER32.KEYEVENTF.KEYUP : 0)
					}
				}
			}
		};

		if (WinAPI.USER32.SendInput((uint)inputs.Length, inputs, WinAPI.USER32.INPUT.Size) == 0) {
			throw new Exception("Failed to emulate keyboard event");
		}

	}

	public static void SimulateMouseDeviceAction(MouseButton button, MouseButtonState state, int x, int y) {
		var inputs = new List<WinAPI.USER32.INPUT>();


		if (button == MouseButton.None) {
			inputs.Add(
				new WinAPI.USER32.INPUT {
					type = WinAPI.USER32.INPUT_TYPE.INPUT_MOUSE,
					di = new WinAPI.USER32.DEVICEINPUTUNION {
						mi = {
							dx = x,
							dy = y,
							dwFlags = WinAPI.USER32.MOUSEEVENTF.MOVE_NOCOALESCE
						}
					}
				}
			);
		} else {

			WinAPI.USER32.MOUSEEVENTF buttonFlag;
			switch (button) {
				case MouseButton.Left:
					buttonFlag = state == MouseButtonState.Down ? WinAPI.USER32.MOUSEEVENTF.LEFTDOWN : WinAPI.USER32.MOUSEEVENTF.LEFTUP;
					break;
				case MouseButton.Middle:
					buttonFlag = state == MouseButtonState.Down ? WinAPI.USER32.MOUSEEVENTF.MIDDLEDOWN : WinAPI.USER32.MOUSEEVENTF.MIDDLEUP;
					break;
				case MouseButton.Right:
					buttonFlag = state == MouseButtonState.Down ? WinAPI.USER32.MOUSEEVENTF.RIGHTDOWN : WinAPI.USER32.MOUSEEVENTF.RIGHTUP;
					break;
				default:
					buttonFlag = 0;
					break;
			}

			inputs.Add(
				new WinAPI.USER32.INPUT {
					type = WinAPI.USER32.INPUT_TYPE.INPUT_MOUSE,
					di = new WinAPI.USER32.DEVICEINPUTUNION {
						mi = {
							dx = x,
							dy = y,
							dwFlags = WinAPI.USER32.MOUSEEVENTF.ABSOLUTE | buttonFlag
						}
					}
				}
			);
		}


		if (WinAPI.USER32.SendInput((uint)inputs.Count, inputs.ToArray(), WinAPI.USER32.INPUT.Size) == 0) {
			throw new Exception("Failed to emulate mouse event");
		}
	}

	/// <summary>Moves the vertical mouse wheel by the signed delta; positive is up and one notch is 120 units.</summary>
	public static void SimulateMouseWheel(int delta) {
		if (delta == 0)
			return;
		var inputs = new[] {
			new WinAPI.USER32.INPUT {
				type = WinAPI.USER32.INPUT_TYPE.INPUT_MOUSE,
				di = new WinAPI.USER32.DEVICEINPUTUNION {
					mi = {
						mouseData = unchecked((WinAPI.USER32.MouseEventDataXButtons)(uint)delta),
						dwFlags = WinAPI.USER32.MOUSEEVENTF.WHEEL
					}
				}
			}
		};
		if (WinAPI.USER32.SendInput((uint)inputs.Length, inputs, WinAPI.USER32.INPUT.Size) != inputs.Length)
			throw new WindowsException(WinAPI.KERNEL32.GetLastError(), "Windows could not emulate mouse wheel input.");
	}

}

