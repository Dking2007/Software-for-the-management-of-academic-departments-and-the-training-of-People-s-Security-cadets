// Dịch toàn trang phía trình duyệt theo từ điển nhãn giao diện (Việt → Anh / Lào).
// Chỉ thay chữ của nút văn bản (nodeValue) và các thuộc tính placeholder/title/aria-label; nguyên văn được nhớ để khôi phục khi về tiếng Việt.
// Không dịch vùng có translate="no", mã bản ghi, ô nhập liệu. Từ đơn chỉ dịch khi đứng riêng (tránh dịch nhầm tên người như "Khoa").
window.dichTrang = (() => {
    let tuDien = null, ngon = "vi", cumTu = [];
    const goc = new WeakMap(), gocThuocTinh = new WeakMap();
    const THUOC_TINH = ["placeholder", "title", "aria-label"];
    const CHU_VIET = /[àáảãạăằắẳẵặâầấẩẫậèéẻẽẹêềếểễệìíỉĩịòóỏõọôồốổỗộơờớởỡợùúủũụưừứửữựỳýỷỹỵđ]/i;
    const BO_QUA = "script,style,textarea,input,code,[translate='no'],.record-code,[contenteditable]";

    function dichChu(chu) {
        const t = chu.trim();
        if (!t || !tuDien) return null;
        if (tuDien[t]) return chu.replace(t, tuDien[t]);
        if (t.length > 160) return null;
        let kq = t, doi = false;
        for (const [vi, khac] of cumTu) {
            if (kq.includes(vi)) { kq = kq.split(vi).join(khac); doi = true; }
        }
        // Câu chỉ dịch được một phần (còn chữ tiếng Việt có dấu) thì giữ nguyên tiếng Việt, tránh câu nửa nọ nửa kia.
        if (!doi || CHU_VIET.test(kq)) return null;
        return chu.replace(t, kq);
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
            if (!nut.closest || !nut.closest("[translate='no']")) {
                let luu = gocThuocTinh.get(nut);
                for (const tt of THUOC_TINH) {
                    if (!nut.hasAttribute || !nut.hasAttribute(tt)) continue;
                    const hienTai = nut.getAttribute(tt);
                    luu = luu || {};
                    const nguyen = luu[tt] && luu[tt].dich === hienTai ? luu[tt].goc : hienTai;
                    if (ngon === "vi") { if (luu[tt] && luu[tt].dich === hienTai) nut.setAttribute(tt, luu[tt].goc); delete luu[tt]; continue; }
                    const moi = dichChu(nguyen);
                    if (moi && moi !== hienTai) { luu[tt] = { goc: nguyen, dich: moi }; nut.setAttribute(tt, moi); }
                }
                if (luu) gocThuocTinh.set(nut, luu);
            }
            for (const con of nut.childNodes) xuLyNut(con);
        }
    }
    let dangXuLy = false;
    const theoDoi = new MutationObserver(ds => {
        if (dangXuLy || ngon === "vi") return;
        dangXuLy = true;
        try { for (const m of ds) { if (m.type === "characterData") xuLyNut(m.target); else m.addedNodes.forEach(xuLyNut); } }
        finally { dangXuLy = false; }
    });
    async function dat(ngonMoi) {
        ngon = ngonMoi === "en" || ngonMoi === "lo" ? ngonMoi : "vi";
        if (ngon !== "vi") {
            try { tuDien = await (await fetch(`/tu-dien/${ngon}`, { cache: "force-cache" })).json(); } catch { tuDien = {}; }
            // Cụm nhiều từ dùng để thay một phần chuỗi; từ đơn chỉ khớp nguyên văn.
            cumTu = Object.entries(tuDien).filter(([vi]) => vi.includes(" ") && vi.length >= 6).sort((a, b) => b[0].length - a[0].length);
        }
        dangXuLy = true;
        try { xuLyNut(document.body); } finally { dangXuLy = false; }
        theoDoi.disconnect();
        if (ngon !== "vi") theoDoi.observe(document.body, { childList: true, subtree: true, characterData: true });
    }
    // Áp dụng ngôn ngữ đã lưu ngay khi trang tải.
    document.addEventListener("DOMContentLoaded", () => { const d = window.tuyChonGiaoDien?.doc(); if (d && d.ngonNgu !== "vi") dat(d.ngonNgu); });
    return { dat };
})();
