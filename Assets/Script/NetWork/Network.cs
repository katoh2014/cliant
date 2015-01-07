using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;

public class Network : MonoBehaviour {

	private SessionTCP m_sessionTcp = null;

	private SessionUDP m_sessionUdp = null;

	private int m_serverNode = -1;

	private int[] m_clientNode = new int[NetConfig.PLAYER_MAX];

	private NodeInfo[] m_reliableNode = new NodeInfo[NetConfig.PLAYER_MAX];

	private NodeInfo[] m_unreliableNode = new NodeInfo[NetConfig.PLAYER_MAX];

	//送受信用のパケット最大サイズ
	private const int m_packetSize = 1400;

	//受信パケット処理関数のデリゲート
	public delegate void RecvNotifier(int node, PacketId id, byte[] data);

	//受信パケット振り分けハッシュテーブル
	private Dictionary<int,RecvNotifier> m_notifier = new Dictionary<int,RecvNotifier>();

	//イベントハンドラ-
	private List<NetEventState> m_eventQueue = new List<NetEventState>();

	private class NodeInfo {
		public int node = 0;
	}

	public enum ConnectionType { 
		Reliable = 0,
		Unreliable,
	}

	void Awake() {
		m_sessionTcp = new SessionTCP();
		m_sessionTcp.RegisterEventHandler(OnEventHandlingReliable);

		m_sessionUdp = new SessionUDP();
		m_sessionUdp.RegisterEventHandler(OnEventHandlingUnreliable);

		for (int i = 0; i < m_clientNode.Length; ++i){
			m_clientNode[i] = -1;
		}
	}

	//Update is called once per frame

	void Update(){

		byte[] packet = new byte[m_packetSize];
		for (int id = 0; id < m_reliableNode.Length; ++id) {
			if (m_reliableNode[id] != null) {
				int node = m_reliableNode[id].node;
				if (IsConnected(node) == true) { 
					//到達保障パケットの受信をします。
					while (m_sessionTcp.Receive(node, ref packet) > 0) { 
						//受信パケットの振り分けをします。
						Receive(node, packet);
					}
				}
			}
		}

		//非到達保障パケットの受信をします。
		for (int id = 0; id < m_unreliableNode.Length; ++id) {
			if (m_unreliableNode[id] != null) {
				int node = m_unreliableNode[id].node;
				if (IsConnected(node) == true) { 
					//到達保障のないパケットを受信します。
					while (m_sessionUdp.Receive(node, ref packet) > 0) { 
						//受信パケットの振り分けをします。
						Receive(node, packet);
					}
				}
			}
		}
	}

	void OnApplicationQuit() {
		Debug.Log("OnApplicationQuit called.");

		StopServer();
		
	}

	public bool StartServer(int port, int connectionMax, ConnectionType type) {
		Debug.Log("Start server called");

		//リスニングソケットを生成
		try
		{
			if (type == ConnectionType.Reliable)
			{
				//到達保障のTCP通信を開始します。
				m_sessionTcp.StartServer(port, connectionMax);

			}
			else
			{

				//到達保障を必要としないUDP通信はいつでも受信できるようにリスニングを開始します。
				m_sessionUdp.StartServer(port, connectionMax);

			}
		}
		catch {
			Debug.Log("Server fail start");
			return false;
		}

		Debug.Log("Server started");
		return true;
	}

	public void StopServer() {
		Debug.Log("StopServer called");

		//サーバー起動を停止
		if (m_sessionTcp != null) {
			m_sessionTcp.StopServer();
		}

		if (m_sessionUdp != null) {
			m_sessionUdp.StopServer();
		}

		Debug.Log("Server stopped.");
	}

	public int Connect(string address, int port, ConnectionType type) {
		int node = -1;

		if (type == ConnectionType.Reliable && m_sessionTcp != null) { 
			//到達保障用のTCP通信を開始します。
			node = m_sessionTcp.Connect(address, port);
		}

		if (type == ConnectionType.Reliable && m_sessionUdp != null) {
			node = m_sessionUdp.Connect(address, port);
		}

		return node;
	}

	public void Disconect(int node) {
		if (m_sessionTcp != null) {
			m_sessionTcp.Disconnect(node);
		}

		if (m_sessionUdp != null) {
			m_sessionUdp.Disconnect(node);
		}
	}

	public void Disconnect() {
		if (m_sessionTcp != null) {
			m_sessionTcp.Disconnect();
		}

		if (m_sessionUdp != null) {
			m_sessionUdp.Disconnect();
		}

		m_notifier.Clear();
	}

