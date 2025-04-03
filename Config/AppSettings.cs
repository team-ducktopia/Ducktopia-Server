namespace Config
{
	public class AppSettings
	{
		// 서버 설정  
		public static int Port { get; set; } = 5557;
		public static string Host { get; set; } = "0.0.0.0";
		public static string ClientVersion { get; set; } = "1.0.0";
		public static int LocationReqTimeTerm { get; set; } = 2000;

		// 데이터베이스 설정  
		public static DatabaseConfig Database { get; set; } = new DatabaseConfig();
		public static RedisConfig Redis { get; set; } = new RedisConfig();
	}

	public class DatabaseConfig
	{
		public static string Name { get; set; } = "database";
		public static string User { get; set; } = "user";
		public static string Password { get; set; } = "password";
		public static string Host { get; set; } = "localhost";
		public static int Port { get; set; } = 3306;
	}

	public class RedisConfig
	{
		public static string User { get; set; } = "user";
		public static string Password { get; set; } = "password";
		public static string Host { get; set; } = "localhost";
		public static int Port { get; set; } = 6379;
		public static string Custom { get; set; } = "Default";
	}
}
