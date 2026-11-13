/* Vai trò tệp: Hiện/ẩn mật khẩu và lấy mã chống giả mạo trước khi gửi biểu mẫu. */
// Đổi cách hiển thị ngay tại trình duyệt, giữ nguyên nội dung ô mật khẩu.
document.addEventListener("click", event => {
    const nut = event.target.closest?.("[data-hien-mat-khau]");
    if (!nut) return;
    const oNhap = document.getElementById(nut.dataset.hienMatKhau);
    if (!(oNhap instanceof HTMLInputElement)) return;
    const hien = oNhap.type === "password";
    oNhap.type = hien ? "text" : "password";
    nut.setAttribute("aria-pressed", String(hien));
    const nhan = hien ? "Ẩn mật khẩu" : "Hiện mật khẩu";
    nut.setAttribute("aria-label", nhan);
    nut.title = nhan;
    nut.querySelector(".rzi").textContent = hien ? "visibility_off" : "visibility";
});

// Lấy token mới bằng HTTP trước khi gửi form để không phụ thuộc token trong circuit.
document.addEventListener("submit", async event => {
    const form = event.target;
    if (!(form instanceof HTMLFormElement) || !form.hasAttribute("data-bieu-mau-phien")) return;
    event.preventDefault();
    if (form.dataset.dangGui === "true") return;
    form.dataset.dangGui = "true";
    const button = form.querySelector('[type="submit"]');
    if (button) button.disabled = true;
    try {
        const response = await fetch("/phien/ma-bao-ve", { credentials: "same-origin", cache: "no-store" });
        if (!response.ok) throw new Error("Không lấy được mã bảo vệ");
        const token = await response.json();
        if (!token.giaTri) throw new Error("Mã bảo vệ trống");
        form.querySelectorAll('input[name="__RequestVerificationToken"]').forEach(input => input.remove());
        const input = document.createElement("input");
        input.type = "hidden";
        input.name = token.tenTruong;
        input.value = token.giaTri;
        form.appendChild(input);
        HTMLFormElement.prototype.submit.call(form);
    } catch {
        form.dataset.dangGui = "false";
        if (button) button.disabled = false;
        let message = form.querySelector('[data-loi-phien]');
        if (!message) {
            message = document.createElement("p");
            message.setAttribute("data-loi-phien", "");
            message.setAttribute("role", "alert");
            form.appendChild(message);
        }
        message.textContent = "Không thể kết nối để xác nhận phiên. Vui lòng tải lại trang và thử lại.";
    }
}, true);
