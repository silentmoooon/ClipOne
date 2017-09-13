var timeout = '';
var selectTimeout = '';
var deleteId = -1;
var deleteNode = '';
var clipObj = [];
var lastSelectedIndex = -1;

$(document).ready(function () {


    $("#delete").on("click", function () {
        $("#tr" + deleteId).parent().addClass("tr_hover");
        callbackObj.deleteClip(deleteId / 1);
        $(deleteNode).remove();
        clipObj.splice(deleteId, 1);
        $("#rightmenu").css("display", "none");
        //display();
    });


});


document.onselectstart = function () {
    event.returnValue = false;
}
document.oncontextmenu = function (e) {
    e.preventDefault();
};


function tdEnter(event) {
	 
    $("#rightmenu").css("display", "none");
    var index = event.getAttribute('index') / 1;
    selectTimeout = setTimeout(function () {
        callbackObj.selectIndex(index);
    }, 300);
    if (clipObj[index].Type == "image") {
        timeout = setTimeout(function () {

            callbackObj.preview(index);
        }, 600);
    }
}

function tdOut() {

	if(selectTimeout)
    clearTimeout(selectTimeout);
	if(timeout)
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

function selectItem(selectIndex) {

    if (lastSelectedIndex != -1) {
        $("#tr" + lastSelectedIndex).trigger("mouseout");
        lastSelectedIndex = selectIndex;
        $("#tr" + selectIndex).addClass("tr_hover");

        $("#tr" + selectIndex).one("mouseout", function () {
            $("#tr" + selectIndex).removeClass("tr_hover");

        });
    }
}

function showList(json, selectIndex) {
	 
	
    json = decodeURIComponent(json.replace(/\+/g, '%20'));
    
    clipObj = JSON.parse(json);
  
    displayData();
	
	 window.scrollTo(1, 1);

    if (clipObj.length > 0) {

        lastSelectedIndex = selectIndex;
        $("#tr" + selectIndex).addClass("tr_hover");

        $("#tr" + selectIndex).one("mouseout", function () {
            $("#tr" + selectIndex).removeClass("tr_hover");

        });


    }

  

}

function displayData() {

	 var tbody = "";
	 
    if (clipObj.length == 0) {
        tbody = " <tr style='cursor: default'> <td  class='td_content' style='cursor: default' > 无记录 </td> </tr>";
        $(".myTable").html(tbody);

    } else {
       

        for (var i = 0; i < clipObj.length; i++) {
           
            var trs = "";
            var num = "";
            if (i < 9) {
                num = "<u>" + (i + 1) + "</u>";
            } else if (i < 35) {
                num = "<u>" + num2key(i + 1) + "</u>";
            } else {
                num = "" + (i + 1);
            }
            if (clipObj[i].Type == "image") {

                trs = " <tr style='cursor: default' class='tr' id='tr" + i + "' index='" + i + "' onmouseup ='callback(this)'  onmouseenter='tdEnter(this)' onmouseleave='tdOut()'> <td  class='td_content' id='td" + i + "'  > <img class='image' src='../" + clipObj[i].DisplayValue + "' /> </td><td class='td_index'  >" + num + "</td> </tr>";
            } else {  //if (clipObj[i].Type=="html"||clipObj[i].Type == "QQ_Unicode_RichEdit_Format"||clipObj[i].Type=="file")
                trs = " <tr style='cursor: default' class='tr' id='tr" + i + "' index='" + i + "' onmouseup ='callback(this)'  onmouseenter='tdEnter(this)' onmouseleave='tdOut()'> <td  class='td_content' id='td" + i + "' > " + clipObj[i].DisplayValue + " </td><td class='td_index'  >" + num + "</td> </tr>";

            }

            tbody += trs;
        }


        $(".myTable").html(tbody);
    }
   


}

function callback(e) {
    var event = window.event;

    if (event.button == 0) {
        $("#tr1").removeClass("tr_hover");
        $("#" + e.id).parent().addClass("tr_hover");
        callbackObj.pasteValue(e.getAttribute('index'));
    } else if (event.button == 2) {
        deleteNode = e;
        deleteId = e.getAttribute('index');
        showmenu(event);
    }

}

