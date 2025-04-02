using Google.Protobuf;
using Google.Protobuf.Compiler;
using Google.Protobuf.Reflection;
using System;
using System.Collections.Generic;
using System.IO;
using static GamePacket;

namespace Server_Core
{
	// �ֻ��� GamePacket �޽���
	// ��Ŷ Ÿ�� ��� ���� (������ �����ؾ� ��)
	public enum ePacketType
	{
		NONE = 0,

		// 회원가입 및 로그인
		REGISTER_REQUEST = 1001,
		REGISTER_RESPONSE = 1002,
		LOGIN_REQUEST = 1003,
		LOGIN_RESPONSE = 1004,
		GET_OUT = 1007,

		// 방 생성 및 참가
		CREATE_ROOM_REQUEST = 2001,
		CREATE_ROOM_RESPONSE = 2002,
		JOIN_ROOM_REQUEST = 2003,
		JOIN_ROOM_RESPONSE = 2004,
		JOIN_ROOM_NOTIFICATION = 2005,
		GET_ROOM_LIST_REQUEST = 2006,
		GET_ROOM_LIST_RESPONSE = 2007,
		LEAVE_ROOM_REQUEST = 2008,
		LEAVE_ROOM_RESPONSE = 2009,
		LEAVE_ROOM_NOTIFICATION = 2010,

		// 게임 시작
		PREPARE_GAME_REQUEST = 3001,  // ✅ 클라이언트 → 서버 (게임 준비 요청)
		PREPARE_GAME_RESPONSE = 3002, // ✅ 서버 → 클라이언트 (게임 준비 응답)
		PREPARE_GAME_NOTIFICATION = 3003, // ✅ 서버 → 클라이언트 (게임 준비 완료 알림)
		START_GAME_REQUEST = 3004,
		START_GAME_RESPONSE = 3005,
		START_GAME_NOTIFICATION = 3006,

		// 플레이어 관련
		PLAYER_POSITION_UPDATE_REQUEST = 4001,  // ✅ 클라이언트 → 서버 (플레이어 위치 업데이트)
		PLAYER_POSITION_UPDATE_NOTIFICATION = 4002, // ✅ 서버 → 클라이언트 (플레이어 위치 업데이트)
		PLAYER_HP_UPDATE_NOTIFICATION = 4003, // ✅ 서버 → 클라이언트 (플레이어 HP 변경 알림)
		PLAYER_ATTACK_REQUEST = 4004, // ✅ 클라이언트 → 서버 (플레이어 기본 공격)
		PLAYER_ATTACK_MONSTER_REQUEST = 4005, // ✅ 클라이언트 → 서버 (플레이어 몬스터 공격 요청)
		PLAYER_ATTACK_NOTIFICATION = 4006, // ✅ 서버 → 클라이언트 (공격 결과 알림)
		PLAYER_DEATH_NOTIFICATION = 4007, // ✅ 서버 → 클라이언트 (플레이어 사망 알림)
		PLAYER_GET_ITEM_REQUEST = 4008, // ✅ 클라이언트 → 서버 (플레이어가 아이템 획득 요청)
		PLAYER_USE_ITEM_REQUEST = 4009, // ✅ 클라이언트 → 서버 (플레이어가 아이템 사용 요청)
		PLAYER_EAT_FOOD_RESPONSE = 4010, // ✅ 서버 → 클라이언트 (플레이어 음식 섭취 응답)
		PLAYER_EQUIP_WEAPON_RESPONSE = 4011, // ✅ 서버 → 클라이언트 (무기 장착 응답)
		PLAYER_DAMAGED_BY_MONSTER_REQUEST = 4012,
		PLAYER_OPEN_BOX_REQUEST = 4013,
		PLAYER_OPEN_BOX_NOTIFICATION = 4014,
		PLAYER_TAKE_OUT_AN_ITEM_REQUEST = 4015,
		PLAYER_TAKE_OUT_AN_ITEM_NOTIFICATION = 4016,
		PLAYER_PUT_AN_ITEM_REQUEST = 4017,
		PLAYER_PUT_AN_ITEM_NOTIFICATION = 4018,
		PLAYER_CLOSE_BOX_REQUEST = 4019,
		PLAYER_CLOSE_BOX_NOTIFICATION = 4020,
		ITEM_SPAWN_NOTIFICATION = 4021,
		PLAYER_GET_ITEM_NOTIFICATION = 4022,
		PLAYER_HUNGER_UPDATE_NOTIFICATION = 4023,
		PLAYER_CHATTING_REQUEST = 4024,
		PLAYER_CHATTING_NOTIFICATION = 4025,
		PLAYER_REVIVAL_NOTIFICATION = 4026,
		PLAYER_SET_OBJECT_REQUEST = 4027,
		PLAYER_SET_OBJECT_RESPONSE = 4028,
		OBJECT_SET_NOTIFICATION = 4029,
		ITEM_DETACHMENT_REQUEST = 4032,
		ITEM_DETACHMENT_NOTIFICATION = 4033,
		DROP_ITEM_REQUEST = 4034,
		DROP_ITEM_NOTIFICATION = 4035,

