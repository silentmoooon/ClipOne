var timeout = '';
var searchTimeout = '';
var deleteId = -1;
var deleteNode = '';
var clipObj = [];

$(document).ready(function () {


    $("#delete").on("click", function () {

        callbackObj.deleteClip(deleteId / 1);
        $(deleteNode).remove();
        clipObj.splice(deleteId, 1);
        $("#rightmenu").css("display", "none");
        display();
    });

    

});


document.onselectstart = function () {
    event.returnValue = false;
}
document.oncontextmenu = function (e) {
    e.preventDefault();
};

function searchResult(value){
	clearTimeout(searchTimeout);
	
	searchTimeout=setTimeout(function(){

		if (value != "") {
		
        for (var i = 0; i < clipObj.length; i++) {
			 
            if (clipObj[i].Type.indexOf(value) < 0 && clipObj[i].DisplayValue.indexOf(value) < 0) {
				
                $("#tr" + i).css("display", "none");
            }else{
				$("#tr" + i).css("display", "block");
			}
        }
    }
    else {
	 
        for (var i = 0; i < clipObj.length; i++) {

            $("#tr" + i).css("display", "block");

        }
    }
	},400);
}

 

function tdEnter(event) {

    $("#rightmenu").css("display", "none");

    timeout = setTimeout(function () {

        callbackObj.preview(event.getAttribute('index') / 1);
    }, 600);
}

function tdOut() {
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

function fun(json) {

    json = decodeURIComponent(json.replace(/\+/g, '%20'));

    clipObj = JSON.parse(json);
    display();

    if (clipObj.length > 0) {

        setTimeout(function () {

            $("#tr1").addClass("tr_hover");

            $("#tr1").one("mouseout", function () {
                $("#tr1").removeClass("tr_hover");

            });

        }, 0);

    }

    $(".table_main").focus();

}

function display() {

    var tbody = "";

    for (var i = 0; i < clipObj.length; i++) {

        var trs = "";
        var num = "";
        if (i <= 9) {
            num = "<u>" + i + "</u>";
        } else if (i <= 35) {
            num = "<u>" + num2key(i) + "</u>";
        } else {
            num = "" + i;
        }
        if (clipObj[i].Type == "image") {

            trs = " <tr style='cursor: default' class='tr' id='tr" + i + "' index='" + i + "' onmouseup ='callback(this)'  onmouseenter='tdEnter(this)' onmouseleave='tdOut()'> <td  class='td_content' id='td" + i + "'  > <img class='image' src='../" + clipObj[i].DisplayValue + "' /> </td><td class='td_index'  >" + num + "</td> </tr>";
        } else {  //if (clipObj[i].Type=="html"||clipObj[i].Type == "QQ_Unicode_RichEdit_Format"||clipObj[i].Type=="file") 
            trs = " <tr style='cursor: default' class='tr' id='tr" + i + "' index='" + i + "' onmouseup ='callback(this)' > <td  class='td_content' id='td" + i + "' > " + clipObj[i].DisplayValue + " </td><td class='td_index'  >" + num + "</td> </tr>";

        }
        /* else  {

             trs = " <tr style='cursor: default'  id='tr" + i + "' > <td  class='td_content' id='td" + i + "' onclick='callback(this)'  > "+clipObj[i].DisplayValue+" </td><td class='td_index'  >" + num + "</td> </tr>";
         }

         */
        /* for (var i = 0; i < obj.length; i++) {

           if(obj[i].Type == "text") {
                $("#td" + i)[0].innerText = clipObj[i].DisplayValue;
           }


        }*/

        tbody += trs;
    }
    $(".myTable").html(tbody);


    if (clipObj.length == 0) {
        tbody = " <tr style='cursor: default'> <td  class='td_content' style='cursor: default' > 无记录 </td> </tr>";
        $(".myTable").html(tbody);

    }
    window.scrollTo(0, 0);


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

