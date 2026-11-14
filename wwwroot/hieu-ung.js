/* Vai trò tệp: Hiệu ứng giao diện, tải tệp, nút về đầu trang và phím tắt tìm kiếm. */
// Hiệu ứng và phím tắt phía trình duyệt. Không chứa dữ liệu hay quyền; tắt hết khi người dùng chọn giảm chuyển động.
(() => {
    const giamChuyenDong = () => window.matchMedia("(prefers-reduced-motion: reduce)").matches;

    // Tải tệp do máy chủ tạo (Excel): nhận base64, tạo liên kết tạm rồi bấm hộ.
    window.taiTep = (tenTep, mime, base64) => {
        const nhiPhan = Uint8Array.from(atob(base64), c => c.charCodeAt(0));
        const url = URL.createObjectURL(new Blob([nhiPhan], { type: mime }));
        const a = document.createElement("a");
        a.href = url; a.download = tenTep; document.body.appendChild(a); a.click(); a.remove();
        setTimeout(() => URL.revokeObjectURL(url), 2000);
    };

    // Sơ đồ dạng cột: cuộn tới cột mới nhất sau khi chọn một mục.
    window.cuonCotCuoi = el => el?.scrollTo?.({ left: el.scrollWidth, behavior: giamChuyenDong() ? "auto" : "smooth" });

    // Nút quay về đầu trang: hiện sau khi cuộn xuống 320 px.
    const CHIEU_CAO_TOI_THIEU = 320;
    function capNhatNutLenDau() {
        const nut = document.getElementById("nut-len-dau");
        if (!nut) return;
        nut.classList.toggle("hien", window.scrollY >= CHIEU_CAO_TOI_THIEU);
    }
    let choKhungHinh = false;
    const henCapNhat = () => { if (choKhungHinh) return; choKhungHinh = true; requestAnimationFrame(() => { choKhungHinh = false; capNhatNutLenDau(); }); };
    window.addEventListener("scroll", henCapNhat, { passive: true });
    window.addEventListener("resize", henCapNhat);
    new ResizeObserver(henCapNhat).observe(document.documentElement);
    document.addEventListener("click", e => {
        if (!e.target.closest?.("#nut-len-dau")) return;
        window.scrollTo({ top: 0, behavior: giamChuyenDong() ? "auto" : "smooth" });
        document.querySelector(".content h1")?.setAttribute("tabindex", "-1");
        document.querySelector(".content h1")?.focus({ preventScroll: true });
    });

    // Ctrl+K / Cmd+K: mở tìm kiếm nhanh bằng cách bấm hộ nút trên thanh trên, rồi đưa con trỏ vào ô tìm.
    document.addEventListener("keydown", e => {
        if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === "k") {
            const nut = document.querySelector("[data-mo-tim-kiem]");
            if (!nut) return;
            e.preventDefault();
            nut.click();
        }
    });
    // Khi hộp tìm kiếm xuất hiện thì focus ô nhập.
    new MutationObserver(() => {
        const o = document.querySelector("[data-o-tim-nhanh]");
        if (o && !o.dataset.daFocus) { o.dataset.daFocus = "1"; o.focus(); }
    }).observe(document.documentElement, { childList: true, subtree: true });

    // Số trên thẻ số liệu chạy lên khi vào trang. Giá trị thật luôn nằm sẵn trong DOM; hiệu ứng chỉ đổi chữ hiển thị tạm thời.
    const dinhDang = new Intl.NumberFormat("vi-VN");
    function chaySo(el) {
        if (el.dataset.daChay || giamChuyenDong()) return;
        // Chỉ sửa nodeValue của đúng nút chữ Blazor đã dựng (không thay nút) để Blazor vẫn cập nhật được về sau.
        const nut = el.firstChild;
        if (!nut || nut.nodeType !== Node.TEXT_NODE) return;
        const dich = parseInt((nut.nodeValue || "").replace(/\D/g, ""), 10);
        if (!Number.isFinite(dich) || dich <= 0) return;
        el.dataset.daChay = "1";
        const batDau = performance.now(), thoiGian = 700;
        const buoc = t => {
            const p = Math.min(1, (t - batDau) / thoiGian);
            const e = 1 - Math.pow(1 - p, 3);
            if (p < 1) { nut.nodeValue = dinhDang.format(Math.round(dich * e)); requestAnimationFrame(buoc); }
            else nut.nodeValue = dinhDang.format(dich);
        };
        requestAnimationFrame(buoc);
    }
    new MutationObserver(() => document.querySelectorAll("[data-chay-so]").forEach(chaySo))
        .observe(document.documentElement, { childList: true, subtree: true, characterData: false });
})();
