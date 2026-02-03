//this method is used to show an element by removing the appropriate hiding class
//we don't use the jquery show/hide methods since they don't work with "display: flex" properly
$.fn.showElement = function () {
    this.removeClass('d-none');
}

//this method is used to hide an element by adding the appropriate hiding class
//we don't use the jquery show/hide methods since they don't work with "display: flex" properly
$.fn.hideElement = function () {
    this.addClass('d-none');
}

function setLocation(url) {
    window.location.href = url;
}

function OpenWindow(query, w, h, scroll) {
    var l = (screen.width - w) / 2;
    var t = (screen.height - h) / 2;

    winprops = 'resizable=1, height=' + h + ',width=' + w + ',top=' + t + ',left=' + l + 'w';
    if (scroll) winprops += ',scrollbars=1';
    var f = window.open(query, "_blank", winprops);
}

function showThrobber(message) {
    $('.throbber-header').html(message);
    window.setTimeout(function () {
        $(".throbber").show();
    }, 1000);
}

$(document).ready(function () {
    $('.multi-store-override-option').each(function (k, v) {
        checkOverriddenStoreValue(v, $(v).attr('data-for-input-selector'));
    });

    //we must intercept all events of pressing the Enter button in the search bar to be sure that the input focus remains in the context of the search
    $("div.card-search").keypress(function (event) {
        if (event.which == 13 || event.keyCode == 13) {
            $("button.btn-search").click();
            return false;
        }
    });

    //pressing Enter in the tablex should not lead to any action
    $("div[id$='-grid']").keypress(function (event) {
        if (event.which == 13 || event.keyCode == 13) {
            return false;
        }
    });
});

function checkAllOverriddenStoreValue(item) {
    $('.multi-store-override-option').each(function (k, v) {
        $(v).attr('checked', item.checked);
        checkOverriddenStoreValue(v, $(v).attr('data-for-input-selector'));
    });
}

function checkOverriddenStoreValue(obj, selector) {
    var elementsArray = selector.split(",");

    // first toggle appropriate hidden inputs for checkboxes
    if ($(selector).is(':checkbox')) {
        var name = $(selector).attr('name');
        $('input:hidden[name="' + name + '"]').attr('disabled', !$(obj).is(':checked'));
    }

    if (!$(obj).is(':checked')) {
        $(selector).attr('disabled', true);
        //Kendo UI elements are enabled/disabled some other way
        $.each(elementsArray, function (key, value) {
            var kenoduiElement = $(value).data("kendoNumericTextBox") || $(value).data("kendoMultiSelect");
            if (kenoduiElement !== undefined && kenoduiElement !== null) {
                kenoduiElement.enable(false);
            }
        });
    }
    else {
        $(selector).removeAttr('disabled');
        //Kendo UI elements are enabled/disabled some other way
        $.each(elementsArray, function (key, value) {
            var kenoduiElement = $(value).data("kendoNumericTextBox") || $(value).data("kendoMultiSelect");
            if (kenoduiElement !== undefined && kenoduiElement !== null) {
                kenoduiElement.enable();
            }
        });
    }
}

function bindBootstrapTabSelectEvent(tabsId, inputId) {
    $('#' + tabsId + ' > div ul li a[data-toggle="pill"]').on('shown.bs.tab', function (e) {
        var tabName = $(e.target).attr("data-tab-name");
        $("#" + inputId).val(tabName);
    });
}

