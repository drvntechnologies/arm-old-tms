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
	var signaturePad = new SignaturePad(canvas);
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
		signaturePad.clear();
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

			if (sigImage) {
				sigImage.setAttribute("src", "");
			}
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
			if ($('#signature-text-input').val() != null && $('#signature-text-input').val() != "") {
				var dataUrl = canvas.toDataURL();
				AddSignature(true);
			}
			else {
				var imageData = signaturePad.isEmpty() ? "" : signaturePad.toDataURL();
				if (imageData == "" || imageData == null) {
					alert('Signature is Required.');
					return false;
				}
				else {
					AddSignature(true);
				}
			}
			var dataUrl = canvas.toDataURL();

			if (sigImage) {
				sigImage.setAttribute("src", dataUrl);
			}

			sigText.innerHTML = dataUrl;
		},
		false
	);
})();

/*End : signature pad*/