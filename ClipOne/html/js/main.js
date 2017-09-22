var previewTimeout = '';
var deleteId = -1;
var deleteNode = '';
var clipObj = [];
var selectIndex = 0;
var searchMode = false;
var isShiftPressed = false;
var lastSelectedIndex = -1;

document.onkeydown = keyDown;
document.onkeyup = keyUp;
function keyDown(event) {

    if (event.ctrlKey && event.keyCode == 70) {  //ctrl+f
        if (!searchMode && clipObj.length > 0) {
            showSearch();
        } else {
            hideSearch();
            callbackObj.changeWindowHeight($("body").height());
        }
    } else if (event.keyCode == 9) {   //tab键

        event.preventDefault();
        scrollTop();
        $("#searchInput")[0].focus();

    }
    else if (event.keyCode == 13) { //回车直接粘贴当前选中项

        callbackObj.pasteValue(selectIndex);
    }
    else if (event.keyCode == 38) { //上

        event.preventDefault();
        if (selectIndex > 0) {

            selectItem(--selectIndex);
        } else if (selectIndex == 0) {
            $("#searchInput")[0].focus();
            searchMode = true;
            scrollTop();
        }
    }
    else if (event.keyCode == 40) { //下

        $("#searchInput")[0].blur();
        searchMode = false;
        event.preventDefault();
        if (selectIndex < clipObj.length - 1) {
            selectItem(++selectIndex);
        }
    }
    else if (!searchMode) {

        if (event.shiftKey) {    //多条操作
            isShiftPressed = true;
            var key = -1;
            if (event.keyCode >= 49 && event.keyCode <= 57) {  //数字键
                key = event.keyCode - 49;
            } else if (event.keyCode >= 65 && event.keyCode <= 90) { //字母键
                key = event.keyCode - 56;
            } else {
                return;
            }

            if (lastSelectedIndex == -1) {
                lastSelectedIndex = key;
            } else {
                callbackObj.pasteValueByRange(lastSelectedIndex, key);
            }
        }
        else if (event.keyCode >= 49 && event.keyCode <= 57) {  //数字键
            callbackObj.pasteValue(event.keyCode - 49);
        } else if (event.keyCode >= 65 && event.keyCode <= 90) { //字母键
            callbackObj.pasteValue(event.keyCode - 56);
        } else if (event.keyCode == 32) {  //空格直接粘贴第0项
            event.preventDefault();
            callbackObj.pasteValue(0);
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

$(document).ready(function () {

    $("#delete").on("click", function () { //删除操作
        $("#tr" + deleteId).parent().addClass("tr_hover");
        callbackObj.deleteClip(deleteId / 1);
        $(deleteNode).remove();

        $("#rightMenu").css("display", "none");


    });

    $("#searchInput").on("input", function (event) {  //查找

        var value = $("#searchInput").val().toLowerCase();
        callbackObj.search(value);
       
    });


});

//屏蔽鼠标选择操作
document.onselectstart = function () {
    event.returnValue = false;
}
//屏蔽右键菜单
document.oncontextmenu = function (e) {
    e.preventDefault();
};

//显示搜索框
function showSearch() {

    $("#searchDiv").css("display", "block");
    $("#searchInput")[0].focus();
    searchMode = true;
    callbackObj.changeWindowHeight($("body").height());
}
//隐藏搜索框
function hideSearch() {
    scrollTop();
    $("#searchDiv").css("display", "none");
    $(".table_main")[0].focus();
    $("#searchInput").val("");
    searchMode = false;
}

//选中时高亮
function trSelect(event) {

    $("#rightMenu").css("display", "none");
    var index = event.getAttribute('index') / 1;
    selectIndex = index;
    selectItem(index);
    if (clipObj[index].Type == "image") {
        previewTimeout = setTimeout(function () {
            callbackObj.preview(index);
        }, 500);
    }
}

//反选
function trUnselect() {
    if (previewTimeout) {
        clearTimeout(previewTimeout);
    }
    callbackObj.hidePreview();
}

//显示右键菜单
function showMenu(e) {

    document.getElementById("rightMenu").style.left = e.pageX + 'px';
    document.getElementById("rightMenu").style.top = e.pageY + 'px';
    document.getElementById("rightMenu").style.display = "block";

}

//隐藏右键菜单
function hideMenu() {
    document.getElementById("rightMenu").style.display = "none";
}

//滚动到顶部
function scrollTop() {

    window.scrollTo(0, 0);
}

//数字转换成字母
function num2key(num) {

    return String.fromCharCode(55 + num);
}

//选中某项
function selectItem(index) {

    $(".tr_hover").removeClass("tr_hover");
    $("#tr" + index).addClass("tr_hover");

}

//显示记录
function showList(json, index) {

    lastSelectedIndex = -1;
    json = decodeURIComponent(json.replace(/\+/g, '%20'));

    clipObj = JSON.parse(json);

    displayData(clipObj);

    $(".table_main")[0].focus();
    callbackObj.changeWindowHeight($("body").height());
    if (clipObj.length > 0) {

        selectIndex = index;

        $("#tr" + index).addClass("tr_hover");

        $("#tr" + index).one("mouseout", function () {
            $("#tr" + index).removeClass("tr_hover");

        });


    }

}

//显示记录
function displayData(data) {

    var tbody = "";

    if (data.length == 0) {
        tbody = " <tr style='cursor: default'> <td  class='td_content' style='cursor: default' > 无记录 </td> </tr>";
        $(".myTable").html(tbody);

    } else {


        for (var i = 0; i < data.length; i++) {

            var trs = "";
            var num = "";
            if (i < 9) {
                num = "<u>" + (i + 1) + "</u>";
            } else if (i < 35) {
                num = "<u>" + num2key(i + 1) + "</u>";
            } else {
                num = "" + (i + 1);
            }
            if (data[i].Type == "image") {

                trs = " <tr style='cursor: default' class='tr' id='tr" + i + "' index='" + i + "' onmouseup ='pasteValue(this)'  onmouseenter='trSelect(this)' onmouseleave='trUnselect()'> <td  class='td_content' id='td" + i + "'  > <img class='image' src='../" + data[i].DisplayValue + "' /> </td><td class='td_index'  >" + num + "</td> </tr>";
            } else {  //if (clipObj[i].Type=="html"||clipObj[i].Type == "QQ_Unicode_RichEdit_Format"||clipObj[i].Type=="file")
                trs = " <tr style='cursor: default' class='tr' id='tr" + i + "' index='" + i + "' onmouseup ='pasteValue(this)'  onmouseenter='trSelect(this)' onmouseleave='trUnselect()'> <td  class='td_content' id='td" + i + "' > " + data[i].DisplayValue + " </td><td class='td_index'  >" + num + "</td> </tr>";

            }

            tbody += trs;
        }

        $(".myTable").html(tbody);
    }

}

//粘贴选择项
function pasteValue(e) {
    var event = window.event;
    if (event.button == 0) {
        if (isShiftPressed) {  //多条
            if (lastSelectedIndex == -1) {
                $("#tr1").removeClass("tr_hover");
                $("#" + e.id).addClass("tr_hover");
                lastSelectedIndex = e.getAttribute('index') / 1;
            } else {
                callbackObj.pasteValueByRange(lastSelectedIndex, e.getAttribute('index') / 1);
            }
        } else {   //单条
            $("#tr1").removeClass("tr_hover");
            $("#" + e.id).addClass("tr_hover");
            callbackObj.pasteValue(e.getAttribute('index') / 1);
        }
    } else if (event.button == 2) { //弹出右键菜单
		 if (isShiftPressed) { 
			 callbackObj.pasteValueByRange(0, e.getAttribute('index') / 1);
		 }else{
        deleteNode = e;
        deleteId = e.getAttribute('index');
        showMenu(event);
		}
    }

}