		// 몬스터 관련
		MONSTER_SPAWN_REQUEST = 5001,  // ✅ 서버 → 클라이언트 (몬스터 생성 요청)
		MONSTER_SPAWN_RESPONSE = 5002, // ✅ 클라이언트 → 서버 (몬스터 좌표 응답)
		MONSTER_WAVE_START_NOTIFICATION = 5003, // ✅ 서버 → 클라이언트 (몬스터 웨이브 시작 알림)
		MONSTER_AWAKE_NOTIFICATION = 5004, // ✅ 서버 → 클라이언트 (몬스터 활성화 알림)
		MONSTER_DEATH_NOTIFICATION = 5005, // ✅ 서버 → 클라이언트 (몬스터 사망 알림)
		MONSTER_MOVE_REQUEST = 5006, // ✅ 클라이언트 → 서버 (몬스터 이동 요청)
		MONSTER_MOVE_NOTIFICATION = 5007, // ✅ 서버 → 클라이언트 (몬스터 이동 알림)
		MONSTER_ATTACK_REQUEST = 5008, // ✅ 클라이언트 → 서버 (몬스터 공격 요청)
		MONSTER_ATTACK_NOTIFICATION = 5009, // ✅ 서버 → 클라이언트 (몬스터 공격 알림)
		MONSTER_HP_UPDATE_NOTIFICATION = 5010, // ✅ 서버 → 클라이언트 (몬스터 HP 업데이트)

		// 코어 관련
		OBJECT_HP_UPDATE_NOTIFICATION = 6001, // ✅ 서버 → 클라이언트 (코어 HP 업데이트)
		OBJECT_DAMAGED_BY_MONSTER_REQUEST = 6002,
		GAME_OVER_NOTIFICATION = 6003, // ✅ 서버 → 클라이언트 (게임 오버 알림)
		OBJECT_DAMAGED_BY_PLAYER_REQUEST = 6004,
		OBJECT_DESTROY_NOTIFICATION = 6005,

		// 에러 관련
		ERROR_NOTIFICATION = 7001, // ✅ 서버 → 클라이언트 (에러 알림)

		GAME_PHASE_UPDATE_NOTIFICATION = 8001,

		ITEM_COMBINATION_REQUEST = 9001,
		ITEM_COMBINATION_NOTIFICATION = 9002,

		GAME_CLEAR_NOTIFICATION = 9999
	}

