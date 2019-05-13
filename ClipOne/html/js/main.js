var previewTimeout = "";
var deleteId = -1;
var deleteNode = "";
var clipObj = [];
var selectIndex = 0;
var searchMode = false;
var isShiftPressed = false;
var lastSelectedIndex = -1;
var storeInterval;
var clearImageInterval;
var maxRecords = 100;
var searchValue = "";

//屏蔽鼠标选择操作
document.onselectstart = function() {
    event.returnValue = false;
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

    //查找
    $("#searchInput").on("input", function(event) {
        var value = $("#searchInput")
            .val()
            .toLowerCase();
        searchValue = value;
        displayData();
    });

    $("body").on("keydown", keyDown);
    $("body").on("keyup", keyUp);

    var str = window.localStorage.getItem("data");
    if (str != null) {
        clipObj = JSON.parse(str);
    }

    maxRecords = window.localStorage.getItem("recordCount");
    if (maxRecords == null) {
        maxRecords = 100;
    }
    storeInterval = setInterval(saveData, 120000);

    clearImageInterval = setInterval(clearImage, 2 * 3600 * 1000);

    displayData();
});

function clearImage() {
    var images = [];
    for (var i = 0; i < clipObj.length; i++) {
        if (clipObj[i].Type == "image") {
            images.push(clipObj[i].DisplayValue);
        }else if (clipObj[i].Type == "QQ_Unicode_RichEdit_Format"&&clipObj[i].Images&&clipObj[i].Images.length>0) {
			var strings=clipObj[i].Images.split(",");
			for(var s in strings){
				images.push(s);
			}
        }
    }
    window.external.notify(
        "clearImage|" + encodeURIComponent(JSON.stringify(images))
    );
}

function keyDown(event) {
    if (event.keyCode == 27) {
        scrollTop();
        window.external.notify("esc|1");
    } else if (event.keyCode == 13) {
        //回车直接粘贴当前选中项
        pasteValue(selectIndex);
    } else if (event.ctrlKey && event.keyCode == 70) {
        //ctrl+f
        if (!searchMode && clipObj.length > 0) {
            showSearch();
        } else {
            hideSearch();
        }
        return;
    } else if (!searchMode) {
        if (event.shiftKey) {
            //多条操作
            isShiftPressed = true;
            var key = -1;
            if (event.keyCode >= 49 && event.keyCode <= 57) {
                //数字键
                key = event.keyCode - 49;
            } else if (event.keyCode >= 65 && event.keyCode <= 90) {
                //字母键
                key = event.keyCode - 56;
            } else {
                return;
            }

            if (lastSelectedIndex == -1) {
                lastSelectedIndex = key;
            } else {
                pasteValueByRange(lastSelectedIndex, key);
            }
        } else if (event.keyCode >= 49 && event.keyCode <= 57) {
            //数字键
            pasteValue(event.keyCode - 49);
        } else if (event.keyCode >= 65 && event.keyCode <= 90) {
            //字母键

            pasteValue(event.keyCode - 56);
        } else if (event.keyCode == 32) {
            //空格直接粘贴第0项
            event.preventDefault();
            pasteValue(0);
        } else if (event.keyCode == 8 || event.keyCode == 46) {
            //退格或者del键删除
           
            var clip=clipObj.splice(selectIndex, 1)[0];
			 if (clip.Type == "image") {
                deleteImage(clip.ClipValue);
            }else if(clip.Type=="QQ_Unicode_RichEdit_Format"){
				 deleteImage(clip.Images);
			}

            displayData();
        }
    }
}

function keyUp(event) {
    if (event.key == "Shift") {
        lastSelectedIndex = -1;
        isShiftPressed = false;
        $(".tr_hover").removeClass("tr_hover");
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
        searchValue = "";
        displayData();
    }
}

//选中时高亮
function trSelect(event) {
    var index = event.getAttribute("index") / 1;
    selectIndex = index;

    if (!isShiftPressed) {
        $(".tr_hover").removeClass("tr_hover");
        $("#tr" + index).addClass("tr_hover");
    }
}

