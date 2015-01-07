//1台の端末で動作させる場合に定義
//#define UNUSE_MATTCHING_SERVER

using UnityEngine;
using System.Collections;
using System.Net;
using System.Threading;


//タイトル画面のシーケンス
public class TitleControl : MonoBehaviour {

	public Texture title_image = null; //タイトル画面

	public const bool is_single = false; //デバッグ用

	public enum STEP { 
		
		NONE = -1,

		WAIT = 0,
		MATCHING,
		WAIT_MATCHING,
		SERVER_START,
		SERVER_CONNECT,
		CLIENT_CONNECT,
		PREPARE,
		CONNECTION,

#if UNUSE_MATCHING_SERVER
		WAIT_SYNC,
#endif
		GAME_START,

		ERROR,
		WAIT_RESTART,

		NUM,
	};

	public STEP				step		= STEP.NONE;
	public STEP				next_step	= STEP.NONE;

	private float			step_timer	= 0.0f;

	private MatchingClient	m_client	= null;

	private Network m_network = null;

	private string m_serverAddress = "";

	private bool m_syncFlag = false;


	//ホストフラグ
	private bool m_isHost = false;

	//エラーメッセージ
	private string m_errorMessage = "";

#if UNUSE_MATCCHING_SERVER
	private int count_ = 0;
#endif

	//================================//
	//MonoBehaviour

	void Start() {
		this.step		= STEP.NONE;
		this.next_step	= STEP.WAIT;

		GlobalParam.GetInstance().
	}

}

