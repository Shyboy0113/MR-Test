using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using WebSocketSharp;

[Serializable]
public class AudioMessage
{
    public string type = "audio";
    public string payload;
    public string timestamp;
}

public class AudioWebSocketClient : MonoBehaviour
{
    [Header("���� ����")]
    public int sampleRate = 16000;
    public int windowSeconds = 10;

    private AudioClip micClip;
    private string micDevice;
    private int recordLength = 10; // ��ȯ ���� ����
    private bool micInitialized = false;

    [Header("WebSocket")]
    public string serverUrl = "ws://localhost:8765";
    private WebSocket ws;

    void Start()
    {
        StartMicrophone();

        // AudioClip�� ����ũ ���� ���̹Ƿ�,
        // �ణ�� �ð��� �ʿ��� (0.5�� �� ���� �õ�)
        Invoke(nameof(SaveLatestAudioToWav), 10f);

        //ConnectWebSocket();
        //StartCoroutine(SendLoop());
    }


    void OnDestroy()
    {
        if (ws != null) ws.Close();
        Microphone.End(null);
    }

    void StartMicrophone()
    {
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("����ũ�� ã�� �� �����ϴ�.");
            return;
        }

        micDevice = Microphone.devices[0];
        micClip = Microphone.Start(micDevice, true, recordLength, sampleRate);
        micInitialized = true;

        Debug.Log("����ũ ���۵�: " + micDevice);
    }

    void ConnectWebSocket()
    {
        ws = new WebSocket(serverUrl);

        ws.OnOpen += (sender, e) =>
        {
            Debug.Log("WebSocket �����");
        };

        ws.OnMessage += (sender, e) =>
        {
            Debug.Log("�����κ��� ����: " + e.Data);
        };

        ws.OnError += (sender, e) =>
        {
            Debug.LogError("WebSocket ����: " + e.Message);
        };

        ws.OnClose += (sender, e) =>
        {
            Debug.Log("WebSocket ���� �����");
        };

        ws.ConnectAsync();
    }

    IEnumerator SendLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(windowSeconds);

            if (micInitialized && ws != null && ws.IsAlive)
            {
                float[] audioSamples = GetRecentAudioSamples();
                byte[] pcmBytes = FloatToPCM16(audioSamples);
                string base64Audio = Convert.ToBase64String(pcmBytes);

                AudioMessage msg = new AudioMessage
                {
                    payload = base64Audio,
                    timestamp = DateTime.UtcNow.ToString("o")
                };

                string json = JsonUtility.ToJson(msg);
                ws.Send(json);

                Debug.Log($"[{msg.timestamp}] {windowSeconds}�� �з� ����� ���۵�");
            }
        }
    }

    float[] GetRecentAudioSamples()
    {
        int micPos = Microphone.GetPosition(micDevice);
        int sampleCount = sampleRate * windowSeconds;
        float[] samples = new float[sampleCount];

        int startPos = micPos - sampleCount;
        if (startPos < 0)
        {
            float[] part1 = new float[-startPos];
            float[] part2 = new float[sampleCount + startPos];
            micClip.GetData(part2, 0);
            micClip.GetData(part1, micClip.samples + startPos);
            Array.Copy(part1, 0, samples, 0, part1.Length);
            Array.Copy(part2, 0, samples, part1.Length, part2.Length);
        }
        else
        {
            micClip.GetData(samples, startPos);
        }

        return samples;
    }

    byte[] FloatToPCM16(float[] samples)
    {
        short[] intData = new short[samples.Length];
        byte[] bytes = new byte[samples.Length * 2];

        for (int i = 0; i < samples.Length; i++)
        {
            intData[i] = (short)(samples[i] * short.MaxValue);
            byte[] byteArr = BitConverter.GetBytes(intData[i]);
            bytes[i * 2] = byteArr[0];
            bytes[i * 2 + 1] = byteArr[1];
        }

        return bytes;
    }

    // ���� ����
    byte[] AddWavHeader(byte[] pcmData, int sampleRate, int channels)
    {
        int bitsPerSample = 16;
        int byteRate = sampleRate * channels * bitsPerSample / 8;
        int blockAlign = channels * bitsPerSample / 8;
        int subchunk2Size = pcmData.Length;
        int chunkSize = 36 + subchunk2Size;

        using (var memoryStream = new System.IO.MemoryStream())
        using (var writer = new System.IO.BinaryWriter(memoryStream))
        {
            // RIFF Header
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(chunkSize);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));

            // fmt Subchunk
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16); // Subchunk1 Size (PCM)
            writer.Write((short)1); // Audio Format (1 = PCM)
            writer.Write((short)channels); // Num Channels
            writer.Write(sampleRate); // Sample Rate
            writer.Write(byteRate); // Byte Rate
            writer.Write((short)blockAlign); // Block Align
            writer.Write((short)bitsPerSample); // BitsPerSample

            // data Subchunk
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(subchunk2Size);
            writer.Write(pcmData);

            return memoryStream.ToArray();
        }
    }


    void SaveWavFile(float[] samples, int sampleRate, string filePath)
    {
        byte[] pcm = FloatToPCM16(samples);
        byte[] wav = AddWavHeader(pcm, sampleRate, 1); // Mono

        System.IO.File.WriteAllBytes(filePath, wav);
    }

    void SaveLatestAudioToWav()
    {
        if (micClip == null) return;

        int sampleLength = sampleRate * 10; // 3�� �з�
        float[] samples = new float[sampleLength];

        int micPos = Microphone.GetPosition(micDevice);
        int startPos = micPos - sampleLength;
        if (startPos < 0) startPos = 0;

        micClip.GetData(samples, startPos);

        string path = Application.persistentDataPath + "/test_record.wav";
        SaveWavFile(samples, sampleRate, path);
        Debug.Log("WAV ���� �����: " + path);
    }



}
