using UnityEngine;
using System.Collections;

public class GiaNoCutscene : MonoBehaviour
{
    public Transform diemBaoTin;
    public Transform diemCuaNha;
    public Transform diemCanhGiuong;
    public float tocDo = 4f;

    [Tooltip("Kéo Canvas_BongBong thả vào đây nè anh")]
    public GameObject khungThoaiUI;

    [Tooltip("Kéo TranQuocTuan thả vào đây nha anh")]
    public PlayerMovement playerScript;

    private Animator anim;

    void Start()
    {
        anim = GetComponent<Animator>();
        transform.position = diemCuaNha.position;

        // Giấu bong bóng đi lúc mới vào game
        if (khungThoaiUI != null) khungThoaiUI.SetActive(false);

        StartCoroutine(DienCangBaoTin());
    }

    IEnumerator DienCangBaoTin()
    {
        // --- NHỊP 1: MỚI VÀO GAME LÀ QUAY PHẢI NGẮM CẢNH ---
        if (playerScript != null)
        {
            playerScript.canMove = false; // Khóa chân

            Animator playerAnim = playerScript.GetComponent<Animator>();
            if (playerAnim != null)
            {
                playerAnim.SetFloat("MoveX", 1f); // Ép quay PHẢI
                playerAnim.SetFloat("MoveY", 0f);
            }
        }

        // 1. Gia nô bắt đầu chạy từ cửa ra giữa sân
        anim.Play("GiaNo_WalkDown");
        while (Vector3.Distance(transform.position, diemBaoTin.position) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, diemBaoTin.position, (tocDo * 2f) * Time.deltaTime);
            yield return null;
        }

        // 2. Gia nô tới nơi, đứng im
        anim.Play("GiaNo_IdleDown");

        // --- NHỊP 2: GIA NÔ TỚI NƠI -> QUỐC TUẤN QUAY PHẮT SANG TRÁI ĐỂ NGHE ---
        if (playerScript != null)
        {
            Animator playerAnim = playerScript.GetComponent<Animator>();
            if (playerAnim != null)
            {
                playerAnim.SetFloat("MoveX", -1f); // Ép quay TRÁI
                playerAnim.SetFloat("MoveY", 0f);
            }
        }

        // Bật bong bóng thoại lên
        if (khungThoaiUI != null) khungThoaiUI.SetActive(true);

        // Chờ 5 giây để máy đánh chữ gõ hết 2 câu thoại
        yield return new WaitForSeconds(5f);

        // Đọc xong thì TẮT BONG BÓNG ĐI
        if (khungThoaiUI != null) khungThoaiUI.SetActive(false);

        // 3. GIẢI HUYỆT: Chữ vừa tắt là mở khóa cho thiếu gia phi theo ngay!
        if (playerScript != null) playerScript.canMove = true;

        // 3. Gia nô quay đầu chạy vội vào nhà
        anim.Play("GiaNo_WalkUp");
        while (Vector3.Distance(transform.position, diemCuaNha.position) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, diemCuaNha.position, (tocDo * 2f) * Time.deltaTime);
            yield return null;
        }

        // 4. Gia nô dịch chuyển vào cạnh giường
        transform.position = diemCanhGiuong.position;
        anim.Play("GiaNo_IdleDown");
    }
}