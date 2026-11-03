const cacThe = new WeakMap();

export function dong(the) {
    const donDep = cacThe.get(the);
    if (donDep) donDep();
    cacThe.delete(the);
    if (the?.isConnected && the.matches(":popover-open")) the.hidePopover();
}

export function mo(ten, the) {
    dong(the);
    if (!ten?.isConnected || !the?.isConnected || the.hidden) return;
    // Top layer giúp hồ sơ không bị cắt bởi vùng cuộn của bảng.
    the.showPopover();
    function canChinh() {
        if (!ten.isConnected || !the.isConnected) { dong(the); return; }
        const neo = ten.getBoundingClientRect();
        const hop = the.getBoundingClientRect();
        const le = 12, cach = 10;
        const benPhai = neo.right + cach;
        const benTrai = neo.left - hop.width - cach;
        const x = benPhai + hop.width <= window.innerWidth - le ? benPhai
            : benTrai >= le ? benTrai : le;
        the.style.left = `${Math.max(le, Math.min(x, window.innerWidth - hop.width - le))}px`;
        the.style.top = `${Math.max(le, Math.min(neo.top, window.innerHeight - hop.height - le))}px`;
    }
    canChinh();
    const quanSat = new ResizeObserver(canChinh);
    quanSat.observe(the);
    window.addEventListener("resize", canChinh);
    document.addEventListener("scroll", canChinh, true);
    cacThe.set(the, () => {
        quanSat.disconnect();
        window.removeEventListener("resize", canChinh);
        document.removeEventListener("scroll", canChinh, true);
    });
}
