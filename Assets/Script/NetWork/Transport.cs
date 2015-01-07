using System.Collections;
using System.Net;
using System.Net.Sockets;

//イベント通知のデリゲート
public delegate void EventHandler(ITransport transport , NetEventState state);

public interface ITransport{

	// Use this for initialization
	bool Initialize(Socket socket);

	//終了処理
	bool Terminate();

	//ノード番号取得
	int GetNodeId();

	//ノード番号設定
	void SetNodeId(int node);

	//接続元エンドポイント取得
	IPEndPoint GetLocalEndPoint();

	//接続先エンドポイント取得
	IPEndPoint GetRemoteEndPoint();

	//送信関数
	int Send(byte[] data,int size);

	//受信関数
	int Receive(ref byte[] buffer, int size);

	//接続処理
	bool Connect(string ipAddress, int port);

	//切断処理
	void Disconnect();

	//送受信処理
	void Dispatch();

	//接続確認関数
	bool IsConnected();

	//イベント関数登録関数
	void RegisterEventHandler(EventHandler handler);

	//イベント関数削除関数
	void UnregisterEventHandler(EventHandler handler);

	//同一端末で実行する際にポート番号で送信元を判別するために
	//キープアライブの番号を返す
	void SetServerPort(int port);


}