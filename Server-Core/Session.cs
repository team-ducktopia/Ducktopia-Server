﻿using System;
using System.Buffers.Binary;
using System.Buffers;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Server_Core
{
	public abstract class PacketSession : Session
	{
		public static readonly int HeaderSize = 2;
		byte[] recvBuff = new byte[1024];

		// [size(2)][packetId(2)][ ... ][size(2)][packetId(2)][ ... ]
		public sealed override int OnRecv(ArraySegment<byte> buffer)
		{
			int processLen = 0;
			int packetCount = 0;

			while (true)
			{
				var recvByteLength = _socket.ReceiveAsync(recvBuff, SocketFlags.None);
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
						break;

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

			// 최소한 헤더는 파싱할 수 있는지 확인
			if (buffer.Count < HeaderSize)
					break;

			// 패킷이 완전체로 도착했는지 확인
			ushort dataSize = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
			if (buffer.Count < dataSize)
				break;

			// 여기까지 왔으면 패킷 조립 가능
			OnRecvPacket(new ArraySegment<byte>(buffer.Array, buffer.Offset, dataSize));
			packetCount++;

			processLen += dataSize;
			buffer = new ArraySegment<byte>(buffer.Array, buffer.Offset + dataSize, buffer.Count - dataSize);

			if (packetCount > 1)
				Console.WriteLine($"패킷 모아보내기 : {packetCount}");

			return processLen;
		}


		

		public abstract void OnRecvPacket(ArraySegment<byte> buffer);
	}

	public abstract class Session
	{
		Socket _socket;
		int _disconnected = 0;

		RecvBuffer _recvBuffer = new RecvBuffer(65535);

		object _lock = new object();
		Queue<ArraySegment<byte>> _sendQueue = new Queue<ArraySegment<byte>>();
		List<ArraySegment<byte>> _pendingList = new List<ArraySegment<byte>>();
		SocketAsyncEventArgs _sendArgs = new SocketAsyncEventArgs();
		SocketAsyncEventArgs _recvArgs = new SocketAsyncEventArgs();

		public abstract void OnConnected(EndPoint endPoint);
		public abstract int OnRecv(ArraySegment<byte> buffer);
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

		public void Send(List<ArraySegment<byte>> sendBuffList)
		{
			if (sendBuffList.Count == 0)
				return;

			lock (_lock)
			{
				foreach (ArraySegment<byte> sendBuff in sendBuffList)
					_sendQueue.Enqueue(sendBuff);

				if (_pendingList.Count == 0)
					RegisterSend();
			}
		}

		public void Send(ArraySegment<byte> sendBuff)
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
				ArraySegment<byte> buff = _sendQueue.Dequeue();
				_pendingList.Add(buff);
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
				if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
				{
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
				else
				{
					Disconnect();
				}
			}
		}

		// 데이터 처리 메서드
		void RegisterRecv()
		{
			if (_disconnected == 1)
				return;

			_recvBuffer.Clean();
			ArraySegment<byte> segment = _recvBuffer.WriteSegment;
			_recvArgs.SetBuffer(segment.Array, segment.Offset, segment.Count);

			try
			{
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
