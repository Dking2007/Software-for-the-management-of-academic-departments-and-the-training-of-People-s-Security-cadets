/* Vai trò tệp: Hướng dẫn nhập môn kiểu game: làm mờ màn hình, khoét sáng từng vùng, kèm bong bóng giải thích theo vai trò và theo trang. */
// Hướng dẫn nhập môn kiểu game: làm mờ màn hình, khoét sáng từng vùng, kèm bong bóng giải thích theo vai trò và theo trang.
// Khi nào hiện (nhớ theo TÀI KHOẢN ở máy chủ, qua MainLayout → DichVuHuongDan, không theo trình duyệt):
//  - Tài khoản chưa từng xem: hiện một thẻ mời nhỏ ở góc (không che màn hình) với ba lựa chọn: Xem / Để sau / Không cần.
//    Bấm Xem hoặc Không cần là ghi nhận ngay, không bao giờ mời lại (kể cả máy khác). "Để sau" chỉ ẩn trong phiên này; sau 3 lần tự thôi.
//  - Hướng dẫn riêng từng trang KHÔNG tự bật; nút lá cờ có chấm nhỏ cho tới khi người dùng xem trang đó một lần.
//  - Nút lá cờ trên thanh trên luôn mở lại được. Bước nào không có vùng tương ứng thì tự bỏ qua.
window.huongDan = (() => {
    const KHOA = "huongDan.daXem.v1.";
    const NUT = {
        tiep: { vi: "Tiếp", en: "Next", lo: "ຕໍ່ໄປ" }, lui: { vi: "Quay lại", en: "Back", lo: "ກັບຄືນ" }, boQua: { vi: "Bỏ qua", en: "Skip", lo: "ຂ້າມ" },
        xong: { vi: "Hoàn tất", en: "Finish", lo: "ສຳເລັດ" }, buoc: { vi: "Bước", en: "Step", lo: "ຂັ້ນຕອນ" }, chung: { vi: "Xem giới thiệu chung", en: "See the general tour", lo: "See the general tour" },
        moiTieuDe: { vi: "Chào mừng bạn đến với hệ thống!", en: "Welcome aboard!", lo: "ຍິນດີຕ້ອນຮັບ!" },
        moiNoiDung: { vi: "Lần đầu dùng? Chuyến tham quan khoảng 1 phút sẽ chỉ cho bạn menu, ô tìm kiếm, thông báo và cài đặt.", en: "First time here? A one-minute tour shows you the menu, search, notices and settings.", lo: "First time here? A one-minute tour shows you the menu, search, notices and settings." },
        moiXem: { vi: "Xem hướng dẫn", en: "Take the tour", lo: "ເບິ່ງຄຳແນະນຳ" },
        moiDeSau: { vi: "Để sau", en: "Later", lo: "ພາຍຫຼັງ" },
        moiKhong: { vi: "Tôi đã quen, không cần", en: "I know my way around", lo: "I know my way around" },
        moiGhiChu: { vi: "Xem lại bất cứ lúc nào bằng nút lá cờ trên thanh trên.", en: "Replay any time with the flag button at the top.", lo: "Replay any time with the flag button at the top." },
        goiYTrang: { vi: "Trang này có hướng dẫn riêng, bấm để xem", en: "This page has its own tour. Select to view", lo: "This page has its own tour. Select to view" },
        tieuDeNut: { vi: "Hướng dẫn nhanh", en: "Quick tour", lo: "Quick tour" }
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
        { khop: /^$/, khoa: "trang-chu", buoc: [
            { chon: ".bang-dau-chu", tieuDe: ["Việc của bạn hôm nay", "Your day at a glance"], noiDung: ["Lời chào kèm một câu tóm tắt: số buổi học, buổi chưa điểm danh, mục cần chú ý, văn bản chưa đọc. Nút bên dưới mở việc chính của vai trò bạn.", "A one-line summary of today, with a button for your main task."] },
            { chon: ".the-tk", tieuDe: ["Thẻ tài khoản", "Your account card"], noiDung: ["Tên, vai trò và phạm vi bạn được làm việc trong phiên này.", "Your name, role and scope for this session."] },
            { chon: ".tc-loi-tat", tieuDe: ["Lối tắt theo vai trò", "Shortcuts"], noiDung: ["Những trang bạn dùng nhiều nhất, xếp theo vai trò.", "The pages you use most, by role."] },
            { chon: ".tc-chi-so", tieuDe: ["Chỉ số hôm nay", "Today's numbers"], noiDung: ["Bấm một ô để đi thẳng tới nơi xử lý. Vạch màu bên trái cho biết ổn (xanh) hay cần làm (vàng).", "Select a box to go straight to where it is handled."] },
            { chon: ".the-can-chu-y", tieuDe: ["Cần chú ý", "Needs attention"], noiDung: ["Các việc tồn đọng trong phạm vi của bạn, việc có số lớn hơn 0 xếp lên đầu.", "Open items in your scope, outstanding ones first."] },
            { chon: ".quy-mo", tieuDe: ["Quy mô Học viện", "Academy at a glance"], noiDung: ["Số khoa, tổ, cán bộ, lớp, học viên. Bấm để mở danh sách tương ứng.", "Counts of faculties, groups, staff, classes and students."] }
        ] },
        { khop: /^thong-bao$/, buoc: [
            { chon: ".dau-bang-tin", tieuDe: ["Văn bản gửi tới bạn", "Notices for you"], noiDung: ["Số văn bản và số chưa đọc. Quản trị viên có nút Đăng văn bản ở đây.", "Totals and unread count. Administrators post new notices here."] },
            { chon: ".tb-chuyen-muc", tieuDe: ["Chuyên mục", "Categories"], noiDung: ["Lọc theo loại: thông báo, kế hoạch, công văn…", "Filter by notice type."] },
            { chon: ".bo-loc-tin", tieuDe: ["Tìm và lọc", "Search and filter"], noiDung: ["Gõ số ký hiệu, tiêu đề hay nội dung; lọc theo nơi nhận hoặc chỉ văn bản chưa đọc.", "Search by number, title or text; filter by recipient or unread."] },
            { chon: ".cot-tieu-de", tieuDe: ["Danh sách văn bản", "Notice list"], noiDung: ["Ghim lên trước, mới nhất sau. Chấm đỏ là văn bản bạn chưa đọc.", "Pinned first, newest next. A red dot marks unread notices."] },
            { chon: ".tb-chi-tiet", tieuDe: ["Nội dung văn bản", "Notice details"], noiDung: ["Số ký hiệu, ngày ban hành, người ký, nơi nhận và nội dung. Bấm ảnh đính kèm để xem lớn; có nút In bản sạch.", "Number, date, signer, recipients and content. Select an image to enlarge it."] }
        ] },
        { khop: /^quan-ly-hoc-vien\/he\/.+$/, khoa: "quan-ly-hoc-vien/he", buoc: [
            { chon: ".hv-tab-he", tieuDe: ["Chọn hệ đào tạo", "Training systems"], noiDung: ["Tổng quan, hệ chính quy hoặc văn bằng 2, kèm số lớp mỗi hệ.", "Overview, regular or second-degree programmes."] },
            { chon: ".hv-ds-khoa", tieuDe: ["Khoá · niên khoá", "Intakes"], noiDung: ["Chọn khoá để xem lớp của khoá đó. Nút ⌃ ⌄ bên phải thu gọn cột này để nhường chỗ cho thẻ lớp.", "Pick an intake. The arrow button collapses this column."] },
            { chon: ".hv-nganh", tieuDe: ["Lọc chuyên ngành", "Filter by major"], noiDung: ["Chỉ hiện lớp của một chuyên ngành; số bên cạnh là số lớp.", "Show only one major's classes."] },
            { chon: ".thanh-lich", tieuDe: ["Tìm và cách xem", "Search and view"], noiDung: ["Gõ mã lớp, chuyên ngành hay chủ nhiệm; chuyển giữa Thẻ lớp và Bảng; mở hoặc thu gọn từng khoá.", "Search, switch between cards and table, expand or collapse intakes."] },
            { chon: ".luoi-the-lop", tieuDe: ["Thẻ lớp", "Class cards"], noiDung: ["Bấm mã lớp để vào trang lớp (học viên, lịch, điểm danh, điểm). Bút để sửa, thùng rác để xoá nếu bạn có quyền.", "Select a class code to open it."] }
        ] },
        { khop: /^quan-ly-hoc-vien\/lop\/.+$/, khoa: "quan-ly-hoc-vien/lop", buoc: [
            { chon: ".thong-tin-don-vi", tieuDe: ["Thông tin lớp", "Class details"], noiDung: ["Mã lớp, hệ, khoá, niên khoá, chuyên ngành, chủ nhiệm và sĩ số.", "Code, system, intake, major, homeroom teacher and size."] },
            { chon: ".rz-tabview-nav", tieuDe: ["Các tab của lớp", "Class tabs"], noiDung: ["Học viên, lịch học, điểm danh, chuyên cần, sổ đầu bài và điểm của riêng lớp này. Bạn chỉ thấy tab mà vai trò được dùng.", "Students, schedule, attendance, logbook and grades for this class."] },
            { chon: ".rz-tabview-panels", tieuDe: ["Nội dung tab", "Tab content"], noiDung: ["Tab Điểm danh ghi theo từng buổi; tab Điểm tự tính tổng kết 30% chuyên cần + 70% kiểm tra.", "Attendance is recorded per session; grades are totalled automatically."] }
        ] },
        { khop: /^dao-tao\/lich$/, buoc: [
            { chon: ".lich-chi-so", tieuDe: ["Số liệu nhanh", "At a glance"], noiDung: ["Tổng số buổi trong kỳ và số buổi hôm nay. Bấm ô \"Đã qua, chưa điểm danh\" để lọc ngay các buổi đó.", "Totals for the period and today. Select \"Past, not recorded\" to filter those sessions."] },
            { chon: "[data-hd=lich-che-do]", tieuDe: ["Bốn cách xem lịch", "Four views"], noiDung: ["Theo lớp: mỗi lớp một hàng, xem cả tuần trên một màn hình. Lưới tuần, Theo ngày và Tháng cho các nhu cầu còn lại.", "By class shows each class on one row. Week grid, By day and Month cover the rest."] },
            { chon: "[data-hd=lich-ky]", tieuDe: ["Đổi tuần, nhảy tới ngày", "Move through time"], noiDung: ["Dùng mũi tên để sang tuần khác, hoặc chọn ngày bất kỳ ở ô lịch: ngày đã chọn được tô nổi, có thể bật \"Chỉ ngày này\".", "Use the arrows, or pick any date: the chosen day is highlighted and you can switch on \"This day only\"."] },
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

    let cacBuoc = [], chiSo = 0, goc = null, khoet = null, pop = null, dangChay = false, truocDo = null, cheDo = "chung";
    // daXem: tập khoá đã xem của tài khoản, nạp từ máy chủ khi MainLayout gọi ketNoi(); null = chưa biết (chưa làm gì cả).
    let cauNoi = null, daXem = null, the = null, duongDanTruoc = null;
    const KHOA_CHUNG = "chung";

    const ngon = () => { const n = window.tuyChonGiaoDien?.doc?.()?.ngonNgu; return n === "en" || n === "lo" ? n : "vi"; };
    // Tiếng Lào: tra theo câu tiếng Việt; câu chưa có bản Lào thì dùng tiếng Anh.
    const chu = cap => ngon() === "vi" ? cap[0] : ngon() === "lo" ? (LAO[cap[0]] ?? cap[1]) : cap[1];
    const LAO = {
        "Menu của bạn có đủ ba mảng: Quản lý khoa, Quản lý đào tạo, Quản lý học viên, cùng Quản trị hệ thống (tài khoản, nhật ký).": "ເມນູຂອງທ່ານມີຄົບສາມຂົງເຂດ: ຄຸ້ມຄອງຄະນະ, ຄຸ້ມຄອງການຝຶກອົບຮົມ, ຄຸ້ມຄອງນັກຮຽນ, ພ້ອມບໍລິຫານລະບົບ (ບັນຊີ, ບັນທຶກ).",
        "Mảng của bạn là Quản lý khoa: khoa, tổ, cán bộ, điểm danh đi làm. Mục \"phối hợp\" là phần bạn được xem để làm việc với mảng khác.": "ຂົງເຂດຂອງທ່ານແມ່ນຄຸ້ມຄອງຄະນະ: ຄະນະ, ໜ່ວຍ, ພະນັກງານ, ລົງຊື່ເຂົ້າວຽກ. ລາຍການ \"ປະສານງານ\" ແມ່ນສ່ວນທີ່ທ່ານເບິ່ງໄດ້ເພື່ອເຮັດວຽກກັບຂົງເຂດອື່ນ.",
        "Mảng của bạn là Quản lý đào tạo: chuyên đề, phân công, lịch, phòng, lịch thi. Mục \"phối hợp\" là phần bạn được xem từ mảng khác.": "ຂົງເຂດຂອງທ່ານແມ່ນຄຸ້ມຄອງການຝຶກອົບຮົມ: ວິຊາ, ການມອບໝາຍ, ຕາຕະລາງ, ຫ້ອງ, ຕາຕະລາງສອບ. ລາຍການ \"ປະສານງານ\" ແມ່ນສ່ວນທີ່ທ່ານເບິ່ງໄດ້ຈາກຂົງເຂດອື່ນ.",
        "Mảng của bạn là Quản lý học viên: lớp, học viên, điểm danh, điểm. Mục \"phối hợp\" là phần bạn được xem từ mảng khác.": "ຂົງເຂດຂອງທ່ານແມ່ນຄຸ້ມຄອງນັກຮຽນ: ຫ້ອງ, ນັກຮຽນ, ເຊັກຊື່, ຄະແນນ. ລາຍການ \"ປະສານງານ\" ແມ່ນສ່ວນທີ່ທ່ານເບິ່ງໄດ້ຈາກຂົງເຂດອື່ນ.",
        "\"Giảng dạy của tôi\" là nơi điểm danh đi làm, xem buổi dạy và lớp. \"Lịch dạy\" có xem tuần, ngày, tháng và xuất Excel.": "\"ການສອນຂອງຂ້ອຍ\" ແມ່ນບ່ອນລົງຊື່ເຂົ້າວຽກ, ເບິ່ງຄາບສອນ ແລະ ຫ້ອງ. \"ຕາຕະລາງສອນ\" ເບິ່ງໄດ້ຕາມອາທິດ, ມື້, ເດືອນ ແລະ ສົ່ງອອກ Excel.",
        "\"Học tập của tôi\" là nơi tự điểm danh, xem điểm, lịch thi và xuất kết quả ra Excel. \"Lịch học\" có xem tuần, ngày, tháng.": "\"ການຮຽນຂອງຂ້ອຍ\" ແມ່ນບ່ອນເຊັກຊື່ເອງ, ເບິ່ງຄະແນນ, ຕາຕະລາງສອບ ແລະ ສົ່ງອອກຜົນເປັນ Excel. \"ຕາຕະລາງຮຽນ\" ເບິ່ງໄດ້ຕາມອາທິດ, ມື້, ເດືອນ.",
        "Tài khoản Người xem chỉ đọc danh sách được phép, chưa thao tác được dữ liệu.": "ບັນຊີຜູ້ເບິ່ງອ່ານໄດ້ສະເພາະລາຍການທີ່ອະນຸຍາດ, ຍັງດຳເນີນງານຂໍ້ມູນບໍ່ໄດ້.",
        "Chào mừng bạn!": "ຍິນດີຕ້ອນຮັບ!",
        "Chuyến tham quan ngắn sẽ chỉ cho bạn từng khu vực trên màn hình. Dùng nút Tiếp hoặc phím mũi tên; bấm ra ngoài khung này để đóng bất kỳ lúc nào.": "ການນຳທ່ຽວສັ້ນໆຈະຊີ້ໃຫ້ທ່ານເຫັນແຕ່ລະພື້ນທີ່ໃນໜ້າຈໍ. ໃຊ້ປຸ່ມຕໍ່ໄປ ຫຼື ປຸ່ມລູກສອນ; ກົດນອກກອບນີ້ເພື່ອປິດໄດ້ທຸກເວລາ.",
        "Menu chức năng": "ເມນູໜ້າທີ່",
        "Thu gọn menu": "ຫຍໍ້ເມນູ",
        "Bấm để thu menu thành dải biểu tượng, dành thêm chỗ cho bảng dữ liệu. Lựa chọn được nhớ cho lần sau.": "ກົດເພື່ອຫຍໍ້ເມນູເປັນແຖບໄອຄອນ, ໃຫ້ມີບ່ອນສຳລັບຕາຕະລາງຂໍ້ມູນຫຼາຍຂຶ້ນ. ການເລືອກຖືກຈື່ໄວ້ສຳລັບຄັ້ງຕໍ່ໄປ.",
        "Bạn đang ở đâu": "ທ່ານຢູ່ໃສ",
        "Đường dẫn cho biết vị trí hiện tại. Bấm một mục để quay lại cấp trên.": "ເສັ້ນທາງບອກຕຳແໜ່ງປັດຈຸບັນ. ກົດລາຍການໜຶ່ງເພື່ອກັບຄືນລະດັບເທິງ.",
        "Tìm mọi thứ (Ctrl + K)": "ຄົ້ນຫາທຸກຢ່າງ (Ctrl + K)",
        "Gõ tự do, không cần dấu, không cần đúng thứ tự: \"duy b10\", \"b10 duy\", \"duy-b10\" đều ra cùng kết quả. Gõ sai một chữ vẫn tìm được.": "ພິມອິດສະຫຼະ, ບໍ່ຕ້ອງມີວັນນະຍຸດ, ບໍ່ຕ້ອງຖືກລຳດັບ: \"duy b10\", \"b10 duy\", \"duy-b10\" ໄດ້ຜົນດຽວກັນ. ພິມຜິດໜຶ່ງຕົວກໍຍັງຫາໄດ້.",
        "Văn bản, thông báo": "ເອກະສານ, ແຈ້ງການ",
        "Số đỏ là văn bản bạn chưa đọc. Bấm để mở trang văn bản.": "ຕົວເລກສີແດງແມ່ນເອກະສານທີ່ທ່ານຍັງບໍ່ອ່ານ. ກົດເພື່ອເປີດໜ້າເອກະສານ.",
        "Cài đặt giao diện": "ຕັ້ງຄ່າໜ້າຕາ",
        "Đổi sáng/tối, màu chủ đạo, cỡ chữ, mật độ và ngôn ngữ. Chỉ lưu trên trình duyệt này.": "ປ່ຽນແຈ້ງ/ມືດ, ສີຫຼັກ, ຂະໜາດຕົວອັກສອນ, ຄວາມໜາແໜ້ນ ແລະ ພາສາ. ບັນທຶກສະເພາະໃນບຣາວເຊີນີ້.",
        "Tài khoản": "ບັນຊີ",
        "Xem hồ sơ cá nhân, đổi mật khẩu và đăng xuất ở đây.": "ເບິ່ງຂໍ້ມູນສ່ວນຕົວ, ປ່ຽນລະຫັດຜ່ານ ແລະ ອອກຈາກລະບົບຢູ່ນີ້.",
        "Xem lại hướng dẫn": "ເບິ່ງຄຳແນະນຳຄືນ",
        "Quên cách dùng? Bấm nút này để xem hướng dẫn của đúng trang đang mở. Cuối trang dài còn có nút quay về đầu trang.": "ລືມວິທີໃຊ້ບໍ? ກົດປຸ່ມນີ້ເພື່ອເບິ່ງຄຳແນະນຳຂອງໜ້າທີ່ເປີດຢູ່. ທ້າຍໜ້າຍາວຍັງມີປຸ່ມກັບຂຶ້ນເທິງ.",
        "Thao tác chính": "ການດຳເນີນງານຫຼັກ",
        "Xuất Excel, nhập Excel và thêm mới nằm ở đây. Bạn chỉ thấy nút mà vai trò của mình được dùng.": "ສົ່ງອອກ Excel, ນຳເຂົ້າ Excel ແລະ ເພີ່ມໃໝ່ຢູ່ນີ້. ທ່ານເຫັນສະເພາະປຸ່ມທີ່ບົດບາດຂອງທ່ານໃຊ້ໄດ້.",
        "Tìm trong danh sách": "ຄົ້ນຫາໃນລາຍການ",
        "Gõ tự do theo tên, mã, đơn vị. Kết quả khớp nhất xếp lên đầu.": "ພິມອິດສະຫຼະຕາມຊື່, ລະຫັດ, ໜ່ວຍງານ. ຜົນທີ່ກົງທີ່ສຸດຂຶ້ນກ່ອນ.",
        "Bảng dữ liệu": "ຕາຕະລາງຂໍ້ມູນ",
        "Bấm tiêu đề cột để sắp xếp, chọn số dòng mỗi trang ở cuối bảng. Rê chuột vào tên để xem thẻ thông tin nhanh.": "ກົດຫົວຖັນເພື່ອຈັດລຽງ, ເລືອກຈຳນວນແຖວຕໍ່ໜ້າຢູ່ທ້າຍຕາຕະລາງ. ວາງເມົາສ໌ໃສ່ຊື່ເພື່ອເບິ່ງບັດຂໍ້ມູນດ່ວນ.",
        "Số liệu nhanh": "ຕົວເລກດ່ວນ",
        "Tổng số buổi trong kỳ và số buổi hôm nay. Bấm ô \"Đã qua, chưa điểm danh\" để lọc ngay các buổi đó.": "ຈຳນວນຄາບໃນໄລຍະ ແລະ ຈຳນວນຄາບມື້ນີ້. ກົດຊ່ອງ \"ຜ່ານແລ້ວ ຍັງບໍ່ເຊັກຊື່\" ເພື່ອກອງຄາບເຫຼົ່ານັ້ນທັນທີ.",
        "Bốn cách xem lịch": "ສີ່ວິທີເບິ່ງຕາຕະລາງ",
        "Theo lớp: mỗi lớp một hàng, xem cả tuần trên một màn hình. Lưới tuần, Theo ngày và Tháng cho các nhu cầu còn lại.": "ຕາມຫ້ອງ: ແຕ່ລະຫ້ອງໜຶ່ງແຖວ, ເບິ່ງທັງອາທິດໃນໜ້າຈໍດຽວ. ຕາຕະລາງອາທິດ, ຕາມມື້ ແລະ ເດືອນ ສຳລັບຄວາມຕ້ອງການອື່ນ.",
        "Đổi tuần, nhảy tới ngày": "ປ່ຽນອາທິດ, ໄປຫາມື້",
        "Dùng mũi tên để sang tuần khác, hoặc chọn ngày bất kỳ ở ô lịch: ngày đã chọn được tô nổi, có thể bật \"Chỉ ngày này\".": "ໃຊ້ລູກສອນເພື່ອໄປອາທິດອື່ນ, ຫຼື ເລືອກມື້ໃດກໍໄດ້ໃນຊ່ອງປະຕິທິນ: ມື້ທີ່ເລືອກຈະຖືກໄຮໄລທ໌, ເປີດ \"ສະເພາະມື້ນີ້\" ໄດ້.",
        "Lọc và tìm trong lịch": "ກອງ ແລະ ຄົ້ນຫາໃນຕາຕະລາງ",
        "Gõ tự do như \"b10 triết\" hoặc chọn theo chuyên đề, trạng thái.": "ພິມອິດສະຫຼະເຊັ່ນ \"b10 triết\" ຫຼື ເລືອກຕາມວິຊາ, ສະຖານະ.",
        "Xuất, nhập Excel": "ສົ່ງອອກ, ນຳເຂົ້າ Excel",
        "Xuất theo khoảng ngày bất kỳ, không cần chuyển lịch về đúng tháng đó. Tệp giữ nguyên bộ lọc đang dùng.": "ສົ່ງອອກຕາມຊ່ວງວັນທີໃດກໍໄດ້, ບໍ່ຕ້ອງປ່ຽນຕາຕະລາງໄປເດືອນນັ້ນ. ໄຟລ໌ຮັກສາຕົວກອງທີ່ໃຊ້ຢູ່.",
        "Hai sơ đồ": "ສອງແຜນຜັງ",
        "Tổ chức – Cán bộ và Đào tạo là hai sơ đồ độc lập.": "ການຈັດຕັ້ງ – ພະນັກງານ ແລະ ການຝຶກອົບຮົມ ແມ່ນສອງແຜນຜັງແຍກກັນ.",
        "Tìm trên sơ đồ": "ຄົ້ນຫາໃນແຜນຜັງ",
        "Gõ tự do, ví dụ \"tổ 1 châu\". Kết quả hiện ở cột riêng, số người khớp hiện trên từng nhánh.": "ພິມອິດສະຫຼະ, ຕົວຢ່າງ \"tổ 1 châu\". ຜົນສະແດງໃນຖັນແຍກ, ຈຳນວນຄົນທີ່ກົງສະແດງເທິງແຕ່ລະງ່າ.",
        "Duyệt từng cấp": "ເບິ່ງແຕ່ລະລະດັບ",
        "Bấm một tổ để thấy cột thành viên, rê chuột vào một người để xem thông tin khái quát. Mỗi cột có nút « để thu gọn khi thiếu chỗ.": "ກົດໜ່ວຍໜຶ່ງເພື່ອເຫັນຖັນສະມາຊິກ, ວາງເມົາສ໌ໃສ່ຄົນໜຶ່ງເພື່ອເບິ່ງຂໍ້ມູນຫຍໍ້. ແຕ່ລະຖັນມີປຸ່ມ « ເພື່ອຫຍໍ້ເມື່ອບ່ອນບໍ່ພໍ.",
        "Chi tiết đơn vị": "ລາຍລະອຽດໜ່ວຍງານ",
        "Lãnh đạo, giới thiệu, cơ cấu và danh sách của mục đang chọn.": "ຜູ້ນຳ, ການແນະນຳ, ໂຄງສ້າງ ແລະ ລາຍຊື່ຂອງລາຍການທີ່ເລືອກ.",
        "Chọn tuần": "ເລືອກອາທິດ",
        "Xem bảng công của tuần trước đó bằng mũi tên.": "ເບິ່ງຕາຕະລາງເຂົ້າວຽກຂອງອາທິດກ່ອນດ້ວຍລູກສອນ.",
        "Chọn khoa": "ເລືອກຄະນະ",
        "Lọc theo khoa. Báo cáo Excel cũng theo khoa đang chọn.": "ກອງຕາມຄະນະ. ລາຍງານ Excel ກໍຕາມຄະນະທີ່ເລືອກ.",
        "Bảng công": "ຕາຕະລາງເຂົ້າວຽກ",
        "Bấm vào ô để ghi bổ sung: có mặt, đi muộn, công tác, nghỉ phép, vắng.": "ກົດໃສ່ຊ່ອງເພື່ອບັນທຶກເພີ່ມ: ມາ, ມາຊ້າ, ໄປວຽກນອກ, ລາພັກ, ຂາດ.",
        "Báo cáo Excel": "ລາຍງານ Excel",
        "Chọn khoảng ngày bất kỳ. Tệp gồm tổng quan, phần cần chú ý, theo cán bộ, theo ngày và bảng công.": "ເລືອກຊ່ວງວັນທີໃດກໍໄດ້. ໄຟລ໌ປະກອບດ້ວຍພາບລວມ, ສ່ວນທີ່ຕ້ອງໃສ່ໃຈ, ຕາມພະນັກງານ, ຕາມມື້ ແລະ ຕາຕະລາງເຂົ້າວຽກ.",
        "Tự điểm danh": "ເຊັກຊື່ເອງ",
        "Cổng mở trước giờ học 30 phút và đóng sau giờ bắt đầu 15 phút.": "ເປີດເຊັກຊື່ກ່ອນຮຽນ 30 ນາທີ ແລະ ປິດຫຼັງເລີ່ມ 15 ນາທີ.",
        "Xuất kết quả học tập": "ສົ່ງອອກຜົນການຮຽນ",
        "Chọn nội dung (bảng điểm, điểm danh, lịch học, lịch thi) và khoảng ngày rồi tải tệp Excel.": "ເລືອກເນື້ອໃນ (ໃບຄະແນນ, ການເຊັກຊື່, ຕາຕະລາງຮຽນ, ຕາຕະລາງສອບ) ແລະ ຊ່ວງວັນທີ ແລ້ວດາວໂຫຼດໄຟລ໌ Excel.",
        "Kết quả và lịch": "ຜົນ ແລະ ຕາຕະລາງ",
        "Điểm theo học kỳ, lịch thi, lịch học tuần này và lịch sử điểm danh.": "ຄະແນນຕາມພາກຮຽນ, ຕາຕະລາງສອບ, ຕາຕະລາງຮຽນອາທິດນີ້ ແລະ ປະຫວັດການເຊັກຊື່.",
        "Điểm danh đi làm": "ລົງຊື່ເຂົ້າວຽກ",
        "Bấm mỗi ngày một lần. Trước 07:30 ghi Có mặt, sau đó ghi Đi muộn.": "ກົດມື້ລະຄັ້ງ. ກ່ອນ 07:30 ບັນທຶກມາ, ຫຼັງຈາກນັ້ນບັນທຶກມາຊ້າ.",
        "Báo cáo giảng dạy": "ລາຍງານການສອນ",
        "Xuất lịch dạy, tổng hợp theo lớp, buổi chưa điểm danh và công của bạn theo khoảng ngày bất kỳ.": "ສົ່ງອອກຕາຕະລາງສອນ, ສັງລວມຕາມຫ້ອງ, ຄາບທີ່ຍັງບໍ່ເຊັກຊື່ ແລະ ການເຂົ້າວຽກຂອງທ່ານຕາມຊ່ວງວັນທີໃດກໍໄດ້.",
        "Công việc của bạn": "ວຽກຂອງທ່ານ",
        "Buổi dạy hôm nay, lớp bạn dạy và buổi đã qua chưa điểm danh. Bấm Điểm danh hoặc Điểm để vào thẳng trang lớp.": "ຄາບສອນມື້ນີ້, ຫ້ອງທີ່ທ່ານສອນ ແລະ ຄາບທີ່ຜ່ານມາຍັງບໍ່ເຊັກຊື່. ກົດເຊັກຊື່ ຫຼື ຄະແນນເພື່ອເຂົ້າໜ້າຫ້ອງໂດຍກົງ."
    };
    const nhan = k => NUT[k][ngon()];
    const giamChuyenDong = () => window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    const duongDan = () => location.pathname.replace(/^\/+|\/+$/g, "");
    const thongTin = () => document.querySelector("[data-huong-dan-nguoi]");
    const hienRo = el => { if (!el) return false; const r = el.getBoundingClientRect(); return r.width > 0 && r.height > 0 && getComputedStyle(el).visibility !== "hidden"; };
    const phanTu = b => b.el || (b.chon ? document.querySelector(b.chon) : null);
    const coDich = b => !(b.chon || b.el) || hienRo(phanTu(b));

    function buocTrang() {
        const p = duongDan();
        const m = THEO_TRANG.find(x => x.khop.test(p));
        if (m) return m.buoc;
        return buocTuDong();
    }

    // Trang chưa có kịch bản riêng: dựng hướng dẫn từ chính bố cục trang đang mở
    // (đầu trang + mô tả, thao tác chính, tab/nhóm, tìm và lọc, vùng dữ liệu). Nhờ vậy nút lá cờ luôn nói về đúng trang này.
    function buocTuDong() {
        const vung = document.querySelector(".content") || document.body;
        const tim = (...ds) => { for (const c of ds) { const el = [...vung.querySelectorAll(c)].find(hienRo); if (el) return el; } return null; };
        const chuNgan = (el, n) => (el?.textContent || "").replace(/\s+/g, " ").trim().slice(0, n);
        const buoc = [];
        const dau = tim(".page-heading-row", ".bang-dau");
        if (dau) {
            const ten = chuNgan(dau.querySelector("h1"), 80);
            const moTa = chuNgan(dau.querySelector(".page-heading p, p"), 220);
            buoc.push({ el: dau, tieuDe: [ten || "Trang này", ten || "This page"], noiDung: [moTa || "Tên trang và phần việc chính ở đây.", moTa || "The page title and what it is for."] });
        }
        const thaoTac = tim(".thao-tac-dau-trang");
        if (thaoTac) {
            const nut = [...thaoTac.querySelectorAll("button, a")].filter(hienRo).map(b => chuNgan(b, 30)).filter(Boolean).slice(0, 4);
            buoc.push({ el: thaoTac, tieuDe: ["Thao tác chính", "Main actions"], noiDung: [nut.length ? `Ở đây có: ${nut.join(", ")}. Bạn chỉ thấy nút mà vai trò của mình được dùng.` : "Các nút thêm mới, nhập và xuất Excel của trang.", "Add, import and export buttons. You only see what your role allows."] });
        }
        const tab = tim(".rz-tabview-nav", "[role=tablist]", ".tb-chuyen-muc", ".hv-tab-he", ".chon-che-do");
        if (tab) buoc.push({ el: tab, tieuDe: ["Chọn nhóm hoặc chế độ xem", "Tabs and views"], noiDung: ["Bấm để chuyển nhóm dữ liệu hoặc cách xem; nội dung bên dưới đổi theo lựa chọn.", "Switch groups or views; the content below follows your choice."] });
        const loc = tim(".panel-toolbar", ".thanh-lich", ".bo-loc-tin", ".search-box");
        if (loc) buoc.push({ el: loc, tieuDe: ["Tìm và lọc", "Search and filter"], noiDung: ["Gõ tự do, không cần dấu; kết quả khớp nhất xếp lên đầu. Dùng các ô lọc để thu hẹp danh sách.", "Type freely; the best matches come first. Use filters to narrow the list."] });
        const duLieu = tim(".directory-grid", ".bang-cuon", ".luoi-the-lop", "table", ".the-bieu-do", ".rz-card");
        if (duLieu && duLieu !== dau) buoc.push({ el: duLieu, tieuDe: ["Dữ liệu của trang", "The data"], noiDung: ["Bấm tiêu đề cột để sắp xếp, rê chuột vào tên để xem thẻ thông tin nhanh, bấm mã để mở chi tiết.", "Sort by column, hover a name for a quick card, select a code for details."] });
        return buoc;
    }

    function batDau(che) {
        if (dangChay) return;
        const vaiTro = thongTin()?.dataset.huongDanVaiTro || "Guest";
        const trang = buocTrang();
        cheDo = che;
        cacBuoc = (cheDo === "chung" ? CHUNG(vaiTro) : cheDo === "day-du" ? [...CHUNG(vaiTro), ...trang] : trang).filter(coDich);
        // Trang không có vùng nào để chỉ (hiếm): một bước giữa màn hình nói tên trang, không chuyển sang giới thiệu chung.
        if (cacBuoc.length === 0 && cheDo === "trang") cacBuoc = [{ tieuDe: [document.title.split("|")[0].trim(), document.title.split("|")[0].trim()], noiDung: ["Trang này chưa có vùng nào cần hướng dẫn thêm.", "Nothing on this page needs a walkthrough."] }];
        if (cacBuoc.length === 0) return;
        dongThe();
        if (cheDo !== "trang") danhDau(KHOA_CHUNG);
        if (cheDo !== "chung") danhDau(khoaTrang());
        capNhatGoiY();
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
        if (!cuoi) nut.append(taoNut("hd-phu", nhan("boQua"), dong));
        const phai = document.createElement("span"); phai.className = "hd-nut-phai";
        if (chiSo > 0) phai.append(taoNut("hd-lui", nhan("lui"), lui));
        const nutTiep = taoNut("hd-tiep", cuoi ? nhan("xong") : nhan("tiep"), tiep);
        phai.append(nutTiep); nut.append(phai);
        pop.append(dau, h, p, tien, nut);
        const el = phanTu(b);
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
        const el = phanTu(b);
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

    // ---------- Ghi nhận theo tài khoản ----------
    function danhDau(khoa) {
        if (!khoa || daXem?.has(khoa)) return;
        daXem?.add(khoa);
        cauNoi?.invokeMethodAsync("DanhDauHuongDan", khoa).catch(() => { });
    }
    // Khoá hướng dẫn của trang đang mở: trang có kịch bản riêng, hoặc kịch bản chung cho mọi trang danh sách.
    function khoaTrang() {
        const p = duongDan().toLowerCase();
        const m = THEO_TRANG.find(x => x.khop.test(p));
        if (!m) return null;
        // Trang có mã trong đường dẫn (lớp B10D58, hệ chinh-quy…) dùng chung một khoá của mẫu, không mỗi lớp một khoá.
        return "trang:" + (m.khoa || p);
    }
    const tenNguoi = () => thongTin()?.dataset.huongDanNguoi || "";

    /** Gọi từ MainLayout sau khi dựng xong: cầu nối .NET và danh sách khoá đã xem của tài khoản. */
    function ketNoi(ref, ds) {
        cauNoi = ref; daXem = new Set(ds || []);
        // Chuyển tiếp từ bản cũ: ai đã xem trên trình duyệt này (localStorage) thì coi như đã xem, không mời lại.
        try { if (localStorage.getItem(KHOA + tenNguoi()) === "1") danhDau(KHOA_CHUNG); } catch { }
        kiemTra();
    }

    function kiemTra() {
        if (!daXem || !thongTin()) return;
        if (!daXem.has(KHOA_CHUNG)) moiNguoiMoi();
        capNhatGoiY();
    }

    // Chấm nhỏ trên nút lá cờ: trang có hướng dẫn riêng mà tài khoản chưa xem. Chỉ gợi ý sau khi đã qua phần giới thiệu chung.
    function capNhatGoiY() {
        const nut = document.querySelector("[data-mo-huong-dan]");
        if (!nut || !daXem) return;
        const k = khoaTrang();
        const co = daXem.has(KHOA_CHUNG) && !!k && !daXem.has(k);
        nut.classList.toggle("co-goi-y", co);
        nut.title = co ? nhan("goiYTrang") : nhan("tieuDeNut");
    }

    // ---------- Thẻ mời người mới (không chặn thao tác) ----------
    const KHOA_DE_SAU = "huongDan.deSau.";
    function moiNguoiMoi() {
        if (the || dangChay) return;
        try { if (sessionStorage.getItem(KHOA_DE_SAU + tenNguoi())) return; } catch { }
        setTimeout(() => {
            if (the || dangChay || daXem?.has(KHOA_CHUNG)) return;
            if (document.querySelector(".rz-dialog")) { setTimeout(moiNguoiMoi, 3000); return; }
            the = document.createElement("section");
            the.className = "hd-the-moi"; the.setAttribute("translate", "no");
            the.setAttribute("role", "dialog"); the.setAttribute("aria-labelledby", "hd-moi-tieu-de");
            const bt = document.createElement("span"); bt.className = "hd-moi-bieu-tuong"; bt.setAttribute("aria-hidden", "true"); bt.textContent = "👋";
            const h = document.createElement("h2"); h.id = "hd-moi-tieu-de"; h.textContent = nhan("moiTieuDe");
            const p = document.createElement("p"); p.textContent = nhan("moiNoiDung");
            const nut = document.createElement("div"); nut.className = "hd-nut";
            const xem = taoNut("hd-tiep", nhan("moiXem"), () => { dongThe(); batDau("chung"); });
            const sau = taoNut("hd-lui", nhan("moiDeSau"), deSau);
            nut.append(xem, sau);
            const khong = taoNut("hd-phu hd-moi-khong", nhan("moiKhong"), () => { danhDau(KHOA_CHUNG); dongThe(); capNhatGoiY(); });
            const ghi = document.createElement("small"); ghi.textContent = nhan("moiGhiChu");
            const x = taoNut("hd-moi-dong", "×", deSau); x.setAttribute("aria-label", nhan("moiDeSau"));
            the.append(x, bt, h, p, nut, khong, ghi);
            the.addEventListener("keydown", e => { if (e.key === "Escape") { e.preventDefault(); deSau(); } });
            document.body.appendChild(the);
            xem.focus({ preventScroll: true });
        }, giamChuyenDong() ? 300 : 1200);
    }
    function deSau() {
        const ten = tenNguoi();
        try {
            sessionStorage.setItem(KHOA_DE_SAU + ten, "1");
            const lan = (+localStorage.getItem(KHOA_DE_SAU + ten) || 0) + 1;
            localStorage.setItem(KHOA_DE_SAU + ten, String(lan));
            if (lan >= 3) danhDau(KHOA_CHUNG); // hoãn 3 lần: coi như không cần, thôi mời
        } catch { }
        dongThe();
    }
    function dongThe() { the?.remove(); the = null; }

    // Đổi trang trong ứng dụng (Blazor không tải lại trang): cập nhật chấm gợi ý theo trang mới.
    new MutationObserver(() => {
        if (location.pathname === duongDanTruoc) return;
        duongDanTruoc = location.pathname;
        setTimeout(kiemTra, 600);
    }).observe(document.documentElement, { childList: true, subtree: true });

    document.addEventListener("click", e => {
        // Nút lá cờ: hướng dẫn trang đang mở. Giới thiệu chung chỉ mở từ thẻ mời người mới hoặc nút ở trang Trợ giúp.
        if (e.target.closest?.("[data-mo-huong-dan-chung]")) batDau("chung");
        else if (e.target.closest?.("[data-mo-huong-dan]")) batDau("trang");
    });

    return { batDau, dong, ketNoi };
})();
