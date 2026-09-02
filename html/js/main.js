/**
 * ClipOne - Modern Native Frontend Controller
 * Zero-dependency ES6+ implementation for WebView2 / Photino
 */

const ClipApp = {
    // 状态管理
    state: {
        clips: [],            // 完整剪贴板列表 ClipModel[]
        maxRecords: 300,      // 最大保存条目数
        searchMode: false,    // 搜索模式开关
        searchValue: "",      // 当前搜索关键词
        selectIndex: 0,       // 当前选中的项在当前可见列表中的索引 (0-based)
        filteredIndices: [],  // 可见项对应 state.clips 的真实索引列表 number[]
        
        // 范围多选 (Shift) - 基于可见列表的索引 (vIndex)
        isShiftPressed: false,
        rangeStartVIndex: -1,
        rangeEndVIndex: -1,
        
        // 离散多选 (Ctrl) - 存储真实条目索引 (clipIdx)
        isCtrlPressed: false,
        multiIndexList: [],   // 真实索引列表 number[]
        
        // 搜索防抖定时器
        searchDebounceTimer: null,
        
        // 阻尼弹性回弹
        bounceOffset: 0,
        bounceTimer: null,
        isBouncing: false
    },

    // DOM 元素缓存
    dom: {},

    /**
     * 初始化
     */
    init() {
        this.cacheDom();
        this.initHotkeySelect();
        this.bindEvents();
        this.initBounceScroll();
        this.displayData();
        
        // 通知 C# 宿主页面就绪
        this.postMessage("ready|1");
    },

    /**
     * 缓存 DOM 节点引用
     */
    cacheDom() {
        this.dom = {
            content: document.querySelector(".content"),
            table: document.getElementById("table_main"),
            tbody: document.querySelector(".myTable"),
            searchDiv: document.getElementById("searchDiv"),
            searchInput: document.getElementById("searchInput"),
            trayMenuModal: document.getElementById("trayMenuModal"),
            trayMenuContainer: document.querySelector(".tray-menu-container"),
            skinSubmenu: document.getElementById("skinSubmenu"),
            themeSubmenu: document.getElementById("themeSubmenu"),
            trayItemSkin: document.getElementById("trayItemSkin"),
            trayItemTheme: document.getElementById("trayItemTheme"),
            trayStartupCheck: document.getElementById("trayStartupCheck"),
            radioThemeSystem: document.getElementById("radioThemeSystem"),
            radioThemeLight: document.getElementById("radioThemeLight"),
            radioThemeDark: document.getElementById("radioThemeDark"),
            hotkeyModal: document.getElementById("hotkeyModal"),
            hkWin: document.getElementById("hkWin"),
            hkAlt: document.getElementById("hkAlt"),
            hkCtrl: document.getElementById("hkCtrl"),
            hkShift: document.getElementById("hkShift"),
            hkKeySelect: document.getElementById("hkKeySelect"),
            btnCancelHotkey: document.getElementById("btnCancelHotkey"),
            btnSaveHotkey: document.getElementById("btnSaveHotkey")
        };
    },

    /**
     * 初始化热键下拉框 (A-Z)
     */
    initHotkeySelect() {
        const select = this.dom.hkKeySelect;
        if (!select) return;
        select.innerHTML = "";
        for (let k = 65; k <= 90; k++) {
            const char = String.fromCharCode(k);
            const opt = document.createElement("option");
            opt.value = k.toString();
            opt.textContent = char;
            select.appendChild(opt);
        }
    },

    /**
     * 事件监听绑定
     */
    bindEvents() {
        // 屏蔽浏览器默认的鼠标划选与右键菜单
        document.addEventListener("selectstart", (e) => e.preventDefault());
        document.addEventListener("contextmenu", (e) => e.preventDefault());

        // 全局键盘监听
        document.addEventListener("keydown", (e) => this.handleKeyDown(e));
        document.addEventListener("keyup", (e) => this.handleKeyUp(e));
        window.addEventListener("keyup", (e) => this.handleKeyUp(e));

        // 搜索输入防抖监听
        if (this.dom.searchInput) {
            this.dom.searchInput.addEventListener("input", () => {
                clearTimeout(this.state.searchDebounceTimer);
                this.state.searchDebounceTimer = setTimeout(() => {
                    this.state.searchValue = this.dom.searchInput.value.trim().toLowerCase();
                    this.displayData();
                    this.updateSelection(0, false);
                }, 60);
            });
        }

        // 热键弹窗按钮
        if (this.dom.btnCancelHotkey) {
            this.dom.btnCancelHotkey.addEventListener("click", () => {
                this.dom.hotkeyModal.style.display = "none";
            });
        }
        if (this.dom.btnSaveHotkey) {
            this.dom.btnSaveHotkey.addEventListener("click", () => this.saveHotkey());
        }

        // 托盘菜单交互
        if (this.dom.trayItemSkin) {
            this.dom.trayItemSkin.addEventListener("click", (e) => {
                e.stopPropagation();
                this.toggleSubmenu(this.dom.skinSubmenu, this.dom.trayItemSkin);
            });
        }
        if (this.dom.trayItemTheme) {
            this.dom.trayItemTheme.addEventListener("click", (e) => {
                e.stopPropagation();
                this.toggleSubmenu(this.dom.themeSubmenu, this.dom.trayItemTheme);
            });
        }

        // 托盘菜单项动作委托
        if (this.dom.trayMenuModal) {
            this.dom.trayMenuModal.addEventListener("click", (e) => {
                const item = e.target.closest(".tray-menu-item[data-action]");
                if (item) {
                    e.stopPropagation();
                    const action = item.getAttribute("data-action");
                    if (action) {
                        this.postMessage("TrayAction|" + action);
                    }
                }
            });
        }

        // 表格条目事件委托 (鼠标悬停高亮与点击粘贴)
        if (this.dom.tbody) {
            this.dom.tbody.addEventListener("mouseover", (e) => {
                const tr = e.target.closest("tr[data-vindex]");
                if (!tr) return;
                const vindex = parseInt(tr.getAttribute("data-vindex"), 10);
                if (!isNaN(vindex) && !this.state.isShiftPressed && !this.state.isCtrlPressed) {
                    this.state.selectIndex = vindex;
                    this.highlightRow(vindex);
                }
            });

            this.dom.tbody.addEventListener("mouseup", (e) => {
                const tr = e.target.closest("tr[data-vindex]");
                if (!tr) return;
                this.handleRowMouseUp(e, tr);
            });
        }

        // WebView2 IPC 消息监听
        if (window.chrome && window.chrome.webview) {
            window.chrome.webview.addEventListener("message", (event) => this.handleWebviewMessage(event));
        }
    },

    /**
     * 发送 IPC 消息给 C# 后端
     */
    postMessage(msg) {
        if (window.chrome && window.chrome.webview) {
            window.chrome.webview.postMessage(msg);
        }
    },

    /**
     * 处理 WebView2 传入的消息
     */
    handleWebviewMessage(event) {
        let msg = event.data;
        if (typeof msg === "string") {
            try {
                msg = JSON.parse(msg);
            } catch (e) {
                console.error("Failed to parse webview message:", e);
                return;
            }
        }
        if (!msg || typeof msg !== "object") return;

        switch (msg.type) {
            case "history":
                this.state.clips = msg.data || [];
                this.displayData();
                break;
            case "add":
                this.addData(msg.data);
                break;
            case "hotkeySettings":
                this.openHotkeyModal(msg.data.Modifier, msg.data.Key);
                break;
            case "show":
                this.show();
                break;
            case "hide":
                this.hide();
                break;
            case "showTrayMenu":
                this.showTrayMenu(msg.data);
                break;
            case "changeSkin":
                if (Array.isArray(msg.css)) {
                    document.querySelectorAll('link[rel="stylesheet"]:not([href*="common.css"])').forEach((el) => el.remove());
                    msg.css.forEach((href) => {
                        const link = document.createElement("link");
                        link.rel = "stylesheet";
                        link.type = "text/css";
                        link.href = href + "?v=" + Date.now();
                        document.head.appendChild(link);
                    });
                }
                break;
        }
    },

    /**
     * 键盘按下事件处理
     */
    handleKeyDown(e) {
        // 热键弹窗激活状态下的 ESC 退出
        if (this.dom.hotkeyModal && this.dom.hotkeyModal.style.display !== "none") {
            if (e.key === "Escape") {
                e.preventDefault();
                this.dom.hotkeyModal.style.display = "none";
            }
            return;
        }

        const visibleCount = this.state.filteredIndices.length;

        // 1. ESC: 优先退出搜索并恢复列表，再次按才关闭主界面
        if (e.key === "Escape") {
            e.preventDefault();
            if (this.state.searchMode) {
                this.hideSearch();
                return;
            }
            this.postMessage("esc|1");
            return;
        }

        // 2. Ctrl + F: 开启/切换搜索框
        if (e.ctrlKey && (e.key === "f" || e.key === "F")) {
            e.preventDefault();
            e.stopPropagation();
            this.toggleSearch();
            return;
        }

        // 3. 方向键导航与快速跳转
        if (e.key === "ArrowDown") {
            e.preventDefault();
            this.navigate(1);
            return;
        }
        if (e.key === "ArrowUp") {
            e.preventDefault();
            this.navigate(-1);
            return;
        }
        if (e.key === "PageDown") {
            e.preventDefault();
            this.navigate(5);
            return;
        }
        if (e.key === "PageUp") {
            e.preventDefault();
            this.navigate(-5);
            return;
        }
        if (e.key === "Home") {
            e.preventDefault();
            this.updateSelection(0);
            return;
        }
        if (e.key === "End") {
            e.preventDefault();
            this.updateSelection(Math.max(0, visibleCount - 1));
            return;
        }

        // 4. 回车键: 粘贴当前高亮选中的项
        if (e.key === "Enter") {
            e.preventDefault();
            if (visibleCount > 0) {
                const targetClipIndex = this.state.filteredIndices[this.state.selectIndex];
                if (targetClipIndex !== undefined) {
                    this.pasteValue(targetClipIndex);
                }
            }
            return;
        }

        // 非搜索模式下的快捷热键
        if (!this.state.searchMode) {
            // Shift 范围多选标记
            if (e.key === "Shift") {
                if (!this.state.isShiftPressed) {
                    this.state.isShiftPressed = true;
                    this.clearSelectionHighlights();
                }
                return;
            }

            // Ctrl 离散多选标记
            if (e.key === "Control") {
                if (!this.state.isCtrlPressed) {
                    this.state.isCtrlPressed = true;
                    this.clearSelectionHighlights();
                }
                return;
            }

            // 数字快捷键 1-9
            if (e.key >= "1" && e.key <= "9" && !e.ctrlKey && !e.altKey && !e.metaKey) {
                const num = parseInt(e.key, 10) - 1;
                if (num < visibleCount) {
                    e.preventDefault();
                    this.pasteValue(this.state.filteredIndices[num]);
                }
                return;
            }

            // 字母快捷键 A-Z
            if (e.key.length === 1 && !e.ctrlKey && !e.altKey && !e.metaKey) {
                const charCode = e.key.toUpperCase().charCodeAt(0);
                if (charCode >= 65 && charCode <= 90) {
                    // A -> index 9, B -> index 10, ...
                    const letterIdx = charCode - 65 + 9;
                    if (letterIdx < visibleCount) {
                        e.preventDefault();
                        this.pasteValue(this.state.filteredIndices[letterIdx]);
                    }
                    return;
                }
            }

            // 空格键直接粘贴第 0 项
            if (e.key === " " || e.code === "Space") {
                e.preventDefault();
                if (visibleCount > 0) {
                    this.pasteValue(this.state.filteredIndices[0]);
                }
                return;
            }

            // Delete / Backspace 删除当前选中项
            if (e.key === "Delete" || e.key === "Backspace") {
                e.preventDefault();
                if (visibleCount > 0) {
                    const targetClipIndex = this.state.filteredIndices[this.state.selectIndex];
                    if (targetClipIndex !== undefined) {
                        this.deleteItem(targetClipIndex);
                    }
                }
                return;
            }
        }
    },

    /**
     * 键盘松开事件处理 (完成范围粘贴或多项粘贴)
     */
    handleKeyUp(e) {
        if (e.key === "Shift") {
            if (this.state.rangeStartVIndex >= 0) {
                if (this.state.rangeEndVIndex >= 0 && this.state.rangeEndVIndex !== this.state.rangeStartVIndex) {
                    this.pasteValueByRange(this.state.rangeStartVIndex, this.state.rangeEndVIndex);
                } else {
                    const clipIdx = this.state.filteredIndices[this.state.rangeStartVIndex];
                    if (clipIdx !== undefined) {
                        this.pasteValue(clipIdx);
                    }
                }
            }
            this.state.rangeStartVIndex = -1;
            this.state.rangeEndVIndex = -1;
            this.state.isShiftPressed = false;
            this.clearSelectionHighlights();
        } else if (e.key === "Control") {
            if (this.state.multiIndexList.length > 0) {
                this.pasteMultiValue();
            }
            this.state.multiIndexList = [];
            this.state.isCtrlPressed = false;
            this.clearSelectionHighlights();
        }
    },

    /**
     * 切换选中项索引并自动平滑滚动
     */
    navigate(delta) {
        const total = this.state.filteredIndices.length;
        if (total === 0) return;
        let next = this.state.selectIndex + delta;
        if (next < 0) next = 0;
        if (next >= total) next = total - 1;
        this.updateSelection(next, true);
    },

    /**
     * 更新选中项状态并滚动到可视区域
     */
    updateSelection(newIndex, autoScroll = true) {
        this.state.selectIndex = newIndex;
        this.highlightRow(newIndex);
        if (autoScroll) {
            this.scrollToSelection();
        }
    },

    /**
     * 高亮指定可见行
     */
    highlightRow(vIndex) {
        this.clearSelectionHighlights();
        const tr = document.getElementById("tr" + vIndex);
        if (tr) {
            tr.classList.add("tr_selected");
        }
    },

    /**
     * 清除选中高亮
     */
    clearSelectionHighlights() {
        document.querySelectorAll(".tr_selected").forEach((el) => el.classList.remove("tr_selected"));
    },

    /**
     * 滚动至选中条目
     */
    scrollToSelection() {
        const tr = document.getElementById("tr" + this.state.selectIndex);
        if (tr) {
            tr.scrollIntoView({ behavior: "smooth", block: "nearest" });
        }
    },

    /**
     * 表格单行鼠标抬起事件
     */
    handleRowMouseUp(e, tr) {
        const clipIdx = parseInt(tr.getAttribute("data-clip-index"), 10);
        const vIdx = parseInt(tr.getAttribute("data-vindex"), 10);
        if (isNaN(clipIdx) || isNaN(vIdx)) return;

        // 左键 (0) 或右键 (2)
        if (e.button === 0 || e.button === 2) {
            if (e.shiftKey || this.state.isShiftPressed) {
                this.state.isShiftPressed = true;
                if (this.state.rangeStartVIndex === -1) {
                    this.state.rangeStartVIndex = vIdx;
                    this.state.rangeEndVIndex = -1;
                    this.clearSelectionHighlights();
                    tr.classList.add("tr_selected");
                } else {
                    this.state.rangeEndVIndex = vIdx;
                    const minV = Math.min(this.state.rangeStartVIndex, this.state.rangeEndVIndex);
                    const maxV = Math.max(this.state.rangeStartVIndex, this.state.rangeEndVIndex);
                    this.clearSelectionHighlights();
                    for (let v = minV; v <= maxV; v++) {
                        const row = document.getElementById("tr" + v);
                        if (row) row.classList.add("tr_selected");
                    }
                }
            } else if (e.ctrlKey || this.state.isCtrlPressed) {
                const foundPos = this.state.multiIndexList.indexOf(clipIdx);
                if (foundPos === -1) {
                    this.state.multiIndexList.push(clipIdx);
                    tr.classList.add("tr_selected");
                } else {
                    this.state.multiIndexList.splice(foundPos, 1);
                    tr.classList.remove("tr_selected");
                }
            } else {
                this.state.rangeStartVIndex = -1;
                this.state.rangeEndVIndex = -1;
                this.state.selectIndex = vIdx;
                this.pasteValue(clipIdx);
            }
        } else if (e.button === 1) {
            // 中键点击：设入剪贴板但不执行粘贴动作
            this.state.rangeStartVIndex = -1;
            this.state.rangeEndVIndex = -1;
            this.setToClipboard(clipIdx);
        }
    },

    /**
     * 渲染剪贴板记录列表
     */
    displayData() {
        if (!this.dom.tbody) return;
        const clips = this.state.clips;
        const search = this.state.searchValue;
        const filtered = [];
        let html = "";

        for (let i = 0; i < clips.length; i++) {
            const item = clips[i];
            if (!item) continue;

            // 过滤判断
            const isMatch =
                search === "" ||
                item.Type === search ||
                (item.Type !== "image" && item.ClipValue && item.ClipValue.toLowerCase().includes(search));

            if (isMatch) {
                const vIndex = filtered.length;
                filtered.push(i);

                let numBadge = "";
                if (vIndex < 9) {
                    numBadge = `<u>${vIndex + 1}</u>`;
                } else if (vIndex < 35) {
                    numBadge = `<u>${String.fromCharCode(55 + (vIndex + 1))}</u>`;
                } else {
                    numBadge = `${vIndex + 1}`;
                }

                if (item.Type === "image") {
                    html += `
                    <tr style="cursor: default" data-clip-index="${i}" data-vindex="${vIndex}" id="tr${vIndex}">
                        <td class="td_content">
                            <img class="image" loading="lazy" src="data:image/png;base64,${item.ClipValue}" alt="clip image" />
                        </td>
                        <td class="td_index">${numBadge}</td>
                    </tr>`;
                } else {
                    let displayStr = item.DisplayValue || "";
                    if (typeof wechatEmojis !== "undefined") {
                        displayStr = displayStr.replace(/\[.*?\]/g, (match) => {
                            if (wechatEmojis[match]) {
                                return `<img src="${wechatEmojis[match]}" style="width:20px;height:20px;vertical-align:-4px;margin:0 2px;" alt="${match}" />`;
                            }
                            return match;
                        });
                    }

                    html += `
                    <tr style="cursor: default" data-clip-index="${i}" data-vindex="${vIndex}" id="tr${vIndex}">
                        <td class="td_content">${displayStr}</td>
                        <td class="td_index">${numBadge}</td>
                    </tr>`;
                }
            }
        }

        this.state.filteredIndices = filtered;

        if (filtered.length === 0) {
            html = `<tr style="cursor: default"><td class="td_content" style="cursor: default; height: 36px; text-align: center; color: var(--text-muted);">无记录</td></tr>`;
        }

        this.dom.tbody.innerHTML = html;

        // 重新恢复选中高亮
        if (this.state.selectIndex >= filtered.length) {
            this.state.selectIndex = Math.max(0, filtered.length - 1);
        }
        if (filtered.length > 0) {
            this.highlightRow(this.state.selectIndex);
        }
    },

    /**
     * 新增剪贴板条目
     */
    addData(obj) {
        if (!obj) return;
        const clips = this.state.clips;

        // 排重已有条目
        for (let i = 0; i < clips.length; i++) {
            if (clips[i] && clips[i].ClipValue === obj.ClipValue) {
                clips.splice(i, 1);
                break;
            }
        }

        // 新增至首位
        clips.unshift(obj);

        // 限制最大条数
        if (clips.length > this.state.maxRecords) {
            clips.length = this.state.maxRecords;
        }

        this.displayData();
    },

    /**
     * 窗口显示时状态初始化与渲染就绪同步
     */
    show() {
        if (this.dom.trayMenuModal) this.dom.trayMenuModal.style.display = "none";
        if (this.dom.content) this.dom.content.style.display = "block";

        this.state.rangeStartVIndex = -1;
        this.state.rangeEndVIndex = -1;
        this.state.isShiftPressed = false;
        this.state.isCtrlPressed = false;

        if (this.state.searchMode) {
            this.hideSearch();
        }

        if (this.dom.content) {
            this.dom.content.scrollTop = 0;
            this.dom.content.focus();
        }

        if (this.state.clips.length > 0) {
            this.updateSelection(0, false);
        }

        // 双帧等待确保 Compositor 已提交渲染后再通知 C# 掀开透明度
        requestAnimationFrame(() => {
            requestAnimationFrame(() => {
                this.postMessage("shown|1");
            });
        });
    },

    /**
     * 窗口隐藏时在后台静默重置状态
     */
    hide() {
        this.state.rangeStartVIndex = -1;
        this.state.rangeEndVIndex = -1;
        this.state.isShiftPressed = false;
        this.state.isCtrlPressed = false;

        if (this.state.searchMode) {
            this.hideSearch();
        }

        if (this.dom.content) {
            this.dom.content.scrollTop = 0;
        }

        if (this.state.clips.length > 0) {
            this.updateSelection(0, false);
        }
    },

    /**
     * 搜索模式控制
     */
    toggleSearch() {
        if (this.state.searchMode) {
            this.hideSearch();
        } else {
            this.showSearch();
        }
    },

    showSearch() {
        if (!this.dom.searchDiv || !this.dom.searchInput) return;
        this.dom.searchDiv.style.display = "flex";
        this.dom.searchInput.focus();
        this.state.searchMode = true;
    },

    hideSearch() {
        if (!this.dom.searchDiv || !this.dom.searchInput) return;
        this.dom.searchDiv.style.display = "none";
        this.state.searchMode = false;
        if (this.dom.searchInput.value !== "") {
            this.dom.searchInput.value = "";
            this.state.searchValue = "";
            this.displayData();
        }
        if (this.dom.content) {
            this.dom.content.focus();
        }
    },

    /**
     * 粘贴单条
     */
    pasteValue(index) {
        const item = this.state.clips[index];
        if (!item) return;

        // 调整至首位
        this.state.clips.splice(index, 1);
        this.state.clips.unshift(item);

        this.postMessage("PasteValue|" + encodeURIComponent(JSON.stringify(item)));
        this.displayData();
    },

    /**
     * 设入剪贴板但不执行粘贴
     */
    setToClipboard(index) {
        const item = this.state.clips[index];
        if (!item) return;

        this.state.clips.splice(index, 1);
        this.state.clips.unshift(item);

        this.postMessage("SetToClipBoard|" + encodeURIComponent(JSON.stringify(item)));
        this.displayData();
    },

    /**
     * 离散多项粘贴 (Ctrl 多选)
     */
    pasteMultiValue() {
        const list = this.state.multiIndexList;
        if (!list || list.length === 0) return;

        const clipList = [];
        list.forEach((idx) => {
            if (this.state.clips[idx]) {
                clipList.push(this.state.clips[idx]);
            }
        });

        if (clipList.length === 0) return;

        // 从后往前删除选中的条目
        const sortedDesc = list.slice().sort((a, b) => b - a);
        sortedDesc.forEach((idx) => this.state.clips.splice(idx, 1));

        // 从后往前插入至顶部
        for (let j = clipList.length - 1; j >= 0; j--) {
            this.state.clips.unshift(clipList[j]);
        }

        this.postMessage("PasteValueList|" + encodeURIComponent(JSON.stringify(clipList)));
        this.displayData();
    },

    /**
     * 范围连续粘贴 (Shift 范围多选) - 严格基于当前可见过滤列表进行范围提取
     */
    pasteValueByRange(startVIndex, endVIndex) {
        const filtered = this.state.filteredIndices;
        if (!filtered || filtered.length === 0) return;

        const start = Math.max(0, Math.min(startVIndex, filtered.length - 1));
        const end = Math.max(0, Math.min(endVIndex, filtered.length - 1));

        if (start === end) {
            const idx = filtered[start];
            if (idx !== undefined) this.pasteValue(idx);
            return;
        }

        const clipList = [];
        const indices = [];

        if (start <= end) {
            for (let v = start; v <= end; v++) {
                const clipIdx = filtered[v];
                if (clipIdx !== undefined && this.state.clips[clipIdx]) {
                    clipList.push(this.state.clips[clipIdx]);
                    indices.push(clipIdx);
                }
            }
        } else {
            for (let v = start; v >= end; v--) {
                const clipIdx = filtered[v];
                if (clipIdx !== undefined && this.state.clips[clipIdx]) {
                    clipList.push(this.state.clips[clipIdx]);
                    indices.push(clipIdx);
                }
            }
        }

        if (clipList.length === 0) return;

        // 从后往前删除选中的真实条目索引，避免移位
        const sortedDesc = indices.slice().sort((a, b) => b - a);
        sortedDesc.forEach((idx) => this.state.clips.splice(idx, 1));
        for (let j = clipList.length - 1; j >= 0; j--) {
            this.state.clips.unshift(clipList[j]);
        }

        this.postMessage("PasteValueList|" + encodeURIComponent(JSON.stringify(clipList)));
        this.displayData();
    },

    /**
     * 删除单条
     */
    deleteItem(index) {
        if (!this.state.clips || index < 0 || index >= this.state.clips.length) return;
        const deletedItem = this.state.clips.splice(index, 1)[0];
        if (deletedItem && deletedItem.Id) {
            this.postMessage("del|" + deletedItem.Id);
        } else {
            this.postMessage("delIndex|" + index);
        }
        this.displayData();
    },

    /**
     * 显示托盘菜单
     */
    showTrayMenu(data) {
        if (!data) return;

        const skins = data.Skins || data.skins || [];
        const currentSkin = data.CurrentSkin || data.currentSkin || "";
        const currentThemeMode = data.CurrentThemeMode || data.currentThemeMode || "System";
        const autoStartup = data.AutoStartup !== undefined ? data.AutoStartup : data.autoStartup;

        // 重置子菜单
        if (this.dom.skinSubmenu) this.dom.skinSubmenu.style.display = "none";
        if (this.dom.themeSubmenu) this.dom.themeSubmenu.style.display = "none";
        document.querySelectorAll(".tray-arrow").forEach((el) => el.classList.remove("expanded"));

        // 渲染皮肤子项
        if (this.dom.skinSubmenu && Array.isArray(skins)) {
            let html = "";
            skins.forEach((skin) => {
                const isChecked = skin.toLowerCase() === currentSkin.toLowerCase();
                html += `
                <div class="tray-menu-item tray-sub-item" data-action="setSkin|${skin}">
                    <span class="tray-radio-indicator ${isChecked ? "checked" : ""}"></span>
                    <span class="tray-text">${skin}</span>
                </div>`;
            });
            this.dom.skinSubmenu.innerHTML = html;
        }

        // 渲染主题模式单选
        const modeLower = currentThemeMode.toLowerCase();
        if (this.dom.radioThemeSystem) this.dom.radioThemeSystem.classList.toggle("checked", modeLower === "system");
        if (this.dom.radioThemeLight) this.dom.radioThemeLight.classList.toggle("checked", modeLower === "light");
        if (this.dom.radioThemeDark) this.dom.radioThemeDark.classList.toggle("checked", modeLower === "dark");

        // 渲染开机自启复选
        if (this.dom.trayStartupCheck) this.dom.trayStartupCheck.classList.toggle("checked", !!autoStartup);

        // 切换显示
        this.hideSearch();
        if (this.dom.content) this.dom.content.style.display = "none";
        if (this.dom.trayMenuModal) this.dom.trayMenuModal.style.display = "flex";
    },

    /**
     * 展开/折叠托盘子菜单
     */
    toggleSubmenu(submenuEl, parentItemEl) {
        if (!submenuEl) return;
        const isHidden = submenuEl.style.display === "none" || !submenuEl.style.display;
        submenuEl.style.display = isHidden ? "flex" : "none";
        if (parentItemEl) {
            const arrow = parentItemEl.querySelector(".tray-arrow");
            if (arrow) arrow.classList.toggle("expanded", isHidden);
        }

        setTimeout(() => {
            if (this.dom.trayMenuContainer) {
                const h = this.dom.trayMenuContainer.offsetHeight + 8;
                this.postMessage("ResizeTrayMenu|" + h);
            }
        }, 50);
    },

    /**
     * 打开热键设置弹窗
     */
    openHotkeyModal(mod, key) {
        if (!this.dom.hotkeyModal) return;
        if (this.dom.hkWin) this.dom.hkWin.checked = (mod & 8) !== 0;
        if (this.dom.hkAlt) this.dom.hkAlt.checked = (mod & 1) !== 0;
        if (this.dom.hkCtrl) this.dom.hkCtrl.checked = (mod & 2) !== 0;
        if (this.dom.hkShift) this.dom.hkShift.checked = (mod & 4) !== 0;
        if (this.dom.hkKeySelect) this.dom.hkKeySelect.value = (key || 86).toString();
        this.dom.hotkeyModal.style.display = "flex";
    },

    /**
     * 保存热键设置
     */
    saveHotkey() {
        let mod = 0;
        if (this.dom.hkWin && this.dom.hkWin.checked) mod |= 8;
        if (this.dom.hkAlt && this.dom.hkAlt.checked) mod |= 1;
        if (this.dom.hkCtrl && this.dom.hkCtrl.checked) mod |= 2;
        if (this.dom.hkShift && this.dom.hkShift.checked) mod |= 4;

        if (mod === 0) {
            alert("请至少选择一个修饰键 (Win / Alt / Ctrl / Shift)");
            return;
        }

        const key = parseInt(this.dom.hkKeySelect.value, 10);
        const dataStr = JSON.stringify({ Modifier: mod, Key: key });
        this.postMessage("SaveHotkey|" + encodeURIComponent(dataStr));
        this.dom.hotkeyModal.style.display = "none";
    },

    /**
     * 顶底丝滑弹性阻尼回弹 (Rubber-band Bounce)
     */
    initBounceScroll() {
        const content = this.dom.content;
        const table = this.dom.table;
        if (!content || !table) return;

        content.addEventListener(
            "wheel",
            (e) => {
                const atTop = content.scrollTop <= 0;
                const atBottom = content.scrollTop + content.clientHeight >= content.scrollHeight - 1;

                if ((atTop && e.deltaY < 0) || (atBottom && e.deltaY > 0)) {
                    const direction = e.deltaY < 0 ? 1 : -1;
                    const added = Math.min(Math.abs(e.deltaY) * 0.18, 10);
                    this.state.bounceOffset = Math.max(-36, Math.min(36, this.state.bounceOffset + direction * added));

                    table.style.transition = "none";
                    table.style.transform = `translate3d(0, ${this.state.bounceOffset}px, 0)`;
                    this.state.isBouncing = true;

                    clearTimeout(this.state.bounceTimer);
                    this.state.bounceTimer = setTimeout(() => {
                        table.style.transition = "transform 0.4s cubic-bezier(0.25, 1, 0.5, 1)";
                        table.style.transform = "translate3d(0, 0px, 0)";
                        this.state.bounceOffset = 0;
                        setTimeout(() => {
                            if (this.state.bounceOffset === 0) {
                                table.style.transition = "";
                                this.state.isBouncing = false;
                            }
                        }, 400);
                    }, 60);
                } else if (this.state.isBouncing && this.state.bounceOffset !== 0) {
                    table.style.transition = "transform 0.25s ease-out";
                    table.style.transform = "translate3d(0, 0px, 0)";
                    this.state.bounceOffset = 0;
                    this.state.isBouncing = false;
                }
            },
            { passive: true }
        );
    }
};

// 页面加载完成后启动
if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", () => ClipApp.init());
} else {
    ClipApp.init();
}