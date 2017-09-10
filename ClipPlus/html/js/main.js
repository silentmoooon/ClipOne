var timeout = '';

document.onselectstart = function () {
    event.returnValue = false;
}
function tdEnter(event) {

    timeout = setTimeout(function () {
		 
        callbackObj.preview(event.id);
    }, 600);
}

function tdOut() {
    clearTimeout(timeout);
	callbackObj.hidePreview();
}
 
function scrollTop(){

    window.scrollTo(0, 0);
}
function fun(json) {
  
    json = decodeURIComponent(json.replace(/\+/g, '%20'));
	 
    var obj = JSON.parse(json);
    var tbody = "";

    for (var i = 0; i < obj.length; i++) {

        var trs = "";
        var num = "";
        if (i < 9) {
            num = "<u>" + (i + 1) + "</u>";
        } else {
            num = i + 1;
        }
        if (obj[i].Type == "image") {
           
            trs = " <tr style='cursor: default'  id='tr" + i + "' > <td  class='td_content' id='td" + i + "' onclick='callback(this)'  onmouseenter='tdEnter(this)' onmouseleave='tdOut()' > <img class='image' src='../" + obj[i].DisplayValue + "' /> </td><td class='td_index'  >" + num + "</td> </tr>";
        } else {  //if (obj[i].Type=="html"||obj[i].Type == "QQ_Unicode_RichEdit_Format"||obj[i].Type=="file") 
            trs = " <tr style='cursor: default'  id='tr" + i + "' > <td  class='td_content' id='td" + i + "' onclick='callback(this)' > "+obj[i].DisplayValue+" </td><td class='td_index'  >" + num + "</td> </tr>";

        }
       /* else  {

            trs = " <tr style='cursor: default'  id='tr" + i + "' > <td  class='td_content' id='td" + i + "' onclick='callback(this)'  > "+obj[i].DisplayValue+" </td><td class='td_index'  >" + num + "</td> </tr>";
        } */

        tbody += trs;
    }
    $(".myTable").html(tbody);
    
   /* for (var i = 0; i < obj.length; i++) {
         
            if(obj[i].Type == "text") {
                 $("#td" + i)[0].innerText = obj[i].DisplayValue;
            }
       

    }*/

    if (obj.length == 0) {
        tbody = " <tr style=\"cursor: default\"> <td  style=\"cursor: default\" id='td\"+i+\"'> 无记录 </td> </tr>";
        $(".myTable").html(tbody);

    }else{
	setTimeout(function(){
		$("#tr1").addClass("tr_hover");

        $("#tr1").one("mouseout",function () {
            $("#tr1").removeClass("tr_hover");

        });

	},0);
        
    }
	 window.scrollTo(0, 0);
	//callbackObj.changeWindowHeight($(".table_main").height(),$(".table_main").css("background-color"));

  //  $("body").css("overflow","auto");


}

function callback(e) {
	
	$("#tr1").removeClass("tr_hover");
    $("#" + e.id).parent().addClass("tr_hover");
    callbackObj.pasteValue(e.id);

}

