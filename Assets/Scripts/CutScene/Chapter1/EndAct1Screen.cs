using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class EndAct1Screen : MonoBehaviour
{
    [Tooltip("Thời gian chờ trước khi tự động chuyển scene (giây)")]
    public float waitTime = 4f;
    
    [Tooltip("Scene tiếp theo sẽ chuyển tới (mặc định là menu chính)")]
    public string nextScene = "Chapter0_Login"; 

    void Start()
    {
        // Khi scene bắt đầu, chạy bộ đếm thời gian
        StartCoroutine(DoiVaChuyenScene());
    }

    IEnumerator DoiVaChuyenScene()
    {
        // Đợi một khoảng thời gian
        yield return new WaitForSeconds(waitTime);
        
        // Chuyển sang scene tiếp theo
        SceneManager.LoadScene(nextScene);
    }
}
