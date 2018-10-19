var previewTimeout = '';
var deleteId = -1;
var deleteNode = '';
var clipObj = [];
var selectIndex = 0;
var searchMode = false;
var isShiftPressed = false;
var lastSelectedIndex = -1;
var storeInterval;
var maxRecords = 100;
var searchValue = '';
 
//屏蔽鼠标选择操作
document.onselectstart = function () {
	event.returnValue = false;
}
//屏蔽右键菜单
document.oncontextmenu = function (e) {
	e.preventDefault();
};

$(document).ready(function () {

	$("#delete").on("click", function () { //删除操作
		$("#tr" + deleteId).parent().addClass("tr_hover");
		clipObj.splice(deleteId, 1);
		//deleteClip(deleteId / 1);
		$("#rightMenu").css("display", "none");
		displayData();

	});

	$("#searchInput").on("input", function (event) {  //查找

		var value = $("#searchInput").val().toLowerCase();
		searchValue = value;
		displayData();

	});

	$(document).on("keydown", keyDown);
	$(document).on("keyup", keyUp);
	 
	var str = window.localStorage.getItem("data");
	if (str != null) {
		clipObj = JSON.parse(str);
	}
	storeInterval = setInterval(saveData, 120000);

});




function keyDown(event) {

	if (event.ctrlKey && event.keyCode == 70) {  //ctrl+f
		if (!searchMode && clipObj.length > 0) {
			showSearch();
		} else {
			hideSearch();

		}
		//changeWindowHeight($("body").height());
	} else if (event.keyCode == 9) {   //tab键

		event.preventDefault();
		scrollTop();
		$("#searchInput")[0].focus();

	}
	else if (event.keyCode == 13) { //回车直接粘贴当前选中项

		pasteValue(selectIndex);
	}
	else if (event.keyCode == 38) { //上

		event.preventDefault();
		if (selectIndex > 0) {

			selectItem(--selectIndex);
			scrollUp();
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
			scrollDown();
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
				pasteValueByRange(lastSelectedIndex, key);
			}
		}
		else if (event.keyCode >= 49 && event.keyCode <= 57) {  //数字键
			pasteValue(event.keyCode - 49);
		} else if (event.keyCode >= 65 && event.keyCode <= 90) { //字母键
			pasteValue(event.keyCode - 56);
		} else if (event.keyCode == 32) {  //空格直接粘贴第0项
			event.preventDefault();
			pasteValue(0);
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
	$(".table_main")[0].focus();
	$("#searchInput").val("");
	searchValue = "";
	displayData();
	lastSelectedIndex = -1;
	isShiftPressed = false;
	searchMode = false;
}


//选中时高亮
function trSelect(event) {

	$("#rightMenu").css("display", "none");
	var index = event.getAttribute('index') / 1;
	selectIndex = index;
	if (!isShiftPressed) {
		selectItem(index);
	}
	if (clipObj[index].Type == "image") {
		previewTimeout = setTimeout(function () {
			preview(clipObj[index].ClipValue);
		}, 500);
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

//选中某项
function selectItem(index) {

	$(".tr_hover").removeClass("tr_hover");
	$("#tr" + index).addClass("tr_hover");

}

//显示记录
function displayData() {

	var tbody = "";

	var matchCount = -1;
	for (var i = 0; i < clipObj.length; i++) {

		var trs = "";
		var num = "";

		if (searchValue == "" || clipObj[i].Type == searchValue || clipObj[i].ClipValue.toLowerCase().indexOf(searchValue) >= 0) {
			matchCount++;
			if (matchCount < 9) {
				num = "<u>" + (matchCount + 1) + "</u>";
			} else if (matchCount < 35) {
				num = "<u>" + num2key((matchCount + 1)) + "</u>";
			} else {
				num = "" + (matchCount + 1);
			}
			if (clipObj[i].Type == "image") {
				//id='td" + i + "' 
				trs = " <tr style='cursor: default' index='" + i + "' id='tr" + matchCount + "' onmouseup ='mouseup(this)'  onmouseenter='trSelect(this)' onmouseleave='trUnselect()'> <td  class='td_content' > <img class='image' src='../" + clipObj[i].DisplayValue + "' /> </td><td class='td_index'  >" + num + "</td> </tr>";
			} else {  //if (clipObj[i].Type=="html"||clipObj[i].Type == "QQ_Unicode_RichEdit_Format"||clipObj[i].Type=="file")
				trs = " <tr style='cursor: default' index='" + i + "' id='tr" + matchCount + "' onmouseup ='mouseup(this)'  onmouseenter='trSelect(this)' onmouseleave='trUnselect()'> <td  class='td_content' > " + clipObj[i].DisplayValue + " </td><td class='td_index'  >" + num + "</td> </tr>";

			}
		}
		tbody += trs;
	}

	$(".myTable").html(tbody);
	if (matchCount == -1) {
		tbody = " <tr style='cursor: default'> <td  class='td_content' style='cursor: default' > 无记录 </td> </tr>";
		$(".myTable").html(tbody);

	} else {
		if (searchValue != "") {
			selectIndex = 0;

		} else {
			selectIndex = 1;
		}

		$("#tr" + selectIndex).addClass("tr_hover");

		$("#tr" + selectIndex).one("mouseout", function () {
			$("#tr" + selectIndex).removeClass("tr_hover");

		});
	}


}

//粘贴选择项
function mouseup(e) {
	var event = window.event;
	if (event.button == 0) {
		if (isShiftPressed) {  //多条
			if (lastSelectedIndex == -1) {
				$("#tr1").removeClass("tr_hover");
				$("#" + e.id).addClass("tr_hover");
				lastSelectedIndex = e.getAttribute('index') / 1;
			} else {
				pasteValueByRange(lastSelectedIndex, e.getAttribute('index') / 1);
			}
		} else {   //单条
			$("#tr1").removeClass("tr_hover");
			$("#" + e.id).addClass("tr_hover");
			pasteValue(e.getAttribute('index') / 1);
		}
	} else if (event.button == 2) { //弹出右键菜单
		if (isShiftPressed) {
			pasteValueByRange(0, e.getAttribute('index') / 1);
		} else {
			deleteNode = e;
			deleteId = e.getAttribute('index');
			showMenu(event);
		}
	}

}

//供本地代码调用

//初始化
function init(recordsNum) {
    maxRecords = recordsNum;
     
    displayData();

}
//隐藏时隐藏搜索框
function hide(json) {
	hideSearch();
	scrollTop();
}
//设置保存最大记录数
function setMaxRecords(records) {
	maxRecords = records;
}
//增加条目
function add(data) {
	data = decodeURIComponent(data.replace(/\+/g, '%20'));
	var obj = JSON.parse(data);
	if (obj.Type == "text") {
		for (var i = 0; i < clipObj.length; i++) {
			if (clipObj[i].ClipValue == obj.ClipValue) {
				clipObj.splice(i, 1);
				break;
			}
		}
	}
	clipObj.splice(0, 0, obj);
	setTimeout(function () {
		if (clipObj.length > maxRecords) {
			//$(".tr" + (maxRecords - 1)).remove();
			var clip = clipObj.splice(maxRecords, 1)[0];
			if (clip.Type == "image") {
				deleteImage(clip.ClipValue);
			}
		}
	}, 0);
    displayData();
	//var num="";
	// $(".myTable >tr").each(function(index,element){
	// 	var tmpIndex=index+1;
	// 	if (tmpIndex < 9) {
	// 		num = "<u>" + (tmpIndex + 1) + "</u>";
	// 	} else if (tmpIndex < 35) {
	// 		num = "<u>" + num2key((tmpIndex + 1)) + "</u>";
	// 	} else {
	// 		num = "" + (tmpIndex + 1);
	// 	}
		 
	// 	$(this).attr("id", (index + 1)).attr("index",(index + 1));
	// 	$(this).children(".td_index").html(num);
	// });
	// $(".myTable")

}



//显示记录
function showRecord() {

    if (clipObj.length != 0) {
        
        if (searchValue != "") {
            selectIndex = 0;

        } else {
            selectIndex = 1;
        }
        $(".tr_hover").removeClass("tr_hover");
        $("#tr" + selectIndex).addClass("tr_hover");

        $("#tr" + selectIndex).one("mouseout", function () {
            $("#tr" + selectIndex).removeClass("tr_hover");

        });
    }

	$(".table_main")[0].focus();
	changeWindowHeight($("body").height());

}



// 回调本地代码

//粘贴单条
function pasteValue(index) {
	obj = clipObj.splice(index, 1)[0];
	clipObj.splice(0, 0, obj);

    window.external.notify("PasteValue:" + encodeURIComponent(JSON.stringify(obj)));
	//callbackObj.pasteValue(encodeURIComponent(JSON.stringify(obj)));
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
    window.external.notify("PasteValueList:" + encodeURIComponent(JSON.stringify(obj)));
	//callbackObj.pasteValueList(encodeURIComponent(JSON.stringify(clipList)));
	displayData();
}

//删除
function deleteImage(path) {
    //callbackObj.deleteImage(path);
    window.external.notify("DeleteImage:" + path);
}
//调整高度
function changeWindowHeight(height) {
    //callbackObj.changeWindowHeight(height);
     
    window.external.notify("ChangeWindowHeight:" + height);
}

//预览
function preview(path) {
    //callbackObj.preview(path);
    window.external.notify("Preview:" + path);
}

//隐藏预览
function hidePreview() {
    //callbackObj.hidePreview();
    window.external.notify("HidePreview:" +"11");
}

//获取所有记录,用来持久化
function getAllClip() {
	return encodeURIComponent(JSON.stringify(clipObj));
}

function saveData() {
	window.localStorage.setItem("data", JSON.stringify(clipObj));

}
function clear() {
	clipObj = [];
	window.localStorage.clear();
}


