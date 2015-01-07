using UnityEngine;
using System;
using System.Collections;
using System.Net;

public enum PacketId { 
	
	//マッチング用パケット
	MatchingRequest = 0,	// マッチング要求パケット
	MatchingResponse,
	SearchRoomResponse,
	StartSessionNotify,

	//ゲーム用パケット
	Equip,
	GameSyncInfo,
	CharacterData,
	AttackData,
	ItemData,
	UseItem,
	DoorState,
	MovingRoom,
	HpData,
	DamageData,
	DamageNotify,
	MonsterData,
	Summon,
	BossDirectAttack,
	BossRangeAttack,
	BossQuickAttack,
	BossDead,
	Prize,
	PrizeResult,
	ChatMessage,

	Max,
}

public enum MatchingRequestId { 

	CreateRoom = 0,
	JoinRoom,
	StartSession,
	SearchRoom,

	Max,
}

public enum MatchingResult { 

	Success = 0,

	RoomIsFull,
	MemberIsFull,
	RoomIsGone,
}

public struct PacketHeader { 
	
	//パケット種別
	public PacketId packetId;
}

//
//マッチングリクエスト
//
public struct MatchingRequest {

	public int version;					//パケットID
	public MatchingRequestId request;	//リクエスト内容
	public int roomId;					//リクエスト要求ルームID
	public string name;					//作成ルーム名
	public int level;					//レベル分けの指定

	public const int roomNameLength = 32;
}

//
//マッチングレスポンス
//

public struct MatchingResponse { 
	
	//リクエストの結果
	public MatchingResult result;

	//リクエスト内容
	public MatchingRequestId request;

	//レスポンスルームID
	public int roomId;

	//
	public string name;

	//参加人数
	public int members;

	//ルーム名の長さ
	public const int roomNameLength = 32;

}

//
//ルーム情報
//
public struct RoomInfo { 
	
	//リクエスト要求ルームID.
	public int roomId;

	//作成ルーム名
	public string name;

	//
	public int members;

	//ルーム名の長さ
	public const int roomNameLength = 32;
}

//
//ルーム検索結果
//
public struct SearchRoomResponse { 
	
	//検索した部屋の数
	public int roomNum;

	//部屋情報
	public RoomInfo[] rooms;
}

//
//ルーム検索結果．
//
public struct EndPointData {

	public string ipAddress;

	public int port;

	//IPアドレスの長さ
	public const int ipAddressLength = 32;
}

//
//セッション情報
//
public struct SessionData {

	public MatchingResult result;

	public int playerId;

	public int members;

	public EndPointData[] endPoints;

}

//
//ゲーム用パケットデータ定義
//

//
//ゲーム前の同期情報
//                
public struct CharEquipment {

	public int globalId;

	//

	public int shotType;
}


//
//全員分の同期情報
//
public struct GameSyncInfo {

	public int seed;
	public CharEquipment[] items;

}

//
//アイテム取得情報
//
public struct ItemData {

	public string itemId;
	public int state;
	public string ownerId;

	public const int itemNameLength = 32;
	public const int charactorNameLength = 64;
}


//
//キャラクター座標情報
//
public struct CharacterCoord {

	public float x;
	public float z;

	public CharacterCoord (float x, float z) {
		this.x = x;
		this.z = z;
	}

	public Vector3 ToVector3() {
		return (new Vector3(this.x, 0.0f, this.z));
	}

	public static CharacterCoord FromVector3(Vector3 v) {
		return (new CharacterCoord(v.x, v.z));
	}

	public static CharacterCoord Lerp(CharacterCoord c0,CharacterCoord c1, float rate){
		CharacterCoord c = new CharacterCoord();

		c.x = Mathf.Lerp(c0.x,c1.x,rate);
		c.z = Mathf.Lerp(c0.z,c1.z,rate);

		return (c);
	}


}


//
//キャラクターの移動情報
//
public struct CharacterData {

	public string characterId;
	public int index;
	public int dataNum;
	public CharacterCoord[] coordinates;

	public const int characterNameLength = 64;

}

//
//キャラクターの攻撃情報
//
public struct AttackData {

	public string characterId;
	public int attackKind;

	public const int characterNameLength = 64;
	
}

//
//モンスターのリスポーン
//
public struct MonsterData {

	public string lairId;
	public string monsterId;

	public const int monsterNameLength = 64;
}

//
//ダメージ量の情報
//
public struct DamageData {

	public string target;
	public int attacker;
	public float damage;

	public const int characterNameLength = 64;

}

//
//キャラクターHPの情報
//
public struct HpData {


	public string characterId;
	public float hp;

	public const int characterNameLength = 64;
}

//
//ドーナツに入った状態
//
public struct CharDoorState {
	public int globalId;
	public string keyId;
	public bool isInTrigger;
	public bool hasKey;

	public const int keyNameLength = 64;

}

//
//ルーム移動通知
//
public struct MovingRoom {

	public string keyId;

	public const int keyNameLength = 32;

}

//
//アイテムの使用情報
//
public struct ItemUseData {
	public int itemFavor;
	public string targetId;
	public string userId;

	public int itemCategory;

	public const int characterNameLength = 64;

}

//
//召喚獣の出現情報
//
public struct SummonData {

	public string summon;

	public const int summonNameLength = 32;
}

//
//ボス攻撃情報
//

//直接攻撃
public struct BossDirectAttack {
	public string target;
	public float power;

	public const int characterNameLength = 64;
}


//範囲攻撃
public struct BossRangeAttack {
	public float power;
	public float range;
}

//クイック攻撃
public struct BossQuickAttack {

	public string target;
	public float power;

	public const int characterNameLength = 64;

}

//ボス死亡通知
public struct BossDead {
	public string bossId;

	public const int bossNameLength=64;
}

//
//ご褒美ケーキ情報
//
public struct PrizeData {
	public string characterId;
	public int cakeNum;

	public const int characterNameLength = 64;
}

//
//ご褒美ケーキ結果パケット定義
//
public struct PrizeResultData {
	public int cakeDataNum; //ケーキデータ数
	public int[] cakeNum;	//食べたケーキの数
}


//
//チャットメッセージ
//
public struct ChatMessage {

	public string characterId;
	public string message;

	public const int characterNameLength = 64;
	public const int messageLength = 64;
}
