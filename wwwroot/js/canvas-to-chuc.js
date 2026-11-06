// Nguồn: module khung nhìn canvas của repo LTMT (sh4mr0ck17/LTMT, wwwroot/js/canvas-to-chuc.js), dùng lại cho sơ đồ tổ chức.
// Tương tác khung nhìn chạy ngay tại trình duyệt, không gửi từng bước kéo lên server.
// Tỉ lệ nhỏ nhất giữ tên đơn vị đọc được (tên phóng 24px × 0.5 ≈ 12px); sơ đồ lớn hơn thì căn theo nút đang chọn thay vì thu nhỏ.
export const GioiHan = Object.freeze({ nhoNhat: 0.5, lonNhat: 2, buocPhong: 1.2, le: 32 });

// Ngưỡng mức chi tiết (semantic zoom): dưới "thap" chỉ hiện loại + tên + badge; từ "cao" hiện đầy đủ.
export const NguongChiTiet = Object.freeze({ thap: 0.6, cao: 0.85 });

// Tỉ lệ tối thiểu khi đưa một nút vào giữa khung nhìn để nút đó đọc được.
export const TiLeDocDuoc = 0.75;

export function tinhMucChiTiet(tiLe) {
    if (tiLe < NguongChiTiet.thap) { return 'thap'; }
    if (tiLe < NguongChiTiet.cao) { return 'vua'; }
    return 'cao';
}

/** Tỉ lệ cần để vừa trọn sơ đồ, chưa áp giới hạn (dùng để biết sơ đồ có quá lớn không). */
export function tinhTiLeVuaKhungGoc(rong, cao, rongSoDo, caoSoDo) {
    return Math.min(1, Math.max(1, rong - GioiHan.le * 2) / Math.max(1, rongSoDo),
        Math.max(1, cao - GioiHan.le * 2) / Math.max(1, caoSoDo));
}

export function gioiHanTiLe(tiLe) {
    return Math.min(GioiHan.lonNhat, Math.max(GioiHan.nhoNhat, tiLe));
}

export function tinhVuaKhung(rong, cao, rongSoDo, caoSoDo) {
    const tiLe = gioiHanTiLe(Math.min(1, Math.max(1, rong - GioiHan.le * 2) / Math.max(1, rongSoDo),
        Math.max(1, cao - GioiHan.le * 2) / Math.max(1, caoSoDo)));
    return { tiLe, x: (rong - rongSoDo * tiLe) / 2, y: (cao - caoSoDo * tiLe) / 2 };
}

export function tinhPhongTai(trangThai, tiLeMoi, x, y) {
    const tiLe = gioiHanTiLe(tiLeMoi);
    const heSo = tiLe / trangThai.tiLe;
    return { tiLe, x: x - (x - trangThai.x) * heSo, y: y - (y - trangThai.y) * heSo };
}

