using UnityEngine;
using WebSocketSharp;

public class WebSocketClient : MonoBehaviour
{
    private WebSocket _webSocket;

    void Start()
    {
        // Input the Server Address
        _webSocket = new WebSocket("ws://localhost:8765"); // 예: 로컬 WebSocket 서버

        // When Connected
        _webSocket.OnOpen += (sender, e) =>
        {
            Debug.Log("WebSocket 연결됨!");
        };

        // When Got Message
        _webSocket.OnMessage += (sender, e) =>
        {
            Debug.Log("서버에서 받은 메시지: " + e.Data);
        };

        // When an error occurs
        _webSocket.OnError += (sender, e) =>
        {
            Debug.LogError("WebSocket 에러: " + e.Message);
        };

        // When Disconnected
        _webSocket.OnClose += (sender, e) =>
        {
            Debug.Log("WebSocket 연결 종료됨.");
        };

        // Trying real connection
        _webSocket.Connect();
    }

    void Update()
    {
        // Spacebar -> Push a message to server
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (_webSocket != null && _webSocket.IsAlive)
            {
                _webSocket.Send("Hellow!");
                Debug.Log("Succeeded to send message");
            }
        }
    }

    void OnDestroy()
    {
        if (_webSocket != null)
        {
            _webSocket.Close();
        }
    }
}
