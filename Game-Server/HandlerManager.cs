using Server_Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game_Server
{
    class HandlerManager : HandlerManagerBase<ePacketType, GamePacket>
	  {

		// 스레드 안전한 싱글톤  
		private static readonly object _lock = new object();
		private static HandlerManagerBase<ePacketType, GamePacket> _instance;

		// 싱글톤 인스턴스 속성  
		public static HandlerManagerBase<ePacketType, GamePacket> Instance
		{
			get
			{
				// 이중 검사 락킹 (Double-Check Locking)  
				if (_instance == null)
				{
					lock (_lock)
					{
						// 인스턴스가 없는 경우에만 생성  
						_instance ??= CreateInstance();
					}
				}
				return _instance;
			}
		}
		protected static HandlerManagerBase<ePacketType, GamePacket> CreateInstance()
		{
			return new HandlerManager();
		}

		protected override void RegisterHandlers()
		{
			// 수동 핸들러 등록 예시  
			RegisterHandler(ePacketType.LOGIN_REQUEST, LoginHandler);
		}


		private async Task LoginHandler(GamePacket packet)
		{
			// 로그인 처리 로직  
			Console.WriteLine($"🔑 [LoginHandler] 로그인 요청 처리: {packet.LoginRequest.Email}");
		}

	}
}
