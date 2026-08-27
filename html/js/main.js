//所有记录
var clipObj = [];

//最大记录数
var maxRecords = 300;

//是否处于搜索模式
var searchMode = false;

//搜索值
var searchValue = "";

//单记录粘贴index
var selectIndex = 0;

//shift是否按下,用来处理范围粘贴
var isShiftPressed = false;
var rangeStartIndex = -1;
var rangeEndIndex = -1;

//alt是否按下,用来处理多记录粘贴
var isCtrlPressed = false;
//多记录粘贴列表
var multiIndexList = []


//存储到localStorage间隔
var storeInterval;

//记录行数
var length = 0;

//屏蔽鼠标选择操作
document.onselectstart = function(e) {
    e.returnValue = false;
};
//屏蔽右键菜单
document.oncontextmenu = function(e) {
    e.preventDefault();
};

$(document).ready(function() {
    $(".content").niceScroll(".table_main", {
        cursorborder: "",
        cursoropacitymin: 0,
        cursoropacitymax: 0.7,
        cursorwidth: "2px",
        cursorcolor: "#808080"
    });

    $(document).on("keydown", keyDown);
    $(document).on("keyup", keyUp);
    $(window).on("keyup", keyUp);

    //查找
    $("#searchInput").on("input", function(event) {
        var value = $("#searchInput")
            .val()
            .toLowerCase();
        searchValue = value;
        displayData();
        $(".tr_selected").removeClass("tr_selected");
        $("#tr0").addClass("tr_selected");
    });

    // 初始化按键下拉框
    for (var k = 65; k <= 90; k++) {
        var char = String.fromCharCode(k);
        $("#hkKeySelect").append("<option value='" + k + "'>" + char + "</option>");
    }

    $("#btnCancelHotkey").on("click", function() {
        $("#hotkeyModal").css("display", "none");
    });

    $("#btnSaveHotkey").on("click", function() {
        var mod = 0;
        if ($("#hkWin").is(":checked")) mod |= 8;
        if ($("#hkAlt").is(":checked")) mod |= 1;
        if ($("#hkCtrl").is(":checked")) mod |= 2;
        if ($("#hkShift").is(":checked")) mod |= 4;

        if (mod === 0) {
            alert("请至少选择一个修饰键 (Win / Alt / Ctrl / Shift)");
            return;
        }

        var key = parseInt($("#hkKeySelect").val());
        var dataStr = JSON.stringify({ Modifier: mod, Key: key });
        window.chrome.webview.postMessage("SaveHotkey|" + encodeURIComponent(dataStr));
        $("#hotkeyModal").css("display", "none");
    });

    window.chrome.webview.addEventListener('message', function(event) {
        var msg = event.data;
        if (typeof msg === 'string') {
            try {
                msg = JSON.parse(msg);
            } catch (e) {
                console.error("Failed to parse webview message:", e);
            }
        }
        if (!msg || typeof msg !== 'object') return;

        if (msg.type === 'history') {
            clipObj = msg.data || [];
            displayData();
        } else if (msg.type === 'add') {
            addData(msg.data);
        } else if (msg.type === 'hotkeySettings') {
            openHotkeyModal(msg.data.Modifier, msg.data.Key);
        } else if (msg.type === 'show') {
            show();
        } else if (msg.type === 'changeSkin') {
            if (Array.isArray(msg.css)) {
                $('link[rel="stylesheet"]').remove();
                msg.css.forEach(function(href) {
                    var link = $('<link>', {
                        rel: 'stylesheet',
                        type: 'text/css',
                        href: href + '?v=' + new Date().getTime()
                    });
                    $('head').append(link);
                });
            }
        }
    });

    displayData();
    window.chrome.webview.postMessage("ready|1");
});

function openHotkeyModal(mod, key) {
    $("#hkWin").prop("checked", (mod & 8) !== 0);
    $("#hkAlt").prop("checked", (mod & 1) !== 0);
    $("#hkCtrl").prop("checked", (mod & 2) !== 0);
    $("#hkShift").prop("checked", (mod & 4) !== 0);
    $("#hkKeySelect").val(key || 86);
    $("#hotkeyModal").css("display", "flex");
}

