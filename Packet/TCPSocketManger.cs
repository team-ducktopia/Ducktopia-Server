using Google.Protobuf;
using static System.Net.Mime.MediaTypeNames;
using System.Net.Sockets;
using System.Net;
using System.IO;
using static GamePacket;

namespace Packet
{
	public abstract class TCPSocketManagerBase<T>
	{
		public bool useDNS = false;
		public Dictionary<PayloadOneofCase, Action<GamePacket>> _onRecv = new Dictionary<PayloadOneofCase, Action<GamePacket>>();

		public Queue<Packet> sendQueue = new Queue<Packet>();
		public Queue<Packet> receiveQueue = new Queue<Packet>();

		public string ip = "13.125.207.234";
		public int port = 5555;

		public Socket socket;
		public string version = "1.0.0";

		byte[] recvBuff = new byte[1024];
		private byte[] remainBuffer = Array.Empty<byte>();

		public bool isConnected;
		bool isInit = false;
		bool successConnected;

		protected void InitPackets()
		{
			if (isInit) return;
			var payloads = Enum.GetNames(typeof(PayloadOneofCase));
			var methods = GetType().GetMethods();
			foreach (var payload in payloads)
			{
				var val = (PayloadOneofCase)Enum.Parse(typeof(PayloadOneofCase), payload);
				var method = GetType().GetMethod(payload);
				//Debug.Log($"🔍 검사 중: {payload} (매핑될 메서드: {method?.Name ?? "없음"})");
				if (method != null)
				{
					try
					{
						var action = (Action<GamePacket>)Delegate.CreateDelegate(typeof(Action<GamePacket>), this, method);
						_onRecv.Add(val, action);
						//Debug.Log($"✅ 성공적으로 매핑됨: {payload} -> {method.Name}");
					}
					catch (Exception ex)
					{
						//Debug.LogError($"🚨 오류 발생: {payload} -> {method.Name}. 메시지: {ex.Message}");
					}
				}
			}
			isInit = true;
		}

		public TCPSocketManagerBase<T> Init(string ip, int port)
		{
			this.ip = "13.125.207.234";
			this.port = 5555;
			InitPackets();
			return this;
		}

		///// <summary>
		///// ��ϵ� ip, port�� ���� ����
		///// send, receiveť �̺�Ʈ ���
		///// </summary>
		///// <param name="callback"></param>
		//public async void Connect(UnityAction callback = null)
		//{

		//    IPHostEntry ipHost = Dns.GetHostEntry("ducktopia-loadbalancer-1900b439129f13b9.elb.ap-northeast-2.amazonaws.com");
		//    IPAddress ipAddress = ipHost.AddressList[0];
		//    IPEndPoint endPoint = new IPEndPoint(ipAddress, port);

		//    //Debug.Log("Tcp Ip : " + ipAddress.MapToIPv4().ToString() + ", Port : " + port);
		//    socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
		//    try
		//    {
		//        await socket.ConnectAsync(endPoint);
		//        isConnected = socket.Connected;
		//        if (isConnected && !successConnected)
		//        {
		//            successConnected = true;
		//        }
		//        OnReceive();
		//        StartCoroutine(OnSendQueue());
		//        StartCoroutine(OnReceiveQueue());
		//        StartCoroutine(Ping());
		//        callback?.Invoke();
		//    }
		//    catch (Exception e)
		//    {
		//        //Debug.Log(e.ToString());
		//    }
		//}

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
					{
						// Debug.Log("⚠️ [OnReceive] 소켓 연결 종료 감지");
						break;
					}
					if (recvByteLength <= 0)
					{
						continue;
					}

					// 🔹 새 버퍼 할당 및 기존 remainBuffer와 결합
					var newBuffer = new byte[remainBuffer.Length + recvByteLength];
					Array.Copy(remainBuffer, 0, newBuffer, 0, remainBuffer.Length);
					Array.Copy(recvBuff, 0, newBuffer, remainBuffer.Length, recvByteLength);
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
						{
							//Debug.Log("⚠️ [OnReceive] 버전 데이터 부족, 대기 중...");
							break;
						}

						// 3️⃣ 버전 데이터 (가변 길이)
						var versionBytes = reader.ReadBytes(versionLength);
						var version = System.Text.Encoding.UTF8.GetString(versionBytes);
						//Debug.Log($"📌 [OnReceive] 버전: {version} (원본: {BitConverter.ToString(versionBytes)})");

						// 4️⃣ 페이로드 길이 (4바이트)
						var payloadLengthBytes = reader.ReadBytes(4);
						Array.Reverse(payloadLengthBytes);
						var payloadLength = BitConverter.ToInt32(payloadLengthBytes);
						//Debug.Log($"📌 [OnReceive] 페이로드 길이: {payloadLength} (원본: {BitConverter.ToString(payloadLengthBytes)})");

						// 페이로드 길이 검사
						if (newBuffer.Length - processedLength < 7 + versionLength + payloadLength)
						{
							//Debug.Log("⚠️ [OnReceive] 페이로드 데이터 부족, 대기 중...");
							break;
						}

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

		///// <summary>
		///// �ı��� (���� �ı� ���� �ʴ´ٸ� �� ���� ��) ���� ���� ����
		///// </summary>
		//private void OnDestroy()
		//{
		//    Disconnect();
		//}

		///// <summary>
		///// ���� ���� ����
		///// </summary>
		///// <param name="isReconnect"></param>
		//public async void Disconnect(bool isReconnect = false)
		//{
		//    StopAllCoroutines();
		//    if (isConnected)
		//    {
		//        this.isConnected = false;
		//        GamePacket packet = new GamePacket();
		//        packet.LoginRequest = new C2SLoginRequest();
		//        Send(packet);
		//        socket.Disconnect(isReconnect);
		//        if (isReconnect)
		//        {
		//            Connect();
		//        }
		//        else
		//        {
		//            if (SceneManager.GetActiveScene().name != "Main")
		//            {
		//                await SceneManager.LoadSceneAsync("Main");
		//            }
		//            else
		//            {
		//                UIManager.Hide<UITopBar>();
		//                UIManager.Hide<UIGnb>();
		//                await UIManager.Show<PopupLogin>();
		//            }
		//        }
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

		//public IEnumerator EndGameCount()
		//{
		//    //1. 커넥트 석시스고, 연결이 끈겼을때. 작동.
		//    float countdown = 3f;
		//    //gameEndPenel.SetActive(true);
		//    UIManager.Show<PopupGameEnd>();
		//    while (countdown > 0)
		//    {
		//        //Debug.Log($"⏳ 게임 종료까지 남은 시간: {countdown}초");
		//        yield return new WaitForSeconds(1f); // 1초씩 대기
		//        countdown--;
		//    }

		//    QuitGame();
		//}
	}
}
