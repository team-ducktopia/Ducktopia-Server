using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Server_Core
{
	public class Listener
	{
		Socket _listenSocket;
		Func<Session> _sessionFactory;
		

		public void Init(IPEndPoint endPoint, Func<Session> sessionFactory, int register = 10, int backlog = 100)
		{
			_listenSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			// Init Server 메서드
			_sessionFactory += sessionFactory;

			// 문지기 교육
			_listenSocket.Bind(endPoint);

			// 영업 시작
			// backlog : 최대 대기수
			_listenSocket.Listen(backlog);

			for (int i = 0; i < register; i++)
			{
				// 1. 새 SocketAsyncEventArgs 객체 생성  
				SocketAsyncEventArgs args = new SocketAsyncEventArgs();

				// 2. 비동기 작업 완료 시 호출될 이벤트 핸들러 등록  
				args.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);

				// 3. 연결 수락 비동기 작업 등록 
				RegisterAccept(args);
			}
		}

		void RegisterAccept(SocketAsyncEventArgs args)
		{
			// 1. 이전 연결 소켓 초기화  
			args.AcceptSocket = null;

			// 2. 비동기 연결 수락 시도  
			bool pending = _listenSocket.AcceptAsync(args);

			// 3. 즉시 완료된 경우 콜백 직접 호출  
			if (pending == false)
				OnAcceptCompleted(null, args);
		}

		void OnAcceptCompleted(object sender, SocketAsyncEventArgs args)
		{
			if (args.SocketError == SocketError.Success)
			{
				Session session = _sessionFactory.Invoke();
				session.Start(args.AcceptSocket);
				session.OnConnected(args.AcceptSocket.RemoteEndPoint);
			}
			else
				Console.WriteLine(args.SocketError.ToString());

			RegisterAccept(args);
		}
	}
}
