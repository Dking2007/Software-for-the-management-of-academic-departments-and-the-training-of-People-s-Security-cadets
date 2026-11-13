/* Vai trò tệp: Xem ảnh phóng to: bấm vào ảnh có thuộc tính data-phong-to (ảnh văn bản, ảnh bài bản tin, ảnh xem trước khi tải lên). */
// Xem ảnh phóng to: bấm vào ảnh có thuộc tính data-phong-to (ảnh văn bản, ảnh bài bản tin, ảnh xem trước khi tải lên).
// Mở lớp phủ toàn màn hình; bấm vào ảnh để chuyển "vừa màn hình" ↔ "kích thước thật"; Esc, nút × hoặc bấm nền để đóng.
// Chỉ thao tác giao diện ở trình duyệt, không gọi máy chủ. Dùng uỷ quyền sự kiện nên ảnh do Blazor vẽ lại vẫn bấm được.
(() => {
    let goc = null, truocDo = null;
    const CHU = {
        vi: { dong: "Đóng", tai: "Tải ảnh", that: "Kích thước thật", vua: "Vừa màn hình" },
        en: { dong: "Close", tai: "Download", that: "Actual size", vua: "Fit to screen" }
    };
    const chu = k => (CHU[document.documentElement.lang] || CHU.vi)[k] || CHU.vi[k];

    function mo(anh) {
        dong();
        truocDo = document.activeElement;
        const ten = anh.getAttribute("alt") || "";
        goc = document.createElement("div");
        goc.className = "xa-goc"; goc.setAttribute("role", "dialog"); goc.setAttribute("aria-modal", "true"); goc.setAttribute("aria-label", ten || "Ảnh");
        goc.setAttribute("translate", "no");
        const thanh = document.createElement("div"); thanh.className = "xa-thanh";
        const tieuDe = document.createElement("strong"); tieuDe.textContent = ten;
        const nutCo = document.createElement("button"); nutCo.type = "button";
        const datNhanCo = that => { nutCo.innerHTML = `<span aria-hidden="true">${that ? "⤡" : "⤢"}</span><span class="xa-nhan">${that ? chu("vua") : chu("that")}</span>`; };
        datNhanCo(false);
        const tai = document.createElement("a"); tai.href = anh.currentSrc || anh.src; tai.download = (ten || "anh").replace(/[\\/:*?"<>|]+/g, " ").slice(0, 60);
        tai.innerHTML = `<span aria-hidden="true">⭳</span><span class="xa-nhan">${chu("tai")}</span>`;
        const x = document.createElement("button"); x.type = "button"; x.className = "xa-dong"; x.textContent = "×"; x.setAttribute("aria-label", chu("dong"));
        thanh.append(tieuDe, nutCo, tai, x);
        const vung = document.createElement("div"); vung.className = "xa-vung";
        const lon = document.createElement("img"); lon.src = anh.currentSrc || anh.src; lon.alt = ten;
        vung.append(lon);
        goc.append(thanh, vung);
        const doiCo = () => { const that = vung.classList.toggle("xa-that"); datNhanCo(that); };
        lon.addEventListener("click", e => { e.stopPropagation(); doiCo(); });
        nutCo.addEventListener("click", doiCo);
        x.addEventListener("click", dong);
        vung.addEventListener("click", e => { if (e.target === vung) dong(); });
        goc.addEventListener("keydown", e => {
            if (e.key === "Tab") { // giữ focus trong lớp phủ
                const ds = [nutCo, tai, x]; const i = ds.indexOf(document.activeElement);
                e.preventDefault(); ds[(i + (e.shiftKey ? -1 : 1) + ds.length) % ds.length].focus();
            }
        });
        document.body.append(goc);
        document.documentElement.style.overflow = "hidden";
        x.focus({ preventScroll: true });
    }

    function dong() {
        if (!goc) return;
        goc.remove(); goc = null;
        document.documentElement.style.overflow = "";
        truocDo?.focus?.({ preventScroll: true });
    }

    document.addEventListener("click", e => {
        const anh = e.target.closest?.("img[data-phong-to]");
        if (!anh || goc) return;
        e.preventDefault(); e.stopPropagation();
        mo(anh);
    }, true);
    // Esc đóng ở mức tài liệu: bấm vào ảnh làm focus rời lớp phủ nên không nghe được ở riêng lớp phủ.
    document.addEventListener("keydown", e => {
        if (goc && e.key === "Escape") { e.preventDefault(); e.stopPropagation(); dong(); }
    }, true);
    // Bàn phím: ảnh có tabindex=0, Enter/Space để mở.
    document.addEventListener("keydown", e => {
        if ((e.key === "Enter" || e.key === " ") && e.target.matches?.("img[data-phong-to]")) { e.preventDefault(); mo(e.target); }
    });
    window.xemAnh = { mo, dong };
})();
