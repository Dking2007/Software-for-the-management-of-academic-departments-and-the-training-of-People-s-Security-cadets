// Tuỳ chọn hiển thị của từng người dùng trên trình duyệt này (localStorage). Không lưu tài khoản hay vai trò.
// Áp dụng bằng thuộc tính data-* và cỡ chữ gốc trên <html>; CSS đọc các thuộc tính này (wwwroot/tich-hop.css).
window.tuyChonGiaoDien = (() => {
    const KHOA = "tuy-chon-giao-dien";
    const MAC_DINH = {
        giaoDien: "sang", ngonNgu: "vi", mau: "xanh-la", coChu: "16", matDo: "vua", doRong: "day",
        phong: "segoe", boGoc: "vua", tuongPhan: false, giamChuyenDong: false, gachChanLienKet: false
    };
    const HOP_LE = {
        giaoDien: ["sang", "toi", "he-thong"], ngonNgu: ["vi", "en", "lo"],
        mau: ["xanh-la", "xanh-duong", "do-do", "tim", "nau", "xam"],
        coChu: ["14", "15", "16", "17", "18", "20", "tu-dong"], matDo: ["gon", "vua", "thoang"], doRong: ["day", "vua"],
        phong: ["segoe", "he-thong", "de-doc", "serif"], boGoc: ["vuong", "vua", "tron"]
    };
    function chuan(d) {
        const kq = { ...MAC_DINH };
        for (const k of Object.keys(MAC_DINH)) {
            const v = d?.[k];
            if (typeof MAC_DINH[k] === "boolean") kq[k] = v === true;
            else if (v !== undefined && HOP_LE[k].includes(String(v))) kq[k] = String(v);
        }
        return kq;
    }
    function doc() {
        try { return chuan(JSON.parse(localStorage.getItem(KHOA) || "{}")); } catch { return { ...MAC_DINH }; }
    }
    const heThongToi = window.matchMedia ? window.matchMedia("(prefers-color-scheme: dark)") : null;
    let hienTai = doc();
    function giaoDienThuc(g) { return g === "he-thong" ? (heThongToi?.matches ? "toi" : "sang") : g; }
    function apDung(t) {
        const r = document.documentElement;
        const gd = giaoDienThuc(t.giaoDien);
        r.dataset.giaoDien = gd;
        const theme = document.getElementById("chu-de-toi");
        if (theme) theme.media = gd === "toi" ? "all" : "not all";
        r.lang = ["en", "lo"].includes(t.ngonNgu) ? t.ngonNgu : "vi";
        r.dataset.mau = t.mau; r.dataset.matDo = t.matDo; r.dataset.doRong = t.doRong; r.dataset.phong = t.phong; r.dataset.boGoc = t.boGoc;
        r.dataset.coChu = t.coChu;
        r.style.fontSize = t.coChu === "tu-dong" ? "" : t.coChu + "px";
        r.toggleAttribute("data-tuong-phan", t.tuongPhan);
        r.toggleAttribute("data-giam-chuyen-dong", t.giamChuyenDong);
        r.toggleAttribute("data-gach-chan", t.gachChanLienKet);
    }
    heThongToi?.addEventListener?.("change", () => { if (hienTai.giaoDien === "he-thong") apDung(hienTai); });
    apDung(hienTai);
    return {
        doc,
        /// Tương thích cũ: chỉ đổi giao diện sáng/tối và ngôn ngữ.
        luu(giaoDien, ngonNgu) { this.luuDayDu({ ...hienTai, giaoDien, ngonNgu }); },
        luuDayDu(t) {
            hienTai = chuan(t);
            apDung(hienTai);
            try { localStorage.setItem(KHOA, JSON.stringify(hienTai)); } catch { /* Vẫn áp dụng nếu trình duyệt chặn lưu. */ }
            return giaoDienThuc(hienTai.giaoDien);
        },
        /// Nút Sáng/Tối trên thanh trên: chỉ đổi giao diện, giữ nguyên mọi tuỳ chọn khác.
        datGiaoDien(g) { return this.luuDayDu({ ...hienTai, giaoDien: g }); },
        macDinh() { return { ...MAC_DINH }; },
        giaoDienThuc() { return giaoDienThuc(hienTai.giaoDien); }
    };
})();
