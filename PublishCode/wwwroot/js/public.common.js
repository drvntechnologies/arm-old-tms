/*
** nopCommerce custom js functions
*/



function OpenWindow(query, w, h, scroll) {
    var l = (screen.width - w) / 2;
    var t = (screen.height - h) / 2;

    winprops = 'resizable=0, height=' + h + ',width=' + w + ',top=' + t + ',left=' + l + 'w';
    if (scroll) winprops += ',scrollbars=1';
    var f = window.open(query, "_blank", winprops);
}

function setLocation(url) {
    window.location.href = url;
}

function displayAjaxLoading(display) {
    if (display) {
        $('.ajax-loading-block-window').show();
    }
    else {
        $('.ajax-loading-block-window').hide('slow');
    }
}

function displayPopupNotification(message, messagetype, modal) {
    //types: success, error, warning
    var container;
    if (messagetype == 'success') {
        //success
        container = $('#dialog-notifications-success');
    }
    else if (messagetype == 'error') {
        //error
        container = $('#dialog-notifications-error');
    }
    else if (messagetype == 'warning') {
        //warning
        container = $('#dialog-notifications-warning');
    }
    else {
        //other
        container = $('#dialog-notifications-success');
    }

    //we do not encode displayed message
    var htmlcode = '';
    if ((typeof message) == 'string') {
        htmlcode = '<p>' + message + '</p>';
    } else {
        for (var i = 0; i < message.length; i++) {
            htmlcode = htmlcode + '<p>' + message[i] + '</p>';
        }
    }

    container.html(htmlcode);

    var isModal = (modal ? true : false);
    container.dialog({
        modal: isModal,
        width: 350
    });
}
function displayJoinedPopupNotifications(notes) {
    if (Object.keys(notes).length === 0) return;

    var container = $('#dialog-notifications-success');
    var htmlcode = document.createElement('div');

    for (var note in notes) {
        if (notes.hasOwnProperty(note)) {
            var messages = notes[note];

            for (var i = 0; i < messages.length; ++i) {
                var elem = document.createElement("div");
                elem.innerHTML = messages[i];
                elem.classList.add('popup-notification');
                elem.classList.add(note);

                htmlcode.append(elem);
            }
        }
    }

    container.html(htmlcode);
    container.dialog({
        width: 350,
        modal: true
    });
}
function displayPopupContentFromUrl(url, title, modal, width) {
    var isModal = (modal ? true : false);
    var targetWidth = (width ? width : 550);
    var maxHeight = $(window).height() - 20;

    $('<div></div>').load(url)
        .dialog({
            modal: isModal,
            width: targetWidth,
            maxHeight: maxHeight,
            title: title,
            close: function(event, ui) {
                $(this).dialog('destroy').remove();
            }
        });
}

function displayBarNotification(message, messagetype, timeout) {
    var notificationTimeout;

    var messages = typeof message === 'string' ? [message] : message;
    if (messages.length === 0)
        return;

    //types: success, error, warning
    var cssclass = ['success', 'error', 'warning'].indexOf(messagetype) !== -1 ? messagetype : 'success';

    //remove previous CSS classes and notifications
    $('#bar-notification')
      .removeClass('success')
      .removeClass('error')
      .removeClass('warning');
    $('.bar-notification').remove();

    //add new notifications
    var htmlcode = document.createElement('div');

    //IE11 Does not support miltiple parameters for the add() & remove() methods
    htmlcode.classList.add('bar-notification', cssclass);
    htmlcode.classList.add(cssclass);

    //add close button for notification
    var close = document.createElement('span');
    close.classList.add('close');
    close.setAttribute('title', document.getElementById('bar-notification').dataset.close);

    for (var i = 0; i < messages.length; i++) {
        var content = document.createElement('p');
        content.classList.add('content');
        content.innerHTML = messages[i];

      htmlcode.appendChild(content);
    }
    
    htmlcode.appendChild(close);

    $('#bar-notification')
      .append(htmlcode);

    $(htmlcode)
        .fadeIn('slow')
        .on('mouseenter', function() {
            clearTimeout(notificationTimeout);
        });

    //callback for notification removing
    var removeNoteItem = function () {
        $(htmlcode).remove();
    };

    $(close).on('click', function () {
        $(htmlcode).fadeOut('slow', removeNoteItem);
    });

    //timeout (if set)
    if (timeout > 0) {
        notificationTimeout = setTimeout(function () {
            $(htmlcode).fadeOut('slow', removeNoteItem);
        }, timeout);
    }
}