	public static class PacketMapper
	{
		// 캐싱을 통한 성능 최적화  
		private static readonly Dictionary<PayloadOneofCase, ePacketType> _packetTypeCache
				= new Dictionary<PayloadOneofCase, ePacketType>()
				{
				{ PayloadOneofCase.None, ePacketType.NONE },

      // 회원가입 및 로그인
      { PayloadOneofCase.RegisterRequest, ePacketType.REGISTER_REQUEST },
			{ PayloadOneofCase.RegisterResponse, ePacketType.REGISTER_RESPONSE },
			{ PayloadOneofCase.LoginRequest, ePacketType.LOGIN_REQUEST },
			{ PayloadOneofCase.LoginResponse, ePacketType.LOGIN_RESPONSE },
			{ PayloadOneofCase.GetOut, ePacketType.GET_OUT },

      // 방 생성 및 참가
      { PayloadOneofCase.CreateRoomRequest, ePacketType.CREATE_ROOM_REQUEST },
			{ PayloadOneofCase.CreateRoomResponse, ePacketType.CREATE_ROOM_RESPONSE },
			{ PayloadOneofCase.JoinRoomRequest, ePacketType.JOIN_ROOM_REQUEST },
			{ PayloadOneofCase.JoinRoomResponse, ePacketType.JOIN_ROOM_RESPONSE },
			{ PayloadOneofCase.JoinRoomNotification, ePacketType.JOIN_ROOM_NOTIFICATION },
			{ PayloadOneofCase.GetRoomListRequest, ePacketType.GET_ROOM_LIST_REQUEST },
			{ PayloadOneofCase.GetRoomListResponse, ePacketType.GET_ROOM_LIST_RESPONSE },
			{ PayloadOneofCase.LeaveRoomRequest, ePacketType.LEAVE_ROOM_REQUEST },
			{ PayloadOneofCase.LeaveRoomResponse, ePacketType.LEAVE_ROOM_RESPONSE },
			{ PayloadOneofCase.LeaveRoomNotification, ePacketType.LEAVE_ROOM_NOTIFICATION },

      // 게임 시작 관련
      { PayloadOneofCase.GamePrepareRequest, ePacketType.PREPARE_GAME_REQUEST },
			{ PayloadOneofCase.GamePrepareResponse, ePacketType.PREPARE_GAME_RESPONSE },
			{ PayloadOneofCase.GamePrepareNotification, ePacketType.PREPARE_GAME_NOTIFICATION },
			{ PayloadOneofCase.GameStartRequest, ePacketType.START_GAME_REQUEST },
			{ PayloadOneofCase.GameStartResponse, ePacketType.START_GAME_RESPONSE },
			{ PayloadOneofCase.GameStartNotification, ePacketType.START_GAME_NOTIFICATION },

      // 플레이어 관련
      { PayloadOneofCase.PlayerPositionUpdateRequest, ePacketType.PLAYER_POSITION_UPDATE_REQUEST },
			{ PayloadOneofCase.PlayerPositionUpdateNotification, ePacketType.PLAYER_POSITION_UPDATE_NOTIFICATION },
			{ PayloadOneofCase.PlayerHpUpdateNotification, ePacketType.PLAYER_HP_UPDATE_NOTIFICATION },
			{ PayloadOneofCase.PlayerAttackRequest, ePacketType.PLAYER_ATTACK_REQUEST },
			{ PayloadOneofCase.PlayerAttackMonsterRequest, ePacketType.PLAYER_ATTACK_MONSTER_REQUEST },
			{ PayloadOneofCase.PlayerAttackNotification, ePacketType.PLAYER_ATTACK_NOTIFICATION },
			{ PayloadOneofCase.PlayerDeathNotification, ePacketType.PLAYER_DEATH_NOTIFICATION },
			{ PayloadOneofCase.PlayerGetItemRequest, ePacketType.PLAYER_GET_ITEM_REQUEST },
			{ PayloadOneofCase.PlayerUseItemRequest, ePacketType.PLAYER_USE_ITEM_REQUEST },
			{ PayloadOneofCase.PlayerEatFoodResponse, ePacketType.PLAYER_EAT_FOOD_RESPONSE },
			{ PayloadOneofCase.PlayerEquipWeaponResponse, ePacketType.PLAYER_EQUIP_WEAPON_RESPONSE },
			{ PayloadOneofCase.PlayerDamagedByMonsterRequest, ePacketType.PLAYER_DAMAGED_BY_MONSTER_REQUEST },
			{ PayloadOneofCase.PlayerOpenBoxRequest, ePacketType.PLAYER_OPEN_BOX_REQUEST },
			{ PayloadOneofCase.PlayerOpenBoxNotification, ePacketType.PLAYER_OPEN_BOX_NOTIFICATION },
			{ PayloadOneofCase.PlayerTakeOutAnItemRequest, ePacketType.PLAYER_TAKE_OUT_AN_ITEM_REQUEST },
			{ PayloadOneofCase.PlayerTakeOutAnItemNotification, ePacketType.PLAYER_TAKE_OUT_AN_ITEM_NOTIFICATION },
			{ PayloadOneofCase.PlayerPutAnItemRequest, ePacketType.PLAYER_PUT_AN_ITEM_REQUEST },
			{ PayloadOneofCase.PlayerPutAnItemNotification, ePacketType.PLAYER_PUT_AN_ITEM_NOTIFICATION },
			{ PayloadOneofCase.PlayerCloseBoxRequest, ePacketType.PLAYER_CLOSE_BOX_REQUEST },
			{ PayloadOneofCase.PlayerCloseBoxNotification, ePacketType.PLAYER_CLOSE_BOX_NOTIFICATION },
			{ PayloadOneofCase.ItemSpawnNotification, ePacketType.ITEM_SPAWN_NOTIFICATION },
			{ PayloadOneofCase.PlayerGetItemNotification, ePacketType.PLAYER_GET_ITEM_NOTIFICATION },
			{ PayloadOneofCase.PlayerHungerUpdateNotification, ePacketType.PLAYER_HUNGER_UPDATE_NOTIFICATION },
			{ PayloadOneofCase.PlayerChattingRequest, ePacketType.PLAYER_CHATTING_REQUEST },
			{ PayloadOneofCase.PlayerChattingNotification, ePacketType.PLAYER_CHATTING_NOTIFICATION },
			{ PayloadOneofCase.PlayerRevivalNotification, ePacketType.PLAYER_REVIVAL_NOTIFICATION },
			{ PayloadOneofCase.PlayerSetObjectRequest, ePacketType.PLAYER_SET_OBJECT_REQUEST },
			{ PayloadOneofCase.PlayerSetObjectResponse, ePacketType.PLAYER_SET_OBJECT_RESPONSE },
     

      // 몬스터 관련
      { PayloadOneofCase.MonsterSpawnRequest, ePacketType.MONSTER_SPAWN_REQUEST },
			{ PayloadOneofCase.MonsterSpawnResponse, ePacketType.MONSTER_SPAWN_RESPONSE },
			{ PayloadOneofCase.MonsterWaveStartNotification, ePacketType.MONSTER_WAVE_START_NOTIFICATION },
			{ PayloadOneofCase.MonsterAwakeNotification, ePacketType.MONSTER_AWAKE_NOTIFICATION },
			{ PayloadOneofCase.MonsterDeathNotification, ePacketType.MONSTER_DEATH_NOTIFICATION },
			{ PayloadOneofCase.MonsterMoveRequest, ePacketType.MONSTER_MOVE_REQUEST },
			{ PayloadOneofCase.MonsterMoveNotification, ePacketType.MONSTER_MOVE_NOTIFICATION },
			{ PayloadOneofCase.MonsterAttackRequest, ePacketType.MONSTER_ATTACK_REQUEST },
			{ PayloadOneofCase.MonsterAttackNotification, ePacketType.MONSTER_ATTACK_NOTIFICATION },
			{ PayloadOneofCase.MonsterHpUpdateNotification, ePacketType.MONSTER_HP_UPDATE_NOTIFICATION },

      // 코어 관련
      { PayloadOneofCase.ObjectHpUpdateNotification, ePacketType.OBJECT_HP_UPDATE_NOTIFICATION },
			{ PayloadOneofCase.ObjectDamagedByMonsterRequest, ePacketType.OBJECT_DAMAGED_BY_MONSTER_REQUEST },
			{ PayloadOneofCase.ObjectDamagedByPlayerRequest, ePacketType.OBJECT_DAMAGED_BY_PLAYER_REQUEST },
			{ PayloadOneofCase.ObjectDestroyNotification, ePacketType.OBJECT_DESTROY_NOTIFICATION },
			{ PayloadOneofCase.ObjectSetNotification, ePacketType.OBJECT_SET_NOTIFICATION },

			{ PayloadOneofCase.GameOverNotification, ePacketType.GAME_OVER_NOTIFICATION },
			{ PayloadOneofCase.GameClearNotification, ePacketType.GAME_CLEAR_NOTIFICATION },
      // 에러 관련
      { PayloadOneofCase.ErrorNotification, ePacketType.ERROR_NOTIFICATION },
			{ PayloadOneofCase.GamePhaseUpdateNotification, ePacketType.GAME_PHASE_UPDATE_NOTIFICATION },
      //아이템 관련
      { PayloadOneofCase.ItemCombinationRequest, ePacketType.ITEM_COMBINATION_REQUEST },
			{ PayloadOneofCase.ItemCombinationNotification, ePacketType.ITEM_COMBINATION_NOTIFICATION },

			{ PayloadOneofCase.ItemDetachmentRequest, ePacketType.ITEM_DETACHMENT_REQUEST },
			{ PayloadOneofCase.ItemDetachmentNotification, ePacketType.ITEM_DETACHMENT_NOTIFICATION },
			{ PayloadOneofCase.DropItemRequest, ePacketType.DROP_ITEM_REQUEST },
			{ PayloadOneofCase.DropItemNotification, ePacketType.DROP_ITEM_NOTIFICATION },
				};

