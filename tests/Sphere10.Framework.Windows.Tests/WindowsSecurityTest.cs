// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using NUnit.Framework;
using System.Security.Principal;
using System.IO;
using Sphere10.Framework.Windows;
using Sphere10.Framework.Windows.Security;
namespace Sphere10.Framework.UnitTests;

[TestFixture]
public class WindowsSecurityTests {
	public string RemoteHostName;


	[OneTimeSetUp]
	public void Init() {
		// NOTE: this is just the local machine being referenced as a remote machine
		RemoteHostName = Environment.MachineName;
	}

	#region Basic tests

	[Test]
	public void TestClass_NTRemoteObject() {

		NTHost host = NTHost.CurrentMachine;

		NTRemoteObject obj = new NTRemoteObject(
			host.Name,
			"Domain",
			"Name",
			host.SID,
			WinAPI.ADVAPI32.SidNameUse.Domain
		);

		Assert.That(obj.Host, Is.EqualTo(host.Name));
		Assert.That(obj.Domain, Is.EqualTo("Domain"));
		Assert.That(obj.Name, Is.EqualTo("Name"));
		Assert.That(obj.SID, Is.EqualTo(host.SID));
		Assert.That(obj.SidNameUsage, Is.EqualTo(WinAPI.ADVAPI32.SidNameUse.Domain));

	}


	[Test]
	public void TestClass_NTDanglingObject() {
		NTHost host = NTHost.CurrentMachine;

		NTDanglingObject obj = new NTDanglingObject(
			host.Name,
			"Name"
		);

		Assert.That(obj.Host, Is.EqualTo(host.Name));
		Assert.That(obj.Name, Is.EqualTo("Name"));
		Assert.That(obj.SID, Is.Null);
	}


	[Test]
	public void TestClass_NTDanglingObject2() {
		NTHost host = NTHost.CurrentMachine;

		NTDanglingObject obj = new NTDanglingObject(
			host.Name,
			host.SID,
			WinAPI.ADVAPI32.SidNameUse.Invalid
		);

		Assert.That(obj.Host, Is.EqualTo(host.Name));
		Assert.That(obj.SID, Is.EqualTo(host.SID));
		Assert.That(obj.NameUse, Is.EqualTo(WinAPI.ADVAPI32.SidNameUse.Invalid));
		Assert.That(obj.Name, Is.EqualTo(string.Empty));
	}

	#endregion

	#region Local tests

	[Test]
	public void TestLocalHost() {
		NTHost host = NTHost.CurrentMachine;
		Assert.That(host, Is.Not.Null);
		Assert.That(Environment.MachineName.ToUpper(), Is.EqualTo(host.Name.ToUpper()));
	}

	[Test]
	public void TestLocalHostGetSid() {
		NTHost host = NTHost.CurrentMachine;
		Assert.That(host.SID, Is.Not.Null);
	}

	[Test]
	public void TestLocalHostGetAdministratorUser() {
		NTHost host = NTHost.CurrentMachine;
		Assert.That(ContainsSid(
				new SecurityIdentifier(WellKnownSidType.AccountAdministratorSid, host.SID),
				host.GetLocalUsers()
			), Is.True);
	}

	[Test]
	public void TestLocalHostAdministratorUserPrivilege() {
		NTHost host = NTHost.CurrentMachine;
		NTLocalUser user = GetObjectByName(host.GetLocalUsers(), "Administrator");
		Assert.That(user.Privilege == UserPrivilege.Admin, Is.True);
	}

	[Test]
	public void TestLocalHostGetGuest() {
		NTHost host = NTHost.CurrentMachine;
		Assert.That(ContainsSid(
				new SecurityIdentifier(WellKnownSidType.AccountGuestSid, host.SID),
				host.GetLocalUsers()
			), Is.True);

	}

