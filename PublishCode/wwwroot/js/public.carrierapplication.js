$(document).ready(function () {
    $('input[name="SelectedTransportTypes"]').click(function () {
        if ($(this).val() == "Other" && $(this).is(':checked')) {
            $("#TransportTypeOther").focus();
        }
        else if ($(this).val() == "Other" && !$(this).is(':checked')) {
            $("#TransportTypeOther").val("");
        }
    });
    $("#TransportTypeOther").click(function () {
        $('input[name = "SelectedTransportTypes"][Value = "Other"]').prop('checked', true);
        $("#TransportTypeOther").focus();
    });
    
    $("#EquipmentTypeOther").click(function () {
        $('input[name = "SelectedEquipmentTypes"][Value = "Other"]').prop('checked', true);
        $("#EquipmentTypeOther").focus();
    });
    $('input[name="SelectedEquipmentTypes"]').click(function () {
        if ($(this).val() == "Other" && $(this).is(':checked')) {
            $("#EquipmentTypeOther").focus();
        }
        else if ($(this).val() == "Other" && !$(this).is(':checked')) {
            $("#EquipmentTypeOther").val("");
        }
    });
});


/*start : signature pad*/

(function () {
    window.requestAnimFrame = (function (callback) {
        return (
            window.requestAnimationFrame ||
            window.webkitRequestAnimationFrame ||
            window.mozRequestAnimationFrame ||
            window.oRequestAnimationFrame ||
            window.msRequestAnimaitonFrame ||
            function (callback) {
                window.setTimeout(callback, 1000 / 60);
            }
        );
    })();

    var canvas = document.getElementById("sig-canvas");
    var ctx = canvas.getContext("2d");
    ctx.strokeStyle = "#222222";
    ctx.lineWidth = 4;

    var drawing = false;
    var mousePos = {
        x: 0,
        y: 0
    };
    var lastPos = mousePos;

    canvas.addEventListener(
        "mousedown",
        function (e) {
            drawing = true;
            lastPos = getMousePos(canvas, e);
        },
        false
    );

    canvas.addEventListener(
        "mouseup",
        function (e) {
            drawing = false;
        },
        false
    );

    canvas.addEventListener(
        "mousemove",
        function (e) {
            mousePos = getMousePos(canvas, e);
        },
        false
    );

    // Add touch event support for mobile
    canvas.addEventListener("touchstart", function (e) { }, false);

    canvas.addEventListener(
        "touchmove",
        function (e) {
            var touch = e.touches[0];
            var me = new MouseEvent("mousemove", {
                clientX: touch.clientX,
                clientY: touch.clientY
            });
            canvas.dispatchEvent(me);
        },
        false
    );

    canvas.addEventListener(
        "touchstart",
        function (e) {
            mousePos = getTouchPos(canvas, e);
            var touch = e.touches[0];
            var me = new MouseEvent("mousedown", {
                clientX: touch.clientX,
                clientY: touch.clientY
            });
            canvas.dispatchEvent(me);
        },
        false
    );

    canvas.addEventListener(
        "touchend",
        function (e) {
            var me = new MouseEvent("mouseup", {});
            canvas.dispatchEvent(me);
        },
        false
    );

    function getMousePos(canvasDom, mouseEvent) {
        var rect = canvasDom.getBoundingClientRect();
        return {
            x: mouseEvent.clientX - rect.left,
            y: mouseEvent.clientY - rect.top
        };
    }

    function getTouchPos(canvasDom, touchEvent) {
        var rect = canvasDom.getBoundingClientRect();
        return {
            x: touchEvent.touches[0].clientX - rect.left,
            y: touchEvent.touches[0].clientY - rect.top
        };
    }

    function renderCanvas() {
        if (drawing) {
            ctx.moveTo(lastPos.x, lastPos.y);
            ctx.lineTo(mousePos.x, mousePos.y);
            ctx.stroke();
            lastPos = mousePos;
        }
    }

    // Prevent scrolling when touching the canvas
    document.body.addEventListener(
        "touchstart",
        function (e) {
            if (e.target == canvas) {
                e.preventDefault();
            }
        },
        false
    );
    document.body.addEventListener(
        "touchend",
        function (e) {
            if (e.target == canvas) {
                e.preventDefault();
            }
        },
        false
    );
    document.body.addEventListener(
        "touchmove",
        function (e) {
            if (e.target == canvas) {
                e.preventDefault();
            }
        },
        false
    );

    (function drawLoop() {
        requestAnimFrame(drawLoop);
        renderCanvas();
    })();

    function clearCanvas() {
        canvas.width = canvas.width;
    }

    // Set up the UI
    var sigText = document.getElementById("sig-dataUrl");
    var sigImage = document.getElementById("sig-image");
    var clearBtn = document.getElementById("sig-clearBtn");
    var submitBtn = document.getElementById("sig-submitBtn");
    clearBtn.addEventListener(
        "click",
        function (e) {
            clearCanvas();
            $("#signature-text-input").val("");
            sigText.innerHTML = "Data URL for your signature will go here!";
            sigImage.setAttribute("src", "");
        },
        false
    );


    //Customize : Event listener for typing
    const textInput = document.getElementById('signature-text-input');
    textInput.addEventListener('input', () => {
        clearCanvas();
        const text = textInput.value;
        // Display the typed text on the canvas
        ctx.fillStyle = '#000';
        ctx.font = '20px Arial';
        ctx.fillText(text, 10, canvas.height - 20);
    });

    submitBtn.addEventListener(
        "click",
        function (e) {
            var dataUrl = canvas.toDataURL();

            sigText.innerHTML = dataUrl;
            sigImage.setAttribute("src", dataUrl);
        },
        false
    );
})();

