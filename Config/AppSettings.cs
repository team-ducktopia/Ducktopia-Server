using System;

public class AppSettings
{
	public int Port;
	public string Host;
	public string ClientVersion;
	public int LocationReqTimeTerm;

	public DatabaseSettings Database;
	public RedisSettings Redis;
}

public class DatabaseSettings
{
	public string Name;
	public string User;
	public string Password;
	public string Host;
	public int Port;
}

public class RedisSettings
{
	public string User;
	public string Password;
	public string Host;
	public int Port;
	public string Custom;
}