		private static readonly Dictionary<ePacketType, PayloadOneofCase> _reversePacketTypeCache
				= new Dictionary<ePacketType, PayloadOneofCase>()
				{
					{ ePacketType.NONE, PayloadOneofCase.None },

			// 회원가입 및 로그인
			{ ePacketType.REGISTER_REQUEST, PayloadOneofCase.RegisterRequest },
			{ ePacketType.REGISTER_RESPONSE, PayloadOneofCase.RegisterResponse },
			{ ePacketType.LOGIN_REQUEST, PayloadOneofCase.LoginRequest },
			{ ePacketType.LOGIN_RESPONSE, PayloadOneofCase.LoginResponse },
			{ ePacketType.GET_OUT, PayloadOneofCase.GetOut },

			// 방 생성 및 참가
			{ ePacketType.CREATE_ROOM_REQUEST, PayloadOneofCase.CreateRoomRequest },
			{ ePacketType.CREATE_ROOM_RESPONSE, PayloadOneofCase.CreateRoomResponse },
			{ ePacketType.JOIN_ROOM_REQUEST, PayloadOneofCase.JoinRoomRequest },
			{ ePacketType.JOIN_ROOM_RESPONSE, PayloadOneofCase.JoinRoomResponse },
			{ ePacketType.JOIN_ROOM_NOTIFICATION, PayloadOneofCase.JoinRoomNotification },
			{ ePacketType.GET_ROOM_LIST_REQUEST, PayloadOneofCase.GetRoomListRequest },
			{ ePacketType.GET_ROOM_LIST_RESPONSE, PayloadOneofCase.GetRoomListResponse },
			{ ePacketType.LEAVE_ROOM_REQUEST, PayloadOneofCase.LeaveRoomRequest },
			{ ePacketType.LEAVE_ROOM_RESPONSE, PayloadOneofCase.LeaveRoomResponse },
			{ ePacketType.LEAVE_ROOM_NOTIFICATION, PayloadOneofCase.LeaveRoomNotification },

			// 게임 시작 관련
			{ ePacketType.PREPARE_GAME_REQUEST, PayloadOneofCase.GamePrepareRequest },
			{ ePacketType.PREPARE_GAME_RESPONSE, PayloadOneofCase.GamePrepareResponse },
			{ ePacketType.PREPARE_GAME_NOTIFICATION, PayloadOneofCase.GamePrepareNotification },
			{ ePacketType.START_GAME_REQUEST, PayloadOneofCase.GameStartRequest },
			{ ePacketType.START_GAME_RESPONSE, PayloadOneofCase.GameStartResponse },
			{ ePacketType.START_GAME_NOTIFICATION, PayloadOneofCase.GameStartNotification },

			// 플레이어 관련
			{ ePacketType.PLAYER_POSITION_UPDATE_REQUEST, PayloadOneofCase.PlayerPositionUpdateRequest },
			{ ePacketType.PLAYER_POSITION_UPDATE_NOTIFICATION, PayloadOneofCase.PlayerPositionUpdateNotification },
			{ ePacketType.PLAYER_HP_UPDATE_NOTIFICATION, PayloadOneofCase.PlayerHpUpdateNotification },
			{ ePacketType.PLAYER_ATTACK_REQUEST, PayloadOneofCase.PlayerAttackRequest },
			{ ePacketType.PLAYER_ATTACK_MONSTER_REQUEST, PayloadOneofCase.PlayerAttackMonsterRequest },
			{ ePacketType.PLAYER_ATTACK_NOTIFICATION, PayloadOneofCase.PlayerAttackNotification },
			{ ePacketType.PLAYER_DEATH_NOTIFICATION, PayloadOneofCase.PlayerDeathNotification },
			{ ePacketType.PLAYER_GET_ITEM_REQUEST, PayloadOneofCase.PlayerGetItemRequest },
			{ ePacketType.PLAYER_USE_ITEM_REQUEST, PayloadOneofCase.PlayerUseItemRequest },
			{ ePacketType.PLAYER_EAT_FOOD_RESPONSE, PayloadOneofCase.PlayerEatFoodResponse },
			{ ePacketType.PLAYER_EQUIP_WEAPON_RESPONSE, PayloadOneofCase.PlayerEquipWeaponResponse },
			{ ePacketType.PLAYER_DAMAGED_BY_MONSTER_REQUEST, PayloadOneofCase.PlayerDamagedByMonsterRequest },
			{ ePacketType.PLAYER_OPEN_BOX_REQUEST, PayloadOneofCase.PlayerOpenBoxRequest },
			{ ePacketType.PLAYER_OPEN_BOX_NOTIFICATION, PayloadOneofCase.PlayerOpenBoxNotification },
			{ ePacketType.PLAYER_TAKE_OUT_AN_ITEM_REQUEST, PayloadOneofCase.PlayerTakeOutAnItemRequest },
			{ ePacketType.PLAYER_TAKE_OUT_AN_ITEM_NOTIFICATION, PayloadOneofCase.PlayerTakeOutAnItemNotification },
			{ ePacketType.PLAYER_PUT_AN_ITEM_REQUEST, PayloadOneofCase.PlayerPutAnItemRequest },
			{ ePacketType.PLAYER_PUT_AN_ITEM_NOTIFICATION, PayloadOneofCase.PlayerPutAnItemNotification },
			{ ePacketType.PLAYER_CLOSE_BOX_REQUEST, PayloadOneofCase.PlayerCloseBoxRequest },
			{ ePacketType.PLAYER_CLOSE_BOX_NOTIFICATION, PayloadOneofCase.PlayerCloseBoxNotification },
			{ ePacketType.ITEM_SPAWN_NOTIFICATION, PayloadOneofCase.ItemSpawnNotification },
			{ ePacketType.PLAYER_GET_ITEM_NOTIFICATION, PayloadOneofCase.PlayerGetItemNotification },
			{ ePacketType.PLAYER_HUNGER_UPDATE_NOTIFICATION, PayloadOneofCase.PlayerHungerUpdateNotification },
			{ ePacketType.PLAYER_CHATTING_REQUEST, PayloadOneofCase.PlayerChattingRequest },
			{ ePacketType.PLAYER_CHATTING_NOTIFICATION, PayloadOneofCase.PlayerChattingNotification },
			{ ePacketType.PLAYER_REVIVAL_NOTIFICATION,  PayloadOneofCase.PlayerRevivalNotification },
			{ ePacketType.PLAYER_SET_OBJECT_REQUEST, PayloadOneofCase.PlayerSetObjectRequest },
			{ ePacketType.PLAYER_SET_OBJECT_RESPONSE, PayloadOneofCase.PlayerSetObjectResponse },

			// 몬스터 관련
			{ ePacketType.MONSTER_SPAWN_REQUEST, PayloadOneofCase.MonsterSpawnRequest },
			{ ePacketType.MONSTER_SPAWN_RESPONSE, PayloadOneofCase.MonsterSpawnResponse },
			{ ePacketType.MONSTER_WAVE_START_NOTIFICATION, PayloadOneofCase.MonsterWaveStartNotification },
			{ ePacketType.MONSTER_AWAKE_NOTIFICATION, PayloadOneofCase.MonsterAwakeNotification },
			{ ePacketType.MONSTER_DEATH_NOTIFICATION, PayloadOneofCase.MonsterDeathNotification },
			{ ePacketType.MONSTER_MOVE_REQUEST, PayloadOneofCase.MonsterMoveRequest },
			{ ePacketType.MONSTER_MOVE_NOTIFICATION, PayloadOneofCase.MonsterMoveNotification },
			{ ePacketType.MONSTER_ATTACK_REQUEST, PayloadOneofCase.MonsterAttackRequest },
			{ ePacketType.MONSTER_ATTACK_NOTIFICATION, PayloadOneofCase.MonsterAttackNotification },
			{ ePacketType.MONSTER_HP_UPDATE_NOTIFICATION, PayloadOneofCase.MonsterHpUpdateNotification },

			// 코어 관련
			{ ePacketType.OBJECT_HP_UPDATE_NOTIFICATION, PayloadOneofCase.ObjectHpUpdateNotification },
			{ ePacketType.OBJECT_DAMAGED_BY_MONSTER_REQUEST, PayloadOneofCase.ObjectDamagedByMonsterRequest },
			{ ePacketType.OBJECT_DAMAGED_BY_PLAYER_REQUEST, PayloadOneofCase.ObjectDamagedByPlayerRequest },
			{ ePacketType.OBJECT_DESTROY_NOTIFICATION, PayloadOneofCase.ObjectDestroyNotification },
			{ ePacketType.OBJECT_SET_NOTIFICATION, PayloadOneofCase.ObjectSetNotification },

			{ ePacketType.GAME_OVER_NOTIFICATION, PayloadOneofCase.GameOverNotification },
			{ ePacketType.GAME_CLEAR_NOTIFICATION, PayloadOneofCase.GameClearNotification },

			// 에러 관련
			{ ePacketType.ERROR_NOTIFICATION, PayloadOneofCase.ErrorNotification },
			{ ePacketType.GAME_PHASE_UPDATE_NOTIFICATION, PayloadOneofCase.GamePhaseUpdateNotification },

			//아이템 관련
			{ ePacketType.ITEM_COMBINATION_REQUEST, PayloadOneofCase.ItemCombinationRequest },
			{ ePacketType.ITEM_COMBINATION_NOTIFICATION, PayloadOneofCase.ItemCombinationNotification },

			{ ePacketType.ITEM_DETACHMENT_REQUEST, PayloadOneofCase.ItemDetachmentRequest },
			{ ePacketType.ITEM_DETACHMENT_NOTIFICATION, PayloadOneofCase.ItemDetachmentNotification },
			{ ePacketType.DROP_ITEM_REQUEST, PayloadOneofCase.DropItemRequest },
			{ ePacketType.DROP_ITEM_NOTIFICATION, PayloadOneofCase.DropItemNotification },
				};


