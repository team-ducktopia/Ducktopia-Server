using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Server_Core
{
	public class RecvBuffer
	{
		// [r][][w][][][][][][][]
		private byte[] _buffer;
		private int _readPos;
		private int _writePos;

		public RecvBuffer(int bufferSize)
		{
			_buffer = new byte[bufferSize];
		}

		public int DataSize { get { return _writePos - _readPos; } }
		public int FreeSize { get { return _buffer.Length - _writePos; } }

		public Span<byte> ReadSegment { get { return _buffer.AsSpan(_readPos, DataSize); } }
		public Span<byte> WriteSegment { get { return _buffer.AsSpan(_writePos, FreeSize); } }

		// 현재 쓰기 위치 Offset 반환  
		public int WriteOffset { get { return _writePos; } }

		// 내부 버퍼 직접 반환  
		public byte[] GetBuffer() => _buffer;

		public void Clean()
		{
			int dataSize = DataSize;
			if (dataSize == 0)
			{
				// 남은 데이터가 없으면 복사하지 않고 커서 위치만 리셋
				_readPos = _writePos = 0;
			}
			else
			{
				// 남은 찌끄레기가 있으면 시작 위치로 복사
				_buffer.AsSpan(_readPos, dataSize).CopyTo(_buffer.AsSpan(0, dataSize));
				_readPos = 0;
				_writePos = dataSize;
			}
		}

		public bool OnRead(int numOfBytes)
		{
			if (numOfBytes > DataSize)
				return false;

			_readPos += numOfBytes;
			return true;
		}

		public bool OnWrite(int numOfBytes)
		{
			if (numOfBytes > FreeSize)
				return false;

			_writePos += numOfBytes;
			return true;
		}

		// 소켓 버퍼 구성을 위한 메서드 추가  
		public void ConfigureSocketBuffer(SocketAsyncEventArgs recvArgs)
		{
			recvArgs.SetBuffer(_buffer, _writePos, FreeSize);
		}
	}
}
