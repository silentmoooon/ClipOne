//是否处于搜索模式
var searchMode = false;

//单记录粘贴index
var selectIndex = 0;

//shift是否按下,用来处理范围粘贴
var isShiftPressed = false;
var rangeStartIndex = -1;

//alt是否按下,用来处理多记录粘贴
var isCtrlPressed = false;
//多记录粘贴列表
var multiIndexList = []
    //多记录粘贴时是否将粘贴记录发送到顶部,默认为true
var multiSendToTop = true;

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

     $("body").on("keydown", keyDown);
    $("body").on("keyup", keyUp);

    //查找
    $("#searchInput").on("input", function(event) {
        var value = $("#searchInput")
            .val()
            .toLowerCase();
        search(value);
        $(".tr_selected").removeClass("tr_selected");
        $("#tr0").addClass("tr_selected");
    });
   



});


function keyDown(event) {
   
    if (event.keyCode == 27) {
        //esc
        if (searchMode) {
            hideSearch();
        }
        window.external.notify("esc|1");
    } else if (event.keyCode == 13) {
        //回车直接粘贴当前选中项
        if (searchMode) {
            pasteValue($("#tr0").attr("index") / 1, true);
        } else {
            pasteValue(selectIndex, true);
        }
    }  else if (event.ctrlKey && event.keyCode == 70) {
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

    if (event.key == "Shift") {
        rangeStartIndex = -1;
        isShiftPressed = false;
    } else if (event.key == "Control") {
        if (multiIndexList.length > 0) {
            pasteMultiValue();
        }
        multiIndexList = []
        isCtrlPressed = false;

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
    $("#searchInput")[0].focus();

    searchMode = true;
}
//隐藏搜索框
function hideSearch() {
    $("#searchDiv").css("display", "none");
    searchMode = false;

    if ($("#searchInput").val() != "") {
        $("#searchInput").val("");
        search("");
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
function applyData(html, count) {
    length = count;
    html = decodeURIComponent(html.replace(/\+/g, "%20"));
    $(".myTable").html(html);

    $(".content").getNiceScroll().resize();
    changeWindowHeight();


}

//粘贴选择项
function mouseup(e) {
    var event = window.event;
    if (event.button == 0 || event.button == 2) {

        if (isShiftPressed) {
            var sendToTop = true;
            if (event.button == 2) {
                sendToTop = false;
            }
            //范围
            if (rangeStartIndex == -1) {
                $("#" + e.id).addClass("tr_selected");
                rangeStartIndex = e.getAttribute("index") / 1;
            } else {
                pasteValueByRange(rangeStartIndex, e.getAttribute("index") / 1, sendToTop);
            }
        } else if (isCtrlPressed) {
            multiSendToTop = event.button == 0

            var key = e.getAttribute("index") / 1;
            var keyIndex = multiIndexList.indexOf(key);
            if (keyIndex == -1) {
                multiIndexList.push(key);
                $("#" + e.id).addClass("tr_selected");
            } else {
                multiIndexList.splice(keyIndex, 1)
                $("#" + e.id).removeClass("tr_selected");
            }
        } else {
            //单条

            var sendToTop = true;
            if (event.button == 2) {
                sendToTop = false;
            }
            pasteValue(e.getAttribute("index") / 1, sendToTop);
        }
    } else if (event.button == 1) {
        setToClipBoard(e.getAttribute("index") / 1);
    }

}

//显示记录
function show() {
    rangeStartIndex = -1;
    isShiftPressed = false;
    isCtrlPressed = false;
    if (searchMode) {
        hideSearch();
    }
    scrollTop();


    if (length != 0) {

        selectIndex = 1;

        $(".tr_selected").removeClass("tr_selected");
        $("#tr" + selectIndex).addClass("tr_selected");

    }
     $(".content")[0].focus();
    $(".content").getNiceScroll().resize();
    changeWindowHeight();

}


// 回调本地代码

//粘贴单条,sednToTop为false则不改变顺序
function pasteValue(index, sendToTop) {
    let command = "PasteValue";
    if (!sendToTop) {
        command += "WithoutTop";
    }
    window.external.notify(
        command + "|" + index);

}


//设置到剪切板但不粘贴
function setToClipBoard(index) {

    window.external.notify(
        "SetToClipBoard|" + index);
}
//粘贴多条
function pasteMultiValue() {
    let command = "PasteValueList";
    if (!multiSendToTop) {
        command += "WithoutTop";
    }
    window.external.notify(
        command + "|" + encodeURIComponent(JSON.stringify(multiIndexList))
    );

}
//粘贴范围
function pasteValueByRange(startIndex, endIndex, sendToTop) {
    let command = "PasteValueRange";
    if (!sendToTop) {
        command += "WithoutTop";
    }

    window.external.notify(
        command + "|" + startIndex + "," + endIndex);

}


function del(index) {
    window.external.notify("del|" + index);
}

function search(value) {
    window.external.notify("search|" + value);
}


//调整高度
function changeWindowHeight() {
    if ($(".content").height() <= 308) {
        $("body").css("height", 308);
    } else {
        $("body").css("height", 617);
    }
    window.external.notify("ChangeWindowHeight|" + $(".content").height());
}


function clear() {
    window.external.notify("clear|");
}