function keyDown(event) {
    if ($("#hotkeyModal").is(":visible")) {
        if (event.keyCode == 27) { // ESC inside modal
            $("#hotkeyModal").css("display", "none");
        }
        return;
    }

    if (event.keyCode == 27) {
        //esc
        if (searchMode) {
            hideSearch();
        }
        window.chrome.webview.postMessage("esc|1");
    } else if (event.keyCode == 13) {
        //回车直接粘贴当前选中项
        if (searchMode) {
            pasteValue($("#tr0").attr("index") / 1, true);
        } else {
            pasteValue(selectIndex, true);
        }
    } else if (event.ctrlKey && event.keyCode == 70) {
        toggleSearch();
    } else if (!searchMode) {
        if (event.shiftKey) {
            //范围操作
            if (!isShiftPressed) {
                isShiftPressed = true;
                $(".tr_selected").removeClass("tr_selected");
            }

        } else if (event.ctrlKey) {
            //多条操作
            if (!isCtrlPressed) {
                isCtrlPressed = true;
                $(".tr_selected").removeClass("tr_selected");
            }

        } else if (event.keyCode >= 49 && event.keyCode <= 57) {
            //数字键
            pasteValue(event.keyCode - 49, true);
        } else if (event.keyCode >= 65 && event.keyCode <= 90) {
            //字母键

            pasteValue(event.keyCode - 56, true);
        } else if (event.keyCode == 32) {
            //空格直接粘贴第0项
            event.preventDefault();
            pasteValue(0, true);
        } else if (event.keyCode == 8 || event.keyCode == 46) {
            //退格或者del键删除

            del(selectIndex);
        }

    }
}


function keyUp(event) {
    if (event.key == "Shift" || event.keyCode == 16) {
        if (rangeStartIndex >= 0) {
            if (rangeEndIndex >= 0 && rangeEndIndex !== rangeStartIndex) {
                // 松开 Shift 时，以第 1 次点击（rangeStartIndex）和最后一次点击（rangeEndIndex）为准连续粘贴
                pasteValueByRange(rangeStartIndex, rangeEndIndex);
            } else {
                // 仅点击了 1 项
                pasteValue(rangeStartIndex);
            }
        }
        rangeStartIndex = -1;
        rangeEndIndex = -1;
        isShiftPressed = false;
        $(".tr_selected").removeClass("tr_selected");
    } else if (event.key == "Control" || event.keyCode == 17) {
        if (multiIndexList.length > 0) {
            pasteMultiValue();
        }
        multiIndexList = [];
        isCtrlPressed = false;
        $(".tr_selected").removeClass("tr_selected");
    }
}

function toggleSearch() {
    if (searchMode) {
        hideSearch();
    } else {
        showSearch();
    }
}
//显示搜索框
function showSearch() {
    $("#searchDiv").css("display", "block");
    $("#searchInput").focus();

    searchMode = true;
}
//隐藏搜索框
function hideSearch() {
    $("#searchDiv").css("display", "none");
    searchMode = false;

    if ($("#searchInput").val() != "") {
        $("#searchInput").val("");
        searchValue = "";
        displayData();
    }
}

//选中时高亮
function trSelect(event) {
    var index = event.getAttribute("index") / 1;
    selectIndex = index;

    if (!isShiftPressed && !isCtrlPressed) {
        $(".tr_selected").removeClass("tr_selected");

    }
}

//滚动到顶部
function scrollTop() {
    $(".content").scrollTop(0);
}

function scrollDown() {
    var div = $(".content");
    var tr = $("#tr" + selectIndex);
    if (tr.offset().top + tr.height() > div.height()) {
        div.scrollTop(tr.height() + div.scrollTop());
    }
}

function scrollUp() {
    var div = $(".content");
    var tr = $("#tr" + selectIndex);
    if (tr.offset().top < 0) {
        div.scrollTop(div.scrollTop() - tr.height());
    }
}
//数字转换成字母
function num2key(num) {
    return String.fromCharCode(55 + num);
}

