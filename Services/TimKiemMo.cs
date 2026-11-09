using System.Text;

namespace Nhom2_BTL.Services;

/// <summary>Mức khớp của một lần tìm: đủ mọi từ, đủ nhưng phải dò gần đúng, hay chỉ khớp một phần từ khoá.</summary>
public enum MucKhop { ChinhXac, GanDung, MotPhan }

/// <summary>
/// Tìm kiếm "không cần theo mẫu": người dùng gõ tuỳ ý ("duy-b10", "b10 duy", "duy,b10", "duyb10", "nguyn van duy").
/// Từ khoá được tách thành các từ theo mọi dấu phân cách; bản ghi khớp khi MỖI từ xuất hiện ở MỘT trong các trường, không kể thứ tự.
/// Không phân biệt hoa thường và dấu. Chỉ so trên các trường trang truyền vào, mà các trường đó lấy từ mặt cắt đã lọc quyền.
/// Ba mức, dừng ở mức đầu tiên có kết quả:
/// 1. Chính xác: đầu từ, trọn từ, mã lớp ("b10" khớp B10D58, số "1" không khớp 10).
/// 2. Gần đúng: gõ sai một, hai ký tự; viết liền; chữ cái đầu của họ tên ("nvd").
/// 3. Một phần: chỉ dùng khi được phép (Ctrl+K, sơ đồ), lấy các bản ghi khớp nhiều từ quan trọng nhất.
/// </summary>
public static class TimKiemMo
{
    // Từ quá chung: ở mức "một phần" không được dùng làm căn cứ, nếu không "tổ 1 châu" sẽ trả về mọi tổ.
    private static readonly HashSet<string> TuChung = new(StringComparer.Ordinal)
        { "to", "lop", "khoa", "hoc", "vien", "phong", "can", "bo", "giang", "chuyen", "nganh", "de", "ma", "he", "cap", "dai", "ta", "uy", "thuong", "trung", "thieu" };

    /// <summary>Chữ thường, bỏ dấu; mọi ký tự không phải chữ hoặc số thành một dấu cách.</summary>
    public static string ChuanHoa(string? s)
    {
        var bo = DichVuTimKiem.BoDau(s);
        if (bo.Length == 0) return "";
        var sb = new StringBuilder(bo.Length);
        var vuaCach = true;
        foreach (var c in bo)
        {
            if (char.IsLetterOrDigit(c)) { sb.Append(c); vuaCach = false; }
            else if (!vuaCach) { sb.Append(' '); vuaCach = true; }
        }
        return sb.ToString().TrimEnd();
    }

    public static string[] TachTu(string? tuKhoa)
        => ChuanHoa(tuKhoa).Split(' ', StringSplitOptions.RemoveEmptyEntries).Distinct().ToArray();

    /// <summary>Lọc và xếp theo độ khớp (mức 1, rồi mức 2). Từ khoá rỗng thì giữ nguyên danh sách và thứ tự.</summary>
    public static List<T> Loc<T>(IEnumerable<T> nguon, string? tuKhoa, Func<T, string?[]> truong)
        => LocChiTiet(nguon, tuKhoa, truong, choPhepMotPhan: false).Ds;

    /// <summary>
    /// Lọc danh sách người (trường đầu là họ tên). Gõ đúng một từ mà có người mang đúng tên đó thì chỉ trả những người ấy:
    /// "anh" ra người tên Anh, không lẫn "Anh Thư". Gõ từ hai từ trở lên thì tìm tự do như <see cref="Loc{T}"/>.
    /// </summary>
    public static List<T> LocNguoi<T>(IEnumerable<T> nguon, string? tuKhoa, Func<T, string?[]> truong)
    {
        var ketQua = Loc(nguon, tuKhoa, truong);
        var tu = TachTu(tuKhoa);
        if (tu.Length != 1 || !tu[0].All(char.IsLetter)) return ketQua;
        var dungTen = ketQua.Where(x => TuCuoi(truong(x).FirstOrDefault()) == tu[0]).ToList();
        return dungTen.Count > 0 ? dungTen : ketQua;
    }

    private static string TuCuoi(string? ten)
    {
        var chu = ChuanHoa(ten);
        return chu[(chu.LastIndexOf(' ') + 1)..];
    }