	[Test]
	public void TestLocalHostGetAdministratorsGroup() {
		NTHost host = NTHost.CurrentMachine;
		NTLocalGroup[] localGroups = host.GetLocalGroups();
		NTLocalGroup group = GetObjectByName(localGroups, "Administrators");
		SecurityIdentifier localHostSid = host.SID;
		SecurityIdentifier adminSid = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, localHostSid);
		Assert.That(ContainsSid(
				adminSid,
				host.GetLocalGroups()
			), Is.True);
	}

	[Test]
	public void TestLocalHostGetGuestsGroup() {
		NTHost host = NTHost.CurrentMachine;
		Assert.That(ContainsSid(
				new SecurityIdentifier(WellKnownSidType.BuiltinGuestsSid, null),
				host.GetLocalGroups()
			), Is.True);
	}

	[Test]
	public void TestLocalHostGetPowerUsersGroup() {
		NTHost host = NTHost.CurrentMachine;
		Assert.That(ContainsSid(
				new SecurityIdentifier(WellKnownSidType.BuiltinPowerUsersSid, null),
				host.GetLocalGroups()
			), Is.True);
	}

	[Test]
	public void TestLocalUserCreateDelete() {
		NTHost host = NTHost.CurrentMachine;
		// find a unique user name
		string userName = GenerateUserName(host);
		NTLocalUser user = host.CreateLocalUser(userName, "pPnNmm*&");
		Assert.That(user, Is.Not.Null);
		Assert.That(ContainsObjectByName(host.GetLocalUsers(), userName), Is.True);
		user.Delete();
		Assert.That(ContainsObjectByName(host.GetLocalUsers(), userName), Is.False);
	}

	[Test]
	public void TestLocalUserUpdateSID() {
		NTHost host = NTHost.CurrentMachine;
		// find a unique user name
		string userName = GenerateUserName(host);
		NTLocalUser user = host.CreateLocalUser(userName, "pPnNmm*&");
		try {
			Assert.That(user.SID, Is.Not.Null);
		} finally {
			user.Delete();
		}
	}

	[Test]
	public void TestLocalUserSIDContainsHostSID() {
		NTHost host = NTHost.CurrentMachine;
		// find a unique user name
		string userName = GenerateUserName(host);
		NTLocalUser user = host.CreateLocalUser(userName, "pPnNmm*&");
		try {
			Assert.That(host.SID, Is.EqualTo(user.SID.AccountDomainSid));
		} finally {
			user.Delete();
		}
	}

	[Test]
	public void TestLocalUserUpdateHomeDirectory() {
		NTHost host = NTHost.CurrentMachine;
		string value = "c:\\";
		// find a unique user name
		string userName = GenerateUserName(host);
		NTLocalUser user = host.CreateLocalUser(userName, "pPnNmm*&");
		try {
			user.HomeDirectory = value;
			user.Update();
			user = GetObjectByName(host.GetLocalUsers(), userName);
			Assert.That(value, Is.EqualTo(user.HomeDirectory));
		} finally {
			user.Delete();
		}
	}

	[Test]
	public void TestLocalUserUpdateLastLogoff() {
		NTHost host = NTHost.CurrentMachine;
		DateTime value = DateTime.MinValue;
		// find a unique user name
		string userName = GenerateUserName(host);
		NTLocalUser user = host.CreateLocalUser(userName, "pPnNmm*&");
		try {
			user.Update();
			user = GetObjectByName(host.GetLocalUsers(), userName);
			Assert.That(user.LastLogoff, Is.EqualTo(value));
		} finally {
			user.Delete();
		}
	}

	[Test]
	public void TestLocalUserUpdateLastLogon() {
		NTHost host = NTHost.CurrentMachine;
		DateTime value = DateTime.MinValue;
		// find a unique user name
		string userName = GenerateUserName(host);
		NTLocalUser user = host.CreateLocalUser(userName, "pPnNmm*&");
		try {
			user.Update();
			user = GetObjectByName(host.GetLocalUsers(), userName);
			Assert.That(user.LastLogon, Is.EqualTo(value));
		} finally {
			user.Delete();
		}
	}

	[Test]
	public void TestLocalUserUpdateLogonHours() {
		NTHost host = NTHost.CurrentMachine;
		byte[] value = new byte[21] { 1, 1, 0, 1, 1, 1, 1, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 1 };
		// find a unique user name
		string userName = GenerateUserName(host);
		NTLocalUser user = host.CreateLocalUser(userName, "pPnNmm*&");
		user.LogonHours = value;
		user.Update();
		user = GetObjectByName(host.GetLocalUsers(), userName);
		Assert.That(value, Is.EqualTo(user.LogonHours));
		user.Delete();
	}

	[Test]
	public void TestLocalUserUpdateLogonServer() {
		NTHost host = NTHost.CurrentMachine;
		// find a unique user name
		string userName = GenerateUserName(host);
		NTLocalUser user = host.CreateLocalUser(userName, "pPnNmm*&");
		try {
			Assert.That(user.LogonServer, Is.EqualTo("\\\\*"));
		} finally {
			user.Delete();
		}
	}

	[Test, Ignore("Not supported on all platforms")]
	public void TestLocalUserUpdateMaxStorage() {
		NTHost host = NTHost.CurrentMachine;
		uint value = 1000;
		// find a unique user name
		string userName = GenerateUserName(host);
		NTLocalUser user = host.CreateLocalUser(userName, "pPnNmm*&");
		try {
//                user.Flags |= UserFlags.
			user.MaxStorage = value;
			user.Update();
			user = GetObjectByName(host.GetLocalUsers(), userName);
			Assert.That(user.MaxStorage, Is.EqualTo(value));
		} finally {
			user.Delete();
		}
	}

	[Test]
	public void TestLocalUserUpdateName() {
		NTHost host = NTHost.CurrentMachine;
		// find a unique user name
		string userName = GenerateUserName(host);
		NTLocalUser user = host.CreateLocalUser(userName, "pPnNmm*&");
		try {
			Assert.That(userName, Is.EqualTo(user.Name));
		} finally {
			user.Delete();
		}
	}

	[Test]
	public void TestLocalUserUpdateNumberOfLogons() {
		NTHost host = NTHost.CurrentMachine;
		// find a unique user name
		string userName = GenerateUserName(host);
		NTLocalUser user = host.CreateLocalUser(userName, "pPnNmm*&");
		try {
			Assert.That(0, Is.EqualTo(user.NumberOfLogons));
		} finally {
			user.Delete();
		}
	}

	[Test]
	public void TestLocalPersistUserPassword() {
		NTHost host = NTHost.CurrentMachine;
		string value = "AbCn1122CeF123";
		// find a unique user name
		string userName = GenerateUserName(host);
		NTLocalUser user = host.CreateLocalUser(userName, "pPnNmm*&");
		user.Password = value;
		user.Update();
		user.Delete();
	}

	[Test]
	public void TestLocalUserUpdatePasswordAge() {
		NTHost host = NTHost.CurrentMachine;
		// find a unique user name
		string userName = GenerateUserName(host);
		NTLocalUser user = host.CreateLocalUser(userName, "pPnNmm*&");
		try {
			Assert.That(0 <= user.PasswordAge.TotalSeconds && user.PasswordAge.TotalSeconds <= 2, Is.True);
		} finally {
			user.Delete();
		}
	}

	[Test]
	public void TestLocalUserUpdatePrivilege() {
		NTHost host = NTHost.CurrentMachine;
		// find a unique user name
		string userName = GenerateUserName(host);
		NTLocalUser user = host.CreateLocalUser(userName, "pPnNmm*&");
		try {
			Assert.That(UserPrivilege.Guest, Is.EqualTo(user.Privilege));
		} finally {
			user.Delete();
		}
	}

	[Test]
	public void TestLocalUserUpdateScriptPath() {
		NTHost host = NTHost.CurrentMachine;
		string value = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "xcopy.exe");
		// find a unique user name
		string userName = GenerateUserName(host);
		NTLocalUser user = host.CreateLocalUser(userName, "pPnNmm*&");
		try {
			user.ScriptPath = value;
			user.Update();
			user = GetObjectByName(host.GetLocalUsers(), userName);
			Assert.That(value, Is.EqualTo(user.ScriptPath));
		} finally {
			user.Delete();
		}
	}


	[Test, Ignore("Not supported on all platforms")]
	public void TestLocalGetUserUnitsPerWeek() {
		NTHost host = NTHost.CurrentMachine;
		uint value = 5;
		// find a unique user name
		string userName = GenerateUserName(host);
		NTLocalUser user = host.CreateLocalUser(userName, "pPnNmm*&");
		try {
			user.UnitsPerWeek = value;
			user.Update();
			user = GetObjectByName(host.GetLocalUsers(), userName);
			Assert.That(value, Is.EqualTo(user.UnitsPerWeek));
		} finally {
			user.Delete();
		}
	}

	[Test, Ignore("Not Supported on all platforms")]
	public void TestLocalUserGetWorkstations() {
		NTHost host = NTHost.CurrentMachine;
		string[] value = new string[] { "W1", "w2" };
		// find a unique user name
		string userName = GenerateUserName(host);
		NTLocalUser user = host.CreateLocalUser(userName, "pPnNmm*&");
		try {
			user.Workstations = value;
			user.Update();
			user = GetObjectByName(host.GetLocalUsers(), userName);
			Assert.That(value, Is.EqualTo(user.Workstations));
		} finally {
			user.Delete();
		}
	}

	[Test]
	public void TestLocalUserWithEmptyMembership() {
		NTHost host = NTHost.CurrentMachine;
		// find a unique user name
		NTLocalUser user = null;
		try {
			user = host.CreateLocalUser(GenerateUserName(host), "pPnNmm*&");
			CollectionAssert.IsEmpty(user.GetMembership());
		} finally {
			try {
				if (user != null) {
					user.Delete();
				}
			} catch {
			}
		}
	}

	[Test]
	public void TestLocalUserWithSingleGroup() {
		NTHost host = NTHost.CurrentMachine;
		// find a unique user name
		NTLocalUser user = null;
		NTLocalGroup group = null;
		try {
			group = host.CreateLocalGroup(GenerateGroupName(host), null);
			user = host.CreateLocalUser(GenerateUserName(host), "pPnNmm*&");
			user.AddMembership(group.Name);
			Assert.That(ContainsObjectByName(user.GetMembership(), group.Name), Is.True);
		} finally {
			try {
				if (user != null) {
					user.Delete();
				}
			} catch {
			}

			try {
				if (user != null) {
					group.Delete();
				}
			} catch {
			}
		}
	}

	[Test]
	public void TestLocalUsersWithMultipleGroups() {
		NTHost host = NTHost.CurrentMachine;
		// find a unique user name
		NTLocalUser user1 = null;
		NTLocalUser user2 = null;
		NTLocalGroup group1 = null;
		NTLocalGroup group2 = null;
		NTLocalGroup group3 = null;
		try {
			group1 = host.CreateLocalGroup(GenerateGroupName(host), "description");
			Assert.That(group1, Is.Not.Null);
			group2 = host.CreateLocalGroup(GenerateGroupName(host), "description");
			Assert.That(group2, Is.Not.Null);
			group3 = host.CreateLocalGroup(GenerateGroupName(host), "description");
			Assert.That(group3, Is.Not.Null);


			user1 = host.CreateLocalUser(GenerateUserName(host), "pPnNmm*&");
			user2 = host.CreateLocalUser(GenerateUserName(host), "pPnNmm*&");

			user1.AddMembership(group1.Name);
			user1.AddMembership(group2.Name);
			user2.AddMembership(group1.Name);
			user2.AddMembership(group3.Name);

			NTLocalGroup[] user1Membership = user1.GetMembership();
			Assert.That(user1Membership, Is.Not.Null);

			Assert.That(ContainsObjectByName(user1Membership, group1.Name), Is.True);
			Assert.That(ContainsObjectByName(user1Membership, group2.Name), Is.True);

			NTLocalGroup[] user2Membership = user2.GetMembership();
			Assert.That(user2Membership, Is.Not.Null);

			Assert.That(ContainsObjectByName(user2Membership, group1.Name), Is.True);
			Assert.That(ContainsObjectByName(user2Membership, group3.Name), Is.True);

		} finally {
			try {
				if (user1 != null) {
					user1.Delete();
				}
			} catch {
			}

			try {
				if (user2 != null) {
					user2.Delete();
				}
			} catch {
			}

			try {
				if (group1 != null) {
					group1.Delete();
				}
			} catch {
			}


			try {
				if (group2 != null) {
					group2.Delete();
				}
			} catch {
			}


			try {
				if (group3 != null) {
					group3.Delete();
				}
			} catch {
			}
		}
	}

	[Test]
	public void TestLocalGroupCreateDelete() {
		NTHost host = NTHost.CurrentMachine;
		string groupName = GenerateGroupName(host);
		NTLocalGroup group = host.CreateLocalGroup(groupName, null);
		Assert.That(group, Is.Not.Null);
		Assert.That(ContainsObjectByName(host.GetLocalGroups(), groupName), Is.True);
		group.Delete();
		Assert.That(ContainsObjectByName(host.GetLocalGroups(), groupName), Is.False);
	}

	[Test]
	public void TestLocalGroupGetDescription() {
		NTHost host = NTHost.CurrentMachine;
		string groupName = GenerateGroupName(host);
		string description = "Test description";
		NTLocalGroup group = host.CreateLocalGroup(groupName, description);
		try {
			group = GetObjectByName(host.GetLocalGroups(), groupName);
			Assert.That(group.Description, Is.EqualTo(description));
		} finally {
			group.Delete();
		}
	}

	[Test]
	public void TestLocalGroupUpdateDescription() {
		NTHost host = NTHost.CurrentMachine;
		string groupName = GenerateGroupName(host);
		string description = "Test description";
		string newDescription = "New description";
		NTLocalGroup group = host.CreateLocalGroup(groupName, description);
		try {
			group.Description = newDescription;
			group.Update();
			group = GetObjectByName(host.GetLocalGroups(), groupName);
			Assert.That(group.Description, Is.EqualTo(newDescription));
		} finally {
			group.Delete();
		}
	}

	[Test]
	public void TestLocalGroupEmptyMembers() {
		NTHost host = NTHost.CurrentMachine;
		// find a unique user name
		NTLocalGroup group = null;
		try {
			group = host.CreateLocalGroup(GenerateGroupName(host), "description");
			CollectionAssert.IsEmpty(group.GetLocalMembers());
		} finally {
			try {
				if (group != null) {
					group.Delete();
				}
			} catch {
			}
		}
	}

	[Test]
	public void TestLocalGroupAddMember() {
		NTHost host = NTHost.CurrentMachine;
		// find a unique user name
		NTLocalUser user = null;
		NTLocalGroup group = null;
		try {
			group = host.CreateLocalGroup(GenerateGroupName(host), null);
			user = host.CreateLocalUser(GenerateUserName(host), "pPnNmm*&");
			group.AddLocalMember(user.Name);
			Assert.That(ContainsObjectByName(group.GetLocalMembers(), user.Name), Is.True);
		} finally {
			try {
				if (user != null) {
					user.Delete();
				}
			} catch {
			}

			try {
				if (user != null) {
					group.Delete();
				}
			} catch {
			}
		}
	}

	[Test]
	public void TestLocalGroupDeleteByObject() {
		NTHost host = NTHost.CurrentMachine;
		// find a unique user name
		NTLocalUser user = null;
		NTLocalGroup group = null;
		try {
			group = host.CreateLocalGroup(GenerateGroupName(host), null);
			user = host.CreateLocalUser(GenerateUserName(host), "pPnNmm*&");
			group.AddLocalMember(user.Name);
			Assert.That(ContainsObjectByName(group.GetLocalMembers(), user.Name), Is.True);
			group.DeleteMember(user);
			CollectionAssert.IsEmpty(group.GetLocalMembers());
		} finally {
			try {
				if (user != null) {
					user.Delete();
				}
			} catch {
			}

			try {
				if (user != null) {
					group.Delete();
				}
			} catch {
			}
		}
	}

	[Test]
	public void TestLocalGroupDeleteBySID() {
		NTHost host = NTHost.CurrentMachine;
		// find a unique user name
		NTLocalUser user = null;
		NTLocalGroup group = null;
		try {
			group = host.CreateLocalGroup(GenerateGroupName(host), null);
			user = host.CreateLocalUser(GenerateUserName(host), "pPnNmm*&");
			group.AddLocalMember(user.Name);
			Assert.That(ContainsObjectByName(group.GetLocalMembers(), user.Name), Is.True);
			group.DeleteMember(user.SID);
			CollectionAssert.IsEmpty(group.GetLocalMembers());
		} finally {
			try {
				if (user != null) {
					user.Delete();
				}
			} catch {
			}

			try {
				if (user != null) {
					group.Delete();
				}
			} catch {
			}
		}
	}

	[Test]
	public void TestLocalGroupDeleteByName() {
		NTHost host = NTHost.CurrentMachine;
		// find a unique user name
		NTLocalUser user = null;
		NTLocalGroup group = null;
		try {
			group = host.CreateLocalGroup(GenerateGroupName(host), null);
			user = host.CreateLocalUser(GenerateUserName(host), "pPnNmm*&");
			group.AddLocalMember(user.Name);
			Assert.That(ContainsObjectByName(group.GetLocalMembers(), user.Name), Is.True);
			group.DeleteLocalMember(user.Name);
			CollectionAssert.IsEmpty(group.GetLocalMembers());
		} finally {
			try {
				if (user != null) {
					user.Delete();
				}
			} catch {
			}

			try {
				if (user != null) {
					group.Delete();
				}
			} catch {
			}
		}
	}

	[Test]
	public void TestLocalGroupWithMultipleMembersWithUserMembershipVerify() {
		NTHost host = NTHost.CurrentMachine;
		// find a unique user name
		NTLocalUser user1 = null;
		NTLocalUser user2 = null;
		NTLocalGroup group1 = null;
		NTLocalGroup group2 = null;
		NTLocalGroup group3 = null;
		try {
			group1 = host.CreateLocalGroup(GenerateGroupName(host), "description");
			Assert.That(group1, Is.Not.Null);
			group2 = host.CreateLocalGroup(GenerateGroupName(host), "description");
			Assert.That(group2, Is.Not.Null);
			group3 = host.CreateLocalGroup(GenerateGroupName(host), "description");
			Assert.That(group3, Is.Not.Null);


			user1 = host.CreateLocalUser(GenerateUserName(host), "pPnNmm*&");
			user2 = host.CreateLocalUser(GenerateUserName(host), "pPnNmm*&");

			group1.AddLocalMember(user1.Name);
			group1.AddLocalMember(user2.Name);
			group2.AddLocalMember(user1.Name);
			group3.AddLocalMember(user2.Name);

			NTLocalGroup[] user1Membership = user1.GetMembership();
			Assert.That(user1Membership, Is.Not.Null);

			Assert.That(ContainsObjectByName(user1Membership, group1.Name), Is.True);
			Assert.That(ContainsObjectByName(user1Membership, group2.Name), Is.True);

			NTLocalGroup[] user2Membership = user2.GetMembership();
			Assert.That(user2Membership, Is.Not.Null);

			Assert.That(ContainsObjectByName(user2Membership, group1.Name), Is.True);
			Assert.That(ContainsObjectByName(user2Membership, group3.Name), Is.True);

		} finally {
			try {
				if (user1 != null) {
					user1.Delete();
				}
			} catch {
			}

			try {
				if (user2 != null) {
					user2.Delete();
				}
			} catch {
			}

			try {
				if (group1 != null) {
					group1.Delete();
				}
			} catch {
			}


			try {
				if (group2 != null) {
					group2.Delete();
				}
			} catch {
			}


			try {
				if (group3 != null) {
					group3.Delete();
				}
			} catch {
			}
		}
	}

	[Test]
	public void TestLocalGroupWithMultipleMembersWithWithGroupMembershipVerify() {
		NTHost host = NTHost.CurrentMachine;
		// find a unique user name
		NTLocalUser user1 = null;
		NTLocalUser user2 = null;
		NTLocalGroup group1 = null;
		NTLocalGroup group2 = null;
		NTLocalGroup group3 = null;
		try {
			group1 = host.CreateLocalGroup(GenerateGroupName(host), "description");
			Assert.That(group1, Is.Not.Null);
			group2 = host.CreateLocalGroup(GenerateGroupName(host), "description");
			Assert.That(group2, Is.Not.Null);
			group3 = host.CreateLocalGroup(GenerateGroupName(host), "description");
			Assert.That(group3, Is.Not.Null);


			user1 = host.CreateLocalUser(GenerateUserName(host), "pPnNmm*&");
			user2 = host.CreateLocalUser(GenerateUserName(host), "pPnNmm*&");

			group1.AddLocalMember(user1.Name);
			group1.AddLocalMember(user2.Name);
			group2.AddLocalMember(user1.Name);
			group3.AddLocalMember(user2.Name);

			NTLocalObject[] group1Members = group1.GetLocalMembers();
			Assert.That(group1Members, Is.Not.Null);
			Assert.That(group1Members.Length, Is.EqualTo(2));
			Assert.That(ContainsObjectByName(group1Members, user1.Name), Is.True);
			Assert.That(ContainsObjectByName(group1Members, user2.Name), Is.True);

			NTLocalObject[] group2Members = group2.GetLocalMembers();
			Assert.That(group2Members, Is.Not.Null);
			Assert.That(group2Members.Length, Is.EqualTo(1));
			Assert.That(ContainsObjectByName(group2Members, user1.Name), Is.True);

			NTLocalObject[] group3Members = group3.GetLocalMembers();
			Assert.That(group3Members, Is.Not.Null);
			Assert.That(group3Members.Length, Is.EqualTo(1));
			Assert.That(ContainsObjectByName(group3Members, user2.Name), Is.True);

		} finally {
			try {
				if (user1 != null) {
					user1.Delete();
				}
			} catch {
			}

			try {
				if (user2 != null) {
					user2.Delete();
				}
			} catch {
			}

			try {
				if (group1 != null) {
					group1.Delete();
				}
			} catch {
			}


			try {
				if (group2 != null) {
					group2.Delete();
				}
			} catch {
			}


			try {
				if (group3 != null) {
					group3.Delete();
				}
			} catch {
			}
		}
	}

	[Test]
	public void TestLocalGroupDeleteMultipleMembersWithWithUserMembershipVerify() {
		NTHost host = NTHost.CurrentMachine;
		// find a unique user name
		NTLocalUser user1 = null;
		NTLocalUser user2 = null;
		NTLocalGroup group1 = null;
		NTLocalGroup group2 = null;
		NTLocalGroup group3 = null;
		try {
			group1 = host.CreateLocalGroup(GenerateGroupName(host), "description");
			Assert.That(group1, Is.Not.Null);
			group2 = host.CreateLocalGroup(GenerateGroupName(host), "description");
			Assert.That(group2, Is.Not.Null);
			group3 = host.CreateLocalGroup(GenerateGroupName(host), "description");
			Assert.That(group3, Is.Not.Null);

			user1 = host.CreateLocalUser(GenerateUserName(host), "pPnNmm*&");
			user2 = host.CreateLocalUser(GenerateUserName(host), "pPnNmm*&");

			group1.AddLocalMember(user1.Name);
			group1.AddLocalMember(user2.Name);
			group2.AddLocalMember(user1.Name);
			group3.AddLocalMember(user2.Name);

			group1.DeleteLocalMember(user1.Name);
			group1.DeleteLocalMember(user2.Name);
			group2.DeleteLocalMember(user1.Name);
			group3.DeleteLocalMember(user2.Name);


			NTLocalGroup[] user1Membership = user1.GetMembership();
			Assert.That(user1Membership, Is.Not.Null);
			Assert.That(user1Membership.Length, Is.EqualTo(0));

			NTLocalGroup[] user2Membership = user2.GetMembership();
			Assert.That(user2Membership, Is.Not.Null);
			Assert.That(user2Membership.Length, Is.EqualTo(0));

		} finally {
			try {
				if (user1 != null) {
					user1.Delete();
				}
			} catch {
			}

			try {
				if (user2 != null) {
					user2.Delete();
				}
			} catch {
			}

			try {
				if (group1 != null) {
					group1.Delete();
				}
			} catch {
			}


			try {
				if (group2 != null) {
					group2.Delete();
				}
			} catch {
			}


			try {
				if (group3 != null) {
					group3.Delete();
				}
			} catch {
			}
		}
	}

	[Test]
	public void TestLocalGroupDeleteMultipleMembersWithWithGroupMembershipVerify() {
		NTHost host = NTHost.CurrentMachine;
		// find a unique user name
		NTLocalUser user1 = null;
		NTLocalUser user2 = null;
		NTLocalGroup group1 = null;
		NTLocalGroup group2 = null;
		NTLocalGroup group3 = null;
		try {
			group1 = host.CreateLocalGroup(GenerateGroupName(host), "description");
			Assert.That(group1, Is.Not.Null);
			group2 = host.CreateLocalGroup(GenerateGroupName(host), "description");
			Assert.That(group2, Is.Not.Null);
			group3 = host.CreateLocalGroup(GenerateGroupName(host), "description");
			Assert.That(group3, Is.Not.Null);


			user1 = host.CreateLocalUser(GenerateUserName(host), "pPnNmm*&");
			user2 = host.CreateLocalUser(GenerateUserName(host), "pPnNmm*&");

			group1.AddLocalMember(user1.Name);
			group1.AddLocalMember(user2.Name);
			group2.AddLocalMember(user1.Name);
			group3.AddLocalMember(user2.Name);

			group1.DeleteLocalMember(user1.Name);
			group1.DeleteLocalMember(user2.Name);
			group2.DeleteLocalMember(user1.Name);
			group3.DeleteLocalMember(user2.Name);


			NTObject[] group1Members = group1.GetMembers();
			Assert.That(group1Members, Is.Not.Null);
			Assert.That(group1Members.Length, Is.EqualTo(0));

			NTObject[] group2Members = group2.GetMembers();
			Assert.That(group2Members, Is.Not.Null);
			Assert.That(group2Members.Length, Is.EqualTo(0));

			NTObject[] group3Members = group3.GetMembers();
			Assert.That(group3Members, Is.Not.Null);
			Assert.That(group3Members.Length, Is.EqualTo(0));

		} finally {
			try {
				if (user1 != null) {
					user1.Delete();
				}
			} catch {
			}

			try {
				if (user2 != null) {
					user2.Delete();
				}
			} catch {
			}

			try {
				if (group1 != null) {
					group1.Delete();
				}
			} catch {
			}


			try {
				if (group2 != null) {
					group2.Delete();
				}
			} catch {
			}


			try {
				if (group3 != null) {
					group3.Delete();
				}
			} catch {
			}
		}
	}

	#endregion

	#region Remote tests

	[Test]
	public void TestRemoteHost() {
		NTHost host = new NTHost(RemoteHostName);
		Assert.That(host, Is.Not.Null);
		Assert.That(host.Name.ToUpper(), Is.EqualTo(RemoteHostName.ToUpper()));
	}

	[Test]
	public void TestRemoteHostGetSid() {
		NTHost host = new NTHost(RemoteHostName);
		Assert.That(host.SID, Is.Not.Null);
	}

	[Test]
	public void TestRemoteHostGetAdministratorUser() {
		NTHost host = new NTHost(RemoteHostName);
		Assert.That(ContainsSid(
				new SecurityIdentifier(WellKnownSidType.AccountAdministratorSid, host.SID),
				host.GetLocalUsers()
			), Is.True);
	}

	[Test]
	public void TestRemoteHostAdministratorUserPrivilege() {
		NTHost host = new NTHost(RemoteHostName);
		NTLocalUser user = GetObjectByName(host.GetLocalUsers(), "Administrator");
		Assert.That(user.Privilege == UserPrivilege.Admin, Is.True);
	}

	[Test]
	public void TestRemoteHostGetGuest() {
		NTHost host = new NTHost(RemoteHostName);
		Assert.That(ContainsSid(
				new SecurityIdentifier(WellKnownSidType.AccountGuestSid, host.SID),
				host.GetLocalUsers()
			), Is.True);

	}

	[Test]
	public void TestRemoteHostGetAdministratorsGroup() {
		NTHost host = new NTHost(RemoteHostName);
		NTLocalGroup[] localGroups = host.GetLocalGroups();
		NTLocalGroup group = GetObjectByName(localGroups, "Administrators");
		SecurityIdentifier localHostSid = host.SID;
		SecurityIdentifier adminSid = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, localHostSid);
		Assert.That(ContainsSid(
				adminSid,
				host.GetLocalGroups()
			), Is.True);
	}

	[Test]
	public void TestRemoteHostGetAdministratorsGroupMembers() {
		NTHost host = new NTHost(RemoteHostName);
		NTLocalGroup[] localGroups = host.GetLocalGroups();
		NTLocalGroup group = GetObjectByName(localGroups, "Administrators");
		NTLocalObject[] members = group.GetLocalMembers();
		Assert.That(members, Is.Not.Null);
		CollectionAssert.IsNotEmpty(members);
	}


	[Test]
	public void TestRemoteHostGetGuestsGroup() {
		NTHost host = new NTHost(RemoteHostName);
		Assert.That(ContainsSid(
				new SecurityIdentifier(WellKnownSidType.BuiltinGuestsSid, null),
				host.GetLocalGroups()
			), Is.True);
	}

	[Test]
	public void TestRemoteHostGetPowerUsersGroup() {
		NTHost host = new NTHost(RemoteHostName);
		Assert.That(ContainsSid(
				new SecurityIdentifier(WellKnownSidType.BuiltinPowerUsersSid, null),
				host.GetLocalGroups()
			), Is.True);
	}

	#endregion


	private string GenerateUserName(NTHost host) {
		string userName = "_TmpUsr";
		while (ContainsObjectByName(host.GetLocalUsers(), userName)) {
			userName = "_TmpUsr" + Guid.NewGuid().ToString().Substring(0, 4);
		}
		return userName;
	}

	private string GenerateGroupName(NTHost host) {
		string groupName = "_TmpGrp";
		while (ContainsObjectByName(host.GetLocalGroups(), groupName)) {
			groupName = "_TmpGrp" + Guid.NewGuid().ToString().Substring(0, 4);
		}
		return groupName;
	}

	private bool ContainsObjectByName<T>(T[] objects, string name) where T : NTLocalObject {
		foreach (T obj in objects) {
			if (obj.Name == name) {
				return true;
			}
		}
		return false;
	}

	private bool ContainsSid<T>(SecurityIdentifier sid, T[] objects) where T : NTLocalObject {
		foreach (T obj in objects) {
			if (obj.SID == sid) {
				return true;
			}
		}
		return false;
	}

	private T GetObjectByName<T>(T[] objects, string name) where T : NTLocalObject {
		foreach (T obj in objects) {
			if (obj.Name == name) {
				return obj;
			}
		}
		throw new Exception(string.Format("NTLocalObject '{0}' not found", name));
	}


}