function display_nop_error(e) {
    if (e.error) {
        if ((typeof e.error) == 'string') {
            //single error
            //display the message
            //alert(e.error);
            toastr.error(e.error);
        } else {
            //array of errors
            var message = "The following errors have occurred:";
            //create a message containing all errors.
            $.each(e.error, function (key, value) {
                if (value.errors) {
                    message += "\n";
                    message += value.errors.join("\n");
                }
            });
            //display the message
            //alert(message);
            toastr.error(message);
        }
        //ignore empty error
    } else if (e.errorThrown) {
        //alert('Error happened');
        toastr.error('Error happened');
    }
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

function saveUserPreferences(url, name, value) {
    var postData = {
        name: name,
        value: value
    };
    addAntiForgeryToken(postData);
    $.ajax({
        cache: false,
        url: url,
        type: "POST",
        data: postData,
        dataType: "json",
        error: function (jqXHR, textStatus, errorThrown) {
            alert('Failed to save preferences.');
        },
        complete: function (jqXHR, textStatus) {
            $("#ajaxBusy span").removeClass("no-ajax-loader");
        }
    });

};

function warningValidation(validationUrl, warningElementName, passedParameters) {
    addAntiForgeryToken(passedParameters);
    var element = $('[data-valmsg-for="' + warningElementName + '"]');

    var messageElement = element.siblings('.field-validation-custom');
    if (messageElement.length == 0) {
        messageElement = $(document.createElement("span"));
        messageElement.addClass('field-validation-custom');
        element.after(messageElement);
    }

    $.ajax({
        cache: false,
        url: validationUrl,
        type: "POST",
        dataType: "json",
        data: passedParameters,
        success: function (data, textStatus, jqXHR) {
            if (data.Result) {
                messageElement.addClass("warning");
                messageElement.html(data.Result);
            } else {
                messageElement.removeClass("warning");
                messageElement.html('');
            }
        },
        error: function (jqXHR, textStatus, errorThrown) {
            messageElement.removeClass("warning");
            messageElement.html('');
        }
    });
};

function toggleNestedSetting(parentSettingName, parentFormGroupId) {
    if ($('input[name="' + parentSettingName + '"]').is(':checked')) {
        $('#' + parentFormGroupId).addClass('opened');
    } else {
        $('#' + parentFormGroupId).removeClass('opened');
    }
}

function parentSettingClick(e) {
    toggleNestedSetting(e.data.parentSettingName, e.data.parentFormGroupId);
}

function initNestedSetting(parentSettingName, parentSettingId, nestedSettingId) {
    var parentFormGroup = $('input[name="' + parentSettingName + '"]').closest('.form-group');
    var parentFormGroupId = $(parentFormGroup).attr('id');
    if (!parentFormGroupId) {
        parentFormGroupId = parentSettingId;
    }
    $(parentFormGroup).addClass('parent-setting').attr('id', parentFormGroupId);
    if ($('#' + nestedSettingId + ' .form-group').length == $('#' + nestedSettingId + ' .form-group.advanced-setting').length) {
        $('#' + parentFormGroupId).addClass('parent-setting-advanced');
    }

    //$(document).on('click', 'input[name="' + parentSettingName + '"]', toggleNestedSetting(parentSettingName, parentFormGroupId));
    $('input[name="' + parentSettingName + '"]').click(
        { parentSettingName: parentSettingName, parentFormGroupId: parentFormGroupId }, parentSettingClick);
    toggleNestedSetting(parentSettingName, parentFormGroupId);
}

//scroll to top
(function ($) {
    $.fn.backTop = function () {
        var backBtn = this;

        var position = 1000;
        var speed = 900;

        $(document).scroll(function () {
            var pos = $(window).scrollTop();

            if (pos >= position) {
                backBtn.fadeIn(speed);
            } else {
                backBtn.fadeOut(speed);
            }
        });

        backBtn.click(function () {
            $("html, body").animate({ scrollTop: 0 }, 900);
        });
    }
}(jQuery));

// Ajax activity indicator bound to ajax start/stop document events
$(document).ajaxStart(function () {
    $('#ajaxBusy').show();
}).ajaxStop(function () {
    $('#ajaxBusy').hide();
});

//no-tabs solution
$(document).ready(function () {
    $(".card.card-secondary >.card-header").click(CardToggle);

    //expanded
    $('.card.card-secondary').on('expanded.lte.cardwidget', function () {
        WrapAndSaveBlockData($(this), false)

        if ($(this).find('table.dataTable').length > 0) {
            setTimeout(function () {
                ensureDataTablesRendered();
            }, 420);
        }
    });

    //collapsed
    $('.card.card-secondary').on('collapsed.lte.cardwidget', function () {
        WrapAndSaveBlockData($(this), true)
    });
});

function CardToggle() {
    var card = $(this).parent(".card.card-secondary");
    card.CardWidget('toggle');
}

function WrapAndSaveBlockData(card, collapsed) {
    var hideAttribute = card.attr("data-hideAttribute");
    saveUserPreferences(rootAppPath + 'admin/preferences/savepreference', hideAttribute, collapsed);
}

//collapse search block
$(document).ready(function () {
    $(".row.search-row").click(ToggleSearchBlockAndSavePreferences);
});

function ToggleSearchBlockAndSavePreferences() {
    $(this).parents(".card-search").find(".search-body").slideToggle();
    var icon = $(this).find(".icon-collapse i");
    if ($(this).hasClass("opened")) {
        icon.removeClass("fa-angle-up");
        icon.addClass("fa-angle-down");
        saveUserPreferences(rootAppPath + 'admin/preferences/savepreference', $(this).attr("data-hideAttribute"), true);
    } else {
        icon.addClass("fa-angle-up");
        icon.removeClass("fa-angle-down");
        saveUserPreferences(rootAppPath + 'admin/preferences/savepreference', $(this).attr("data-hideAttribute"), false);
    }

    $(this).toggleClass("opened");
}

function ensureDataTablesRendered() {
    $.fn.dataTable.tables({ visible: true, api: true }).columns.adjust();
}

function reloadAllDataTables(itemCount) {
    //depending on the number of elements, the time for animation of opening the menu should increase
    var timePause = 300;
    if (itemCount) {
        timePause = itemCount * 100;
    }
    $('table[class^="table"]').each(function () {
        setTimeout(function () {
            ensureDataTablesRendered();
        }, timePause);
    });
}

/**
 * @param {string} alertId Unique identifier of alert
 * @param {any} text Message text
 */
function showAlert(alertId, text) {
    $('#' + alertId + '-info').text(text);
    $('#' + alertId).click();
}

//scrolling and hidden DataTables issue workaround
//More info - https://datatables.net/examples/api/tabs_and_scrolling.html
$(document).ready(function () {
    $('button[data-card-widget="collapse"]').on('click', function (e) {
        //hack with waiting animation. 
        //when page is loaded, a box that should be collapsed have style 'display: none;'.that's why a table is not updated
        setTimeout(function () {
            ensureDataTablesRendered();
        }, 1);
    });

    // when tab item click
    $('.nav-tabs .nav-item').on('click', function (e) {
        setTimeout(function () {
            ensureDataTablesRendered();
        }, 1);
    });

    $('ul li a[data-toggle="tab"]').on('shown.bs.tab', function (e) {
        ensureDataTablesRendered();
    });

    $('#advanced-settings-mode').on('click', function (e) {
        ensureDataTablesRendered();
    });

    //when sidebar-toggle click
    $('#nopSideBarPusher').on('click', function (e) {
        reloadAllDataTables();
    });
});

/**
 * @param {string} masterCheckbox Master checkbox selector
 * @param {string} childCheckbox Child checkbox selector
 */
function prepareTableCheckboxes(masterCheckbox, childCheckbox) {
    //Handling the event of clicking on the master checkbox
    $(masterCheckbox).click(function () {
        $(childCheckbox).prop('checked', $(this).prop('checked'));
    });

    //Handling the event of clicking on a child checkbox
    $(childCheckbox).change(function () {
        $(masterCheckbox).prop('checked', $(childCheckbox + ':not(:checked)').length === 0 ? true : false);
    });

    //Determining the state of the master checkbox by the state of its children
    $(masterCheckbox).prop('checked', $(childCheckbox).length == $(childCheckbox + ':checked').length && $(childCheckbox).length > 0);
}

function commonPhoneNumber(ids) {
    $(ids).each(function () {
        var $input = $(this);
        $input.attr('placeholder', '(XXX) XXX-XXXX');

        function formatPhone(value) {
            let numbers = value.replace(/\D/g, '').substring(0, 10);
            let formatted = '';

            if (numbers.length > 0) formatted = '(' + numbers.substring(0, 3);
            if (numbers.length >= 4) formatted += ') ' + numbers.substring(3, 6);
            if (numbers.length >= 7) formatted += '-' + numbers.substring(6, 10);

            return formatted;
        }

        // Initial formatting if already has value
        $input.val(formatPhone($input.val()));

        $input.on('input', function () {
            let unformatted = this.value.replace(/\D/g, '');
            let formatted = formatPhone(unformatted);
            this.value = formatted;

            // Attempt to keep cursor at the logical position
            this.setSelectionRange(formatted.length, formatted.length);
        });
    });
}

function allowNumberOnly(ids) {
    $(ids).each(function () {
        var $input = $(this);

        function sanitizeNumber(value) {
            return value.replace(/\D/g, '');
        }

        $input.on('input', function () {
            let sanitized = sanitizeNumber(this.value);
            this.value = sanitized;
        });
    });
}

function allowOnlyPrimaryDomain(ids) {
    $(ids).each(function () {
        var $input = $(this);

        $input.on('input', function () {

            let val = $(this).val()?.toLowerCase() ?? "";

            // Allow only a-z, A-Z, 0-9, ., and -
            val = val.replace(/[^a-zA-Z0-9\.\-]/g, '');

            // Only keep the first dot (for main domain)
            const parts = val.split('.');
            if (parts.length > 2) {
                val = parts[0] + '.' + parts[1]; // discard extra subdomains
            }

            $(this).val(val);
        });
    });
}

// set autocomplete to off.
$('input').attr('autocomplete', 'off');

function allowMaximumCharacters(ids, maxLength = 1000, closest = undefined, charCount = undefined) {
    $(ids).each(function () {
        var $input = $(this);
        $input.on('input', function (event) {
            let text = $(this).val();

            if (text.length > maxLength) {
                $(this).val(text.substring(0, maxLength));
            }

            if (closest && charCount) {
                $(this).closest(closest).find(charCount).text($(this).val().length);
            }
        });
    });
}

function leadQuoteDetailCommonFuncation(id) {
    switch (id) {
        case "btnConvertToQuote":
            clearListAndSetValues('.leadToQuote', "#QuoteDeposit", "#QuotePrice", "0");
            break;

        case "btnReassign":
            clearListAndSetValue('.leadRessign', "#Shipper", "0");
            break;

        case "btnOnHold":
            clearListAndSetValue('.leadOnHold', "#InternalNote", "");
            break;

        case "btnCancel":
            clearListAndSetValue('.leadCancel', "#ShipperNotes", "");
            break;

        case "btnQuoteDetailReassign":
            clearListAndSetValue('.quoteRessign', "#AssignedToValue", "0");
            break;

        case "btnQuoteDetaiOnHold":
            clearListAndSetValue('.quoteOnHold', "#Note", "");
            break;

        case "btnQuoteDetailCancel":
            clearListAndSetValue('.quoteCancel', "#CancelNote", "");
            break;

        // Add more cases as needed

        default:
            console.error("Invalid id: " + id);
    }
}

function clearListAndSetValues(listSelector, input1Selector, input2Selector, value) {
    $(listSelector + ' li').remove();
    $(input1Selector).val(value);
    $(input2Selector).val(value);
}

function clearListAndSetValue(listSelector, inputSelector, value) {
    $(listSelector + ' li').remove();
    $(inputSelector).val(value);
}
function loadPdfIntoIframe(base64Data, contentType) {
    const blob = base64ToBlob(base64Data, contentType);
    const url = URL.createObjectURL(blob);

    // Get the iframe element by ID
    const iframe = document.getElementById('carrierViewFrame');

    // Set the src attribute of the iframe to the Blob URL
    iframe.src = url;
}

// Function to convert Base64 to Blob
function base64ToBlob(base64, type = "application/pdf") {
    const byteCharacters = atob(base64);
    const byteNumbers = new Array(byteCharacters.length);
    for (let i = 0; i < byteCharacters.length; i++) {
        byteNumbers[i] = byteCharacters.charCodeAt(i);
    }
    const byteArray = new Uint8Array(byteNumbers);
    return new Blob([byteArray], { type: type });
}

function openNav(id) {
    const element = document.getElementById(id);
    element.classList.toggle("open");
    $("body").addClass("custom-modal-opened");
}

function closeNav(id) {
    const element = document.getElementById(id);
    element.classList.remove("open");
    $("body").removeClass("custom-modal-opened");
}

$(document).ready(function () {
    try {
        $('select:not([multiple]):not(.no-kendo)').kendoDropDownList({
            filter: "contains",
            filtering: function (e) {
                var filter = e.filter;
            }
        });
    }
    catch
    {

    }
});

function cancelSearch(id) {
    $(id).click(function () {
        location.reload();
    });
}

function roundDecimal(number, decimalPoint = 0, useCurrency = true, useRounding = false) {
    if (isNaN(number) || typeof number !== "number") {
        number = 0;
    }

    let rounded = number;

    if (useRounding) {
        rounded = Math.floor(number) + (number % 1 > 0.01 ? 1 : 0);
    }

    if (useCurrency) {
        return new Intl.NumberFormat("en-US", {
            style: "currency",
            currency: "USD",
            currencySign: "accounting",
            minimumFractionDigits: decimalPoint,
            maximumFractionDigits: decimalPoint
        }).format(rounded);
    } else {
        return decimalPoint > 0 ? rounded.toFixed(decimalPoint) : rounded;
    }
}

function DeliveryPastDate(id) {
    var presentDate = new Date().toISOString().split('T')[0];
    $(id).attr('min', presentDate);
}

function syncActiveDatesBetweenCalendars(sourceCalendarId, targetCalendarId) {

    if ($(sourceCalendarId).length === 0 || $(targetCalendarId).length === 0) {
        return;
    }

    let sourceDatePicker = $(sourceCalendarId).kendoDatePicker({
        format: "MMM dd, yyyy",
        culture: "en-US",
    }).data("kendoDatePicker");

    let targetDatePicker = $(targetCalendarId).kendoDatePicker({
        format: "MMM dd, yyyy",
        culture: "en-US",
    }).data("kendoDatePicker");

    let initialSourceDate = $(sourceCalendarId).val();
    if (initialSourceDate) {
        targetDatePicker.min(initialSourceDate);
        //$(targetCalendarId).value(initialSourceDate);
    }

    sourceDatePicker.bind("change", function () {
        var startDate = this.value();
        if (startDate) {
            targetDatePicker.min(startDate);
            //$(targetCalendarId).val(moment(startDate).format('MMM DD, YYYY'));
        }
    });
}

function isTodayOrPastDate(endDate) {
    const date = moment(endDate);

    const today = moment().startOf('day');

    return date.isSameOrBefore(today, 'day');
}

function calculateDateRange(startDate, endDate) {
    const start = moment(startDate);
    const end = moment(endDate);

    const differenceInDays = end.diff(start, 'days') + 1;

    return differenceInDays;
}


function prepareDateRangePicker(calenderId, firstDateId, LastDateId, setEndDateRange = 1, setDestinationDateRange = null, url = null, formateDate = 'MMM DD, YYYY') {
    let $firstDateId = $(firstDateId);
    let $LastDateId = $(LastDateId);

    let startDate = $firstDateId.val();
    let endDate = $LastDateId.val();
    //if (!startDate) {
    //    $firstDateId.val(moment().startOf('day').format('YYYY-MM-DD'));
    //}

    //if (!endDate) {
    //    $LastDateId.val(moment().add(1, 'days').format('YYYY-MM-DD'));
    //}

    var minDate = moment();
    if (isTodayOrPastDate(startDate)) {
        minDate = moment(startDate);
    }

    $(calenderId).daterangepicker({
        locale: {
            format: formateDate,
        },
        minDate: minDate,
        timePicker: false,
        timePicker24Hour: false,
        timePickerSeconds: false,
        autoUpdateInput: false,
        startDate: startDate ? moment(startDate) : moment().startOf('day'),
        endDate: endDate ? moment(endDate) : moment().add(setEndDateRange, 'days')
    }, function (start, end) {
        $(calenderId).val(start.format(formateDate) + ' - ' + end.format(formateDate));
        $firstDateId.val(start.format('YYYY-MM-DD'));
        $LastDateId.val(end.format('YYYY-MM-DD'));

        setDeliverySpreadDate(url, setDestinationDateRange, calenderId, true, formateDate);
    });

    if (startDate && endDate) {
        $(calenderId).val(moment(startDate).format(formateDate) + ' - ' + moment(endDate).format(formateDate));

        //const picker = $(calenderId).data('daterangepicker')
        //picker.minDate = moment(startDate);

        //if (isTodayOrPastDate(endDate)) {

        //    const range = calculateDateRange(startDate, endDate);
        //    const picker = $(calenderId).data('daterangepicker');
        //    picker.setStartDate(moment().startOf('day'));
        //    picker.setEndDate(moment().add(range, 'days'));
        //}
    }
    else {
        $(calenderId).val("");
    }

    $(calenderId).on('apply.daterangepicker', function (ev, picker) {
        $(this).val(picker.startDate.format(formateDate) + ' - ' + picker.endDate.format(formateDate));
        $firstDateId.val(picker.startDate.format('YYYY-MM-DD'));
        $LastDateId.val(picker.endDate.format('YYYY-MM-DD'));

        setDeliverySpreadDate(url, setDestinationDateRange, calenderId, true, formateDate);
    });

    $(calenderId).on('input', function () {
        const value = $(this).val();

        if (value === '') {
            $firstDateId.val("");
            $LastDateId.val("");

            setDeliverySpreadDate(url, setDestinationDateRange, calenderId, true, formateDate);
        }
    });
}

function addDateInMoment(momentDate, day = 0) {

    if (!moment.isMoment(momentDate)) {
        momentDate = moment();
    }

    if (!momentDate.isValid()) {
        momentDate = moment();
    }

    if (day == 0) {
        return momentDate.startOf('day');
    }

    return momentDate.add(day, 'days');
}

let originLat = 0;
let originLng = 0;
let destinationLat = 0;
let destinationLng = 0;
let distanceInMile = 0;

function setDeliverySpreadDate(url, destinationId, sourceId, setToDefaultFillInInput = false, formateDate = 'MMM DD, YYYY', distanceId = '') {

    if (typeof (url) !== "string" || url.trim().length <= 0 ||
        typeof (destinationId) !== "string" || destinationId.trim().length <= 0) {

        return false;
    }

    if ((originLat == 0 && originLng == 0) || (destinationLat == 0 && destinationLng == 0)) {
        const deliveryStartDate = $(destinationId).closest('div.input-name').find(".startdate");
        const deliveryEndDate = $(destinationId).closest('div.input-name').find(".enddate");

        $(destinationId).val("");
        deliveryStartDate.val("");
        deliveryEndDate.val("");

        distanceInMile = 0;

        if (distanceId != undefined && distanceId != null && distanceId.length > 0) {
            $(distanceId).val(`${distanceInMile} Mile`);
        }

        return false;
    }

    let postData = {
        srcLat1: originLat,
        srcLon1: originLng,
        destLat2: destinationLat,
        destLon2: destinationLng,
    };

    addAntiForgeryToken(postData);

    $.post(url, postData, function (response) {
        let picker = $(sourceId).data('daterangepicker');

        let startDate = moment(picker.startDate);
        if (!startDate.isValid()) {
            startDate = moment();
        }

        let endDate = moment(picker.endDate);
        if (!endDate.isValid()) {
            endDate = moment();
        }

        const deliverySpread = $(destinationId).data('daterangepicker');

        if (response >= 0 && response < 501) {

            deliverySpread.setStartDate(addDateInMoment(startDate, 0));
            deliverySpread.setEndDate(addDateInMoment(endDate, 3));

        } else if (response >= 501 && response < 1001) {

            deliverySpread.setStartDate(addDateInMoment(startDate, 1));
            deliverySpread.setEndDate(addDateInMoment(endDate, 5));

        } else if (response >= 1001 && response < 1251) {

            deliverySpread.setStartDate(addDateInMoment(startDate, 2));
            deliverySpread.setEndDate(addDateInMoment(endDate, 7));

        } else if (response >= 1251 && response < 1501) {

            deliverySpread.setStartDate(addDateInMoment(startDate, 2));
            deliverySpread.setEndDate(addDateInMoment(endDate, 7));

        } else if (response >= 1501 && response < 2001) {

            deliverySpread.setStartDate(addDateInMoment(startDate, 3));
            deliverySpread.setEndDate(addDateInMoment(endDate, 9));

        } else if (response >= 2001 && response < 2501) {

            deliverySpread.setStartDate(addDateInMoment(startDate, 4));
            deliverySpread.setEndDate(addDateInMoment(endDate, 10));

        } else if (response >= 2501 && response < 3001) {

            deliverySpread.setStartDate(addDateInMoment(startDate, 4));
            deliverySpread.setEndDate(addDateInMoment(endDate, 12));

        } else if (response >= 3001) {

            deliverySpread.setStartDate(addDateInMoment(startDate, 4));
            deliverySpread.setEndDate(addDateInMoment(endDate, 14));

        }

        if (setToDefaultFillInInput) {
            const value = $(sourceId).val();

            const deliveryStartDate = $(destinationId).closest('div.input-name').find(".startdate");
            const deliveryEndDate = $(destinationId).closest('div.input-name').find(".enddate");

            if (value.trim().length === 0) {
                $(destinationId).val("");
                deliveryStartDate.val("");
                deliveryEndDate.val("");
            } else {
                $(destinationId).val(deliverySpread.startDate.format(formateDate) + ' - ' + deliverySpread.endDate.format(formateDate));
                deliveryStartDate.val(deliverySpread.startDate.format('YYYY-MM-DD'));
                deliveryEndDate.val(deliverySpread.endDate.format('YYYY-MM-DD'));
            }
        }

        distanceInMile = parseFloat(response).toFixed(0);

        if (distanceId != undefined && distanceId != null && distanceId.length > 0) {
            if (distanceInMile == 0 || distanceInMile == 1) {
                $(distanceId).val(`${distanceInMile} Mile`);
            }
            else {
                $(distanceId).val(`${distanceInMile} Miles`);
            }
        }
    });
}

function setDeliverySpreadDateFromDistance(distance, destinationId, sourceId, selectId, setToDefaultFillInInput = false, formateDate = 'MMM DD, YYYY') {

    if (!destinationId || $(destinationId).length == 0) {
        return false;
    }

    const deliveryStartDate = $(destinationId).closest('div.input-name').find(".startdate");
    const deliveryEndDate = $(destinationId).closest('div.input-name').find(".enddate");

    if (isNaN(distance) || typeof (distance) !== "number" || !distance || distance === 0) {
        $(destinationId).val("");
        deliveryStartDate.val("");
        deliveryEndDate.val("");

        return false;
    }

    const pickupStartDate = $(selectId).closest('div.input-name').find(".startdate");
    const pickupEndDate = $(selectId).closest('div.input-name').find(".enddate");

    let startDate = pickupStartDate.val();
    let endDate = pickupEndDate.val();
    //let srcElement = document.querySelector(sourceId);
    //if (srcElement && srcElement._flatpickr) {
    //    let picker = srcElement._flatpickr;
    //    if (picker.selectedDates.length > 0) {
    //        startDate = picker.selectedDates[0];
    //        endDate = picker.selectedDates[picker.selectedDates.length - 1];
    //    }
    //}

    startDate = moment(startDate);
    if (!startDate.isValid()) {
        startDate = moment();
    }

    endDate = moment(endDate);
    if (!endDate.isValid()) {
        endDate = moment();
    }

    let startDateValue = "";
    let endDateValue = "";

    if (distance >= 0 && distance < 501) {

        startDateValue = addDateInMoment(startDate, 0);
        endDateValue = addDateInMoment(endDate, 3);

    } else if (distance >= 501 && distance < 1001) {

        startDateValue = addDateInMoment(startDate, 1);
        endDateValue = addDateInMoment(endDate, 5);

    } else if (distance >= 1001 && distance < 1251) {

        startDateValue = addDateInMoment(startDate, 2);
        endDateValue = addDateInMoment(endDate, 7);

    } else if (distance >= 1251 && distance < 1501) {

        startDateValue = addDateInMoment(startDate, 2);
        endDateValue = addDateInMoment(endDate, 7);

    } else if (distance >= 1501 && distance < 2001) {
        startDateValue = addDateInMoment(startDate, 3);
        endDateValue = addDateInMoment(endDate, 9);

    } else if (distance >= 2001 && distance < 2501) {

        startDateValue = addDateInMoment(startDate, 4);
        endDateValue = addDateInMoment(endDate, 10);

    } else if (distance >= 2501 && distance < 3001) {

        startDateValue = addDateInMoment(startDate, 4);
        endDateValue = addDateInMoment(endDate, 12);

    } else if (distance >= 3001) {

        startDateValue = addDateInMoment(startDate, 4);
        endDateValue = addDateInMoment(endDate, 14);

    }

    if (setToDefaultFillInInput) {
        const value = $(selectId).val();

        if (value.trim().length === 0) {
            $(destinationId).val("");
            deliveryStartDate.val("");
            deliveryEndDate.val("");
        } else {
            $(destinationId).val(startDateValue.format(formateDate) + ' - ' + endDateValue.format(formateDate));
            deliveryStartDate.val(startDateValue.format('YYYY-MM-DD'));
            deliveryEndDate.val(endDateValue.format('YYYY-MM-DD'));
        }
    }
}

function convertToNumber(value) {
    var converted = Number(value);
    return isNaN(converted) ? 0 : converted;
}

function filterValidationMessagesById(validationSummaryId) {
    var validationSummary = $("#" + validationSummaryId);

    if (validationSummary.length > 0) {
        validationSummary.find("ul li").each(function () {
            var message = $(this);

            if (message.text().trim() === "") {
                message.hide();
            }
            else if (message.text().includes("required")) {
                message.fadeOut(500);
            } else {
                message.show();
            }
        });
    }
}

window.onload = function () {
    filterValidationMessagesById("custom-validation-summary");
};

function setDateRangePicker(calenderId, blackoutDates, setLastDate = 1, setDestinationDateRange = null, url = null, formateDate = 'MMM DD, YYYY', distanceId = '') {

    function getNextWorkingDay(date) {
        while (date.day() === 0 || date.day() === 6 || date.day() === moment().day() || blackoutDates.includes(date.format("MM/DD/YYYY"))) {
            date.add(1, 'days');
        }

        return date;
    }

    let $calenderId = $(calenderId);

    let $firstDateId = $calenderId.closest('div.input-name').find('.startdate').first();
    let $LastDateId = $calenderId.closest('div.input-name').find('.enddate').first();

    let startDate = $firstDateId.val();
    let endDate = $LastDateId.val();

    let minDate = moment();
    if (isTodayOrPastDate(startDate)) {
        minDate = moment(startDate);
    } else {
        minDate = getNextWorkingDay(minDate);
    }

    function getDefaultRange(minDate) {
        let start = minDate;
        let end = start.clone().add(setLastDate, 'days');

        // If today is Sunday, start from next Monday
        if (start.day() === 0 || end.day() === 0 || blackoutDates.includes(start.format("MM/DD/YYYY"))) {
            start.add(1, 'week').startOf('week').add(1, 'days'); // Move to next Monday
            end = start.clone().add(setLastDate, 'days');
        }
        //else {
        //    // If end date lands on Sunday, move it to Monday
        //    if (end.day() === 0) end.add(1, 'days');
        //}

        return { start, end };
    }

    let range = getDefaultRange(minDate);

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
            // Disable Sunday (0)
            return date.day() === 0 || blackoutDates.includes(date.format("MM/DD/YYYY"));
        }
    }, function (start, end) {
        $calenderId.val(start.format('MM/DD/YYYY') + ' - ' + end.format('MM/DD/YYYY'));
        $firstDateId.val(start.format('YYYY-MM-DD'));
        $LastDateId.val(end.format('YYYY-MM-DD'));

        setDeliverySpreadDate(url, setDestinationDateRange, calenderId, true, formateDate, distanceId);
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

        setDeliverySpreadDate(url, setDestinationDateRange, calenderId, true, formateDate, distanceId);
    });

    $calenderId.on('input', function () {
        const value = $(this).val();

        if (value === '') {
            $firstDateId.val("");
            $LastDateId.val("");
            $calenderId.daterangepicker({
                startDate: range.start,
                endDate: range.end
            });

            setDeliverySpreadDate(url, setDestinationDateRange, calenderId, true, formateDate, distanceId);
        }
    });
}

