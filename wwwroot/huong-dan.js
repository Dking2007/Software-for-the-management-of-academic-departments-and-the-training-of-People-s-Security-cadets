// Hướng dẫn nhập môn kiểu game: làm mờ màn hình, khoét sáng từng vùng, kèm bong bóng giải thích theo vai trò và theo trang.
// Tự chạy một lần cho mỗi tài khoản trên trình duyệt này; nút hình lá cờ trên thanh trên mở lại bất kỳ lúc nào.
// Chỉ đọc giao diện đang hiển thị, không gọi máy chủ, không chứa dữ liệu nghiệp vụ. Bước nào không có vùng tương ứng thì tự bỏ qua.
window.huongDan = (() => {
    const KHOA = "huongDan.daXem.v1.";
    const NUT = {
        tiep: { vi: "Tiếp", en: "Next", lo: "ຕໍ່ໄປ" }, lui: { vi: "Quay lại", en: "Back", lo: "ກັບຄືນ" }, boQua: { vi: "Bỏ qua", en: "Skip", lo: "ຂ້າມ" },
        xong: { vi: "Hoàn tất", en: "Finish", lo: "ສຳເລັດ" }, buoc: { vi: "Bước", en: "Step", lo: "ຂັ້ນຕອນ" }, chung: { vi: "Xem giới thiệu chung", en: "See the general tour", lo: "See the general tour" }
    };

    const VAI_TRO = {
        Admin: ["Menu của bạn có đủ ba mảng: Quản lý khoa, Quản lý đào tạo, Quản lý học viên, cùng Quản trị hệ thống (tài khoản, nhật ký).", "Your menu has all three areas: faculties, training and students, plus system administration."],
        ManagerKhoa: ["Mảng của bạn là Quản lý khoa: khoa, tổ, cán bộ, điểm danh đi làm. Mục \"phối hợp\" là phần bạn được xem để làm việc với mảng khác.", "Your area is faculty management. Items marked \"coordination\" are read-only views of other areas."],
        ManagerDaoTao: ["Mảng của bạn là Quản lý đào tạo: chuyên đề, phân công, lịch, phòng, lịch thi. Mục \"phối hợp\" là phần bạn được xem từ mảng khác.", "Your area is training management: subjects, assignments, schedules, rooms and exams."],
        ManagerLop: ["Mảng của bạn là Quản lý học viên: lớp, học viên, điểm danh, điểm. Mục \"phối hợp\" là phần bạn được xem từ mảng khác.", "Your area is student management: classes, students, attendance and grades."],
        GiangVien: ["\"Giảng dạy của tôi\" là nơi điểm danh đi làm, xem buổi dạy và lớp. \"Lịch dạy\" có xem tuần, ngày, tháng và xuất Excel.", "\"My teaching\" is where you check in, see sessions and classes. \"Schedule\" offers week, day and month views with Excel export."],
        HocVien: ["\"Học tập của tôi\" là nơi tự điểm danh, xem điểm, lịch thi và xuất kết quả ra Excel. \"Lịch học\" có xem tuần, ngày, tháng.", "\"My studies\" is where you check in, see grades and exams, and export results to Excel. \"Schedule\" offers week, day and month views."],
        Guest: ["Tài khoản Người xem chỉ đọc danh sách được phép, chưa thao tác được dữ liệu.", "A viewer account can only read the permitted lists."]
    };

    const CHUNG = v => [
        { tieuDe: ["Chào mừng bạn!", "Welcome!"], noiDung: ["Chuyến tham quan ngắn sẽ chỉ cho bạn từng khu vực trên màn hình. Dùng nút Tiếp hoặc phím mũi tên; bấm ra ngoài khung này để đóng bất kỳ lúc nào.", "This short tour points out each area of the screen. Use Next or the arrow keys; select anywhere outside this box to close it."] },
        { chon: ".sidebar .nav-scrollable", tieuDe: ["Menu chức năng", "Main menu"], noiDung: VAI_TRO[v] || VAI_TRO.Guest },
        { chon: ".nut-sidebar", tieuDe: ["Thu gọn menu", "Collapse the menu"], noiDung: ["Bấm để thu menu thành dải biểu tượng, dành thêm chỗ cho bảng dữ liệu. Lựa chọn được nhớ cho lần sau.", "Collapse the menu into icons to give tables more room. Your choice is remembered."] },
        { chon: ".duong-dan", tieuDe: ["Bạn đang ở đâu", "Where you are"], noiDung: ["Đường dẫn cho biết vị trí hiện tại. Bấm một mục để quay lại cấp trên.", "The breadcrumb shows your location. Select an item to go back up."] },
        { chon: ".nut-tim-nhanh", tieuDe: ["Tìm mọi thứ (Ctrl + K)", "Find anything (Ctrl + K)"], noiDung: ["Gõ tự do, không cần dấu, không cần đúng thứ tự: \"duy b10\", \"b10 duy\", \"duy-b10\" đều ra cùng kết quả. Gõ sai một chữ vẫn tìm được.", "Type freely, without accents or a fixed order: \"duy b10\" and \"b10 duy\" give the same result. Small typos are tolerated."] },
        { chon: ".nut-thong-bao", tieuDe: ["Văn bản, thông báo", "Notices"], noiDung: ["Số đỏ là văn bản bạn chưa đọc. Bấm để mở trang văn bản.", "The red number counts unread notices. Select it to open them."] },
        { chon: ".nut-cai-dat", tieuDe: ["Cài đặt giao diện", "Appearance"], noiDung: ["Đổi sáng/tối, màu chủ đạo, cỡ chữ, mật độ và ngôn ngữ. Chỉ lưu trên trình duyệt này.", "Change theme, colour, text size, density and language. Saved on this browser only."] },
        { chon: ".menu-nguoi-dung", tieuDe: ["Tài khoản", "Account"], noiDung: ["Xem hồ sơ cá nhân, đổi mật khẩu và đăng xuất ở đây.", "Open your profile, change your password or sign out here."] },
        { chon: "[data-mo-huong-dan]", tieuDe: ["Xem lại hướng dẫn", "Replay the tour"], noiDung: ["Quên cách dùng? Bấm nút này để xem hướng dẫn của đúng trang đang mở. Cuối trang dài còn có nút quay về đầu trang.", "Forgot something? This button replays the tour for the current page. Long pages also get a back-to-top button."] }
    ];

    const DANH_SACH = [
        { chon: ".thao-tac-dau-trang", tieuDe: ["Thao tác chính", "Main actions"], noiDung: ["Xuất Excel, nhập Excel và thêm mới nằm ở đây. Bạn chỉ thấy nút mà vai trò của mình được dùng.", "Export, import and add buttons live here. You only see what your role allows."] },
        { chon: ".panel-toolbar .search-box", tieuDe: ["Tìm trong danh sách", "Search this list"], noiDung: ["Gõ tự do theo tên, mã, đơn vị. Kết quả khớp nhất xếp lên đầu.", "Type a name, code or unit. The best matches come first."] },
        { chon: ".directory-grid", tieuDe: ["Bảng dữ liệu", "Data table"], noiDung: ["Bấm tiêu đề cột để sắp xếp, chọn số dòng mỗi trang ở cuối bảng. Rê chuột vào tên để xem thẻ thông tin nhanh.", "Sort by selecting a column header and pick rows per page below. Hover a name for a quick card."] }
    ];

    const THEO_TRANG = [
        { khop: /^dao-tao\/lich$/, buoc: [
            { chon: ".lich-chi-so", tieuDe: ["Số liệu nhanh", "At a glance"], noiDung: ["Tổng số buổi trong kỳ và số buổi hôm nay. Bấm ô \"Đã qua, chưa điểm danh\" để lọc ngay các buổi đó.", "Totals for the period and today. Select \"Past, not recorded\" to filter those sessions."] },
            { chon: "[data-hd=lich-che-do]", tieuDe: ["Bốn cách xem lịch", "Four views"], noiDung: ["Theo lớp: mỗi lớp một hàng, xem cả tuần trên một màn hình. Lưới tuần, Theo ngày và Tháng cho các nhu cầu còn lại.", "By class shows each class on one row. Week grid, By day and Month cover the rest."] },
            { chon: "[data-hd=lich-ky]", tieuDe: ["Đổi tuần, nhảy tới ngày", "Move through time"], noiDung: ["Dùng mũi tên để sang tuần khác, hoặc chọn ngày bất kỳ ở ô lịch.", "Use the arrows, or pick any date."] },
            { chon: "[data-hd=lich-loc]", tieuDe: ["Lọc và tìm trong lịch", "Filter and search"], noiDung: ["Gõ tự do như \"b10 triết\" hoặc chọn theo chuyên đề, trạng thái.", "Type freely, or filter by subject and status."] },
            { chon: ".thao-tac-dau-trang", tieuDe: ["Xuất, nhập Excel", "Excel"], noiDung: ["Xuất theo khoảng ngày bất kỳ, không cần chuyển lịch về đúng tháng đó. Tệp giữ nguyên bộ lọc đang dùng.", "Export any date range without moving the calendar. Current filters are kept."] }
        ] },
        { khop: /^khoa\/so-do$/, buoc: [
            { chon: ".sd-tab", tieuDe: ["Hai sơ đồ", "Two charts"], noiDung: ["Tổ chức – Cán bộ và Đào tạo là hai sơ đồ độc lập.", "Organisation and Training are separate charts."] },
            { chon: ".sd-thanh-cong-cu .search-box", tieuDe: ["Tìm trên sơ đồ", "Search the chart"], noiDung: ["Gõ tự do, ví dụ \"tổ 1 châu\". Kết quả hiện ở cột riêng, số người khớp hiện trên từng nhánh.", "Type freely. Results appear in their own column with counts on each branch."] },
            { chon: ".sd-cot-wrap", tieuDe: ["Duyệt từng cấp", "Browse level by level"], noiDung: ["Bấm một tổ để thấy cột thành viên, rê chuột vào một người để xem thông tin khái quát. Mỗi cột có nút « để thu gọn khi thiếu chỗ.", "Select a group to see its members and hover a person for a summary. Each column can be collapsed with «."] },
            { chon: ".sd-chi-tiet", tieuDe: ["Chi tiết đơn vị", "Unit details"], noiDung: ["Lãnh đạo, giới thiệu, cơ cấu và danh sách của mục đang chọn.", "Leaders, description and members of the selected item."] }
        ] },
        { khop: /^khoa\/cham-cong$/, buoc: [
            { chon: ".dieu-khien-tuan", tieuDe: ["Chọn tuần", "Choose a week"], noiDung: ["Xem bảng công của tuần trước đó bằng mũi tên.", "Use the arrows to see earlier weeks."] },
            { chon: ".tb-chuyen-muc", tieuDe: ["Chọn khoa", "Choose a faculty"], noiDung: ["Lọc theo khoa. Báo cáo Excel cũng theo khoa đang chọn.", "Filter by faculty. The Excel report follows this choice."] },
            { chon: ".khoi-cong", tieuDe: ["Bảng công", "Timesheet"], noiDung: ["Bấm vào ô để ghi bổ sung: có mặt, đi muộn, công tác, nghỉ phép, vắng.", "Select a cell to record attendance."] },
            { chon: "[data-hd=cong-excel]", tieuDe: ["Báo cáo Excel", "Excel report"], noiDung: ["Chọn khoảng ngày bất kỳ. Tệp gồm tổng quan, phần cần chú ý, theo cán bộ, theo ngày và bảng công.", "Pick any range. The report has a summary, watch list, per-person, per-day and timesheet sheets."] }
        ] },
        { khop: /^cua-toi$/, buoc: [
            { chon: ".the-tu-diem-danh", tieuDe: ["Tự điểm danh", "Self check-in"], noiDung: ["Cổng mở trước giờ học 30 phút và đóng sau giờ bắt đầu 15 phút.", "The gate opens 30 minutes before class and closes 15 minutes after it starts."] },
            { chon: "[data-hd=ca-nhan-excel]", tieuDe: ["Xuất kết quả học tập", "Export your results"], noiDung: ["Chọn nội dung (bảng điểm, điểm danh, lịch học, lịch thi) và khoảng ngày rồi tải tệp Excel.", "Choose what to include and the date range, then download."] },
            { chon: ".luoi-ca-nhan", tieuDe: ["Kết quả và lịch", "Results and schedule"], noiDung: ["Điểm theo học kỳ, lịch thi, lịch học tuần này và lịch sử điểm danh.", "Grades by term, exams, this week's classes and attendance history."] }
        ] },
        { khop: /^giang-day$/, buoc: [
            { chon: ".the-tu-diem-danh", tieuDe: ["Điểm danh đi làm", "Daily check-in"], noiDung: ["Bấm mỗi ngày một lần. Trước 07:30 ghi Có mặt, sau đó ghi Đi muộn.", "Check in once a day. Before 07:30 counts as present, later as late."] },
            { chon: "[data-hd=ca-nhan-excel]", tieuDe: ["Báo cáo giảng dạy", "Teaching report"], noiDung: ["Xuất lịch dạy, tổng hợp theo lớp, buổi chưa điểm danh và công của bạn theo khoảng ngày bất kỳ.", "Export your schedule, per-class totals, missing attendance and your timesheet for any range."] },
            { chon: ".luoi-ca-nhan", tieuDe: ["Công việc của bạn", "Your work"], noiDung: ["Buổi dạy hôm nay, lớp bạn dạy và buổi đã qua chưa điểm danh. Bấm Điểm danh hoặc Điểm để vào thẳng trang lớp.", "Today's sessions, your classes and missing attendance, with shortcuts into each class."] }
        ] }
    ];

    let cacBuoc = [], chiSo = 0, goc = null, khoet = null, pop = null, dangChay = false, tuDongDaChay = false, truocDo = null, cheDo = "chung";

    const ngon = () => { const n = window.tuyChonGiaoDien?.doc?.()?.ngonNgu; return n === "en" || n === "lo" ? n : "vi"; };
    const chu = cap => ngon() === "vi" ? cap[0] : cap[1];
    const nhan = k => NUT[k][ngon()];
    const giamChuyenDong = () => window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    const duongDan = () => location.pathname.replace(/^\/+|\/+$/g, "");
    const thongTin = () => document.querySelector("[data-huong-dan-nguoi]");
    const hienRo = el => { if (!el) return false; const r = el.getBoundingClientRect(); return r.width > 0 && r.height > 0 && getComputedStyle(el).visibility !== "hidden"; };
    const coDich = b => !b.chon || hienRo(document.querySelector(b.chon));

    function buocTrang() {
        const p = duongDan();
        const m = THEO_TRANG.find(x => x.khop.test(p));
        if (m) return m.buoc;
        return document.querySelector(".panel-toolbar .search-box") ? DANH_SACH : [];
    }

    function batDau(che) {
        if (dangChay) return;
        const vaiTro = thongTin()?.dataset.huongDanVaiTro || "Guest";
        const trang = buocTrang();
        cheDo = che === "chung" || trang.length === 0 ? "chung" : che;
        cacBuoc = (cheDo === "chung" ? CHUNG(vaiTro) : cheDo === "day-du" ? [...CHUNG(vaiTro), ...trang] : trang).filter(coDich);
        if (cacBuoc.length === 0) return;
        truocDo = document.activeElement;
        dungKhung();
        dangChay = true; chiSo = 0;
        hien();
    }

    function dungKhung() {
        goc = document.createElement("div");
        goc.className = "hd-goc"; goc.setAttribute("translate", "no");
        goc.innerHTML = '<div class="hd-chan"></div><div class="hd-khoet"></div><div class="hd-pop" role="dialog" aria-modal="true" aria-labelledby="hd-tieu-de" aria-describedby="hd-noi-dung" tabindex="-1"></div>';
        document.body.appendChild(goc);
        khoet = goc.querySelector(".hd-khoet"); pop = goc.querySelector(".hd-pop");
        // Bấm ra ngoài bong bóng (phần nền hoặc vùng đang khoét sáng) thì đóng hướng dẫn.
        goc.addEventListener("click", e => { if (e.target === goc || e.target.classList.contains("hd-chan")) dong(); });
        document.addEventListener("keydown", phim, true);
        window.addEventListener("resize", datViTri);
        window.addEventListener("scroll", datViTri, true);
    }

    function dong() {
        if (!dangChay) return;
        dangChay = false;
        document.removeEventListener("keydown", phim, true);
        window.removeEventListener("resize", datViTri);
        window.removeEventListener("scroll", datViTri, true);
        goc?.remove(); goc = khoet = pop = null;
        const ten = thongTin()?.dataset.huongDanNguoi;
        if (ten) try { localStorage.setItem(KHOA + ten, "1"); } catch { }
        truocDo?.focus?.({ preventScroll: true });
    }

    function phim(e) {
        if (e.key === "Escape") { e.preventDefault(); e.stopPropagation(); dong(); }
        else if (e.key === "ArrowRight") { e.preventDefault(); e.stopPropagation(); tiep(); }
        else if (e.key === "ArrowLeft") { e.preventDefault(); e.stopPropagation(); lui(); }
        else if (e.key === "Tab") {
            // Giữ focus trong bong bóng để người dùng bàn phím không đi lạc xuống trang đang bị che.
            const ds = [...pop.querySelectorAll("button")];
            if (ds.length === 0) return;
            const i = ds.indexOf(document.activeElement);
            e.preventDefault();
            ds[(i + (e.shiftKey ? -1 : 1) + ds.length) % ds.length].focus();
        }
    }
    const tiep = () => { if (chiSo >= cacBuoc.length - 1) dong(); else { chiSo++; hien(); } };
    const lui = () => { if (chiSo > 0) { chiSo--; hien(); } };

    function taoNut(lop, chuNut, xuLy) {
        const n = document.createElement("button");
        n.type = "button"; n.className = lop; n.textContent = chuNut; n.addEventListener("click", xuLy);
        return n;
    }

    function hien() {
        const b = cacBuoc[chiSo];
        const cuoi = chiSo === cacBuoc.length - 1;
        pop.textContent = "";
        const dau = document.createElement("div"); dau.className = "hd-dau";
        const huy = document.createElement("span"); huy.className = "hd-huy-hieu"; huy.textContent = `${nhan("buoc")} ${chiSo + 1}/${cacBuoc.length}`;
        dau.append(huy);
        const h = document.createElement("h2"); h.id = "hd-tieu-de"; h.textContent = chu(b.tieuDe);
        const p = document.createElement("p"); p.id = "hd-noi-dung"; p.textContent = chu(b.noiDung);
        const tien = document.createElement("div"); tien.className = "hd-tien-do";
        const vach = document.createElement("span"); vach.style.transform = `scaleX(${(chiSo + 1) / cacBuoc.length})`; tien.append(vach);
        const nut = document.createElement("div"); nut.className = "hd-nut";
        if (cuoi && cheDo === "trang") nut.append(taoNut("hd-phu", nhan("chung"), () => { dong(); batDau("chung"); }));
        else if (!cuoi) nut.append(taoNut("hd-phu", nhan("boQua"), dong));
        const phai = document.createElement("span"); phai.className = "hd-nut-phai";
        if (chiSo > 0) phai.append(taoNut("hd-lui", nhan("lui"), lui));
        const nutTiep = taoNut("hd-tiep", cuoi ? nhan("xong") : nhan("tiep"), tiep);
        phai.append(nutTiep); nut.append(phai);
        pop.append(dau, h, p, tien, nut);
        const el = b.chon ? document.querySelector(b.chon) : null;
        if (el) {
            const r = el.getBoundingClientRect();
            if (r.top < 0 || r.bottom > innerHeight) el.scrollIntoView({ block: r.height > innerHeight * .7 ? "start" : "center", behavior: "auto" });
        }
        khoet.classList.remove("hd-vao"); pop.classList.remove("hd-vao");
        void pop.offsetWidth; // chạy lại hiệu ứng vào cho bước mới
        khoet.classList.add("hd-vao"); pop.classList.add("hd-vao");
        datViTri();
        nutTiep.focus({ preventScroll: true });
    }

    function datViTri() {
        if (!dangChay || !pop) return;
        const b = cacBuoc[chiSo];
        const el = b.chon ? document.querySelector(b.chon) : null;
        const W = innerWidth, H = innerHeight, L = 12;
        const pw = pop.offsetWidth, ph = pop.offsetHeight;
        const chan = goc.querySelector(".hd-chan");
        // Màn chào mừng không trỏ vào vùng nào nên làm nhám nền để bong bóng không lẫn vào trang; các bước sau chỉ làm tối.
        chan.classList.toggle("hd-nham", !el || !hienRo(el));
        if (!el || !hienRo(el)) {
            khoet.style.display = "none";
            chan.style.clipPath = "none";
            pop.style.transform = `translate(${Math.round((W - pw) / 2)}px, ${Math.round((H - ph) / 2)}px)`;
            return;
        }
        const r = el.getBoundingClientRect(), dem = 6;
        const x = Math.max(4, r.left - dem), y = Math.max(4, r.top - dem);
        const w = Math.min(W - 8, r.right + dem) - x, h = Math.min(H - 8, r.bottom + dem) - y;
        khoet.style.display = "block";
        khoet.style.width = `${w}px`; khoet.style.height = `${h}px`;
        khoet.style.transform = `translate(${x}px, ${y}px)`;
        // Nền mờ nhám phủ cả màn hình, trừ một lỗ bo góc đúng bằng vùng đang giới thiệu.
        const bk = Math.min(10, w / 2, h / 2);
        chan.style.clipPath = `path(evenodd, "M0 0H${W}V${H}H0Z M${x + bk} ${y}H${x + w - bk}A${bk} ${bk} 0 0 1 ${x + w} ${y + bk}V${y + h - bk}A${bk} ${bk} 0 0 1 ${x + w - bk} ${y + h}H${x + bk}A${bk} ${bk} 0 0 1 ${x} ${y + h - bk}V${y + bk}A${bk} ${bk} 0 0 1 ${x + bk} ${y}Z")`;
        // Thử đặt bên phải, dưới, trái, trên; không vị trí nào vừa thì đặt giữa màn hình.
        const cach = 14;
        const viTri = [
            [x + w + cach, Math.min(Math.max(L, y), H - ph - L), x + w + cach + pw <= W - L],
            [Math.min(Math.max(L, x), W - pw - L), y + h + cach, y + h + cach + ph <= H - L],
            [x - pw - cach, Math.min(Math.max(L, y), H - ph - L), x - pw - cach >= L],
            [Math.min(Math.max(L, x), W - pw - L), y - ph - cach, y - ph - cach >= L]
        ].find(v => v[2]) || [(W - pw) / 2, (H - ph) / 2];
        pop.style.transform = `translate(${Math.round(viTri[0])}px, ${Math.round(viTri[1])}px)`;
    }

    // Lần đầu tài khoản này vào hệ thống trên trình duyệt: chờ giao diện dựng xong rồi tự chạy.
    function thuTuDong() {
        const t = thongTin();
        if (!t || tuDongDaChay || dangChay) return;
        let daXem = false;
        try { daXem = localStorage.getItem(KHOA + t.dataset.huongDanNguoi) === "1"; } catch { daXem = true; }
        tuDongDaChay = true;
        if (!daXem) setTimeout(() => { if (!document.querySelector(".rz-dialog")) batDau("day-du"); }, giamChuyenDong() ? 300 : 900);
    }
    new MutationObserver(thuTuDong).observe(document.documentElement, { childList: true, subtree: true });

    document.addEventListener("click", e => {
        if (e.target.closest?.("[data-mo-huong-dan]")) batDau("trang");
    });

    return { batDau, dong };
})();
