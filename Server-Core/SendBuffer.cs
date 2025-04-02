using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Server_Core
{
	public class SendBufferHelper
	{
		public static ThreadLocal<SendBuffer> CurrentBuffer = new ThreadLocal<SendBuffer>(() => { return null; });

		public static int ChunkSize { get; set; } = 65535;

		public static Span<byte> Open(int reserveSize)
		{
			if (CurrentBuffer.Value == null)
				CurrentBuffer.Value = new SendBuffer(ChunkSize);

			if (CurrentBuffer.Value.FreeSize < reserveSize)
				CurrentBuffer.Value = new SendBuffer(ChunkSize);

			return CurrentBuffer.Value.Open(reserveSize);
		}

		public static ReadOnlySpan<byte> Close(int usedSize)
		{
			return CurrentBuffer.Value.Close(usedSize);
		}
	}

	public class SendBuffer
	{
		// [][][][][][][][][u][]
		Memory<byte> _buffer;
		int _usedSize = 0;

		public int FreeSize { get { return _buffer.Length - _usedSize; } }

		public SendBuffer(int chunkSize)
		{
			_buffer = new byte[chunkSize];
		}

		public Span<byte> Open(int reserveSize)
		{
			if (reserveSize > FreeSize)
				return Span<byte>.Empty;

			return _buffer.Span.Slice(_usedSize, reserveSize);
		}

		public ReadOnlySpan<byte> Close(int usedSize)
		{
			ReadOnlySpan<byte> segment = _buffer.Span.Slice(_usedSize, usedSize);
			_usedSize += usedSize;
			return segment;
		}
	}
}
