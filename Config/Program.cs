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
				Host.CreateDefaultBuilder(args)  // 기본 호스트 빌더 생성  
				.ConfigureServices((hostContext, services) =>  // 서비스 구성  
				{
					// 설정 바인딩: JSON의 "AppSettings" 섹션을 AppSettings 클래스에 매핑  
					services.Configure<AppSettings>(
					hostContext.Configuration.GetSection("AppSettings")
			);

					// AppSettings를 싱글톤으로 직접 등록  
					services.AddSingleton(sp =>
					{
						// IOptions<AppSettings>에서 실제 AppSettings 값 추출  
						var options = sp.GetRequiredService<IOptions<AppSettings>>();
						return options.Value;
					});
				});
	}
}
