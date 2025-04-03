using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Config;
using Server_Core;
using Microsoft.Extensions.Configuration;
using DotNetEnv;

namespace Game_Server
{
	class Program
	{
		static Listener _listener = new Listener();

		static async Task Main(string[] args)
		{
			// 실행 중인 어셈블리(.exe)의 디렉토리 기준으로 .env 파일 로드
			string baseDir = AppDomain.CurrentDomain.BaseDirectory;
			string envPath = Path.Combine(baseDir, ".env");
			Env.Load(envPath);

			// 환경변수 가져오기
			string? hostStr = Environment.GetEnvironmentVariable("APPSETTINGS_HOST");
			string? portStr = Environment.GetEnvironmentVariable("APPSETTINGS_PORT");

			if (string.IsNullOrWhiteSpace(hostStr) || string.IsNullOrWhiteSpace(portStr))
			{
				Console.WriteLine("환경변수 APPSETTINGS_HOST 또는 APPSETTINGS_PORT가 없습니다.");
				return;
			}

			if (!int.TryParse(portStr, out int port))
			{
				Console.WriteLine("PORT는 숫자여야 합니다.");
				return;
			}

			IPAddress ipAddr = IPAddress.Parse(hostStr);
			IPEndPoint endPoint = new IPEndPoint(ipAddr, port);

			_listener.Init(endPoint, () => SessionManager.Instance.Generate());

			Console.WriteLine($"{endPoint} 에서 Listening...");

			// host.RunAsync(); → host가 string이기 때문에 충돌
			// 서버 루프가 없다면 여기서 블로킹하거나, IHost 구현체 사용 고려
			await Task.Delay(-1); // 프로그램을 종료하지 않고 대기
		}
	}
}