	public void RegisterReceiveNotification(PacketId id, RecvNotifier notifier) {
		int index = (int)id;

		m_notifier.Add(index, notifier);
	}

	public void ClearReciveNotification() {
		m_notifier.Clear();
	}

	public void UnregisterReciveNotification(PacketId id) {
		int index = (int)id;

		if (m_notifier.ContainsKey(index)) {
			m_notifier.Remove(index);
		}
	}

	//イベント通知関数登録
	public NetEventState GetEventState(){
		if (m_eventQueue.Count == 0) {
			return null;
		}

		NetEventState state = m_eventQueue[0];

		m_eventQueue.RemoveAt(0);

		return state;
	}




	public bool IsConnected(int node) {
		if (m_sessionTcp != null) {
			if (m_sessionTcp.IsConnected(node)) {
				return true;
			}
		}

		if (m_sessionUdp != null) {
			if (m_sessionUdp.IsConnected(node)) {
				return true;
			}
		}

		return false;
	}

	public bool IsServer() {
		if (m_sessionTcp == null) {
			return false;
		}

		return m_sessionTcp.IsServer();
	}

	public IPEndPoint GetLocalEndpoint(int node) { 
		
		if(m_sessionTcp == null){
			return default(IPEndPoint);
		}
		return m_sessionTcp.GetLocalEndPoint(node);
	}

	public int Send<T>(int node, PacketId id, IPacket<T> packet) {
		int sendSize = 0;

		if (m_sessionTcp != null) { 
			//モジュールで使用するヘッダ情報を生成します。
			PacketHeader header = new PacketHeader();
			HeaderSerializer serializer = new HeaderSerializer();

			header.packetId = id;

			byte[] headerData = null;
			if (serializer.Serialize(header) == true) {
				headerData = serializer.GetSerializedData();
			}
			byte[] packetData = packet.GetData();

			byte[] data = new byte[headerData.Length + packetData.Length];

			int headerSize = Marshal.SizeOf(typeof(PacketHeader));
			Buffer.BlockCopy(headerData, 0, data, 0, headerSize);
			Buffer.BlockCopy(packetData, 0, data, headerSize, packetData.Length);

			//string str = "Send Packet["+ id +"]";

			sendSize = m_sessionTcp.Send(node, data, data.Length);

		}

		return sendSize;
	}

	private void Receive(int node, byte[] data) {
		PacketHeader header = new PacketHeader();
		HeaderSerializer serializer = new HeaderSerializer();

		serializer.SetDeserializedData(data);
		bool ret = serializer.Deserialize(ref header);

		if (ret == false) {
			Debug.Log("Invalide header data.");
			//パケットとして認識できないので破棄します。
			return;

		}

		int packetId = (int)header.packetId;
		if (m_notifier.ContainsKey(packetId) &&
			m_notifier[packetId] != null) {
			int headerSize = Marshal.SizeOf(typeof(PacketHeader));
			byte[] packetData = new byte[data.Length - headerSize];
			Buffer.BlockCopy(data, headerSize, packetData, 0, packetData.Length);

			m_notifier[packetId](node, header.packetId, packetData);
		}
	}

	public void OnEventHandlingReliable(int node, NetEventState state) {
		Debug.Log("OnEventHandling called");
		string str = "Node:" + node + "type:" + state.type.ToString() + "State:" + state.type + "[" + state.result + "]";
		Debug.Log(str);

		switch (state.type) {
			case NetEventType.Connect: {
				for (int i = 0; i < m_reliableNode.Length; ++i) {
					if (m_reliableNode[i] == null)
					{
						NodeInfo info = new NodeInfo();

						info.node = node;
						m_reliableNode[i] = info;
						break;
					}
					else if(m_reliableNode[i].node == -1) {
						m_reliableNode[i].node = node;
					}
				} 
			}break;

			case NetEventType.Disconnect:{
				for(int i = 0 ;i<m_reliableNode.Length; ++i){
					if(m_reliableNode[i] == null && m_reliableNode[i].node == node){
						m_reliableNode[i].node = -1;
						break;
					}
				}
					
			}break;

		}

		if (m_eventQueue != null) { 
			//イベント登録
			NetEventState eState = new NetEventState();
			eState.Node = node;
			eState.type = state.type;
			eState.result = NetEventResult.Success;
			m_eventQueue.Add(eState);
		}
	}

}