function htmlEncode(value) {
    return $('<div/>').text(value).html();
}

function htmlDecode(value) {
    return $('<div/>').html(value).text();
}


// CSRF (XSRF) security
function addAntiForgeryToken(data) {
    //if the object is undefined, create a new one.
    if (!data) {
        data = {};
    }
    //add token
    var tokenInput = $('input[name=__RequestVerificationToken]');
    if (tokenInput.length) {
        data.__RequestVerificationToken = tokenInput.val();
    }
    return data;
};
function commonPhoneNumber(ids) {
    $(ids).each(function () {
        var $input = $(this);

        $input.attr('placeholder', '(XXX) XXX-XXXX');

        var phone = $input.val().trim();
        if (phone.length === 10 && !isNaN(phone)) {
            var formattedPhone = `(${phone.substr(0, 3)}) ${phone.substr(3, 3)}-${phone.substr(6, 4)}`;
            $input.val(formattedPhone);
        }

        $input.on('input', function (event) {
            let rawInput = this.value.replace(/\D/g, ''); // Keep only numbers

            if (rawInput.length > 10) {
                rawInput = rawInput.substring(0, 10); // Limit to 10 digits
            }

            let formattedInput = '';
            if (rawInput.length > 0) {
                formattedInput = '(' + rawInput.substring(0, 3);
            }
            if (rawInput.length >= 4) {
                formattedInput += ') ' + rawInput.substring(3, 6);
            }
            if (rawInput.length >= 7) {
                formattedInput += '-' + rawInput.substring(6, 10);
            }

            let cursorPosition = this.selectionStart;
            let prevLength = this.value.length;

            this.value = formattedInput;

            // Adjust cursor position after formatting
            let newCursorPosition = cursorPosition + (formattedInput.length - prevLength);
            this.setSelectionRange(newCursorPosition, newCursorPosition);
        });

        $input.on('keydown', function (event) {
            // Allow Backspace, Delete, Tab, Enter, Escape, Arrow keys
            if ($.inArray(event.key, ['Backspace', 'Delete', 'Tab', 'Escape', 'Enter', 'ArrowLeft', 'ArrowRight']) !== -1) {
                return;
            }

            // Allow Ctrl + A, C, V, X
            if (event.ctrlKey && $.inArray(event.key.toLowerCase(), ['a', 'c', 'v', 'x']) !== -1) {
                return;
            }

            // Allow Function keys (F1–F12), Page Up, Page Down, Home, End, Print Screen
            if ((event.keyCode >= 112 && event.keyCode <= 123) || // F1–F12
                (event.keyCode >= 33 && event.keyCode <= 36) ||  // Page Up, Page Down, Home, End
                event.keyCode === 44) { // Print Screen
                return;
            }

            // Allow only numbers (0-9 from keyboard & numpad)
            if ((event.key >= '0' && event.key <= '9') || (event.keyCode >= 96 && event.keyCode <= 105)) {
                return;
            }

            // Prevent all other keys
            event.preventDefault();
        });

        $input.on('paste drop', function (event) {
            event.preventDefault();

            let content = event.originalEvent.clipboardData ? event.originalEvent.clipboardData.getData('text') :
                event.originalEvent.dataTransfer ? event.originalEvent.dataTransfer.getData('text') : '';

            let sanitizedContent = content.replace(/\D/g, ''); // Keep only numbers
            if (sanitizedContent.length > 10) {
                sanitizedContent = sanitizedContent.substring(0, 10); // Limit to 10 digits
            }

            let formattedNewValue = '';
            if (sanitizedContent.length > 0) {
                formattedNewValue = '(' + sanitizedContent.substring(0, 3);
            }
            if (sanitizedContent.length >= 4) {
                formattedNewValue += ') ' + sanitizedContent.substring(3, 6);
            }
            if (sanitizedContent.length >= 7) {
                formattedNewValue += '-' + sanitizedContent.substring(6, 10);
            }

            this.value = formattedNewValue;
        });
    });
}

function allowNumberOnly(ids) {
    $(ids).each(function () {
        var $input = $(this);
        $input.on('keydown', function (event) {
            if ($.inArray(event.keyCode, [8, 9, 13, 27, 46, 37, 38, 39, 40, 36, 35, 33, 34, 45]) !== -1 ||
                (event.ctrlKey === true && (event.key === 'a' || event.key === 'v' || event.key === 'c' || event.key === 'x')) ||
                (event.keyCode >= 112 && event.keyCode <= 123)) {
                return;
            }

            if (!/^[0-9]$/.test(event.key)) {
                event.preventDefault();
            }
        });

        $input.on('paste drop', function (event) {
            let content = event.originalEvent.clipboardData ? event.originalEvent.clipboardData.getData('text') :
                event.originalEvent.dataTransfer ? event.originalEvent.dataTransfer.getData('text') : '';

            if (!/^\d+$/.test(content)) {
                event.preventDefault();
            }
        });
    });
}