/*End : signature pad*/


/*Start: canvas signature & terms condition validation */

$(document).ready(function () {
    $('#carrier-application-button').on('click', function () {

        var canvas = document.getElementById("sig-canvas");
        var dataUrl = canvas.toDataURL();
        const blankCanvas = document.createElement('canvas');
        blankCanvas.width = canvas.width;
        blankCanvas.height = canvas.height;
        const base64Canvas = canvas.toDataURL("image/png").split(';base64,')[1];

        if (canvas.toDataURL() === blankCanvas.toDataURL()) {

            $("#SignatureBase64String").val("");

        }
        else {
            $("#SignatureBase64String").val(base64Canvas);
            $("#signature-error-message").hide();
        }
    });

    $('#order-Bill-of-lading').on('click', function () {

        var canvas = document.getElementById("sig-canvas");
        var dataUrl = canvas.toDataURL();
        const blankCanvas = document.createElement('canvas');
        blankCanvas.width = canvas.width;
        blankCanvas.height = canvas.height;
        const base64Canvas = canvas.toDataURL("image/png").split(';base64,')[1];

        if (canvas.toDataURL() === blankCanvas.toDataURL()) {

            $("#CustomerSignatureBase64String").val("");

        }
        else {
            $("#CustomerSignatureBase64String").val(base64Canvas);
            $("#signature-error-message").hide();
        }
    });

    $('#order-Bill-of-lading').on('click', function () {

        var canvas = document.getElementById("sig-canvas-2");
        var dataUrl = canvas.toDataURL();
        const blankCanvas = document.createElement('canvas');
        blankCanvas.width = canvas.width;
        blankCanvas.height = canvas.height;
        const base64Canvas = canvas.toDataURL("image/png").split(';base64,')[1];

        if (canvas.toDataURL() === blankCanvas.toDataURL()) {

            $("#DriverSignatureBase64String").val("");

        }
        else {
            $("#DriverSignatureBase64String").val(base64Canvas);
            $("#signature-error-message").hide();
        }
    });

    $("#fileInsurance").change(function () {
        var input = document.getElementById('fileInsurance');
        var output = document.getElementById('filenameInsurance');
        var children = "";
        for (var i = 0; i < input.files.length; ++i) {
            children += '<li style="list-style: none !important;">' + input.files.item(i).name + '</li>';
        }
        output.innerHTML = '<ul>' + children + '</ul>';
    });
});

