using UnityEngine;
using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;

public class TransportUDP : ITransport {

	private int m_nodeId = -1;

	private Socket m_socket = null;

	private IPEndPoint m_localEndPoint = null;

	private IPEndPoint m_remoteEndPoint = null;

	//送信バッファ
	private PacketQueue m_sendQueue = new PacketQueue();

	//受信バッファ
	private PacketQueue m_recvQueue = new PacketQueue();

	//送信バッファ
	private int m_packetSize = 1400;

	//接続フラグ
	private bool m_isRequested = false;

	//受信フラグ
	private bool m_isConnected = false;

	//タイムアウト時間
	private const int m_timeOutSec = 10;

	//タイムアウトのティッカー
	private DateTime m_timeOutTicker;

	//キープアライブインターバル
	private const int m_keeAliveInter = 1;

	//キープアライブティッカー
	private DateTime m_keepAliveTicker;

	//接続時、即時にキープアライブを送信するためのフラグ
	private bool m_isFirst = false;

	//接続確認用のダミーパケットデータ
	public const string m_requestData = "KeepAlive.";

	//イベントハンドラ
	private EventHandler m_handler;

	//同一端末実行時の判別用にリスニングソケットのポート番頭を保存
	private int m_serverPort = -1;

	public TransportUDP(){

	}

	public TransportUDP(Socket socket) {
		m_socket = socket;
	}

	public bool Initialize(Socket socket) {
		m_socket = socket;
		m_isRequested = true;

		return true;
	}

	public bool Terminate() {
		m_socket = null;

		return true;
	}

	public int GetNodeId() {
		return m_nodeId;
	}

	public void SetNodeId(int node) {
		m_nodeId = node;
	}

	public IPEndPoint GetLocalEndPoint() {
		return m_localEndPoint;
	}

	public IPEndPoint GetRemoteEndPoint() {
		return m_remoteEndPoint;
	}

	public void SetServerPort(int port) {
		m_serverPort = port;
	}

	public bool Connect(string ipAddress, int port) {
		if (m_socket == null) {
			m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			Debug.Log("Create new socket.");
		}

		try
		{
			//ホスト名を取得する
			string hostname = Dns.GetHostName();
			
			//ホスト名からIPアドレスを取得する
			IPAddress[] adrList = Dns.GetHostAddresses(hostname);
			foreach (IPAddress address in adrList) {
				if (address.AddressFamily == AddressFamily.InterNetwork) {
					m_localEndPoint = new IPEndPoint(address, m_serverPort);
					break;
				}
			}

			m_remoteEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);
			m_isRequested = true;
			Debug.Log("Connection success");
			Debug.Log("Local:" + m_localEndPoint.Address.ToString());
			Debug.Log("Remote:" + m_remoteEndPoint.Address.ToString() + ":" + m_remoteEndPoint.Port);
		}
		catch {
			m_isRequested = false;
			Debug.Log("Connect fail");
		}

		string str = "TransportUDP connect:" + m_isRequested.ToString();
		Debug.Log(str);
		if (m_handler != null) {
			NetEventState state = new NetEventState();
			state.type = NetEventType.Connect;
			state.result = (m_isRequested == true) ? NetEventResult.Success : NetEventResult.Failure;
			m_handler(this, state);
			Debug.Log("event handler called");
		}

		m_keepAliveTicker = DateTime.Now;
		m_isFirst = true;

		return m_isRequested;
	}

	public void Disconnect() {

		m_isRequested = false;

		if (m_socket != null) {
			m_socket.Shutdown(SocketShutdown.Both);
			m_socket.Close();
			m_socket = null;
		}

		if (m_handler != null) {
			NetEventState state = new NetEventState();
			state.type = NetEventType.Disconnect;
			state.result = NetEventResult.Success;
			m_handler(this, state);
		}
	}

	public int Send(byte[] data, int size) {

		if (m_sendQueue == null) {
			return 0;
		}

		return m_sendQueue.Enqueue(data, size);
	}

	public int Receive(ref byte[] buffer, int size) {
		if (m_recvQueue == null) {
			return 0;
		}

		return m_recvQueue.Dequeu(ref buffer, size);
	}

	public void RegisterEventHandler(EventHandler handler) {
		m_handler += handler;
	}

	public void UnregistereventHandler(EventHandler handler) {
		m_handler -= handler;
	}

	//接続要求した
	public bool IsRequested() {
		return m_isRequested;
	}

	//
	public bool IsConnected() {
		return m_isConnected;
	}

	public void Dispatch() {
		//送信処理
		DispatchSend();

		//タイムアウト処理
		CheckTimeout();

		//キープアライブ
		if (m_socket != null) { 
			//通信相手に接続を開始したことを定期的に通知する。
			TimeSpan ts = DateTime.Now - m_keepAliveTicker;

			if (ts.Seconds > m_keeAliveInter || m_isFirst) { 
				//UDPの接続に関して、サンプルコードではハンドシェイクを行わないため、
				//同一端末で実行する際にポート番号で送信元を判別しなければなりません。
				//このため、接続のトリガーとなるキープアライブのパケットにIPアドレスと
				//ポート番号を載せて判別させるようにしています。
				string message = m_localEndPoint.Address.ToString() + ":" + m_serverPort + ":" + m_requestData;
				byte[] request = System.Text.Encoding.UTF8.GetBytes(message);
				m_socket.SendTo(request, request.Length, SocketFlags.None, m_remoteEndPoint);
				m_keepAliveTicker = DateTime.Now;
				m_isFirst = false;
				Debug.Log("SendMsg" + message);
			}
		}
	}

	void CheckTimeout()
	{
		TimeSpan ts = DateTime.Now - m_timeOutTicker;

		if (m_isRequested && m_isConnected && ts.Seconds > m_timeOutSec) {
			Debug.Log("Disconnecty because of timeout");
			//タイムアウトする時間までにデータが届かなかった
			//理解を簡単にするために、あえて通信スレッドからメインスレッドを呼び出して、
			//本来ならば切断リクエストを発行して、メインスレッド側でリクエストを監視し、
			//メインスレッド側の処理で切断を行うようにしましょう
			Disconnect();
		}
	}

	void DispatchSend()
	{
		if (m_socket == null) {
			return;
		}

		try
		{
			//送信処理
			if (m_socket.Poll(0, SelectMode.SelectWrite))
			{
				byte[] buffer = new byte[m_packetSize];

				int sendSize = m_sendQueue.Dequeu(ref buffer, buffer.Length);
				while (sendSize > 0)
				{
					m_socket.SendTo(buffer, sendSize, SocketFlags.None, m_remoteEndPoint);
					sendSize = m_sendQueue.Dequeu(ref buffer, buffer.Length);
				}
			}
		}
		catch {
			return;
		}
	}

	public void setReceiveData(byte[] data, int size, IPEndPoint endPoint) {
		string str = System.Text.Encoding.UTF8.GetString(data).Trim('\0');
		if (str.Contains(m_requestData)) { 
			//接続要求パケット受信
			if (m_isConnected == false && m_handler != null) {
				NetEventState state = new NetEventState();
				state.type = NetEventType.Connect;
				state.result = NetEventResult.Success;
				m_handler(this, state);

				IPEndPoint ep = m_localEndPoint;
				Debug.Log("[UPD]Connected from client.[address:" + ep.Address.ToString() + " port:" + ep.Port + "]");
			}

			m_isConnected = true;
			m_timeOutTicker = DateTime.Now;
		}
		else if (size > 0) {
			m_recvQueue.Enqueue(data, size);
		}

	}

}