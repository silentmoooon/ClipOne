var timeout = '';
var selectTimeout = '';
var deleteId = -1;
var deleteNode = '';
var clipObj = [];
var displayObj = [];
var lastSelectedIndex = -1;
var selectIndex = 0;
var searchMode = false;
var isShiftPressed = false;
var firstSelectedIndex = -1;

document.onkeydown = keyDown;
document.onkeyup = keyUp;
function keyDown(event) {

    if (event.ctrlKey && event.keyCode == 70) {
        if (!searchMode && clipObj.length > 0) {
            showSearch();
        } else {
            closeSearch();
            callbackObj.changeWindowHeight($("body").height());
        }
    } else if (event.keyCode == 9) {   //tab键

        event.preventDefault();
        scrollTop();
        $("#searchInput")[0].focus();

    }
    else if (event.keyCode == 13) { //回车

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

        if (event.shiftKey) {
            isShiftPressed = true;
            var key = -1;
            if (event.keyCode >= 49 && event.keyCode <= 57) {  //数字键
                key = event.keyCode - 49;
            } else if (event.keyCode >= 65 && event.keyCode <= 90) { //字母键
                key = event.keyCode - 56;
            } else {
                return;
            }

            if (firstSelectedIndex == -1) {
                firstSelectedIndex = key;
            } else {
                callbackObj.pasteValueByRange(firstSelectedIndex, key);
            }
        }
        else if (event.keyCode >= 49 && event.keyCode <= 57) {  //数字键

            callbackObj.pasteValue(event.keyCode - 49);
        } else if (event.keyCode >= 65 && event.keyCode <= 90) { //字母键
            callbackObj.pasteValue(event.keyCode - 56);
        } else if (event.keyCode == 32) {  //空格
            event.preventDefault();
            callbackObj.pasteValue(0);
        }


    }

}

function keyUp(event) {

    if (event.key == "Shift") {

        firstSelectedIndex = -1;
        isShiftPressed = false;
        $(".tr_hover").removeClass("tr_hover");
    }
}

$(document).ready(function () {


    $("#delete").on("click", function () { //删除操作
        $("#tr" + deleteId).parent().addClass("tr_hover");
        callbackObj.deleteClip(deleteId / 1);
        $(deleteNode).remove();
        // clipObj.splice(deleteId, 1);
        $("#rightmenu").css("display", "none");


    });



    $("#searchInput").on("input", function (event) {
        console.log("input")
        displayObj = [];
        var value = $("#searchInput").val().toLowerCase();
        callbackObj.search(value);
        //for(var i=0;i<clipObj.length;i++){
        //	if(clipObj[i].Type.toLowerCase()==value||clipObj[i].ClipValue.toLowerCase().indexOf(value)>=0){
        //	int length=displayObj.putsh(clipObj[i]);
        //	displayObj[length-1].SourceId=i;
        //	}
        //}
        //displayData(displayObj);
    });


});

document.onselectstart = function () {
    event.returnValue = false;
}
document.oncontextmenu = function (e) {
    e.preventDefault();
};

function showSearch() {

    $("#searchDiv").css("display", "block");
    $("#searchInput")[0].focus();
    searchMode = true;
    callbackObj.changeWindowHeight($("body").height());
}
function closeSearch() {
    scrollTop();
    $("#searchDiv").css("display", "none");
    $(".table_main")[0].focus();
    $("#searchInput").val("");
    searchMode = false;
}
function tdEnter(event) {

    $("#rightmenu").css("display", "none");
    var index = event.getAttribute('index') / 1;
    selectTimeout = setTimeout(function () {
        callbackObj.selectIndex(index);
    }, 200);
    if (clipObj[index].Type == "image") {
        timeout = setTimeout(function () {

            callbackObj.preview(index);
        }, 500);
    }
}

function tdOut() {

    if (selectTimeout)
        clearTimeout(selectTimeout);
    if (timeout)
        clearTimeout(timeout);

    callbackObj.hidePreview();
}

function showmenu(e) {

    document.getElementById("rightmenu").style.left = e.pageX + 'px';
    document.getElementById("rightmenu").style.top = e.pageY + 'px';
    document.getElementById("rightmenu").style.display = "block";

}


function hiddenrightmenu() {
    document.getElementById("rightmenu").style.display = "none";
}

function scrollTop() {

    window.scrollTo(0, 0);
}

function num2key(num) {

    return String.fromCharCode(55 + num);
}

function selectItem(index) {

    if (lastSelectedIndex != -1) {
        $("#tr" + lastSelectedIndex).trigger("mouseout");
        lastSelectedIndex = index;
        $("#tr" + index).addClass("tr_hover");

        $("#tr" + index).one("mouseout", function () {
            $("#tr" + index).removeClass("tr_hover");

        });
    }
}

function showList(json, index) {

    firstSelectedIndex = -1;
    json = decodeURIComponent(json.replace(/\+/g, '%20'));

    clipObj = JSON.parse(json);

    displayData(clipObj);

    $(".table_main")[0].focus();
    callbackObj.changeWindowHeight($("body").height());
    if (clipObj.length > 0) {

        lastSelectedIndex = index;
        selectIndex = index;

        $("#tr" + index).addClass("tr_hover");

        $("#tr" + index).one("mouseout", function () {
            $("#tr" + index).removeClass("tr_hover");

        });


    }



}

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

                trs = " <tr style='cursor: default' class='tr' id='tr" + i + "' index='" + i + "' onmouseup ='callback(this)'  onmouseenter='tdEnter(this)' onmouseleave='tdOut()'> <td  class='td_content' id='td" + i + "'  > <img class='image' src='../" + data[i].DisplayValue + "' /> </td><td class='td_index'  >" + num + "</td> </tr>";
            } else {  //if (clipObj[i].Type=="html"||clipObj[i].Type == "QQ_Unicode_RichEdit_Format"||clipObj[i].Type=="file")
                trs = " <tr style='cursor: default' class='tr' id='tr" + i + "' index='" + i + "' onmouseup ='callback(this)'  onmouseenter='tdEnter(this)' onmouseleave='tdOut()'> <td  class='td_content' id='td" + i + "' > " + data[i].DisplayValue + " </td><td class='td_index'  >" + num + "</td> </tr>";

            }

            tbody += trs;
        }


        $(".myTable").html(tbody);
    }



}

function callback(e) {

    var event = window.event;

    if (event.button == 0) {
        if (isShiftPressed) {
            if (firstSelectedIndex == -1) {
                $("#tr1").removeClass("tr_hover");
                $("#" + e.id).addClass("tr_hover");
                firstSelectedIndex = e.getAttribute('index') / 1;
            } else {
                callbackObj.pasteValueByRange(firstSelectedIndex, e.getAttribute('index') / 1);
            }
        } else {
            $("#tr1").removeClass("tr_hover");
            $("#" + e.id).addClass("tr_hover");
            callbackObj.pasteValue(e.getAttribute('index') / 1);
        }
    } else if (event.button == 2) {
        deleteNode = e;
        deleteId = e.getAttribute('index');
        showmenu(event);
    }

}

