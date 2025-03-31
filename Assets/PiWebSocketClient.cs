using System;
using UnityEngine;
using WebSocketSharp;

[Serializable]
public class AudioAnalysisMessage
{
    public int label;
    public float decibel;
}

public class PiWebSocketClient : MonoBehaviour
{
    public string serverUrl = "ws://192.168.0.100:8765";  // Raspberry Pi의 IP 주소와 포트
    private WebSocket ws;

    void Start()
    {
        ConnectWebSocket();
    }

    void ConnectWebSocket()
    {
        ws = new WebSocket(serverUrl);

        ws.OnOpen += (sender, e) =>
        {
            Debug.Log("✅ WebSocket 연결됨");
        };

        ws.OnMessage += (sender, e) =>
        {
            try
            {
                AudioAnalysisMessage data = JsonUtility.FromJson<AudioAnalysisMessage>(e.Data);
                Debug.Log($"📡 수신: 라벨 = {data.label}, 데시벨 = {data.decibel}");

                HandleLabel(data.label, data.decibel);
            }
            catch (Exception ex)
            {
                Debug.LogError("⚠️ JSON 파싱 실패: " + ex.Message);
            }
        };

        ws.OnError += (sender, e) =>
        {
            Debug.LogError("❌ WebSocket 에러: " + e.Message);
        };

        ws.OnClose += (sender, e) =>
        {
            Debug.LogWarning("⚠️ WebSocket 연결 종료됨. 2초 후 재시도...");
            Invoke(nameof(ConnectWebSocket), 2f);
        };

        ws.ConnectAsync();
    }

    void HandleLabel(int label, float decibel)
    {
        // 예시 경고 처리
        if (label == 3 && decibel > 65f)
        {
            Debug.LogWarning("🚨 큰 자동차 소리 감지됨! 조심하세요.");
            // TODO: UIManager.ShowWarning(), 진동, 알림 등
        }
        else
        {
            Debug.Log("🔈 일반적인 환경 소리.");
        }
    }

    void OnDestroy()
    {
        if (ws != null && ws.IsAlive)
        {
            ws.Close();
        }
    }
}