    /// <summary>Như <see cref="Loc{T}"/> nhưng cho biết mức khớp, và có thể lùi xuống mức "một phần".</summary>
    public static (List<T> Ds, MucKhop Muc) LocChiTiet<T>(IEnumerable<T> nguon, string? tuKhoa, Func<T, string?[]> truong, bool choPhepMotPhan = true)
    {
        var tu = TachTu(tuKhoa);
        if (tu.Length == 0) return (nguon.ToList(), MucKhop.ChinhXac);
        var cum = ChuanHoa(tuKhoa);
        var chiMuc = nguon.Select((x, i) => (Muc: x, ThuTu: i, Chu: truong(x).Select(ChuanHoa).ToArray())).ToList();

        var muc1 = chiMuc.Select(x => (x.Muc, x.ThuTu, Diem: ChamChinhXac(tu, cum, x.Chu))).Where(x => x.Diem > 0).ToList();
        if (muc1.Count > 0) return (muc1.OrderByDescending(x => x.Diem).ThenBy(x => x.ThuTu).Select(x => x.Muc).ToList(), MucKhop.ChinhXac);

        var muc2 = chiMuc.Select(x => (x.Muc, x.ThuTu, Diem: ChamGanDung(tu, x.Chu))).Where(x => x.Diem > 0).ToList();
        if (muc2.Count > 0) return (muc2.OrderByDescending(x => x.Diem).ThenBy(x => x.ThuTu).Select(x => x.Muc).ToList(), MucKhop.GanDung);

        if (!choPhepMotPhan) return ([], MucKhop.ChinhXac);
        var tuManh = tu.Where(LaTuManh).ToArray();
        if (tuManh.Length == 0) return ([], MucKhop.ChinhXac);
        var muc3 = chiMuc.Select(x => (x.Muc, x.ThuTu, So: tuManh.Count(t => CoTu(t, x.Chu)))).Where(x => x.So > 0).ToList();
        if (muc3.Count == 0) return ([], MucKhop.ChinhXac);
        var nhieuNhat = muc3.Max(x => x.So);
        return (muc3.Where(x => x.So == nhieuNhat).OrderBy(x => x.ThuTu).Select(x => x.Muc).ToList(), MucKhop.MotPhan);
    }

    /// <summary>Kiểm tra một bản ghi (mức 1 hoặc 2). Dùng cho danh sách nhỏ không cần xếp hạng.</summary>
    public static bool Khop(string? tuKhoa, params string?[] truong)
    {
        var tu = TachTu(tuKhoa);
        if (tu.Length == 0) return true;
        var chu = truong.Select(ChuanHoa).ToArray();
        return ChamChinhXac(tu, ChuanHoa(tuKhoa), chu) > 0 || ChamGanDung(tu, chu) > 0;
    }

    /// <summary>Các đoạn của <paramref name="ten"/> cần tô sáng, không chồng nhau, theo vị trí tăng dần.</summary>
    public static List<(int Dau, int Dai)> VungKhop(string? ten, string? tuKhoa)
    {
        var kq = new List<(int Dau, int Dai)>();
        if (string.IsNullOrEmpty(ten)) return kq;
        // Bản chuẩn hoá giữ nguyên độ dài để vị trí khớp trùng vị trí trên chữ gốc có dấu.
        var chu = string.Concat(ten.Select(c =>
        {
            var b = DichVuTimKiem.BoDau(c.ToString());
            return b.Length == 1 && char.IsLetterOrDigit(b[0]) ? b[0] : ' ';
        }));
        foreach (var t in TachTu(tuKhoa))
            for (var i = chu.IndexOf(t, StringComparison.Ordinal); i >= 0; i = chu.IndexOf(t, i + 1, StringComparison.Ordinal))
                if (DiemTaiViTri(t, chu, i) > 0) kq.Add((i, t.Length));
        kq.Sort();
        var gop = new List<(int Dau, int Dai)>();
        foreach (var v in kq)
        {
            if (gop.Count > 0 && v.Dau <= gop[^1].Dau + gop[^1].Dai)
                gop[^1] = (gop[^1].Dau, Math.Max(gop[^1].Dai, v.Dau + v.Dai - gop[^1].Dau));
            else gop.Add(v);
        }
        return gop;
    }

    private static bool LaTuManh(string t) => t.Any(char.IsDigit) ? t.Length >= 2 : t.Length >= 3 && !TuChung.Contains(t);

    /// <summary>Tổng điểm khi MỌI từ đều khớp, 0 nếu thiếu một từ. Khớp ở trường đầu (thường là tên) được ưu tiên.</summary>
    private static int ChamChinhXac(string[] tu, string cum, string[] chu)
    {
        var tong = 0;
        foreach (var t in tu)
        {
            var tot = 0;
            for (var i = 0; i < chu.Length; i++)
            {
                var d = DiemTrongChu(t, chu[i]);
                if (d == 0) continue;
                if (i == 0)
                {
                    d += 10;
                    if (chu[0] == t || chu[0].EndsWith(" " + t, StringComparison.Ordinal)) d += 15; // tên riêng của người Việt nằm cuối họ tên
                }
                else d = d * 85 / 100;
                tot = Math.Max(tot, d);
            }
            if (tot == 0) return 0;
            tong += tot;
        }
        if (chu.Length > 0 && tu.Length > 1 && chu[0].Contains(cum, StringComparison.Ordinal)) tong += 50;
        if (chu.Length > 0 && chu[0] == cum) tong += 100;
        return tong;
    }

