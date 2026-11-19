using System;
using System.Collections.Generic;

namespace Game2;

public class AutoLogin
{
	public bool waitToNextLogin;

	public long lastTimeWait;

	public long lastTimeLogin;

	public int attempts;

	public bool warnedNoAccount;

	public bool hasSetUserPass;

	public string accAutoLogin = "";

	public AutoLogin()
	{
		lastTimeWait = mSystem.currentTimeMillis();
	}

	public Account GetAccWithUsername(List<Account> accounts)
	{
		foreach (Account account in accounts)
		{
			if (string.Equals((account.getUsername() ?? string.Empty).Trim(), (accAutoLogin ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase))
			{
				return account;
			}
		}
		return new Account("", "");
	}
}
