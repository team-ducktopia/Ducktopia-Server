using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;


namespace Config
{
	public class Program
	{
		public static void Main(string[] args)
		{
			CreateHostBuilder(args).Build().Run();
		}

		public static IHostBuilder CreateHostBuilder(string[] args) =>
				Host.CreateDefaultBuilder(args)
						.ConfigureServices((hostContext, services) =>
						{
							// 설정 바인딩  
							services.Configure<AppSettings>(
									hostContext.Configuration.GetSection("AppSettings")
							);

							// 서비스 등록  
							services.AddSingleton(sp =>
							{
								var options = sp.GetRequiredService<IOptions<AppSettings>>();
								return options.Value;
							});
						});
	}
}