    private static int DiemTrongChu(string t, string chu)
    {
        var tot = 0;
        for (var i = chu.IndexOf(t, StringComparison.Ordinal); i >= 0; i = chu.IndexOf(t, i + 1, StringComparison.Ordinal))
            tot = Math.Max(tot, DiemTaiViTri(t, chu, i));
        return tot;
    }

    private static int DiemTaiViTri(string t, string chu, int i)
    {
        var dauTu = i == 0 || chu[i - 1] == ' ';
        var cuoi = i + t.Length;
        var hetTu = cuoi == chu.Length || chu[cuoi] == ' ';
        if (dauTu && hetTu) return 100;
        var truocLaSo = i > 0 && char.IsDigit(chu[i - 1]);
        var sauLaSo = cuoi < chu.Length && char.IsDigit(chu[cuoi]);
        if (t.All(char.IsDigit))
        {
            // "1" không được khớp 10 hay D51; từ 3 chữ số trở lên mới dò giữa dãy (một phần số hiệu, điện thoại).
            if (truocLaSo || sauLaSo) return t.Length >= 3 ? 25 : 0;
            return 60;
        }
        if (t.Any(char.IsDigit))
        {
            // Mã như "b1": xếp B1D58 trước B10D58, nhưng vẫn giữ cả hai.
            if (sauLaSo && char.IsDigit(t[^1])) return dauTu ? 45 : 20;
            return dauTu ? 80 : 35;
        }
        if (!dauTu) return 0; // chữ thuần chỉ khớp đầu từ: "anh" không khớp "Thanh" ở mức 1
        return t.Length == 1 ? 30 : 70;
    }

    private static int ChamGanDung(string[] tu, string[] chu)
    {
        var tong = 0;
        foreach (var t in tu)
        {
            var d = DiemGanDungMotTu(t, chu);
            if (d == 0) return 0;
            tong += d;
        }
        return tong;
    }

    private static bool CoTu(string t, string[] chu) => chu.Any(c => DiemTrongChu(t, c) > 0) || DiemGanDungMotTu(t, chu) > 0;

    private static int DiemGanDungMotTu(string t, string[] chu)
    {
        var tot = 0;
        foreach (var c in chu) tot = Math.Max(tot, DiemTrongChu(t, c));
        if (tot > 0) return tot;
        var ghep = string.Join(' ', chu.Where(c => c != ""));
        var chiChu = t.All(char.IsLetter);
        if (chiChu && t.Length >= 3 && ghep.Contains(t, StringComparison.Ordinal)) return 16;
        if (GhepDuoc(t, ghep, 2)) return 15;
        if (chiChu && t.Length is >= 2 and <= 5 && chu.Length > 0)
        {
            var dau = string.Concat(chu[0].Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(w => w[0]));
            if (dau == t) return 14;
        }
        if (chiChu && t.Length >= 4)
        {
            var choPhep = t.Length >= 8 ? 2 : 1;
            foreach (var w in ghep.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                if (w.Length >= 3 && w.All(char.IsLetter) && Math.Abs(w.Length - t.Length) <= choPhep && KhoangCach(t, w, choPhep) <= choPhep) return 10;
        }
        return 0;
    }

    /// <summary>Từ viết liền: "duyb10" tách được thành "duy" + "b10", mỗi phần có trong bản ghi.</summary>
    private static bool GhepDuoc(string t, string ghep, int soLanCat)
    {
        if (t.Length < 4) return false;
        for (var i = 2; i <= t.Length - 2; i++)
        {
            if (DiemTrongChu(t[..i], ghep) < 30) continue;
            var phanSau = t[i..];
            if (DiemTrongChu(phanSau, ghep) >= 30) return true;
            if (soLanCat > 1 && GhepDuoc(phanSau, ghep, soLanCat - 1)) return true;
        }
        return false;
    }

    /// <summary>Khoảng cách chỉnh sửa Levenshtein, dừng sớm khi đã vượt ngưỡng.</summary>
    private static int KhoangCach(string a, string b, int nguong)
    {
        var truoc = new int[b.Length + 1];
        var nay = new int[b.Length + 1];
        for (var j = 0; j <= b.Length; j++) truoc[j] = j;
        for (var i = 1; i <= a.Length; i++)
        {
            nay[0] = i;
            var nhoNhat = nay[0];
            for (var j = 1; j <= b.Length; j++)
            {
                nay[j] = Math.Min(Math.Min(nay[j - 1] + 1, truoc[j] + 1), truoc[j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
                nhoNhat = Math.Min(nhoNhat, nay[j]);
            }
            if (nhoNhat > nguong) return nguong + 1;
            (truoc, nay) = (nay, truoc);
        }
        return truoc[b.Length];
    }
}
