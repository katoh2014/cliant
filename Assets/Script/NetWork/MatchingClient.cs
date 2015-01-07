//1台の端末で動作をさせる場合に定義
//#define UNUSE_MATCHING_SERVER

using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Net;

public class MatchingClient : MonoBehaviour { 
	
	//マッチングできる最大の部屋数
	private const int maxRoomNum = 4;

	//参加できる最大プレイヤー数
	private const int maxMemberNum = NetConfig.Pr

}