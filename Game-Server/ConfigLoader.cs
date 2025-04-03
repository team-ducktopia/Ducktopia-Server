using DotNetEnv;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Config
{	public static class ConfigLoader
	{
		public static AppSettings LoadConfig(string envFilePath)
		{
			Env.Load(envFilePath); // .env 경로

			var config = new AppSettings
			{
				Port = int.Parse(Environment.GetEnvironmentVariable("APPSETTINGS_PORT") ?? "0"),
				Host = Environment.GetEnvironmentVariable("APPSETTINGS_HOST"),
				ClientVersion = Environment.GetEnvironmentVariable("APPSETTINGS_CLIENTVERSION"),
				LocationReqTimeTerm = int.Parse(Environment.GetEnvironmentVariable("APPSETTINGS_LOCATIONREQTIMETERM") ?? "0"),

				Database = new DatabaseSettings
				{
					Name = Environment.GetEnvironmentVariable("APPSETTINGS_DATABASE_NAME"),
					User = Environment.GetEnvironmentVariable("APPSETTINGS_DATABASE_USER"),
					Password = Environment.GetEnvironmentVariable("APPSETTINGS_DATABASE_PASSWORD"),
					Host = Environment.GetEnvironmentVariable("APPSETTINGS_DATABASE_HOST"),
					Port = int.Parse(Environment.GetEnvironmentVariable("APPSETTINGS_DATABASE_PORT") ?? "3306"),
				},

				Redis = new RedisSettings
				{
					User = Environment.GetEnvironmentVariable("APPSETTINGS_REDIS_USER"),
					Password = Environment.GetEnvironmentVariable("APPSETTINGS_REDIS_PASSWORD"),
					Host = Environment.GetEnvironmentVariable("APPSETTINGS_REDIS_HOST"),
					Port = int.Parse(Environment.GetEnvironmentVariable("APPSETTINGS_REDIS_PORT") ?? "6379"),
					Custom = Environment.GetEnvironmentVariable("APPSETTINGS_REDIS_CUSTOM"),
				}
			};

			return config;
		}
	}

}
