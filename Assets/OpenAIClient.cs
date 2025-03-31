using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

public class OpenAIClient : MonoBehaviour
{
    private string apiKey = "sk-xxxx..."; // 너의 OpenAI API 키
    private string openAiUrl = "https://api.openai.com/v1/chat/completions";

    public void RequestChat(int label)
    {
        string prompt = $"소리 라벨 {label}은 어떤 위험에 해당하는지 설명해줘.";
        StartCoroutine(SendRequest(prompt));
    }

    IEnumerator SendRequest(string prompt)
    {
        string jsonBody = @"{
            ""model"": ""gpt-3.5-turbo"",
            ""messages"": [{""role"":""user"",""content"":""" + prompt + @"""}],
            ""temperature"": 0.7
        }";

        var request = new UnityWebRequest(openAiUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("✅ GPT 응답: " + request.downloadHandler.text);
            // 여기서 응답 JSON을 파싱해서 메시지만 추출하면 됨
        }
        else
        {
            Debug.LogError("❌ 오류: " + request.error);
        }
    }
}
