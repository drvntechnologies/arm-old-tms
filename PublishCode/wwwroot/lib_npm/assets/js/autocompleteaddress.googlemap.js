function AutoComplete(url, zip, city, state, country, address) {
    var places = new google.maps.places.Autocomplete(document.getElementById(zip));
    google.maps.event.addListener(places, 'place_changed', function () {
        var place = places.getPlace();
        var Address = "";
        var Zip = "";
        var City = "";
        var StateAbbreviation = "";
        var TwoLetterCountry = "";

        for (var i = 0; i < place.address_components.length; i++) {
            var component = place.address_components[i];
            var addressType = component.types[0];

            switch (addressType) {
                case 'street_number':
                    Address = component.long_name + Address;
                    break;
                case 'route':
                    Address += component.short_name;
                    break;
                case 'locality':
                    City = component.long_name;
                    break;
                case 'administrative_area_level_1':
                    StateAbbreviation = component.short_name;
                    break;
                case 'postal_code':
                    Zip = component.long_name;
                    break;
                case 'country':
                    TwoLetterCountry = component.short_name;
                    break;
            }
        }

        if (zip != null)
            document.getElementById(zip).value = Zip;

        if (city != null)
            document.getElementById(city).value = City;

        if (address != null)
            document.getElementById(address).value = Address;

        $.ajax({
            cache: false,
            type: "GET",
            url: url,
            data: {
                stateAbbreviation: StateAbbreviation,
                twoLetterCountry: TwoLetterCountry
            },
            dataType: "json",
            success: function (data, textStatus, jqXHR) {
                if (data.result) {
                    if (state != null)
                        document.getElementById(state).value = data.state;

                    if (country != null)
                        document.getElementById(country).value = data.country;
                }
            },
            error: function (jqXHR, textStatus, errorThrown) {

            }
        });
    });
}