function setSingleDatePicker(calenderId, blackoutDates, setDestinationDateRange = null, url = null, formateDate = 'MMM DD, YYYY', distanceId = '') {

    function getNextWorkingDay(date) {
        while (date.day() === 0 || date.day() === 6 || date.day() === moment().day() || blackoutDates.includes(date.format("MM/DD/YYYY"))) {
            date.add(1, 'days');
        }

        return date;
    }

    let $calenderId = $(calenderId);

    let $firstDateId = $calenderId.closest('div.input-name').find('.startdate').first();

    let startDate = $firstDateId.val();

    let minDate = moment();
    if (isTodayOrPastDate(startDate)) {
        minDate = moment(startDate);
    }
    else {
        minDate = getNextWorkingDay(minDate);
    }

    $calenderId.daterangepicker({
        locale: {
            format: 'MM/DD/YYYY',
        },
        singleDatePicker: true,
        minDate: minDate,
        timePicker: false,
        timePicker24Hour: false,
        timePickerSeconds: false,
        autoUpdateInput: false,
        startDate: startDate ? moment(startDate) : moment().startOf('day'),
        isInvalidDate: function (date) {
            return date.day() === 0 || date.day() === 6 || blackoutDates.includes(date.format("MM/DD/YYYY"));
        }
    }, function (start, end) {
        $calenderId.val(start.format('MM/DD/YYYY'));
        $firstDateId.val(start.format('YYYY-MM-DD'));

        setDeliverySpreadDate(url, setDestinationDateRange, calenderId, true, formateDate, distanceId);
    });

    if (startDate) {
        $calenderId.val(moment(startDate).format('MM/DD/YYYY'));
    }
    else {
        $calenderId.val("");
    }

    $calenderId.on('apply.daterangepicker', function (ev, picker) {
        $(this).val(picker.startDate.format('MM/DD/YYYY'));
        $firstDateId.val(picker.startDate.format('YYYY-MM-DD'));

        setDeliverySpreadDate(url, setDestinationDateRange, calenderId, true, formateDate, distanceId);
    });

    $calenderId.on('input', function () {
        const value = $(this).val();

        if (value === '') {
            $firstDateId.val("");
            $calenderId.daterangepicker({
                startDate: moment().startOf('day'),
            });

            setDeliverySpreadDate(url, setDestinationDateRange, calenderId, true, formateDate, distanceId);
        }
    });
}