		public static ePacketType ConvertToPacketType(PayloadOneofCase payloadType)
		{
			return _packetTypeCache.TryGetValue(payloadType, out var packetType)
					? packetType
					: ePacketType.NONE;
		}

		public static PayloadOneofCase ConvertToPayloadCase(ePacketType packetType)
		{
			return _reversePacketTypeCache.TryGetValue(packetType, out var payloadType)
					? payloadType
					: PayloadOneofCase.None;
		}
	}

	public class Packet
	{
		// [PacketType(2)][VerstionLength(1)][Version(..)][PayloadLength(4)][Payload(..)]
		private ePacketType _type;
		public ePacketType Type { get { return _type; } }
		private string _version;
		public string Version { get { return _version; } }
		private byte[] _payloadBytes;
		public byte[] PayloadBytes { get { return _payloadBytes; } }
		public GamePacket GamePacket
		{
			get
			{
				GamePacket gamePacket = new GamePacket();
				gamePacket.MergeFrom(_payloadBytes);
				return gamePacket;
			}
		}

		public Packet(ePacketType type, string version, Span<byte> payload)
		{
			_type = type;
			_version = version;
			_payloadBytes = payload.ToArray();
		}

		public ArraySegment<byte> ToByteArray()
		{
			var stream = new MemoryStream();
			var writer = new BinaryWriter(stream);
			byte[] bytes = new byte[1024];
			var fields = GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			foreach (var field in fields)
			{
				if (field.FieldType == typeof(int))
				{
					bytes = BitConverter.GetBytes((int)field.GetValue(this));
					Array.Reverse(bytes);
					writer.Write(bytes);
				}
				else if (field.FieldType == typeof(string))
				{
					var str = (string)field.GetValue(this);
					//writer.Write((char)UTF8Encoding.UTF8.GetBytes(str).Length);
					writer.Write(str);
				}
				else if (field.FieldType == typeof(bool))
				{
					writer.Write((bool)field.GetValue(this));
				}
				else if (field.FieldType == typeof(short))
				{
					bytes = BitConverter.GetBytes((short)field.GetValue(this));
					Array.Reverse(bytes);
					writer.Write(bytes);
				}
				else if (field.FieldType == typeof(float))
				{
					bytes = BitConverter.GetBytes((float)field.GetValue(this));
					Array.Reverse(bytes);
					writer.Write(bytes);
				}
				else if (field.FieldType == typeof(double))
				{
					bytes = BitConverter.GetBytes((double)field.GetValue(this));
					Array.Reverse(bytes);
					writer.Write(bytes);
				}
				else if (field.FieldType.IsEnum)
				{
					bytes = BitConverter.GetBytes((short)(int)field.GetValue(this));
					Array.Reverse(bytes);
					writer.Write(bytes);
				}
				else
				{
					using (MemoryStream memory = new MemoryStream())
					{
						var array = (byte[])field.GetValue(this);
						bytes = BitConverter.GetBytes(array.Length);
						Array.Reverse(bytes);
						writer.Write(bytes);
						writer.Write(new ArraySegment<byte>(array));
						memory.Dispose();
					}
				}
			}
			writer.Flush();
			stream.Dispose();
			return stream.ToArray();
		}

	}
}