using System;
using System.Buffers.Binary;
using System.Buffers;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using static GamePacket;
using Google.Protobuf.WellKnownTypes;

namespace Server_Core
{
	public abstract class PacketSession : Session
	{
		public static readonly int HeaderSize = 2;

		// [PacketType(2)][VerstionLength(1)][Version(..)][PayloadLength(4)][Payload(..)]
		public sealed override int OnRecv(Span<byte> buffer)
		{
			int processedLength = 0;
			int packetCount = 0;

			var recvByteLength = buffer.Length;
			//Console.WriteLine($"📥 [OnReceive] 수신된 데이터 길이: {recvByteLength} 바이트");

			while (processedLength < recvByteLength)
			{
				if (recvByteLength - processedLength < 3)
					break;

				ushort count = 0;
				// 1️⃣ 패킷 타입 (2바이트)
				Span<byte> typeBytes = buffer.Slice(count, 2);
				//빅 엔디안 -> 리틀 엔디안 변경
				ePacketType ePacketType = (ePacketType)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(typeBytes));
				// 기본 엔디안
				// ePacketType ePacketType = (ePacketType)BitConverter.ToInt16(typeBytes);
				PayloadOneofCase type = PacketMapper.ConvertToPayloadCase(ePacketType);
				count += 2;
				//Console.WriteLine($"📌 [OnReceive] 패킷 타입: {type} (원본: {BitConverter.ToString(typeBytes)})");

				// 2️⃣ 버전 길이 (1바이트) + 숫자변환
				ushort versionLength = buffer[count];
				count += 1;
				//Console.WriteLine($"📌 [OnReceive] 버전 길이: {versionLength}");

				// 버전 길이 검사
				if (recvByteLength - processedLength < 3 + versionLength)
					break;

				// 3️⃣ 버전 데이터 (가변 길이)
				Span<byte> versionBytes = buffer.Slice(count, versionLength);
				string version = Encoding.UTF8.GetString(versionBytes);
				count += versionLength;
				//Console.WriteLine($"📌 [OnReceive] 버전: {version} (원본: {BitConverter.ToString(versionBytes)})");

				// 4️⃣ 페이로드 길이 (4바이트)
				Span<byte> payloadLengthBytes = buffer.Slice(count, 4);
				//빅 엔디안 -> 리틀 엔디안 변경
				int payloadLength = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(payloadLengthBytes));
				count += 4;
				//Console.WriteLine($"📌 [OnReceive] 페이로드 길이: {payloadLength} (원본: {BitConverter.ToString(payloadLengthBytes)})");

				// 페이로드 길이 검사
				if (recvByteLength - processedLength < 3 + versionLength + 4 + payloadLength)
					break;

				// 5️⃣ 페이로드 데이터
				Span<byte> payloadBytes = buffer.Slice(count, payloadLength);
				count += (ushort)payloadLength;
				//Console.WriteLine($"📦 [OnReceive] 페이로드 데이터: {BitConverter.ToString(payloadBytes)}");

				// 6️⃣ 패킷 생성 및 큐에 추가
				var packetType = PacketMapper.ConvertToPacketType(type);
				var packet = new Packet(packetType, version, payloadBytes);

				// 여기까지 왔으면 패킷 조립 가능
				OnRecvPacket(packet);
				packetCount++;

				//Console.WriteLine($"✅ [OnReceive] 큐에 추가됨 (패킷 타입: {type}, 현재 큐 크기: {receiveQueue.Count})");
				processedLength += 7 + versionLength + payloadLength;
			}

			if (packetCount > 1)
				Console.WriteLine($"패킷 모아보내기 : {packetCount}");

