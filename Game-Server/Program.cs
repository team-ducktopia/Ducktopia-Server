using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Config;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Server_Core;

namespace Game_Server
{
	class Program
	{
		static Listener _listener = new Listener();

		static void Main(string[] args)
		{
			Console.WriteLine(AppSettings.Port);
			CreateHostBuilder(args).Build().Run();
			Console.WriteLine(AppSettings.Port);


			// DNS (Domain Name System)
			string host = Dns.GetHostName();
			IPHostEntry ipHost = Dns.GetHostEntry(host);
			IPAddress ipAddr = ipHost.AddressList[1];
			IPEndPoint endPoint = new IPEndPoint(ipAddr, 5555);

			_listener.Init(endPoint, () => { return SessionManager.Instance.Generate(); });
			Console.WriteLine($"{endPoint}에서 Listening...");

			while (true)
			{
			}
		}

		// 호스트 빌더 (환경변수 가져오기) 
		public static IHostBuilder CreateHostBuilder(string[] args) =>
		// 기본 호스트 빌더 생성  
		Host.CreateDefaultBuilder(args)
				// 서비스 구성  
				.ConfigureServices((hostContext, services) =>
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