//反选
function trUnselect(event) {
    if (previewTimeout) {
        clearTimeout(previewTimeout);
        previewTimeout = undefined;
        hidePreview();
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
            clipObj[i].ClipValue.toLowerCase().indexOf(searchValue) >= 0
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
                    "' onmouseup ='mouseup(this)'  onmouseenter='trSelect(this)' )'> <td  class='td_content' > <img class='image' src='../" +
                    clipObj[i].DisplayValue +
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

    $(".myTable").html(tbody);
    if (matchCount == -1) {
        tbody =
            " <tr style='cursor: default'> <td  class='td_content' style='cursor: default' > 无记录 </td> </tr>";
        $(".myTable").html(tbody);
    }

    $(".content")
        .getNiceScroll()
        .resize();
    changeWindowHeight($("body").height());
}

//粘贴选择项
function mouseup(e) {
    var event = window.event;
    if (event.button == 0) {
        if (isShiftPressed) {
            //多条
            if (lastSelectedIndex == -1) {
                $("#tr1").removeClass("tr_hover");
                $("#" + e.id).addClass("tr_hover");
                lastSelectedIndex = e.getAttribute("index") / 1;
            } else {
                pasteValueByRange(lastSelectedIndex, e.getAttribute("index") / 1);
            }
        } else {
            //单条

            $("#tr1").removeClass("tr_hover");
            $("#" + e.id).addClass("tr_hover");

            pasteValue(e.getAttribute("index") / 1);
        }
    } else if (event.button == 2) {
        //弹出右键菜单
        if (isShiftPressed) {
            pasteValueByRange(0, e.getAttribute("index") / 1);
        }
    }
}

//设置保存最大记录数
function setMaxRecords(records) {
    maxRecords = records;
    window.localStorage.setItem("recordCount", maxRecords);
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
        setTimeout(function() {
            var clip = clipObj.splice(maxRecords, 1)[0];
            if (clip.Type == "image") {
                deleteImage(clip.ClipValue);
            }else if(clip.Type=="QQ_Unicode_RichEdit_Format"){
				 deleteImage(clip.Images);
			}
        }, 0);
    }
    displayData();
}

//显示记录
function showRecord() {
    lastSelectedIndex = -1;
    isShiftPressed = false;
    searchMode = false;

    scrollTop();

    $("#searchDiv").css("display", "none");
    if ($("#searchInput").val() != "") {
        $("#searchInput").val("");
        searchValue = "";
    }

    if (clipObj.length != 0) {
        if (searchValue != "") {
            selectIndex = 0;
        } else {
            selectIndex = 1;
        }
        $(".tr_hover").removeClass("tr_hover");
        $("#tr" + selectIndex).addClass("tr_hover");

        //$("#tr" + selectIndex).one("mouseout", function () {
        //	$("#tr" + selectIndex).removeClass("tr_hover");

        //});
    }
    $("#searchText").show();
    $("#searchText")[0].focus();
    $("#searchText").hide();
}

// 回调本地代码

//粘贴单条
function pasteValue(index) {
   
    obj = clipObj.splice(index, 1)[0];
    window.external.notify(
        "PasteValue|" + encodeURIComponent(JSON.stringify(obj))
    );
    clipObj.splice(0, 0, obj);
 
    displayData();
}
//粘贴多条
function pasteValueByRange(startIndex, endIndex) {
    var clipList = [];
    if (endIndex > startIndex) {
        for (var i = startIndex; i <= endIndex; i++) {
            var result = clipObj.splice(i, 1)[0];
            clipObj.splice(0, 0, result);
            clipList.push(result);
        }
    } else if (endIndex < startIndex) {
        for (var i = startIndex; i >= endIndex; i--) {
            var result = clipObj.splice(i, 1)[0];
            clipObj.splice(0, 0, result);
            clipList.push(result);
        }
    } else {
        pasteValue(startIndex);
        return;
    }
   
    window.external.notify(
        "PasteValueList|" + encodeURIComponent(JSON.stringify(clipList))
    );
   
    displayData();
}

//删除
function deleteImage(path) {
    window.external.notify("DeleteImage|" + path);
}

 
//调整高度
function changeWindowHeight(height) {
    window.external.notify("ChangeWindowHeight|" + height);
}

 
//获取所有记录,用来持久化
function getAllClip() {
    return encodeURIComponent(JSON.stringify(clipObj));
}

function saveData() {
    window.localStorage.setItem("data", JSON.stringify(clipObj));
}

function saveRecordCount() {
    window.localStorage.setItem("recordCount", maxRecords);
}

function clear() {
    clipObj = [];
    window.localStorage.clear();
    displayData();
}