			return processedLength;
		}

		public abstract void OnRecvPacket(Packet packet);
	}

	public abstract class Session
	{
		Socket _socket;
		int _disconnected = 0;

		RecvBuffer _recvBuffer = new RecvBuffer(65535);

		object _lock = new object();
		Queue<ReadOnlyMemory<byte>> _sendQueue = new Queue<ReadOnlyMemory<byte>>();
		List<ArraySegment<byte>> _pendingList = new List<ArraySegment<byte>>();
		SocketAsyncEventArgs _sendArgs = new SocketAsyncEventArgs();
		SocketAsyncEventArgs _recvArgs = new SocketAsyncEventArgs();

		public abstract void OnConnected(EndPoint endPoint);
		public abstract int OnRecv(Span<byte> buffer);
		public abstract void OnSend(int numOfBytes);
		public abstract void OnDisconnected(EndPoint endPoint);

		void Clear()
		{
			lock (_lock)
			{
				_sendQueue.Clear();
				_pendingList.Clear();
			}
		}

		public void Start(Socket socket)
		{
			_socket = socket;

			// 비동기 작업 완료 시 처리할 이벤트 추가
			_recvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnRecvCompleted);
			_sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);

			RegisterRecv();
		}

		public void Send(List<ReadOnlyMemory<byte>> sendBuffList)
		{
			if (sendBuffList.Count == 0)
				return;

			lock (_lock)
			{
				foreach (ReadOnlyMemory<byte> sendBuff in sendBuffList)
					_sendQueue.Enqueue(sendBuff);

				if (_pendingList.Count == 0)
					RegisterSend();
			}
		}

		public void Send(ReadOnlyMemory<byte> sendBuff)
		{
			lock (_lock)
			{
				_sendQueue.Enqueue(sendBuff);
				if (_pendingList.Count == 0)
					RegisterSend();
			}
		}

		public void Disconnect()
		{
			if (Interlocked.Exchange(ref _disconnected, 1) == 1)
				return;

			OnDisconnected(_socket.RemoteEndPoint);
			_socket.Shutdown(SocketShutdown.Both);
			_socket.Close();
			Clear();
		}

		#region 네트워크 통신

		void RegisterSend()
		{
			if (_disconnected == 1)
				return;

			while (_sendQueue.Count > 0)
			{
				ReadOnlyMemory<byte> buffer = _sendQueue.Dequeue();
				_pendingList.Add(buffer.Span.ToArray());
			}
			_sendArgs.BufferList = _pendingList;

			try
			{
				bool pending = _socket.SendAsync(_sendArgs);
				if (pending == false)
					OnSendCompleted(null, _sendArgs);
			}
			catch (Exception e)
			{
				Console.WriteLine($"RegisterSend Failed {e}");
			}
		}

		void OnSendCompleted(object sender, SocketAsyncEventArgs args)
		{
			lock (_lock)
			{
				if (args.BytesTransferred <= 0 || args.SocketError != SocketError.Success)
					Disconnect();

				try
				{
					_sendArgs.BufferList = null;
					_pendingList.Clear();

					OnSend(_sendArgs.BytesTransferred);

					if (_sendQueue.Count > 0)
						RegisterSend();
				}
				catch (Exception e)
				{
					Console.WriteLine($"OnSendCompleted Failed {e}");
				}
			}
		}

		// 데이터 처리 메서드
		void RegisterRecv()
		{
			if (_disconnected == 1)
				return;

			// 버퍼 정리 
			_recvBuffer.Clean();

			try
			{
				// 비동기 수신 이벤트에 버퍼 설정  
				_recvBuffer.ConfigureSocketBuffer(_recvArgs);

				bool pending = _socket.ReceiveAsync(_recvArgs);
				// 처리 완료 시 바로 다음 작업으로
				if (pending == false)
					OnRecvCompleted(null, _recvArgs);
			}
			catch (Exception e)
			{
				Console.WriteLine($"RegisterRecv Failed {e}");
			}
		}

		void OnRecvCompleted(object sender, SocketAsyncEventArgs args)
		{
			if (args.BytesTransferred <= 0 || args.SocketError != SocketError.Success)
				Disconnect();

			try
			{
				// Write 커서 이동
				if (_recvBuffer.OnWrite(args.BytesTransferred) == false)
				{
					Disconnect();
					return;
				}

				// 컨텐츠 쪽으로 데이터를 넘겨주고 얼마나 처리했는지 받는다
				int processLen = OnRecv(_recvBuffer.ReadSegment);
				if (processLen < 0 || _recvBuffer.DataSize < processLen)
				{
					Disconnect();
					return;
				}

				// Read 커서 이동
				if (_recvBuffer.OnRead(processLen) == false)
				{
					Disconnect();
					return;
				}

				RegisterRecv();
			}
			catch (Exception e)
			{
				Console.WriteLine($"OnRecvCompleted Failed {e}");
			}
		}
	}
	#endregion
}
