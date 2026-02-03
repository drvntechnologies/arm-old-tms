function DisplayLiveLocationOnMap(id, origin, destination, latitude, longitude) {
    var directionsService = new google.maps.DirectionsService;
    var lat = latitude;
    var long = longitude;
    var LatLng = new google.maps.LatLng(lat, long);
    var mapOptions = {
        mapTypeControl: false,
        center: LatLng,
        zoom: 20,
        mapTypeId: google.maps.MapTypeId.ROADMAP
    };

    var map = new google.maps.Map(document.getElementById(id), mapOptions);
    var marker = new google.maps.Marker({
        position: LatLng,
        map: map,
        icon: {
            path: 'm 12,2.4000002 c -2.7802903,0 -5.9650002,1.5099999 -5.9650002,5.8299998 0,1.74375 1.1549213,3.264465 2.3551945,4.025812 1.2002732,0.761348 2.4458987,0.763328 2.6273057,2.474813 L 12,24 12.9825,14.68 c 0.179732,-1.704939 1.425357,-1.665423 2.626049,-2.424188 C 16.809241,11.497047 17.965,9.94 17.965,8.23 17.965,3.9100001 14.78029,2.4000002 12,2.4000002 Z',
            fillColor: '#0000FF',
            fillOpacity: 1.0,
            strokeColor: '#000000',
            strokeWeight: 1,
            scale: 2,
            anchor: new google.maps.Point(12, 24),
        },
    });

    var getInfoWindow = new google.maps.InfoWindow({
        content: '<div class="custom-infowindow">Driver Current Location</div>',
        pixelOffset: new google.maps.Size(0, 30)
    });

    marker.addListener('mouseover', function () {
        getInfoWindow.open(map, marker);
    });

    marker.addListener('mouseout', function () {
        getInfoWindow.close();
    });

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
    directionsService.route(request, function (response, status) {
        if (status == google.maps.DirectionsStatus.OK) {
            directionsDisplay.setDirections(response);
        }
    });
}