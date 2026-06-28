using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Lớp lo việc PERSIST dữ liệu save: PlayerPrefs (local) + backup lên server (HTTP).
/// Tách khỏi GameManager để GameManager chỉ còn lo "save cái gì" (dựng PlayerSave từ
/// trạng thái gameplay), còn "lưu thế nào" nằm ở đây. Stateless → dễ test phần local.
/// </summary>
public static class SaveService
{
    /// <summary>Key PlayerPrefs cho từng slot.</summary>
    public static string LocalKey(int slot) => "PlayerSave_" + slot;

    // ─── Local (PlayerPrefs) ─────────────────────────────────────────────

    public static void SaveLocal(int slot, PlayerSave save)
    {
        PlayerPrefs.SetString(LocalKey(slot), JsonUtility.ToJson(save));
        PlayerPrefs.Save();
    }

    public static bool HasLocal(int slot) => PlayerPrefs.HasKey(LocalKey(slot));

    public static bool TryLoadLocal(int slot, out string json)
    {
        if (PlayerPrefs.HasKey(LocalKey(slot)))
        {
            json = PlayerPrefs.GetString(LocalKey(slot));
            return true;
        }
        json = null;
        return false;
    }

    public static void DeleteLocal(int slot)
    {
        PlayerPrefs.DeleteKey(LocalKey(slot));
        PlayerPrefs.Save();
    }

    // ─── Server (HTTP) ───────────────────────────────────────────────────

    /// <summary>Coroutine: POST save lên server làm backup. onComplete(true) nếu thành công.</summary>
    public static IEnumerator BackupToServer(string url, PlayerSave save, Action<bool> onComplete = null)
    {
        byte[] body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(save));

        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            bool ok = req.result == UnityWebRequest.Result.Success;
            if (ok)
                Debug.Log("[SaveService] Backup lên máy chủ thành công.");
            else
                Debug.LogWarning("[SaveService] Không chạm tới máy chủ — game vẫn lưu an toàn ở local. Lỗi: " + req.error);

            onComplete?.Invoke(ok);
        }
    }

    /// <summary>Coroutine: GET save từ server theo url. onResult(json) hoặc onResult(null) nếu thất bại.</summary>
    public static IEnumerator LoadFromServer(string url, Action<string> onResult)
    {
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();
            onResult?.Invoke(req.result == UnityWebRequest.Result.Success ? req.downloadHandler.text : null);
        }
    }
}
