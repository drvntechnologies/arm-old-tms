function DisplayRouteOnMap(id, origin, destination, destinationTimeId, roadDestinationId) {
    var directionsService = new google.maps.DirectionsService;

    var mapOptions = {
        mapTypeControl: true,
        center: { lat: -100.8688, lng: 151.2195 },
        zoom: 20,
        mapTypeId: google.maps.MapTypeId.ROADMAP
    };

    var map = new google.maps.Map(document.getElementById(id), mapOptions);
    var directionsDisplay = new google.maps.DirectionsRenderer({
        map: map,
        preserveViewport: false,
        polylineOptions: {
            strokeColor: '#188ae2',
            strokeOpacity: 1.0
        }
    });
    var request = {
        origin: origin,
        destination: destination,
        travelMode: google.maps.TravelMode.DRIVING,
        avoidHighways: false,
        avoidTolls: false,
        provideRouteAlternatives: true
    };

    try {
        directionsService.route(request, function (response, status) {

            try {

                if (status == google.maps.DirectionsStatus.OK) {
                    directionsDisplay.setDirections(response);
                    var directionsData = response.routes[0].legs[0];
                    $(destinationTimeId).text(directionsData.duration.text);
                    $(roadDestinationId).text("( " + directionsData.distance.text + " )");
                } else if (status === google.maps.DirectionsStatus.ZERO_RESULTS) {

                    var geocoder = new google.maps.Geocoder();

                    geocoder.geocode({ address: origin }, function (originResults, originStatus) {
                        if (originStatus === "OK") {
                            var originLatLng = originResults[0].geometry.location;

                            geocoder.geocode({ address: destination }, function (destResults, destStatus) {
                                if (destStatus === "OK") {
                                    var destinationLatLng = destResults[0].geometry.location;

                                    var flightPath = new google.maps.Polyline({
                                        map: map,
                                        path: [originLatLng, destinationLatLng],
                                        geodesic: true,
                                        strokeColor: '#188ae2',
                                        strokeOpacity: 1.0,
                                    });

                                    function setMarker(latLng, text) {
                                        new google.maps.Marker({
                                            position: latLng,
                                            map: map,
                                            label: {
                                                text: text,
                                                color: "white"
                                            }
                                        });
                                    }

                                    setMarker(originLatLng, "A");
                                    setMarker(destinationLatLng, "B");

                                    var bounds = new google.maps.LatLngBounds();
                                    bounds.extend(originLatLng);
                                    bounds.extend(destinationLatLng);
                                    map.fitBounds(bounds);

                                    var airDist = google.maps.geometry.spherical.computeDistanceBetween(originLatLng, destinationLatLng);
                                    $(destinationTimeId).text("");
                                    $(roadDestinationId).text((airDist / 1000).toFixed(1) + " km by Air");
                                }
                            });
                        }
                    });
                }
            } catch (innerErr) {
                console.error("Directions processing failed for Google Map: ", innerErr);
            }
        });
    } catch (outerErr) {
        console.error("Directions request failed for Google Map: ", outerErr);
    }
}