using Google.Protobuf;
using static System.Net.Mime.MediaTypeNames;
using System.Net.Sockets;
using System.Net;
using System.IO;
using static GamePacket;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Reflection;

namespace Server_Core
{
	// 추상화된 기본 핸들러 매니저  
	public abstract class BaseHandlerManager<TPacketType, TGamePacket>
			where TPacketType : Enum
			where TGamePacket : class
	{
		// 스레드 안전한 싱글톤  
		private static readonly object _lock = new object();
		private static BaseHandlerManager<TPacketType, TGamePacket> _instance;

		// 스레드 안전한 핸들러 저장소  
		protected ConcurrentDictionary<TPacketType, Func<TGamePacket, Task>> Handlers
				= new ConcurrentDictionary<TPacketType, Func<TGamePacket, Task>>();

		// 싱글톤 인스턴스 속성  
		public static BaseHandlerManager<TPacketType, TGamePacket> Instance
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

		// 인스턴스 생성 메서드  
		private static BaseHandlerManager<TPacketType, TGamePacket> CreateInstance()
		{
			// 파생 클래스의 타입을 사용  
			var derivedType = typeof(BaseHandlerManager<TPacketType, TGamePacket>);

			// null 체크 추가  
			var instance = Activator.CreateInstance(derivedType);

			if (instance is BaseHandlerManager<TPacketType, TGamePacket> handlerManager)
				return handlerManager;

			throw new InvalidOperationException("인스턴스 생성 실패");
		}
		


		// 핸들러 인터페이스  
		public interface IPacketHandler
		{
			Task HandleAsync(TGamePacket packet);
		}

		// 생성자에서 핸들러 자동 등록  
		protected BaseHandlerManager()
		{
			RegisterHandlers();
		}

		// 추상 메서드로 핸들러 등록 구현 강제  
		protected abstract void RegisterHandlers();

		// 패킷 처리 메서드  
		public virtual async Task OnRecvPacketAsync(TPacketType packetType, TGamePacket packet)
		{
			if (Handlers.TryGetValue(packetType, out var handler))
			{
				try
				{
					await handler(packet);
				}
				catch (Exception ex)
				{
					// 공통 예외 처리  
					await HandleExceptionAsync(packetType, packet, ex);
				}
			}
			else
			{
				// 핸들러가 없는 경우 로깅  
				await HandleMissingHandlerAsync(packetType, packet);
			}
		}

		// 핸들러 등록 메서드  
		protected void RegisterHandler(TPacketType packetType, Func<TGamePacket, Task> handler)
		{
			Handlers[packetType] = handler;
		}

		// 예외 처리 가상 메서드  
		protected virtual Task HandleExceptionAsync(TPacketType packetType, TGamePacket packet, Exception ex)
		{
			// 로깅, 모니터링 등 공통 예외 처리  
			Console.WriteLine($"Packet Type: {packetType}, Error: {ex.Message}");
			return Task.CompletedTask;
		}

		// 핸들러 없음 처리 가상 메서드  
		protected virtual Task HandleMissingHandlerAsync(TPacketType packetType, TGamePacket packet)
		{
			Console.WriteLine($"No handler found for packet type: {packetType}");
			return Task.CompletedTask;
		}
	}
}
