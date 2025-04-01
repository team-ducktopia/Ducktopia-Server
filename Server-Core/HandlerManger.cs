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
	// 핸들러 인터페이스 (선택적)  
	public interface IPacketHandler
	{
		void Handle(GamePacket packet);
	}

	// 핸들러 매니저 클래스  
	public class PacketHandlerRegistry
	{
		// 스레드 안전한 딕셔너리 사용  
		private static readonly ConcurrentDictionary<ePacketType, Func<GamePacket, Task>> _handlers
				= new ConcurrentDictionary<ePacketType, Func<GamePacket, Task>>();

		// 생성자에서 핸들러 자동 등록  
		static PacketHandlerRegistry()
		{
			RegisterHandlers();
		}

		// 핸들러 자동 등록 메서드  
		private static void RegisterHandlers()
		{
			// 현재 어셈블리의 모든 타입 순회  
			var handlerTypes = Assembly.GetExecutingAssembly()
					.GetTypes()
					.Where(t => t.GetInterfaces().Contains(typeof(IPacketHandler)));

			foreach (var handlerType in handlerTypes)
			{
				// 핸들러 타입의 메서드 순회  
				var methods = handlerType.GetMethods()
						.Where(m => m.GetCustomAttributes(typeof(PacketHandlerAttribute), false).Any());

				foreach (var method in methods)
				{
					var attribute = method.GetCustomAttribute<PacketHandlerAttribute>();

					// 핸들러 메서드를 델리게이트로 변환  
					var handler = CreateHandlerDelegate(handlerType, method);

					// 패킷 타입에 핸들러 매핑  
					_handlers[attribute.PacketType] = handler;
				}
			}
		}

		// 핸들러 델리게이트 생성 메서드  
		private static Func<GamePacket, Task> CreateHandlerDelegate(Type handlerType, MethodInfo method)
		{
			// 핸들러 인스턴스 생성 (의존성 주입 고려)  
			var instance = Activator.CreateInstance(handlerType);

			// 델리게이트 생성  
			return (Func<GamePacket, Task>)Delegate.CreateDelegate(
					typeof(Func<GamePacket, Task>),
					instance,
					method
			);
		}

		// 패킷 처리 메서드  
		public static async Task HandlePacket(ePacketType packetType, GamePacket packet)
		{
			if (_handlers.TryGetValue(packetType, out var handler))
			{
				await handler(packet);
			}
			else
			{
				// 핸들러가 없는 경우 로깅 또는 예외 처리  
				Console.WriteLine($"No handler found for packet type: {packetType}");
			}
		}
	}

	// 패킷 핸들러 특성  
	[AttributeUsage(AttributeTargets.Method)]
	public class PacketHandlerAttribute : Attribute
	{
		public ePacketType PacketType { get; }

		public PacketHandlerAttribute(ePacketType packetType)
		{
			PacketType = packetType;
		}
	}


	class HandlerManger
	{
		static HandlerManger _handler = new HandlerManger();
		public static HandlerManger Instance { get { return _handler; } }

		public Dictionary<PayloadOneofCase, Action<GamePacket>> _method = new Dictionary<PayloadOneofCase, Action<GamePacket>>();

		public Queue<Packet> sendQueue = new Queue<Packet>();
		public Queue<Packet> receiveQueue = new Queue<Packet>();

		public string version = "1.0.0";

		byte[] recvBuff = new byte[1024];
		private byte[] remainBuffer = Array.Empty<byte>();

		public bool isConnected;
		bool isInit = false;
		bool successConnected;
		public HandlerManger()
		{
			if (isInit) return;
			var payloads = Enum.GetNames(typeof(PayloadOneofCase));
			var methods = GetType().GetMethods();
			foreach (var payload in payloads)
			{
				var val = (PayloadOneofCase)Enum.Parse(typeof(PayloadOneofCase), payload);
				var method = GetType().GetMethod(payload);
				Console.WriteLine($"🔍 검사 중: {payload} (매핑될 메서드: {method?.Name ?? "없음"})");
				if (method == null) 
					continue;
				
				try
				{
					var action = (Action<GamePacket>)Delegate.CreateDelegate(typeof(Action<GamePacket>), this, method);
					_method.Add(val, action);
					Console.WriteLine($"✅ 성공적으로 매핑됨: {payload} -> {method.Name}");
				}
				catch (Exception ex)
				{
					Console.WriteLine($"🚨 오류 발생: {payload} -> {method.Name}. 메시지: {ex.Message}");
				}
			}
			isInit = true;
		}

		private async void OnReceive()
		{
			if (socket == null)
			{
				return;
			}

			while (socket.Connected && isConnected)
			{
				try
				{
					var recvByteLength = await socket.ReceiveAsync(recvBuff, SocketFlags.None);
					//Debug.Log($"📥 [OnReceive] 수신된 데이터 길이: {recvByteLength} 바이트");

					if (!isConnected)
						// Debug.Log("⚠️ [OnReceive] 소켓 연결 종료 감지");
						break;

					if (recvByteLength <= 0)
						continue;

					// 🔹 새 버퍼 할당 및 기존 remainBuffer와 결합
					byte[] newBuffer = ArrayPool<byte>.Shared.Rent(remainBuffer.Length + recvByteLength);
					try
					{
						Buffer.BlockCopy(remainBuffer, 0, newBuffer, 0, remainBuffer.Length);
						Buffer.BlockCopy(recvBuff, 0, newBuffer, remainBuffer.Length, recvByteLength);
						// 사용 후 처리  
					}
					finally
					{
						ArrayPool<byte>.Shared.Return(newBuffer);
					}
					//Debug.Log($"🔄 [OnReceive] 새로운 버퍼 길이: {newBuffer.Length} 바이트");

					var processedLength = 0;
					while (processedLength < newBuffer.Length)
					{
						if (newBuffer.Length - processedLength < 7)
						{
							//Debug.Log("⚠️ [OnReceive] 남은 데이터가 최소 패킷 크기(11바이트)보다 작음");
							break;
						}

						using var stream = new MemoryStream(newBuffer, processedLength, newBuffer.Length - processedLength);
						using var reader = new BinaryReader(stream);

						// 1️⃣ 패킷 타입 (2바이트)
						var typeBytes = reader.ReadBytes(2);
						Array.Reverse(typeBytes);
						var epacketType = (ePacketType)BitConverter.ToInt16(typeBytes);
						var type = Packet.ConvertToPayloadCase(epacketType);
						//Debug.Log($"📌 [OnReceive] 패킷 타입: {type} (원본: {BitConverter.ToString(typeBytes)})");

						// 2️⃣ 버전 길이 (1바이트)
						var versionLength = reader.ReadByte();
						//Debug.Log($"📌 [OnReceive] 버전 길이: {versionLength}");

						// 버전 길이 검사
						if (newBuffer.Length - processedLength < 7 + versionLength)
							break;

						// 3️⃣ 버전 데이터 (가변 길이)
						var versionBytes = reader.ReadBytes(versionLength);
						var version = System.Text.Encoding.UTF8.GetString(versionBytes);
						//Debug.Log($"📌 [OnReceive] 버전: {version} (원본: {BitConverter.ToString(versionBytes)})");

						// 4️⃣ 페이로드 길이 (4바이트)
						byte[] payloadLengthBytes = reader.ReadBytes(4);
						int payloadLength = BinaryPrimitives.ReadInt32BigEndian(payloadLengthBytes);
						//Debug.Log($"📌 [OnReceive] 페이로드 길이: {payloadLength} (원본: {BitConverter.ToString(payloadLengthBytes)})");

						// 페이로드 길이 검사
						if (newBuffer.Length - processedLength < 7 + versionLength + payloadLength)
							break;

						// 5️⃣ 페이로드 데이터
						var payloadBytes = reader.ReadBytes(payloadLength);
						//Debug.Log($"📦 [OnReceive] 페이로드 데이터: {BitConverter.ToString(payloadBytes)}");

						// 6️⃣ 패킷 생성 및 큐에 추가
						var packetType = Packet.ConvertToPacketType(type);
						var packet = new Packet(packetType, version, payloadBytes);
						receiveQueue.Enqueue(packet);
						//Debug.Log($"✅ [OnReceive] 큐에 추가됨 (패킷 타입: {type}, 현재 큐 크기: {receiveQueue.Count})");

						processedLength += (7 + versionLength + payloadLength);
					}

					// 남은 데이터 처리
					var remainLength = newBuffer.Length - processedLength;
					if (remainLength > 0)
					{
						remainBuffer = new byte[remainLength];
						Array.Copy(newBuffer, processedLength, remainBuffer, 0, remainLength);
						//Debug.Log($"🔄 [OnReceive] 남은 버퍼 크기: {remainLength} 바이트");
					}
					else
					{
						remainBuffer = Array.Empty<byte>();
						//Debug.Log("🛑 [OnReceive] 남은 버퍼 없음, 초기화 완료");
					}
				}
				catch (Exception e)
				{
					//Debug.LogError($"🚨 [OnReceive] 예외 발생: {e.Message}\n{e.StackTrace}");
				}
			}

			if (socket != null && socket.Connected)
			{
				//Debug.Log("🔄 [OnReceive] 재실행: 소켓 연결 유지 중...");
				OnReceive();
			}
		}
		/// <summary>
		/// �ܺο��� ���Ͽ� �޽����� ������ ȣ��
		/// GamePacket ���·� �޾� Packet Ŭ������ ���� sendQueue�� ����Ѵ�.
		/// </summary>
		/// <param name="gamePacket"></param>
		public void Send(GamePacket gamePacket)
		{
			if (socket == null) return;
			var byteArray = gamePacket.ToByteArray();
			// 🔥 `PayloadOneofCase` → `ePacketType` 변환 추가
			var packetType = Packet.ConvertToPacketType(gamePacket.PayloadCase);
			var packet = new Packet(packetType, version, byteArray);
			sendQueue.Enqueue(packet);
		}

		///// <summary>
		///// sendQueue�� �����Ͱ� ���� �� ���Ͽ� ����
		///// </summary>
		///// <returns></returns>
		//IEnumerator OnSendQueue()
		//{
		//    while (true)
		//    {
		//        yield return new WaitUntil(() => sendQueue.Count > 0);
		//        var packet = sendQueue.Dequeue();

		//        var bytes = packet.ToByteArray();
		//        var sent = socket.Send(bytes, SocketFlags.None);
		//        //Debug.Log($"Send Packet: {packet.type}, Sent bytes: {sent}");

		//        yield return new WaitForSeconds(0.001f);
		//    }
		//}


		///// <summary>
		///// receiveQueue�� �����Ͱ� ���� �� ��Ŷ Ÿ�Կ� ���� �̺�Ʈ ȣ��
		///// </summary>
		///// <returns></returns>
		//IEnumerator OnReceiveQueue()
		//{
		//    while (true)
		//    {
		//        yield return new WaitUntil(() => receiveQueue.Count > 0);
		//        // try
		//        // {
		//        var packet = receiveQueue.Dequeue();
		//        //Debug.Log("Receive Packet : " + packet.type.ToString());
		//        var payloadType = Packet.ConvertToPayloadCase(packet.type); // 변환 추가
		//        _onRecv[payloadType].Invoke(packet.gamePacket);
		//        // }
		//        // catch (Exception e)
		//        // {
		//        //     Debug.Log(e);
		//        // }
		//        yield return new WaitForSeconds(0.001f);
		//    }
		//}

		//public IEnumerator Ping()
		//{
		//    while (socket.Connected)
		//    {
		//        yield return new WaitForSeconds(1);
		//    }
		//    if (successConnected && !socket.Connected)
		//    {
		//        StartCoroutine(EndGameCount());
		//    }
		//}
	}
}