export function khoiTao(khung) {
    const panel = khung.closest('[data-canvas-panel]');
    const vungToanManHinh = panel.closest('.tc-khong-gian-canvas') || panel;
    const san = khung.querySelector('[data-canvas-stage]');
    const nhanTiLe = panel.querySelector('[data-canvas-zoom]');
    const nutToanManHinh = panel.querySelector('[data-canvas-action="toan-man-hinh"]');
    const suKien = new AbortController();
    const tuyChon = { signal: suKien.signal };
    const conTro = new Map();
    let trangThai = { x: 0, y: 0, tiLe: 1 };
    let vuaKhung = true;
    let khoaDangChon = null;
    let lanCanCu = null;
    let dauBoCucCu = null;
    let kichThuocCu = { rong: khung.clientWidth, cao: khung.clientHeight };
    let coHuy = false;
    khung.classList.add('tc-canvas-san-sang');

    function ve() {
        if (coHuy) { return; }
        san.style.transform = `translate(${trangThai.x}px, ${trangThai.y}px) scale(${trangThai.tiLe})`;
        san.dataset.lod = tinhMucChiTiet(trangThai.tiLe);
        nhanTiLe.textContent = `${Math.round(trangThai.tiLe * 100)}%`;
        panel.querySelector('[data-canvas-action="thu-nho"]').disabled = trangThai.tiLe <= GioiHan.nhoNhat;
        panel.querySelector('[data-canvas-action="phong-to"]').disabled = trangThai.tiLe >= GioiHan.lonNhat;
    }

    function canKhung() {
        const tiLeGoc = tinhTiLeVuaKhungGoc(khung.clientWidth, khung.clientHeight, san.offsetWidth, san.offsetHeight);
        if (tiLeGoc < GioiHan.nhoNhat) {
            // Sơ đồ quá lớn: không thu nhỏ vô hạn, căn theo nút đang chọn (hoặc gốc) ở tỉ lệ nhỏ nhất còn đọc được.
            trangThai.tiLe = GioiHan.nhoNhat;
            const nutGoc = san.querySelector('[data-canvas-node]');
            const khoa = khoaDangChon && timNut(khoaDangChon) ? khoaDangChon : nutGoc?.dataset.canvasNode;
            if (khoa) { veNut(khoa, false); }
            else { ve(); }
            vuaKhung = true;
            return;
        }
        trangThai = tinhVuaKhung(khung.clientWidth, khung.clientHeight, san.offsetWidth, san.offsetHeight);
        vuaKhung = true;
        ve();
    }

    function timNut(khoa) {
        return Array.from(san.querySelectorAll('[data-canvas-node]')).find(n => n.dataset.canvasNode === khoa);
    }

    function veNut(khoa, damBaoDocDuoc = true) {
        const nut = timNut(khoa);
        if (!nut) { return; }
        // Nút được tìm phải đọc được ngay cả khi trước đó đã thu nhỏ toàn sơ đồ.
        if (damBaoDocDuoc) { trangThai.tiLe = Math.max(TiLeDocDuoc, trangThai.tiLe); }
        trangThai.x = khung.clientWidth / 2 - (nut.offsetLeft + nut.offsetWidth / 2) * trangThai.tiLe;
        trangThai.y = khung.clientHeight / 2 - (nut.offsetTop + nut.offsetHeight / 2) * trangThai.tiLe;
        vuaKhung = false;
        ve();
    }

    function phong(tiLe, x = khung.clientWidth / 2, y = khung.clientHeight / 2) {
        trangThai = tinhPhongTai(trangThai, tiLe, x, y);
        vuaKhung = false;
        ve();
    }

    khung.addEventListener('wheel', e => {
        e.preventDefault();
        const bien = khung.getBoundingClientRect();
        phong(trangThai.tiLe * Math.exp(-Math.max(-100, Math.min(100, e.deltaY)) * 0.006), e.clientX - bien.left, e.clientY - bien.top);
    }, { ...tuyChon, passive: false });

    khung.addEventListener('pointerdown', e => {
        if ((e.button !== 0 && e.button !== 1) || e.target.closest('button, a, input, select')) { return; }
        e.preventDefault();
        khung.focus({ preventScroll: true });
        khung.setPointerCapture(e.pointerId);
        conTro.set(e.pointerId, { x: e.clientX, y: e.clientY });
        khung.classList.add('tc-canvas-dang-keo');
    }, tuyChon);

    khung.addEventListener('pointermove', e => {
        const cu = conTro.get(e.pointerId);
        if (!cu) { return; }
        const moi = { x: e.clientX, y: e.clientY };
        if (conTro.size === 2) {
            const khac = Array.from(conTro.entries()).find(([ma]) => ma !== e.pointerId)[1];
            const khoangCu = Math.hypot(cu.x - khac.x, cu.y - khac.y);
            const khoangMoi = Math.hypot(moi.x - khac.x, moi.y - khac.y);
            const bien = khung.getBoundingClientRect();
            if (khoangCu > 0) {
                trangThai = tinhPhongTai(trangThai, trangThai.tiLe * khoangMoi / khoangCu,
                    (cu.x + khac.x) / 2 - bien.left, (cu.y + khac.y) / 2 - bien.top);
            }
            trangThai.x += (moi.x - cu.x) / 2;
            trangThai.y += (moi.y - cu.y) / 2;
        } else {
            trangThai.x += moi.x - cu.x;
            trangThai.y += moi.y - cu.y;
        }
        conTro.set(e.pointerId, moi);
        vuaKhung = false;
        ve();
    }, tuyChon);

    function ketThucKeo(e) {
        conTro.delete(e.pointerId);
        if (khung.hasPointerCapture(e.pointerId)) { khung.releasePointerCapture(e.pointerId); }
        if (conTro.size === 0) { khung.classList.remove('tc-canvas-dang-keo'); }
    }
    khung.addEventListener('pointerup', ketThucKeo, tuyChon);
    khung.addEventListener('pointercancel', ketThucKeo, tuyChon);
    khung.addEventListener('lostpointercapture', ketThucKeo, tuyChon);

    khung.addEventListener('keydown', e => {
        if (e.target !== khung || e.ctrlKey || e.metaKey || e.altKey) { return; }
        const buoc = e.shiftKey ? 120 : 40;
        switch (e.key) {
            case '+': case '=': phong(trangThai.tiLe * GioiHan.buocPhong); break;
            case '-': case '_': phong(trangThai.tiLe / GioiHan.buocPhong); break;
            case '0': case 'Home': canKhung(); break;
            case 'ArrowLeft': trangThai.x += buoc; vuaKhung = false; ve(); break;
            case 'ArrowRight': trangThai.x -= buoc; vuaKhung = false; ve(); break;
            case 'ArrowUp': trangThai.y += buoc; vuaKhung = false; ve(); break;
            case 'ArrowDown': trangThai.y -= buoc; vuaKhung = false; ve(); break;
            default: return;
        }
        e.preventDefault();
    }, tuyChon);

    khung.addEventListener('focusin', e => {
        const nut = e.target.closest('[data-canvas-node]');
        if (!nut) { return; }
        const bien = nut.getBoundingClientRect();
        const vung = khung.getBoundingClientRect();
        if (bien.left < vung.left || bien.right > vung.right || bien.top < vung.top || bien.bottom > vung.bottom) {
            veNut(nut.dataset.canvasNode);
        }
    }, tuyChon);

    panel.addEventListener('click', async e => {
        const nut = e.target.closest('[data-canvas-action]');
        if (!nut) { return; }
        switch (nut.dataset.canvasAction) {
            case 'phong-to': phong(trangThai.tiLe * GioiHan.buocPhong); break;
            case 'thu-nho': phong(trangThai.tiLe / GioiHan.buocPhong); break;
            case 'vua-khung': canKhung(); break;
            case 'nut-dang-chon': veNut(khoaDangChon); break;
            case 'toan-man-hinh':
                try {
                    if (document.fullscreenElement === vungToanManHinh) { await document.exitFullscreen(); }
                    else { await vungToanManHinh.requestFullscreen(); }
                } catch {
                    nut.title = 'Trình duyệt chưa hỗ trợ chế độ toàn màn hình.';
                }
                break;
        }
    }, tuyChon);

    document.addEventListener('fullscreenchange', () => {
        const coToanManHinh = document.fullscreenElement === vungToanManHinh;
        nutToanManHinh.setAttribute('aria-pressed', String(coToanManHinh));
        nutToanManHinh.title = coToanManHinh ? 'Thoát toàn màn hình (Esc)' : 'Toàn màn hình';
    }, tuyChon);
    if (!vungToanManHinh.requestFullscreen) { nutToanManHinh.disabled = true; }

    const theoDoiKichThuoc = new ResizeObserver(() => {
        if (vuaKhung) { canKhung(); }
        else {
            trangThai.x += (khung.clientWidth - kichThuocCu.rong) / 2;
            trangThai.y += (khung.clientHeight - kichThuocCu.cao) / 2;
            ve();
        }
        kichThuocCu = { rong: khung.clientWidth, cao: khung.clientHeight };
    });
    theoDoiKichThuoc.observe(khung);

    return {
        capNhat(khoa, lanCan, dauBoCuc) {
            if (coHuy) { return; }
            const canLai = lanCanCu !== lanCan;
            const doiViTri = dauBoCucCu !== dauBoCuc;
            const doiLuaChon = khoaDangChon !== khoa;
            khoaDangChon = khoa;
            lanCanCu = lanCan;
            dauBoCucCu = dauBoCuc;
            if (canLai) { canKhung(); }
            else if ((doiViTri || doiLuaChon) && khoa) { veNut(khoa, doiLuaChon); }
            else { ve(); }
        },
        huy() {
            coHuy = true;
            suKien.abort();
            theoDoiKichThuoc.disconnect();
            conTro.clear();
            khung.classList.remove('tc-canvas-san-sang', 'tc-canvas-dang-keo');
        }
    };
}
