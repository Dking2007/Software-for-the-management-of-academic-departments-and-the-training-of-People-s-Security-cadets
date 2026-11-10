// Thẻ nổi dùng chung: phần tử có [data-noi] chứa (hoặc đứng ngay trước) một .noi-dung-noi ẩn do máy chủ dựng sẵn.
// Rê chuột hoặc focus vào phần tử thì chép nội dung đó ra một thẻ duy nhất ở lớp trên cùng (popover) để không bị bảng cuộn che.
// Không chứa dữ liệu hay quyền: chỉ hiển thị lại phần máy chủ đã cho phép dựng trong trang.
(() => {
    const TRE_MO = 220, TRE_DONG = 140;
    let the = null, neoHienTai = null, henMo = 0, henDong = 0;

    function taoThe() {
        if (the?.isConnected) return the;
        the = document.createElement("div");
        the.id = "the-noi-chung";
        the.className = "the-noi";
        the.setAttribute("role", "tooltip");
        if ("popover" in HTMLElement.prototype) the.setAttribute("popover", "manual");
        the.addEventListener("mouseenter", () => clearTimeout(henDong));
        the.addEventListener("mouseleave", () => henAn());
        document.body.appendChild(the);
        return the;
    }
    const noiDungCua = neo => neo.querySelector(":scope > .noi-dung-noi") ?? (neo.nextElementSibling?.classList.contains("noi-dung-noi") ? neo.nextElementSibling : null);

    function canChinh(neo) {
        const r = neo.getBoundingClientRect(), h = the.getBoundingClientRect();
        const le = 10, cach = 8;
        let x = r.right + cach;
        if (x + h.width > innerWidth - le) x = r.left - h.width - cach;
        if (x < le) x = Math.min(Math.max(le, r.left), innerWidth - h.width - le);
        let y = r.top;
        // Không đủ chỗ hai bên thì đặt dưới (hoặc trên) phần tử.
        if (x + h.width > r.left && x < r.right) y = r.bottom + h.height + cach < innerHeight ? r.bottom + cach : r.top - h.height - cach;
        the.style.left = `${Math.round(Math.max(le, x))}px`;
        the.style.top = `${Math.round(Math.max(le, Math.min(y, innerHeight - h.height - le)))}px`;
    }

    function hien(neo) {
        const nd = noiDungCua(neo);
        if (!nd || !neo.isConnected) return;
        taoThe();
        the.innerHTML = nd.innerHTML;
        the.classList.add("dang-hien");
        try { if (the.hasAttribute("popover") && !the.matches(":popover-open")) the.showPopover(); } catch { }
        neoHienTai = neo;
        neo.setAttribute("aria-describedby", "the-noi-chung");
        canChinh(neo);
    }
    function an() {
        clearTimeout(henMo);
        if (!the) return;
        the.classList.remove("dang-hien");
        try { if (the.matches(":popover-open")) the.hidePopover(); } catch { }
        neoHienTai?.removeAttribute("aria-describedby");
        neoHienTai = null;
    }
    const henAn = () => { clearTimeout(henDong); henDong = setTimeout(an, TRE_DONG); };
    function henHien(neo) {
        clearTimeout(henDong); clearTimeout(henMo);
        if (neo === neoHienTai) return;
        henMo = setTimeout(() => hien(neo), neoHienTai ? 60 : TRE_MO);
    }

    document.addEventListener("mouseover", e => {
        const neo = e.target.closest?.("[data-noi]");
        if (neo) henHien(neo);
        else if (!e.target.closest?.("#the-noi-chung") && (neoHienTai || henMo)) { clearTimeout(henMo); henMo = 0; henAn(); }
    });
    document.addEventListener("focusin", e => { const neo = e.target.closest?.("[data-noi]"); if (neo) henHien(neo); });
    document.addEventListener("focusout", e => { if (e.target.closest?.("[data-noi]")) henAn(); });
    document.addEventListener("keydown", e => { if (e.key === "Escape") an(); });
    // Bấm vào phần tử (mở hộp thoại, chuyển trang) hoặc cuộn thì đóng thẻ cho gọn.
    document.addEventListener("click", e => { if (!e.target.closest?.("#the-noi-chung")) an(); }, true);
    addEventListener("scroll", () => { if (neoHienTai) canChinh(neoHienTai); }, { passive: true, capture: true });
    // Blazor dựng lại trang: phần tử neo biến mất thì đóng thẻ.
    new MutationObserver(() => { if (neoHienTai && !neoHienTai.isConnected) an(); }).observe(document.documentElement, { childList: true, subtree: true });
})();
