namespace Config
{
	public class AppSettings
	{
		// 서버 설정  
		public int Port { get; set; } = 5557;
		public string Host { get; set; } = "0.0.0.0";
		public string ClientVersion { get; set; } = "1.0.0";
		public int LocationReqTimeTerm { get; set; } = 2000;

		// 데이터베이스 설정  
		public DatabaseConfig Database { get; set; } = new DatabaseConfig();
		public RedisConfig Redis { get; set; } = new RedisConfig();
	}

	public class DatabaseConfig
	{
		public string Name { get; set; } = "database";
		public string User { get; set; } = "user";
		public string Password { get; set; } = "password";
		public string Host { get; set; } = "localhost";
		public int Port { get; set; } = 3306;
	}

	public class RedisConfig
	{
		public string User { get; set; } = "user";
		public string Password { get; set; } = "password";
		public string Host { get; set; } = "localhost";
		public int Port { get; set; } = 6379;
		public string Custom { get; set; } = "Default";
	}
}