//显示记录
function displayData() {
    var tbody = "";

    var matchCount = -1;

    for (var i = 0; i < clipObj.length; i++) {
        if (clipObj[i] == null) {
            clipObj.splice(i, 1);
            i--;
        }
        var trs = "";
        var num = "";

        if (
            searchValue == "" ||
            clipObj[i].Type == searchValue ||
            clipObj[i].Type != "image" && clipObj[i].ClipValue.toLowerCase().indexOf(searchValue) >= 0
        ) {
            matchCount++;
            if (matchCount < 9) {
                num = "<u>" + (matchCount + 1) + "</u>";
            } else if (matchCount < 35) {
                num = "<u>" + num2key(matchCount + 1) + "</u>";
            } else {
                num = "" + (matchCount + 1);
            }
            if (clipObj[i].Type == "image") {

                trs =
                    " <tr style='cursor: default' index='" +
                    i +
                    "' id='tr" +
                    matchCount +
                    "' onmouseup ='mouseup(this)'  onmouseenter='trSelect(this)'> <td  class='td_content' > <img class='image' src='data:image/png;base64," +
                    clipObj[i].ClipValue +
                    "' /> </td><td class='td_index'  >" +
                    num +
                    "</td> </tr>";

            } else {
                let displayStr = clipObj[i].DisplayValue;
                if (typeof wechatEmojis !== 'undefined') {
                    displayStr = displayStr.replace(/\[.*?\]/g, function(match) {
                        if (wechatEmojis[match]) {
                            return "<img src='" + wechatEmojis[match] + "' style='width:20px;height:20px;vertical-align:-4px;margin:0 2px;' />";
                        }
                        return match;
                    });
                }
                
                trs =
                    " <tr style='cursor: default' index='" +
                    i +
                    "' id='tr" +
                    matchCount +
                    "' onmouseup ='mouseup(this)'  onmouseenter='trSelect(this)' > <td  class='td_content' >  " +
                    displayStr +
                    " </td><td class='td_index'  >" +
                    num +
                    "</td> </tr>";
            }
        }
        tbody += trs;
    }


    if (matchCount == -1) {
        tbody = " <tr style='cursor: default'> <td  class='td_content' style='cursor: default;height:30px;' > 无记录 </td> </tr>";

    }
    $(".myTable").html(tbody);


    $(".content").getNiceScroll().resize();

}

//设置保存最大记录数
function setMaxRecords(records) {
    if (records <= 0) return;
    maxRecords = records;
    if (clipObj.length > maxRecords) {
        clipObj = clipObj.slice(0, maxRecords);
        displayData();
    }

}

//增加条目
function addData(obj) {

    if (obj == null) {
        return;
    }


    for (var i = 0; i < clipObj.length; i++) {
        if (clipObj[i].ClipValue == obj.ClipValue) {
            clipObj.splice(i, 1);
            break;
        }
    }

    clipObj.splice(0, 0, obj);

    if (clipObj.length > maxRecords) {
        clipObj.splice(clipObj.length - 1, 1)[0];
    }
    displayData();

}


//显示时初始化状态
function show() {
    rangeStartIndex = -1;
    rangeEndIndex = -1;
    isShiftPressed = false;
    isCtrlPressed = false;
    if (searchMode) {
        hideSearch();
    }
    scrollTop();

    if (clipObj.length != 0) {
        selectIndex = 1;
        $(".tr_selected").removeClass("tr_selected");
        $("#tr" + selectIndex).addClass("tr_selected");
    }

    $(".content").getNiceScroll().resize();
    $(".content")[0].focus();
}

//粘贴选择项
function mouseup(e) {
    var event = window.event;
    var clickedIndex = e.getAttribute("index") / 1;

    if (event.button == 0 || event.button == 2) {
        if (event.shiftKey || isShiftPressed) {
            isShiftPressed = true;
            if (rangeStartIndex === -1) {
                // 第 1 次点击：确定起始项
                rangeStartIndex = clickedIndex;
                rangeEndIndex = -1;
                $(".tr_selected").removeClass("tr_selected");
                $("#" + e.id).addClass("tr_selected");
            } else {
                // 后续多次点击：以第 1 次点击和本次最后一次点击为准，动态高亮其间的所有项（取消其余项的选中效果）
                rangeEndIndex = clickedIndex;
                var minIdx = Math.min(rangeStartIndex, rangeEndIndex);
                var maxIdx = Math.max(rangeStartIndex, rangeEndIndex);
                $(".tr_selected").removeClass("tr_selected");
                for (var k = minIdx; k <= maxIdx; k++) {
                    $("#tr" + k).addClass("tr_selected");
                }
            }
            // 按住 Shift 期间不执行粘贴，等松开 Shift 键 (keyUp) 时才执行！
        } else if (event.ctrlKey || isCtrlPressed) {
            var keyIndex = multiIndexList.indexOf(clickedIndex);
            if (keyIndex == -1) {
                multiIndexList.push(clickedIndex);
                $("#" + e.id).addClass("tr_selected");
            } else {
                multiIndexList.splice(keyIndex, 1);
                $("#" + e.id).removeClass("tr_selected");
            }
        } else {
            rangeStartIndex = -1;
            rangeEndIndex = -1;
            selectIndex = clickedIndex;
            pasteValue(clickedIndex);
        }
    } else if (event.button == 1) {
        rangeStartIndex = -1;
        rangeEndIndex = -1;
        setToClipBoard(clickedIndex);
    }
}


