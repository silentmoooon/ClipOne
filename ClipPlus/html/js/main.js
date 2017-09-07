var timeout = '';

document.onselectstart = function () {
    event.returnValue = false;
}
function enter1(event) {

    timeout = setTimeout(function () {

        callbackObj.preview(event.id);
    }, 600);
}

function out1() {
    clearTimeout(timeout);
	callbackObj.hidePreview();
}

function scrollTop(){

    window.scrollTo(0, 0);
}
function fun(json) {
   
    //$("body").css("overflow-x","hidden");
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
        if (obj[i].indexOf("``+image|") == 0) {
            trs = obj[i].replace("``+image|", "");
            trs = " <tr style='cursor: default'  id='tr" + i + "' > <td  class='td_content' id='td" + i + "' onclick='callback(this)'  onmouseenter='enter1(this)' onmouseleave='out1()' > <img class='image' src='../" + trs + "' /> </td><td class='td_index'  >" + num + "</td> </tr>";
        } else if (obj[i].indexOf("``+html|") == 0) {
            trs = " <tr style='cursor: default'  id='tr" + i + "' > <td  class='td_content' id='td" + i + "' onclick='callback(this)' >  </td><td class='td_index'  >" + num + "</td> </tr>";

        }
        else {

            trs = " <tr style='cursor: default'  id='tr" + i + "' > <td  class='td_content' id='td" + i + "' onclick='callback(this)'  >  </td><td class='td_index'  >" + num + "</td> </tr>";
        }

        tbody += trs;
    }
    $(".myTable").html(tbody);
    
    for (var i = 0; i < obj.length; i++) {
        var trs = "";
        if (obj[i].indexOf("``+image|") == 0) {

        } else {
            var trs = "";
            if (obj[i].indexOf("``+file|") == 0) {
                trs = obj[i].replace("``+file|", "");
                $("#td" + i)[0].innerText = trs;
            } else if (obj[i].indexOf("``+html|") == 0) {

                trs = obj[i].replace("``+html|", "");
                $("#td" + i).html(trs);

            }
            else {
                $("#td" + i)[0].innerText = obj[i];
            }
        }

    }

    if (obj.length == 0) {
        tbody = " <tr style=\"cursor: default\"> <td  style=\"cursor: default\" id='td\"+i+\"'> 无记录 </td> </tr>";
        $(".myTable").html(tbody);

    }else{
        $("#tr1").addClass("tr_hover");

        $("#tr1").one("mouseout",function () {
            $("#tr1").removeClass("tr_hover");

        });
    }

  //  $("body").css("overflow","auto");


}

function callback(e) {
	
	$("#tr1").removeClass("tr_hover");
    $("#" + e.id).parent().addClass("tr_hover");
    callbackObj.pasteValue(e.id);
}