/*Etart: w9 file upload*/



/*Start: w9 file upload */
$(document).ready(function () {
    var dropZoneId = "drop-zone";
    var buttonId = "clickHere";
    var mouseOverClass = "mouse-over";
    var dropZone = $("#" + dropZoneId);
    var inputFile = dropZone.find("input");
    var finalFiles = {};
    $(function () {
        var ooleft = dropZone.offset().left;
        var ooright = dropZone.outerWidth() + ooleft;
        var ootop = dropZone.offset().top;
        var oobottom = dropZone.outerHeight() + ootop;

        document.getElementById(dropZoneId).addEventListener("dragover", function (e) {
            e.preventDefault();
            e.stopPropagation();
            dropZone.addClass(mouseOverClass);
            var x = e.pageX;
            var y = e.pageY;

            if (!(x < ooleft || x > ooright || y < ootop || y > oobottom)) {
                inputFile.offset({
                    top: y - 15,
                    left: x - 100
                });
            } else {
                inputFile.offset({
                    top: -400,
                    left: -400
                });
            }

        }, true);

        if (buttonId != "") {
            var clickZone = $("#" + buttonId);

            var oleft = clickZone.offset().left;
            var oright = clickZone.outerWidth() + oleft;
            var otop = clickZone.offset().top;
            var obottom = clickZone.outerHeight() + otop;

            $("#" + buttonId).mousemove(function (e) {
                var x = e.pageX;
                var y = e.pageY;
                if (!(x < oleft || x > oright || y < otop || y > obottom)) {
                    inputFile.offset({
                        top: y - 15,
                        left: x - 160
                    });
                } else {
                    inputFile.offset({
                        top: -400,
                        left: -400
                    });
                }
            });
        }

        document.getElementById(dropZoneId).addEventListener("drop", function (e) {
            $("#" + dropZoneId).removeClass(mouseOverClass);
        }, true);


        inputFile.on('change', function (e) {
            var fileNum = this.files.length,
                initial = 0,
                counter = 0;

            $.each(this.files, function (idx, elm) {
                finalFiles[idx] = elm;
            });

            for (initial; initial < fileNum; initial++) {
                counter = counter + 1;
                $('#filename').append('<div id="file_' + initial + '"><span class="fa-stack fa-lg"><i class="fa fa-file fa-stack-1x "></i><strong class="fa-stack-1x" style="color:#FFF; font-size:12px; margin-top:2px;">' + counter + '</strong></span> ' + this.files[initial].name + '&nbsp;&nbsp;<span class="fa fa-times-circle fa-lg closeBtn" onclick="removeLine(this)" title="remove"></span></div>');
            }
        });
    })

    function removeLine(obj) {
        inputFile.val('');
        var jqObj = $(obj);
        var container = jqObj.closest('div');
        var index = container.attr("id").split('_')[1];
        container.remove();
        delete finalFiles[index];
    }
});


/*End: w9 file upload */


/*Start: Certificate of Insurance file upload */

