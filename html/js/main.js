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

//alt是否按下,用来处理多记录粘贴
var isCtrlPressed = false;
//多记录粘贴列表
var multiIndexList = []
    //多记录粘贴时是否将粘贴记录发送到顶部,默认为true
var multiSendToTop = true;

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

    $("body").on("keydown", keyDown);
    $("body").on("keyup", keyUp);

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


    var str = window.localStorage.getItem("data");
    if (str != null) {
        clipObj = JSON.parse(str);
    }

    displayData();

    storeInterval = setInterval(saveData, 60000);


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
                    "' onmouseup ='mouseup(this)'  onmouseenter='trSelect(this)' )'> <td  class='td_content' > <img class='image' src='data:image/png;base64," +
                    clipObj[i].ClipValue +
                    "' /> </td><td class='td_index'  >" +
                    num +
                    "</td> </tr>";

            } else {
                trs =
                    " <tr style='cursor: default' index='" +
                    i +
                    "' id='tr" +
                    matchCount +
                    "' onmouseup ='mouseup(this)'  onmouseenter='trSelect(this)' '> <td  class='td_content' >  " +
                    clipObj[i].DisplayValue +
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
function addData(data) {

    data = decodeURIComponent(data.replace(/\+/g, "%20"));
    var obj = JSON.parse(data);

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


// 回调本地代码

//粘贴单条,sednToTop为false则不改变顺序
//粘贴单条
function pasteValue(index, sendToTop) {
    var obj = clipObj[index];
    if (sendToTop) {
        clipObj.splice(index, 1)[0];
        clipObj.splice(0, 0, obj);
    }
    window.external.notify(
        "PasteValue|" + encodeURIComponent(JSON.stringify(obj))
    );

    displayData();
}


//设置到剪切板但不粘贴

function setToClipBoard(index, ) {
    var obj = clipObj[index];
    clipObj.splice(index, 1)[0];
    clipObj.splice(0, 0, obj);

    window.external.notify(
        "SetToClipBoard|" + encodeURIComponent(JSON.stringify(obj))
    );

    displayData();
}
//粘贴多条
function pasteMultiValue() {
    var clipList = [];
    var lastIndex = -1;
    var diffLenth = 0;
    multiIndexList.forEach(index => {

        var result = clipObj[index];
        if (multiSendToTop) {

            if (lastIndex >= 0 && lastIndex > index) {
                diffLenth++;
                index = index + diffLenth;
                result = clipObj[index];
            }
            clipObj.splice(index, 1)[0];
            clipObj.splice(0, 0, result);
        }
        clipList.push(result);
        lastIndex = index;
    });
    window.external.notify(
        "PasteValueList|" + encodeURIComponent(JSON.stringify(clipList))
    );
    if (multiSendToTop) {
        displayData();
    }

}

//粘贴范围
function pasteValueByRange(startIndex, endIndex, sendToTop) {
    var clipList = [];
    if (endIndex > startIndex) {
        for (var i = startIndex; i <= endIndex; i++) {
            var result = clipObj[i];
            if (sendToTop) {
                clipObj.splice(i, 1)[0];
                clipObj.splice(0, 0, result);
            }
            clipList.push(result);
        }
    } else if (endIndex < startIndex) {
        for (var i = startIndex; i >= endIndex; i--) {
            var result = clipObj[i];
            if (sendToTop) {
                clipObj.splice(i, 1)[0];
            }
            clipList.push(result);
        }
        if (sendToTop) {
            clipList.forEach(value => {
                clipObj.splice(0, 0, value);
            });
        }
    } else {
        pasteValue(startIndex, sendToTop);
        return;
    }

    window.external.notify(
        "PasteValueList|" + encodeURIComponent(JSON.stringify(clipList))
    );
    if (multiSendToTop) {
        displayData();
    }

}


function del(index) {
    clipObj.splice(index, 1)[0];
}

function search(value) {
    window.external.notify("search|" + value);
}


function saveData() {
    window.localStorage.setItem("data", JSON.stringify(clipObj));
}



function clear() {
    clipObj = [];
    window.localStorage.clear();
    displayData();

}