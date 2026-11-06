namespace Nhom2_BTL.Services;

// Chỉ là tùy chọn hiển thị của từng phiên, hoàn toàn không tham gia phân quyền.
public sealed class TuyChonGiaoDien
{
    public string GiaoDien { get; private set; } = "sang";
    public string NgonNgu { get; private set; } = "vi";
    public event Action? DaThayDoi;
    public void CapNhat(string? giaoDien, string? ngonNgu)
    {
        var mau = giaoDien == "toi" ? "toi" : "sang";
        var tieng = ngonNgu is "en" or "lo" ? ngonNgu : "vi";
        if (GiaoDien == mau && NgonNgu == tieng) return;
        GiaoDien = mau; NgonNgu = tieng; DaThayDoi?.Invoke();
    }
    /// <summary>Dịch nhãn theo từ điển chung Việt – Anh – Lào (TuDienGiaoDien); không có trong từ điển thì giữ nguyên.</summary>
    public string Dich(string chu) => TuDienGiaoDien.Dich(NgonNgu, chu);
}
