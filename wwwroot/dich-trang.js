// Dịch toàn trang phía trình duyệt theo từ điển nhãn giao diện (Việt → Anh / Lào).
// Chỉ thay chữ của nút văn bản (nodeValue) và các thuộc tính placeholder/title/aria-label; nguyên văn được nhớ để khôi phục khi về tiếng Việt.
// Không dịch vùng có translate="no", mã bản ghi, ô nhập liệu. Từ đơn chỉ dịch khi đứng riêng (tránh dịch nhầm tên người như "Khoa").
// Cách tìm bản dịch cho một đoạn chữ, theo thứ tự:
//   1. khớp nguyên văn;
//   2. khớp khuôn có số: "20 lớp đang học" tìm mục "{0} lớp đang học" rồi điền lại số (số, ngày, giờ, mã như D58, B1D58);
//   3. bỏ dấu câu ở hai đầu ("Hôm nay:" → "Hôm nay") rồi thử lại 1–2;
//   4. tách theo dấu phân cách " · ", " | ", " › ", " – " và dịch từng phần;
//   5. thay các cụm nhiều từ có trong từ điển; chỉ nhận nếu không còn chữ tiếng Việt (tránh câu nửa nọ nửa kia).
window.dichTrang = (() => {
    let tuDien = null, ngon = "vi", cumTu = [];
    const goc = new WeakMap(), gocThuocTinh = new WeakMap();
    const THUOC_TINH = ["placeholder", "title", "aria-label"];
    const CHU_VIET = /[àáảãạăằắẳẵặâầấẩẫậèéẻẽẹêềếểễệìíỉĩịòóỏõọôồốổỗộơờớởỡợùúủũụưừứửữựỳýỷỹỵđ]/i;
    const BO_QUA = "script,style,textarea,input,code,[translate='no'],.record-code,[contenteditable],.rzi,.material-icons";
    // Số và mã: 20, 1.234, 09/11, 07:00–10:15, 2026–2030, D58, K6, B10D58, CD-01, T1–4.
    const SO = /(?:\b[A-Z]{1,3}\d+[A-Z]?\d*(?:-\d+)?\b|\d+(?:[.,:/–-]\d+)*)/g;
    const PHAN_CACH = /(\s+·\s+|\s+\|\s+|\s+›\s+)/;

    // Tên người (có thể kèm cấp bậc): giữ nguyên tên riêng, chỉ dịch cấp bậc. Ví dụ "Thượng tá Mai Quốc Khôi".
    const CAP_BAC = "(?:Đại|Thượng|Trung|Thiếu) (?:tướng|tá|úy|uý)";
    const MAU_TEN = "\\p{Lu}\\p{Ll}+(?: \\p{Lu}\\p{Ll}+){1,4}";
    const TEN = new RegExp("(?:" + CAP_BAC + " )?" + MAU_TEN, "gu");
    const LA_TEN = new RegExp("^(" + CAP_BAC + " )?" + MAU_TEN + "(?: \\([^)]{1,20}\\))?$", "u");
    function dichTen(ten) {
        const m = ten.match(new RegExp(`^(${CAP_BAC}) (.*)$`, "u"));
        return m && tuDien[m[1]] ? `${tuDien[m[1]]} ${m[2]}` : ten;
    }
    function dien(mau, so, ten) {
        return mau.replace(/\{ten(\d*)\}/g, (_, i) => dichTen(ten[+(i || 0)] ?? "")).replace(/\{(\d+)\}/g, (_, i) => so[+i] ?? "");
    }
    function theoKhuon(t) {
        if (tuDien[t]) return tuDien[t];
        const so = [];
        const khuon = t.replace(SO, m => { so.push(m); return `{${so.length - 1}}`; });
        if (so.length && tuDien[khuon]) return dien(tuDien[khuon], so, []);
        // Khuôn có tên người: "Trạng thái của {ten}", "{ten} ({0})". Chỉ dùng khi từ điển có đúng khuôn đó.
        const ten = [];
        const khuonTen = khuon.replace(TEN, m => { ten.push(m); return ten.length === 1 ? "{ten}" : `{ten${ten.length - 1}}`; });
        if (ten.length && tuDien[khuonTen]) return dien(tuDien[khuonTen], so, ten);
        return null;
    }
    function motDoan(t) {
        const kq = theoKhuon(t);
        if (kq) return kq;
        // Dấu câu, ký hiệu ở hai đầu: dịch phần lõi rồi gắn lại.
        const m = t.match(/^([\s(“"«‹•·–,-]*)(.*?)([\s:.,;…›»”")!?*·–✓]*)$/s);
        if (m && (m[1] || m[3]) && m[2]) { const loi = theoKhuon(m[2]); if (loi) return m[1] + loi + m[3]; }
        return null;
    }
    function thayCum(t) {
        let kq = t, doi = false;
        for (const [vi, khac] of cumTu) {
            if (kq.includes(vi)) { kq = kq.split(vi).join(khac); doi = true; }
        }
        return doi && !CHU_VIET.test(kq) ? kq : null;
    }
    function dichChu(chu) {
        const t = chu.trim();
        if (!t || !tuDien || !CHU_VIET.test(t)) return null;
        // Chữ nhiều dòng (nội dung văn bản): dịch từng dòng, dòng không dịch được giữ nguyên.
        if (t.includes("\n")) {
            if (tuDien[t]) return chu.replace(t, tuDien[t]);
            const dong = t.split("\n").map(d => (CHU_VIET.test(d) ? dichChu(d) : null) ?? d);
            const kq = dong.join("\n");
            return kq !== t ? chu.replace(t, kq) : null;
        }
        let kq = motDoan(t);
        if (!kq && PHAN_CACH.test(t)) {
            const phan = t.split(PHAN_CACH);
            const dich = phan.map((p, i) => i % 2 === 1 || !CHU_VIET.test(p) ? p
                : (motDoan(p.trim()) ?? thayCum(p.trim()) ?? (LA_TEN.test(p.trim()) ? dichTen(p.trim()) : null)));
            if (dich.every(p => p !== null)) kq = dich.join("");
        }
        if (!kq && t.length <= 200) kq = thayCum(t);
        // Chỉ là tên người (có thể kèm cấp bậc): giữ tên, dịch cấp bậc.
        if (!kq && LA_TEN.test(t)) { const ten = dichTen(t); if (ten !== t) kq = ten; }
        return kq ? chu.replace(t, kq) : null;
    }
    function xuLyNut(nut) {
        if (nut.nodeType === Node.TEXT_NODE) {
            const cha = nut.parentElement;
            if (!cha || cha.closest(BO_QUA)) return;
            const nguyen = goc.has(nut) && goc.get(nut).dich === nut.nodeValue ? goc.get(nut).goc : nut.nodeValue;
            const moi = ngon === "vi" ? null : dichChu(nguyen);
            if (moi && moi !== nut.nodeValue) { goc.set(nut, { goc: nguyen, dich: moi }); nut.nodeValue = moi; }
            else if (ngon === "vi" && goc.has(nut)) { if (goc.get(nut).dich === nut.nodeValue) nut.nodeValue = goc.get(nut).goc; goc.delete(nut); }
        } else if (nut.nodeType === Node.ELEMENT_NODE) {
            if (nut.matches && nut.matches("script,style")) return;
            xuLyThuocTinh(nut);
            for (const con of nut.childNodes) xuLyNut(con);
        }
    }
    function xuLyThuocTinh(nut) {
        if (!nut.hasAttribute || (nut.closest && nut.closest("[translate='no']"))) return;
        let luu = gocThuocTinh.get(nut);
        for (const tt of THUOC_TINH) {
            if (!nut.hasAttribute(tt)) continue;
            const hienTai = nut.getAttribute(tt);
            luu = luu || {};
            const nguyen = luu[tt] && luu[tt].dich === hienTai ? luu[tt].goc : hienTai;
            if (ngon === "vi") { if (luu[tt] && luu[tt].dich === hienTai) nut.setAttribute(tt, luu[tt].goc); delete luu[tt]; continue; }
            const moi = dichChu(nguyen);
            if (moi && moi !== hienTai) { luu[tt] = { goc: nguyen, dich: moi }; nut.setAttribute(tt, moi); }
        }
        if (luu) gocThuocTinh.set(nut, luu);
    }
    let dangXuLy = false;
    const theoDoi = new MutationObserver(ds => {
        if (dangXuLy || ngon === "vi") return;
        dangXuLy = true;
        try {
            for (const m of ds) {
                if (m.type === "characterData") xuLyNut(m.target);
                else if (m.type === "attributes") xuLyThuocTinh(m.target);
                else m.addedNodes.forEach(xuLyNut);
            }
        }
        finally { dangXuLy = false; }
    });
    async function dat(ngonMoi) {
        ngon = ngonMoi === "en" || ngonMoi === "lo" ? ngonMoi : "vi";
        if (ngon !== "vi") {
            // Phiên bản từ điển đổi mỗi khi bổ sung mục, để trình duyệt không giữ bản cũ.
            try { tuDien = await (await fetch(`/tu-dien/${ngon}?v=${window.phienBanTuDien || "1"}`, { cache: "no-cache" })).json(); } catch { tuDien = {}; }
            // Cụm nhiều từ dùng để thay một phần chuỗi; từ đơn chỉ khớp nguyên văn. Mục khuôn có {0} không dùng để thay cụm.
            cumTu = Object.entries(tuDien).filter(([vi]) => vi.includes(" ") && vi.length >= 6 && !vi.includes("{")).sort((a, b) => b[0].length - a[0].length);
        }
        dangXuLy = true;
        try { xuLyNut(document.body); } finally { dangXuLy = false; }
        theoDoi.disconnect();
        if (ngon !== "vi") theoDoi.observe(document.body, { childList: true, subtree: true, characterData: true, attributes: true, attributeFilter: THUOC_TINH });
    }
    // Áp dụng ngôn ngữ đã lưu ngay khi trang tải.
    document.addEventListener("DOMContentLoaded", () => { const d = window.tuyChonGiaoDien?.doc(); if (d && d.ngonNgu !== "vi") dat(d.ngonNgu); });
    return { dat, dichChu: chu => dichChu(chu) };
})();