var dropZoneIdInsurance = "drop-zone-insurance";
var buttonIdInsurance = "clickHereInsurance";
var mouseOverClassInsurance = "mouse-over";
var dropZoneInsurance = $("#" + dropZoneIdInsurance);
var inputFileInsurance = dropZoneInsurance.find("input");
var finalFilesInsurance = {};
$(function () {
    var ooleftInsurance = dropZoneInsurance.offset().left;
    var oorightInsurance = dropZoneInsurance.outerWidth() + ooleftInsurance;
    var ootopInsurance = dropZoneInsurance.offset().top;
    var oobottomInsurance = dropZoneInsurance.outerHeight() + ootopInsurance;

    document.getElementById(dropZoneIdInsurance).addEventListener("dragover", function (e) {
        e.preventDefault();
        e.stopPropagation();
        dropZoneInsurance.addClass(mouseOverClassInsurance);
        var x = e.pageX;
        var y = e.pageY;

        if (!(x < ooleftInsurance || x > oorightInsurance || y < ootopInsurance || y > oobottomInsurance)) {
            inputFileInsurance.offset({
                top: y - 15,
                left: x - 100
            });
        } else {
            inputFileInsurance.offset({
                top: -400,
                left: -400
            });
        }

    }, true);

    if (buttonIdInsurance != "") {
        var clickZoneInsurance = $("#" + buttonIdInsurance);

        var oleftInsurance = clickZoneInsurance.offset().left;
        var orightInsurance = clickZoneInsurance.outerWidth() + oleftInsurance;
        var otopInsurance = clickZoneInsurance.offset().top;
        var obottomInsurance = clickZoneInsurance.outerHeight() + otopInsurance;

        $("#" + buttonIdInsurance).mousemove(function (e) {
            var x = e.pageX;
            var y = e.pageY;
            if (!(x < oleftInsurance || x > orightInsurance || y < otopInsurance || y > obottomInsurance)) {
                inputFileInsurance.offset({
                    top: y - 15,
                    left: x - 160
                });
            } else {
                inputFileInsurance.offset({
                    top: -400,
                    left: -400
                });
            }
        });
    }

    document.getElementById(dropZoneIdInsurance).addEventListener("drop", function (e) {
        $("#" + dropZoneIdInsurance).removeClass(mouseOverClassInsurance);
    }, true);


    inputFileInsurance.on('change', function (e) {
        //finalFiles = {};
        //$('#filename').html("");
        var fileNumInsurance = this.files.length,
            initialInsurance = 0,
            counterInsurance = 0;

        $.each(this.files, function (idx, elm) {
            finalFilesInsurance[idx] = elm;
        });

        for (initialInsurance; initialInsurance < fileNumInsurance; initialInsurance++) {
            counterInsurance = counterInsurance + 1;
            $('#filenameInsurance').append('<div id="fileInsurance_' + initialInsurance + '"><span class="fa-stack fa-lg"><i class="fa fa-file fa-stack-1x "></i><strong class="fa-stack-1x" style="color:#FFF; font-size:12px; margin-top:2px;">' + counterInsurance + '</strong></span> ' + this.files[initialInsurance].name + '&nbsp;&nbsp;<span class="fa fa-times-circle fa-lg closeBtn" onclick="removeLineInsurance(this)" title="remove"></span></div>');
        }
    });



})

function removeLineInsurance(obj) {
    inputFileInsurance.val('');
    var jqObj = $(obj);
    var container = jqObj.closest('div');
    var index = container.attr("id").split('_')[1];
    container.remove();

    delete finalFilesInsurance[index];
}

/*End: Certificate of Insurance file upload */

/*start : driver signature pad*/