function allowAplhaNumericWithMaxLength(ids, maxLength) {
    $(ids).each(function () {
        var $input = $(this);
        $input.on('input', function () {
            let value = $(this).val().replace(/[^a-zA-Z0-9]/g, '');

            if (value.length > maxLength) {
                value = value.substring(0, maxLength);
            }

            $(this).val(value);
        });
    });
};

function allowUniversalZip(ids, maxLength = 12, showPlaceholder = true) {
    $(ids).each(function () {
        var $input = $(this);

        if (showPlaceholder) {
            $input.attr('placeholder', "Enter Zip Code");
        }

        $input.on('input', function () {
            let value = $(this).val().replace(/[^A-Za-z0-9\s\-]/g, '');

            if (value.length > maxLength) {
                value = value.substring(0, maxLength);
            }

            $(this).val(value);
        });
    });
};

function allowNumericWithMaxLength(ids, maxLength, showPlaceholder = true) {
    $(ids).each(function () {
        var $input = $(this);

        if (showPlaceholder) {
            $input.attr('placeholder', "X".repeat(maxLength));
        }

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

function toBoolean(val) {
    if (typeof val === 'boolean') return val;

    if (typeof val === 'string') {
        const str = val.trim().toLowerCase();
        return str === 'true' || str === 'yes';
    }

    return false;
}

function addKendoDropdownValue($id, value) {
    if ((!($id instanceof jQuery))) return false;

    if ($id.length <= 0) return false;

    $id.data("kendoDropDownList").value(value);
}

let vehicles = [];

function getVehicleJson(url) {

    if (typeof (url) !== 'string') return false;

    return $.getJSON(url)
        .then(function (data) {
            return data;
        })
        .catch(function (jqXHR, textStatus, errorThrown) {
            console.error("Failed to load JSON file.");
            return [];
        });
}

function prepareMakeForVehicle(make) {

    if (typeof (make) !== 'string') return false;

    $(document).on("input focus", make, function () {
        var inputField = $(this);
        var searchVal = inputField.val();

        var makes = [...new Set(vehicles.map(v => v.make))]
                        .filter(m => searchVal
                            ? m.toLowerCase().includes(searchVal.trim().toLowerCase())
                            : true);            
        showDropdown(inputField, makes);        
    });
}

function prepareModelForVehicle(model, make, closestField = "tr") {

    if (typeof (model) !== 'string' || typeof (make) !== 'string') return false;

    $(document).on("input focus", model, function () {
        let inputField = $(this);
        let searchVal = inputField.val();
        let makeInput = inputField.closest(closestField).find(make).val();

        if (makeInput.length == 0) {            
            hideDropdown();            
            return false;
        }

        let filteredModels = vehicles
            .filter(v => v.make.toLowerCase() === makeInput.toLowerCase())
            .map(v => v.model)
            .filter(m => searchVal ? m.toLowerCase().includes(searchVal.trim().toLowerCase()) : true);
        
        showDropdown(inputField, [...new Set(filteredModels)]);        
    });
}

function showDropdown(inputField, suggestions) {
    hideDropdown();
    var $dropdown = $("<div class='suggestion-list'></div>");
    var dropdown = $("<ul></ul>");

    if (suggestions.length === 0) {
        dropdown.append("<li class='no-records'>No records available</li>");
    } else {
        suggestions.forEach(function (item) {
            dropdown.append("<li>" + item + "</li>");
        });
    }

    $dropdown.append(dropdown);
    //inputField.after($dropdown);

    //$dropdown.css({
    //    width: inputField.outerWidth(),
    //});

    $("body").append($dropdown);

    function updateDropdownPosition() {
        let inputOffset = inputField.offset();
        let inputHeight = inputField.outerHeight();
        let inputWidth = inputField.outerWidth();
        let dropdownWidth = inputWidth; // Match input width

        let leftPos = inputOffset.left;
        let rightBoundary = $(window).width() - (leftPos + dropdownWidth);

        if (rightBoundary < 0) {
            leftPos += rightBoundary - 10;
        }

        $dropdown.css({
            width: dropdownWidth + "px",
            position: "absolute",
            top: (inputOffset.top + inputHeight) + "px",
            left: leftPos + "px",
            display: "block",
            zIndex: 1050
        });
    }

    updateDropdownPosition();

    let listItems = dropdown.find("li");
    let selectedIndex = -1;
    let usingMouse = true;

    inputField.off("keydown").on("keydown", function (e) {
        if (listItems.length === 0) return;

        if (e.key === "ArrowDown" || e.key === "ArrowUp") {
            usingMouse = false;
        }

        if (e.key === "ArrowDown") {
            e.preventDefault();
            selectedIndex = (selectedIndex + 1) % listItems.length;
        }
        else if (e.key === "ArrowUp") {
            e.preventDefault();
            selectedIndex = (selectedIndex - 1 + listItems.length) % listItems.length;
        }
        else if (e.key === "Enter") {
            e.preventDefault();
            if (selectedIndex >= 0 && !$(listItems.eq(selectedIndex)).hasClass("no-records")) {
                inputField.val(listItems.eq(selectedIndex).text());
                hideDropdown();
            }
        }

        listItems.removeClass("active");

        if (selectedIndex < 0) {
            return;
        }
        var activeItem = listItems.eq(selectedIndex);
        activeItem.addClass("active");

        if (activeItem.length) {
            activeItem[0].scrollIntoView({ block: 'nearest', behavior: 'smooth' });
        }
    });

    listItems.on("mouseenter", function () {
        if (!usingMouse) {
            listItems.removeClass("active");
            $(this).addClass("active");
            selectedIndex = listItems.index(this);
        }
    });

    listItems.on("mouseleave", function () {
        usingMouse = true;
    });

    $(document).on("mousedown", ".suggestion-list li", function () {
        if (!$(this).hasClass("no-records")) {
            var inputField = $(this).closest(".suggestion-list").prev("input");
            inputField.val($(this).text());
            hideDropdown();
        }
    });

    $(".suggestion-list li").on("mousedown", function () {
        if (!$(this).hasClass("no-records")) {
            inputField.val($(this).text());
            hideDropdown();
        }
    });

    // **Detect scrolling inside `.table-responsive` and update dropdown position**
    $(".table-responsive").on("scroll", function () {
        updateDropdownPosition();
    });

    // Handle window resize or scroll to keep alignment
    $(window).on("resize scroll", function () {
        updateDropdownPosition();
    });
}

function hideDropdown() {
    $(".suggestion-list").remove();
}

$(document).on("mousedown", function (e) {
    if ($(e.target).attr('data-type') === "Make" ||
        $(e.target).attr('data-type') === "Model") {

        return;
    }

    if (!$(e.target).closest(".suggestion-list").length) {
        hideDropdown();
    }
});

function debounce(func, delay) {
    let timer;
    return function (...args) {
        if (timer) {
            clearTimeout(timer);
        }
        timer = setTimeout(() => func.apply(this, args), delay);
    };
}

function isValidEmail(email) {
    if (typeof (email) !== "string" || email.trim().length <= 0) {
        return false;
    }

    var regex = /^(?!\.)[a-zA-Z0-9._%+-]{1,64}@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;
    if (!regex.test(email)) {
        return false;
    }

    return true;
}

function openPicker(id) {
    $(id).css("display", "block");
}

function closePicker(id) {
    $(id).css("display", "none");
}

function setDateRangePickerByFlatpickr(id, applyBtnId, setInputId, divWrapperId, blackoutDates, headerContentHtml = "", setDestinationDateRange = "", formateDate = 'MMM DD, YYYY') {
    let isProcessing = false;

    let selectedDate = null;
    let today = new Date();
    today.setHours(0, 0, 0, 0);

    function isWeekend(date) {
        let day = date.getDay();
        return day === 0;
    }

    function isBlackoutDate(date) {
        let dateStr = moment(date).format("YYYY-MM-DD");
        let adjustedBlackoutDates = blackoutDates.map(date => moment(date).format("YYYY-MM-DD"));
        return adjustedBlackoutDates.includes(dateStr);
    }

    function isTodayOrPastDate(date) {
        return date <= today;
    }

    function getValidConsecutiveDays(startDate) {
        let selectedDays = [];
        let tempDate = new Date(startDate);
        let forwardDate = new Date(startDate);
        let backwardDate = new Date(startDate);

        while (selectedDays.length < 3 && !isWeekend(forwardDate) && !isBlackoutDate(forwardDate) && !isTodayOrPastDate(forwardDate)) {
            selectedDays.push(new Date(forwardDate));
            forwardDate.setDate(forwardDate.getDate() + 1);
        }

        while (selectedDays.length < 3 && !isWeekend(backwardDate) && !isBlackoutDate(backwardDate) && !isTodayOrPastDate(backwardDate)) {
            backwardDate.setDate(backwardDate.getDate() - 1);
            selectedDays.unshift(new Date(backwardDate));
        }

        return selectedDays.length === 3 ? selectedDays : null;
    }

    function disableInvalidDates(date) {
        let selectedDays = getValidConsecutiveDays(date);
        if (!selectedDays) return true;
        return selectedDays.some(d => isWeekend(d) || isBlackoutDate(d) || isTodayOrPastDate(d));
    }

    function getNextWorkingDay(date) {
        while (date.day() === 0 || date.day() === 6 || date.day() === moment().day() || blackoutDates.includes(date.format("MM/DD/YYYY"))) {
            date.add(1, 'days');
        }

        return date;
    }

    let $calenderId = $(id);

    let $firstDateId = $calenderId.closest('div.input-name').find('.startdate').first();
    let $lastDateId = $calenderId.closest('div.input-name').find('.enddate').first();

    let startDate = $firstDateId.val();
    let endDate = $lastDateId.val();

    if (startDate && endDate) {
        $(setInputId)
            .val(moment(startDate).format('MM/DD/YYYY')
                + ' - '
                + moment(endDate).format('MM/DD/YYYY'))
            .trigger('change');
    }
    else {
        $(setInputId)
            .val("")
            .trigger('change');
    }

    let minDate = moment();
    if (isTodayOrPastDate(startDate)) {
        minDate = moment(startDate);
    }
    else {
        minDate = getNextWorkingDay(minDate);
    }

    flatpickr(id, {
        dateFormat: "m-d-Y",
        allowInput: false,
        inline: true,
        disable: [disableInvalidDates],
        minDate: "today",
        onReady: function (selectedDates, dateStr, instance) {
            // Create a heading element
            let heading = document.createElement("div");
            heading.innerHTML = headerContentHtml;

            // Insert the heading before the calendar
            instance.calendarContainer.insertBefore(heading, instance.calendarContainer.firstChild);
        },
        onChange: function (selectedDates, dateStr, instance) {
            if (selectedDates.length === 0) return;
            selectedDate = selectedDates[0];
            let selectedDays = getValidConsecutiveDays(selectedDate);
            if (!selectedDays) return;

            document.querySelectorAll(".flatpickr-day").forEach(day => {
                day.classList.remove("highlighted");
            });

            setTimeout(() => {
                document.querySelectorAll(".flatpickr-day").forEach(day => {
                    let dayDate = day.dateObj;
                    if (selectedDays.some(d =>
                        d.getFullYear() === dayDate.getFullYear() &&
                        d.getMonth() === dayDate.getMonth() &&
                        d.getDate() === dayDate.getDate())) {
                        day.classList.add("highlighted");

                        let startDate = moment(selectedDays[0]).format("MM/DD/YYYY");
                        let endDate = moment(selectedDays[selectedDays.length - 1]).format("MM/DD/YYYY");

                        $(setInputId).val(`${startDate} - ${endDate}`).trigger('change');
                        $firstDateId.val(moment(selectedDays[0]).format('YYYY-MM-DD'));
                        $lastDateId.val(moment(selectedDays[selectedDays.length - 1]).format('YYYY-MM-DD'));

                        setDeliverySpreadDateFromDistance(distanceInMile, setDestinationDateRange, id, setInputId, true, formateDate);
                    }

                    isProcessing = true;
                });
            }, 10);
        }
    });

    $(document).on("click", applyBtnId, function (e) {
        e.preventDefault();

        if (selectedDate) {
            let selectedDays = getValidConsecutiveDays(selectedDate);
            if (selectedDays) {
                let startDate = moment(selectedDays[0]).format("MM/DD/YYYY");
                let endDate = moment(selectedDays[selectedDays.length - 1]).format("MM/DD/YYYY");

                $(setInputId).val(`${startDate} - ${endDate}`).trigger('change');
                $firstDateId.val(moment(selectedDays[0]).format('YYYY-MM-DD'));
                $lastDateId.val(moment(selectedDays[selectedDays.length - 1]).format('YYYY-MM-DD'));
            }
        }
        closePicker(divWrapperId);
    });

    //Close picker on outside click & update input if a date was selected
    $(document).on("click", function (event) {
        let wrapper = $(divWrapperId);
        let input = $(setInputId);

        const hasCalenderIcon = event.target.classList.contains("fa-calendar-alt") || event.target.classList.contains("pickup-date-picker");
        if (hasCalenderIcon) {
            return;
        }

        if (wrapper.length && !$.contains(wrapper[0], event.target) && event.target !== input[0] || !event.target.hasClass === "pickup-date-picker") {
            if (isProcessing && selectedDate) {
                let selectedDays = getValidConsecutiveDays(selectedDate);
                if (selectedDays) {
                    let startDate = moment(selectedDays[0]).format("MM/DD/YYYY");
                    let endDate = moment(selectedDays[selectedDays.length - 1]).format("MM/DD/YYYY");

                    $(setInputId).val(`${startDate} - ${endDate}`).trigger('change');
                    $firstDateId.val(moment(selectedDays[0]).format('YYYY-MM-DD'));
                    $lastDateId.val(moment(selectedDays[selectedDays.length - 1]).format('YYYY-MM-DD'));
                }
                isProcessing = false;
            }
            closePicker(divWrapperId);
        }
    });
}

function setDatePickerByFlatpickr(id, blackoutDates, headerContentHtml = "", setDestinationDateRange = "", formateDate = 'MMM DD, YYYY') {
    //let selectedDate = null;
    let today = new Date();
    today.setHours(0, 0, 0, 0);

    function isWeekend(date) {
        let day = date.getDay();
        return day === 0 || day === 6;
    }

    function isBlackoutDate(date) {
        let dateStr = moment(date).format("YYYY-MM-DD");
        let adjustedBlackoutDates = blackoutDates.map(date => moment(date).format("YYYY-MM-DD"));
        return adjustedBlackoutDates.includes(dateStr);
    }

    function isTodayOrPastDate(date) {
        return date <= today;
    }

    function disableInvalidDates(date) {
        return isWeekend(date) || isBlackoutDate(date) || isTodayOrPastDate(date);
    }

    function getNextWorkingDay(date) {
        while (date.day() === 0 || date.day() === 6 || date.day() === moment().day() || blackoutDates.includes(date.format("MM/DD/YYYY"))) {
            date.add(1, 'days');
        }

        return date;
    }

    let $calenderId = $(id);

    let $firstDateId = $calenderId.closest('div.input-name').find('.startdate').first();
    let $lastDateId = $calenderId.closest('div.input-name').find('.enddate').first();

    let startDate = $firstDateId.val();

    if (startDate) {
        $calenderId.val(moment(startDate).format('MM/DD/YYYY')).trigger('change');
    }
    else {
        $calenderId.val("").trigger('change');
    }

    let minDate = moment();
    if (isTodayOrPastDate(startDate)) {
        minDate = moment(startDate);
    }
    else {
        minDate = getNextWorkingDay(minDate);
    }

    flatpickr(id, {
        dateFormat: "m-d-Y",
        allowInput: false,
        disable: [disableInvalidDates],
        minDate: "today",
        onReady: function (selectedDates, dateStr, instance) {
            // Create a heading element
            let heading = document.createElement("div");
            heading.innerHTML = headerContentHtml;

            // Insert the heading before the calendar
            instance.calendarContainer.insertBefore(heading, instance.calendarContainer.firstChild);
        },
        onChange: function (selectedDates, dateStr, instance) {
            if (selectedDates.length === 0) return;
            //selectedDate = selectedDates[0];

            let formattedDate = moment(selectedDates[0]).format("MM/DD/YYYY");
            $calenderId.val(formattedDate).trigger('change');

            $firstDateId.val(moment(selectedDates[0]).format('YYYY-MM-DD'));
            $lastDateId.val(moment(selectedDates[0]).format('YYYY-MM-DD'));

            setDeliverySpreadDateFromDistance(distanceInMile, setDestinationDateRange, id, id, true, formateDate);
        }
    });
}

function clearDatFlatPicker(setInputId) {
    try {
        let picker = document.querySelector(setInputId)._flatpickr;
        if (picker) {
            picker.clear();
        }
        $(setInputId).val("");
    }
    catch (error) {
        console.error(error);
    }
}

async function getData(url, data) {
    return new Promise((resolve, reject) => {
        $.get(url, data)
            .done(response => resolve(response))
            .fail(error => reject(error));
    });
}

async function getDistance(url, origin, destination) {

    if (typeof (url) !== "string" || url.trim().length <= 0 ||
        typeof (origin) !== "string" || origin.trim().length != 5 ||
        typeof (destination) !== "string" || destination.trim().length != 5) {

        distanceInMile = 0;

        return distanceInMile;
    }

    let postData = {
        origin: origin,
        destination: destination
    };

    addAntiForgeryToken(postData);

    try {
        let response = await getData(url, postData);

        if (response) {
            distanceInMile = response.Item3;
            return distanceInMile;
        } else {
            distanceInMile = 0;
            return distanceInMile;
        }
    } catch (error) {
        console.error("Error: ", error);

        distanceInMile = 0;
        return distanceInMile;
    }
}