// 回调本地代码

//粘贴单条
function pasteValue(index) {
    if (!clipObj || !clipObj[index]) return;
    var obj = clipObj[index];
    
    clipObj.splice(index, 1)[0];
    clipObj.splice(0, 0, obj);
    
    window.chrome.webview.postMessage(
        "PasteValue|" + encodeURIComponent(JSON.stringify(obj))
    );

    displayData();
}

//设置到剪切板但不粘贴
function setToClipBoard(index) {
    if (!clipObj || !clipObj[index]) return;
    var obj = clipObj[index];
    clipObj.splice(index, 1)[0];
    clipObj.splice(0, 0, obj);

    window.chrome.webview.postMessage(
        "SetToClipBoard|" + encodeURIComponent(JSON.stringify(obj))
    );

    displayData();
}

//粘贴多条 (Ctrl 多选)
function pasteMultiValue() {
    if (!multiIndexList || multiIndexList.length === 0) return;
    var clipList = [];
    multiIndexList.forEach(function(index) {
        if (clipObj[index]) {
            clipList.push(clipObj[index]);
        }
    });

    if (clipList.length === 0) return;

    var sortedDesc = multiIndexList.slice().sort(function(a, b) { return b - a; });
    sortedDesc.forEach(function(idx) {
        clipObj.splice(idx, 1);
    });
    for (var j = clipList.length - 1; j >= 0; j--) {
        clipObj.unshift(clipList[j]);
    }

    window.chrome.webview.postMessage(
        "PasteValueList|" + encodeURIComponent(JSON.stringify(clipList))
    );

    displayData();
}

//粘贴范围 (Shift 范围)
function pasteValueByRange(startIndex, endIndex) {
    if (!clipObj || clipObj.length === 0) return;

    var start = Math.max(0, Math.min(startIndex, clipObj.length - 1));
    var end = Math.max(0, Math.min(endIndex, clipObj.length - 1));

    if (start === end) {
        pasteValue(start);
        return;
    }

    var clipList = [];
    var indices = [];
    if (start <= end) {
        // 从前往后选：按正向顺序粘贴 (1 -> 2 -> 3)
        for (var i = start; i <= end; i++) {
            if (clipObj[i]) {
                clipList.push(clipObj[i]);
                indices.push(i);
            }
        }
    } else {
        // 从后往前选：按反向顺序粘贴 (5 -> 4 -> 3 -> 2 -> 1)
        for (var i = start; i >= end; i--) {
            if (clipObj[i]) {
                clipList.push(clipObj[i]);
                indices.push(i);
            }
        }
    }

    if (clipList.length === 0) return;

    // 从后往前删除选中的索引，避免移位
    var sortedDesc = indices.slice().sort(function(a, b) { return b - a; });
    sortedDesc.forEach(function(idx) {
        clipObj.splice(idx, 1);
    });
    for (var j = clipList.length - 1; j >= 0; j--) {
        clipObj.unshift(clipList[j]);
    }

    window.chrome.webview.postMessage(
        "PasteValueList|" + encodeURIComponent(JSON.stringify(clipList))
    );

    displayData();
}

function del(index) {
    clipObj.splice(index, 1)[0];
}

function search(value) {
    window.chrome.webview.postMessage("search|" + value);
}






function clear() {
    clipObj = [];
    displayData();
}