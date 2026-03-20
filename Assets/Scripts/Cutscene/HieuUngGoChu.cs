using UnityEngine;
using TMPro;
using System.Collections;

public class HieuUngGoChu : MonoBehaviour
{
    public float tocDoGo = 0.05f;
    public float thoiGianDoiQuaCau = 1.2f; // Khựng lại 1.2s để anh đọc xong câu 1

    [Tooltip("Gõ các câu thoại vào đây")]
    [TextArea(2, 3)]
    public string[] cacCauThoai; // Chứa nhiều câu

    private TextMeshProUGUI textComponent;

    void Awake()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
    }

    void OnEnable()
    {
        StartCoroutine(ChayHieuUng());
    }

    IEnumerator ChayHieuUng()
    {
        foreach (string cauThoai in cacCauThoai)
        {
            textComponent.text = ""; // Xóa chữ cũ

            // Gõ từng chữ cái
            foreach (char chuCai in cauThoai.ToCharArray())
            {
                textComponent.text += chuCai;
                yield return new WaitForSeconds(tocDoGo);
            }

            // Gõ xong 1 câu, dừng lại chờ trước khi qua câu mới
            yield return new WaitForSeconds(thoiGianDoiQuaCau);
        }
    }
}