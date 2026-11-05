// Chỉ lưu cách hiển thị; không lưu tài khoản hay vai trò trong trình duyệt.
window.tuyChonGiaoDien = (() => {
    function doc() {
        try {
            const duLieu = JSON.parse(localStorage.getItem("tuy-chon-giao-dien") || "{}");
            return { giaoDien: duLieu?.giaoDien === "toi" ? "toi" : "sang", ngonNgu: duLieu?.ngonNgu === "en" ? "en" : "vi" };
        } catch { return { giaoDien: "sang", ngonNgu: "vi" }; }
    }
    function apDung(giaoDien, ngonNgu) {
        document.documentElement.dataset.giaoDien = giaoDien === "toi" ? "toi" : "sang";
        const theme = document.getElementById("chu-de-toi");
        if (theme) theme.media = giaoDien === "toi" ? "all" : "not all";
        document.documentElement.lang = ngonNgu === "en" ? "en" : "vi";
    }
    const cu = doc(); apDung(cu.giaoDien, cu.ngonNgu);
    return {
        doc,
        luu(giaoDien, ngonNgu) {
            apDung(giaoDien, ngonNgu);
            try { localStorage.setItem("tuy-chon-giao-dien", JSON.stringify({ giaoDien, ngonNgu })); } catch { /* Vẫn áp dụng nếu trình duyệt chặn lưu. */ }
        }
    };
})();
