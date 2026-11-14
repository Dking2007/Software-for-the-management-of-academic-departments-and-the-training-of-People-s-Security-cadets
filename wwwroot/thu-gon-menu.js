/* Vai trò tệp: Lưu và khôi phục trạng thái thu gọn menu trên trình duyệt. */
// Nhớ trạng thái thu gọn của thanh điều hướng trên trình duyệt này (localStorage).
// Chỉ là tiện ích hiển thị: không lưu tài khoản hay vai trò; trình duyệt chặn lưu thì vẫn dùng bình thường.
window.thuGonMenu = {
    doc() {
        try {
            const v = localStorage.getItem("menu-thu-gon");
            if (v === "1") return true;
            if (v === "0") return false;
        } catch { /* bỏ qua */ }
        // Chưa chọn lần nào: tự thu gọn ở màn hình vừa (dưới 1100 px) để nhường chỗ cho nội dung.
        return window.matchMedia ? window.matchMedia("(max-width: 1100px)").matches : false;
    },
    luu(thuGon) {
        try { localStorage.setItem("menu-thu-gon", thuGon ? "1" : "0"); } catch { /* bỏ qua */ }
    }
};