(function () {
    window.requestAnimFrame = (function (callback) {
        return (
            window.requestAnimationFrame ||
            window.webkitRequestAnimationFrame ||
            window.mozRequestAnimationFrame ||
            window.oRequestAnimationFrame ||
            window.msRequestAnimaitonFrame ||
            function (callback) {
                window.setTimeout(callback, 1000 / 60);
            }
        );
    })();

    var canvas = document.getElementById("sig-canvas-2");
    var ctx = canvas.getContext("2d");
    ctx.strokeStyle = "#222222";
    ctx.lineWidth = 4;

    var drawing = false;
    var mousePos = {
        x: 0,
        y: 0
    };
    var lastPos = mousePos;

    canvas.addEventListener(
        "mousedown",
        function (e) {
            drawing = true;
            lastPos = getMousePos(canvas, e);
        },
        false
    );

    canvas.addEventListener(
        "mouseup",
        function (e) {
            drawing = false;
        },
        false
    );

    canvas.addEventListener(
        "mousemove",
        function (e) {
            mousePos = getMousePos(canvas, e);
        },
        false
    );

    // Add touch event support for mobile
    canvas.addEventListener("touchstart", function (e) { }, false);

    canvas.addEventListener(
        "touchmove",
        function (e) {
            var touch = e.touches[0];
            var me = new MouseEvent("mousemove", {
                clientX: touch.clientX,
                clientY: touch.clientY
            });
            canvas.dispatchEvent(me);
        },
        false
    );

    canvas.addEventListener(
        "touchstart",
        function (e) {
            mousePos = getTouchPos(canvas, e);
            var touch = e.touches[0];
            var me = new MouseEvent("mousedown", {
                clientX: touch.clientX,
                clientY: touch.clientY
            });
            canvas.dispatchEvent(me);
        },
        false
    );

    canvas.addEventListener(
        "touchend",
        function (e) {
            var me = new MouseEvent("mouseup", {});
            canvas.dispatchEvent(me);
        },
        false
    );

    function getMousePos(canvasDom, mouseEvent) {
        var rect = canvasDom.getBoundingClientRect();
        return {
            x: mouseEvent.clientX - rect.left,
            y: mouseEvent.clientY - rect.top
        };
    }

    function getTouchPos(canvasDom, touchEvent) {
        var rect = canvasDom.getBoundingClientRect();
        return {
            x: touchEvent.touches[0].clientX - rect.left,
            y: touchEvent.touches[0].clientY - rect.top
        };
    }

    function renderCanvas() {
        if (drawing) {
            ctx.moveTo(lastPos.x, lastPos.y);
            ctx.lineTo(mousePos.x, mousePos.y);
            ctx.stroke();
            lastPos = mousePos;
        }
    }

    // Prevent scrolling when touching the canvas
    document.body.addEventListener(
        "touchstart",
        function (e) {
            if (e.target == canvas) {
                e.preventDefault();
            }
        },
        false
    );
    document.body.addEventListener(
        "touchend",
        function (e) {
            if (e.target == canvas) {
                e.preventDefault();
            }
        },
        false
    );
    document.body.addEventListener(
        "touchmove",
        function (e) {
            if (e.target == canvas) {
                e.preventDefault();
            }
        },
        false
    );

    (function drawLoop() {
        requestAnimFrame(drawLoop);
        renderCanvas();
    })();

    function clearCanvas() {
        canvas.width = canvas.width;
    }

    // Set up the UI
    var sigText = document.getElementById("sig-dataUrl");
    var sigImage = document.getElementById("sig-image");
    var clearBtn = document.getElementById("driver-sig-clearBtn");
    var submitBtn = document.getElementById("driver-sig-submitBtn");
    clearBtn.addEventListener(
        "click",
        function (e) {
            clearCanvas();
            $("#driver-signature-text-input").val("");
            sigText.innerHTML = "Data URL for your signature will go here!";
            sigImage.setAttribute("src", "");
        },
        false
    );


    //Customize : Event listener for typing
    const textInput = document.getElementById('driver-signature-text-input');
    textInput.addEventListener('input', () => {
        clearCanvas();
        const text = textInput.value;
        // Display the typed text on the canvas
        ctx.fillStyle = '#000';
        ctx.font = '20px Arial';
        ctx.fillText(text, 10, canvas.height - 20);
    });

    submitBtn.addEventListener(
        "click",
        function (e) {
            var dataUrl = canvas.toDataURL();

            sigText.innerHTML = dataUrl;
            sigImage.setAttribute("src", dataUrl);
        },
        false
    );
})();

/*End : driver signature pad*/

updateList = function () {
    var input = document.getElementById('bolFile');
    var output = document.getElementById('filename');
    var children = "";
    for (var i = 0; i < input.files.length; ++i) {
        children += '<li>' + input.files.item(i).name + '</li>';
    }
    output.innerHTML = '<ul>' + children + '</ul>';
}

otherPicture = function () {
    var input = document.getElementById('bolOther');
    var output = document.getElementById('other');
    var children = "";
    for (var i = 0; i < input.files.length; ++i) {
        var file = input.files[i];
        switch (file.name.substring(file.name.lastIndexOf('.') + 1).toLowerCase()) {
            case 'jpeg': case 'jpg': case 'png':
                break;
            default:
                $("#bolOther").val('');
                $("#other ul").remove();
                // error message here
                alert("Only jpg/jpeg/png files are allowed.");
                break;
        }
        children += '<li>' + input.files.item(i).name + '</li>';
    }
    output.innerHTML = '<ul>' + children + '</ul>';
}