function isTodayOrPastDate(endDate) {
    const date = moment(endDate);

    const today = moment().startOf('day');

    return date.isSameOrBefore(today, 'day');
}

function prepareDateRangePicker(calenderId, setLastDate = 1) {
    const $calenderId = $(calenderId);
    let $firstDateId = $calenderId.closest('div.input-name').find('.start-date');
    let $LastDateId = $calenderId.closest('div.input-name').find('.end-date');

    let startDate = $firstDateId.val();
    let endDate = $LastDateId.val();
    function getDefaultRange() {
        let start = moment().startOf('day');
        let end = moment().add(setLastDate, 'days');

        // If today is Sunday, start from next Monday
        if (start.day() === 0 || end.day() === 0) {
            start.add(1, 'week').startOf('week').add(1, 'days'); // Move to next Monday
            end = start.clone().add(setLastDate, 'days');
        }
        //else {
        //    // If end date lands on Sunday, move it to Monday
        //    if (end.day() === 0) end.add(1, 'days');
        //}

        return { start, end };
    }

    let range = getDefaultRange();

    var minDate = moment();
    if (isTodayOrPastDate(startDate)) {
        minDate = moment(startDate);
    }

    $calenderId.daterangepicker({
        locale: {
            format: 'MM/DD/YYYY',
        },
        minDate: minDate,
        timePicker: false,
        timePicker24Hour: false,
        timePickerSeconds: false,
        autoUpdateInput: false,
        startDate: startDate ? moment(startDate) : range.start,
        endDate: endDate ? moment(endDate) : range.end,
        isInvalidDate: function (date) {
            // Disable Saturday (6) and Sunday (0)
            return date.day() === 6 || date.day() === 0;
        }
    }, function (start, end) {
        $calenderId.val(start.format('MM/DD/YYYY') + ' - ' + end.format('MM/DD/YYYY'));
        $firstDateId.val(start.format('YYYY-MM-DD'));
        $LastDateId.val(end.format('YYYY-MM-DD'));
    });

    if (startDate && endDate) {
        $calenderId.val(moment(startDate).format('MM/DD/YYYY') + ' - ' + moment(endDate).format('MM/DD/YYYY'));
    }
    else {
        $calenderId.val("");
    }

    $calenderId.on('apply.daterangepicker', function (ev, picker) {
        $(this).val(picker.startDate.format('MM/DD/YYYY') + ' - ' + picker.endDate.format('MM/DD/YYYY'));
        $firstDateId.val(picker.startDate.format('YYYY-MM-DD'));
        $LastDateId.val(picker.endDate.format('YYYY-MM-DD'));
    });

    $calenderId.on('input', function () {
        const value = $(this).val();

        if (value === '') {
            $firstDateId.val("");
            $LastDateId.val("");
        }
    });
}

function allowNumericWithMaxLength(ids, maxLength) {
    $(ids).each(function () {
        var $input = $(this);
        $input.on('input', function () {
            let value = $(this).val().replace(/[^0-9]/g, '');

            if (value.length > maxLength) {
                value = value.substring(0, maxLength);
            }

            $(this).val(value);
        });
    });
};

function removeSpecialCharactersAndNumber(selectors, allowSpace = false) {

    $(selectors).each(function () {
        var selector = $(this);
        selector.on("input", function () {
            if (allowSpace) {
                $(this).val($(this).val().replace(/[^a-zA-Z\s]/g, ''));
            }
            else {
                $(this).val($(this).val().replace(/[^a-zA-Z]/g, ''));
            }
        });
    });
}

function restrictToCityNameFormat(selectors) {
    $(selectors).each(function () {
        const $input = $(this);
        $input.on("input", function () {
            let value = $input.val();

            value = value.replace(/[^a-zA-Z\s.\-']/g, '');
            value = value.substring(0, 75);

            $input.val(value);
        });
    });
}

function enforceLowerCase(selectors) {
    $(selectors).each(function () {
        var selector = $(this);
        selector.on("input", function () {
            $(this).val($(this).val().toLowerCase());
        });
    });
}