var LandingPageController = function ($scope, $http, $filter, $rootScope) {

    $scope.highlight = [];
    $scope.showDBListToggle = true;
    $scope.toggleDBList = function () {
        $scope.showDBListToggle = !$scope.showDBListToggle;
    }

    $scope.decideList = function (serverName) {
        console.log(serverName);

        var show;
        for (var i = 0; i < $scope.servers.length; i++) {

            if ($scope.servers[i].name == serverName) {
                show = true;
            }
            else {
                show = false;
            }
            console.log($scope.servers[i].name + " and " + show);
        }
        console.log(serverName + " " + show);

        return show;

    }




    $scope.pageLoading = false;
    $scope.database = null;
    $scope.table = null;
    $scope.tables = [];
    delete $scope.error;
    $scope.servers = [];
    $scope.data = {
        selectedTable: ""
    };
    $scope.rememberMe = false;
    $scope.showSelect = false;

    $scope.showServerAddedMessage = true;

    $scope.searchFocus = function () {
        $scope.showSelect = true;
    }
    $scope.searchText = {};
    $scope.showAll = false;

    $scope.sort = {
        column: '',
        descending: false
    };
    $scope.changeSorting = function (column) {

        var sort = $scope.sort;

        if (sort.column == column) {
            sort.descending = !sort.descending;
        } else {
            sort.column = column;
            sort.descending = false;
        }
    };
    $scope.licensedate = new Date("2019-12-31"); //yyyy-mm-dd
    $scope.daysremaining = Math.round(($scope.licensedate - new Date(Date.now())) / (1000 * 60 * 60 * 24));



    function contains(a, obj) {
        for (var i = 0; i < a.length; i++) {
            if (a[i].name === obj) {
                return true;
            }
        }
        return false;
    }


    $scope.removeServer = function (server) {
        // var result = confirm('Want to Delete?');
        bootbox.confirm("Want to Delete a Server?", function (result) {
            if (result) {
                localStorage.removeItem(server.name);
                for (var j = 0; j < $scope.servers.length; j++) {
                    if ($scope.servers[j].name == server.name) {
                        $scope.servers.splice(j, 1);
                    }
                }
                location.reload();
            }
        });


    }


    $scope.setup = function (x, z, r) {
        $scope.showSelect = false;
        $scope.tables = [];
        $scope.showAddSection = false;

        $scope.showAll = false;

        for (var i = 0; i < $scope.servers.length; i++) {
            document.getElementById($scope.servers[i].name).style.fontWeight = "normal";
        }
        document.getElementById(x).style.fontWeight = "bold";

        $scope.server = x;

        for (var i = 0; i < $scope.servers.length; i++) {
            $scope.servers[i].showDBList = false;
            $scope.servers[i].showDBIcon = false;

        }
        for (var i = 0; i < $scope.servers.length; i++) {
            if ($scope.servers[i].name == $scope.server) {
                $scope.servers[i].showDBList = true;
                $scope.servers[i].showDBIcon = true;

            }
            else {
                $scope.servers[i].showDBList = false;
                $scope.servers[i].showDBIcon = false;


            }

        }

        $scope.connectionString = z;
        $scope.username = z.indexOf("uid") > -1 ? z.substr(z.indexOf("uid") + 4, z.indexOf("pwd") - 1 - (z.indexOf("uid") + 4)) : "";
        $scope.password = z.indexOf("pwd") > -1 ? z.substr(z.indexOf("pwd") + 4) : "";//, z.indexOf("remember") - 3 - (z.indexOf("pwd") + 4)) : "";

        if ($scope.connectionString.indexOf("integrated security=True") != -1) {
            $scope.integratedSecurity = true;
        }
        else {
            $scope.integratedSecurity = false;
        }

        if (r == true) {
            $scope.connect(2);
        }
        else {
            $scope.showServerAddedMessage = false;
            angular.element('#jj').trigger('click');
        }

    }

    $scope.getServers = function () {

        $scope.servers = [];

        for (var i = 0; i < localStorage.length; i++) {
            var v = localStorage.getItem(localStorage.key(i));

            var v1 = JSON.parse(v);
            $scope.servers.push({ name: v1.name, value: v1.value, remember: v1.remember, showDBList: false, showDBIcon: false });
        }

    }
    $scope.getServers();
    // Connect
    $scope.connect = function (k) {
        $scope.showDBListToggle = true;

        delete $scope.error;
        if (k == 1) {
            $scope.connectionString = null;

            for (var i = 0; i < $scope.servers.length; i++) {
                $scope.servers[i].showDBList = false;
                $scope.servers[i].showDBIcon = false;

            }

        }
        if (!$scope.integratedSecurity && (!$scope.username || !$scope.password) && (!$scope.connectionString))
            return;

        if (!$scope.connectionString) {

            $scope.connectionString = 'server=' + $scope.server + ';' + (($scope.integratedSecurity) ? 'integrated security=True' : 'uid=' + $scope.username + ';pwd=' + $scope.password);
            // alert($scope.connectionString);
            if (typeof (Storage) !== "undefined") {
                // Code for localStorage/sessionStorage.
                if (k == 1) {
                    if (!contains($scope.servers, $scope.server)) {

                        $scope.servers.push({ name: $scope.server, value: $scope.connectionString, remember: $scope.rememberMe, showDBList: false, showDBIcon: false });
                        localStorage.setItem($scope.server, JSON.stringify({ name: $scope.server, value: $scope.connectionString, remember: $scope.rememberMe }));
                    }
                    else {
                        //   alert("Server is already added.");

                        if ($scope.showServerAddedMessage) {
                            bootbox.alert("Server is already added.", function () {

                            });
                        }


                        for (var i = 0; i < localStorage.length; i++) {
                            if (localStorage.key(i) == $scope.server) {
                                localStorage.removeItem($scope.server);
                                for (var j = 0; j < $scope.servers.length; j++) {
                                    if ($scope.servers[j].name == $scope.server) {
                                        $scope.servers[j].remember = $scope.rememberMe;
                                    }
                                }
                                localStorage.setItem($scope.server, JSON.stringify({ name: $scope.server, value: $scope.connectionString, remember: $scope.rememberMe }));

                            }
                        }


                    }
                }
            } else {
                // Sorry! No Web Storage support..
            }
        }




        if (new Date($.now()) > $scope.licensedate) {
            bootbox.alert("Trial license for this product has been expired, please contact your service provider");
        }
        else {


            $scope.pageLoading = true;
            $http.get('/api/Data/GetDatabases?connectionString=' + $scope.connectionString).success(function (data) {
                $scope.databases = data;



                $scope.pageLoading = false;
                for (var i = 0; i < $scope.databases.length; i++) {
                    $scope.highlight[i] = false;          //     document.getElementById($scope.databases[i]).style.fontWeight = "normal";

                }
            })
            .error(function (err) {
                delete err.StackTrace;
                //$scope.error = 'An Error has occurred while establishing connection!' + JSON.stringify(err);
                //bootbox.alert("<img src=\"error.png\" style=\"width:30px;height:30px;\">  An Error has occurred while establishing connection! Please verify server name or username/password.");
                bootbox.alert({
                    message: JSON.stringify(err),
                    title: "<img src=\"error.png\" style=\"width:30px;height:30px;\">  An Error has occurred while establishing connection! Please verify server name or username/password.",
                    backdrop: true
                });
                $scope.pageLoading = false;
            });

        }

    };
    // Get Tables
    $scope.loadTables = function (dbName) {
        //alert($scope.server+" "+dbName);
        $scope.database = dbName;
        $scope.highlight = [];


        for (var i = 0; i < $scope.databases.length; i++) {
            $scope.highlight[i] = false;

            if ($scope.databases[i] == dbName) {
                $scope.highlight[i] = true;
            }
            //     document.getElementById($scope.databases[i]).style.fontWeight = "normal";

        }



        //     document.getElementById($scope.database).style.fontWeight = "bold";

        $scope.showSelect = false;
        $scope.tables = [];
        $scope.showAddSection = false;

        $scope.showAll = false;
        $scope.search = "";


        delete $scope.error;
        $scope.pageLoading = true;
        $scope.connectionString = 'server=' + $scope.server + ';' + (($scope.integratedSecurity) ? 'integrated security=True' : 'uid=' + $scope.username + ';pwd=' + $scope.password) + ';Database=' + $scope.database;
        $http.get('/api/Data/GetTables?connectionString=' + $scope.connectionString).success(function (data) {

            $scope.tables = data;


            $scope.pageLoading = false;
        })
            .error(function (err) {
                delete err.StackTrace;
                //$scope.error = 'An Error has occurred while loading tables!' + JSON.stringify(err); 
                bootbox.alert({
                    message: JSON.stringify(err),
                    title: "<img src=\"error.png\" style=\"width:30px;height:30px;\">  An Error has occurred while loading tables!",
                    backdrop: true
                });
                $scope.pageLoading = false;
            });
    }

    $scope.columns = [];

    $scope.loadTableData = function () {
        delete $scope.error;
        $scope.showSelect = false;
        $scope.showAddSection = false;
        $scope.showAll = true;
        $scope.searchText = {};

        if (!$scope.table) {
            return;
        }


        $scope.pageLoading = true;
        $scope.record = {};
        $http.get('/api/Data/GetColumns?connectionString=' + $scope.connectionString + '&tableName=' + $scope.table).success(function (data) {
            var fields = JSON.parse(data.Data);
            fields.unshift({ "COLUMN_NAME": "Action" });
            $scope.columns = fields;
            //$scope.columns = JSON.parse(data.Data);
            $scope.pageLoading = false;
        })
            .error(function (err) {
                delete err.StackTrace;
                //$scope.error = 'An Error has occurred while loading columns!' + JSON.stringify(err);
                bootbox.alert({
                    message: JSON.stringify(err),
                    title: "<img src=\"error.png\" style=\"width:30px;height:30px;\">  An Error has occurred while loading columns!",
                    backdrop: true
                });
                $scope.pageLoading = false;
            });

        $http.get('/api/Data/GetData?connectionString=' + $scope.connectionString + '&tableName=' + $scope.table).success(function (data) {
            $scope.records = JSON.parse(data.Data);
            $scope.pageLoading = false;
        })
            .error(function (err) {
                delete err.StackTrace;
                //$scope.error = 'An Error has occurred while loading data!' + JSON.stringify(err);
                bootbox.alert({
                    message: JSON.stringify(err),
                    title: "<img src=\"error.png\" style=\"width:30px;height:30px;\">  An Error has occurred while loading data!",
                    backdrop: true
                });
                $scope.pageLoading = false;
            });
    }

    $scope.showAddPane = function (record) {
        $scope.addMode = record ? false : true;
        if (record) {
            for (var i = 0; i < $scope.columns.length; i++) {
                if ($scope.columns[i]['COLUMN_NAME'] !== 'Action') {
                    $scope.columns[i]['data'] = record[$scope.columns[i]['COLUMN_NAME']];

                    if ($scope.columns[i]['IS_REF']) {
                        for (var count = 0; count < $scope.columns[i]['POSSIBLE_VALUES'].split(',').length; count++) {
                            if ($scope.columns[i]['POSSIBLE_VALUES'].split(',')[count].indexOf(" ==> ") > -1) {
                                if ($scope.columns[i]['POSSIBLE_VALUES'].split(',')[count].substring(0, $scope.columns[i]['POSSIBLE_VALUES'].split(',')[count].indexOf(" ==> ")) == $scope.columns[i]['data']) {
                                    $scope.columns[i]['data'] = $scope.columns[i]['POSSIBLE_VALUES'].split(',')[count];
                                    break;
                                }
                            } else {
                                if ($scope.columns[i]['POSSIBLE_VALUES'].split(',')[count] == $scope.columns[i]['data']) {
                                    $scope.columns[i]['data'] = $scope.columns[i]['POSSIBLE_VALUES'].split(',')[count];
                                    break;
                                }
                            }
                        }
                    }

                    if ($scope.columns[i]['DATA_TYPE'].indexOf('date') > -1) {
                        $scope.columns[i]['data'] = $filter('date')(record[$scope.columns[i]['COLUMN_NAME']], 'yyyy-MM-dd');
                    }
                    if ($scope.columns[i]['COLUMN_NAME'] == 'change_date')
                        $scope.columns[i]['data'] = $filter('date')(window.date || Date.now(), 'yyyy-MM-dd');
                    if ($scope.columns[i]['COLUMN_NAME'] == 'e_change_action_code')
                        $scope.columns[i]['data'] = "U";
                }
            };
            $scope.addSectionTitle = "Edit Record";
        } else {
            for (var i = 0; i < $scope.columns.length; i++) {
                if ($scope.columns[i]['COLUMN_NAME'] !== 'Action') {
                    delete $scope.columns[i]['data'];
                    if ($scope.columns[i]['COLUMN_NAME'] == 'change_date')
                        $scope.columns[i]['data'] = $filter('date')(window.date || Date.now(), 'yyyy-MM-dd');
                    if ($scope.columns[i]['COLUMN_NAME'] == 'e_change_action_code')
                        $scope.columns[i]['data'] = "I";
                }
            };
            $scope.addSectionTitle = "New Record";
        }
        $scope.showAddSection = true;
    }


    $scope.deleteRecord = function (record) {

        delete $scope.error;
        var r = [record];
        var row = [];
        for (var i in r) {
            var key = i;
            var val = r[i];
            for (var j in val) {
                if (j != "$$hashKey") {
                    row.push({
                        "Key": j,
                        "Value": val[j]
                    });
                }
            }
        }


        //   var result = confirm('Want to Delete?');
        bootbox.confirm("Want to Delete a record?", function (result) {


            if (result) {
                $scope.pageLoading = true;
                $http.post('/api/Data/RemoveData?connectionString=' + $scope.connectionString + '&tableName=' + $scope.table, row).success(function (data) {
                    if (data == "1")
                        //   alert("Record successfully deleted");
                        bootbox.alert("Record successfully deleted", function () {

                        });
                    $scope.searchText = {};
                    $http.get('/api/Data/GetData?connectionString=' + $scope.connectionString + '&tableName=' + $scope.table).success(function (data) {
                        $scope.records = JSON.parse(data.Data);
                        $scope.pageLoading = false;
                    })
                        .error(function (err) {
                            delete err.StackTrace;
                            //$scope.error = 'An Error has occurred while loading data!' + JSON.stringify(err);
                            bootbox.alert({
                                message: JSON.stringify(err),
                                title: "<img src=\"error.png\" style=\"width:30px;height:30px;\">  An Error has occurred while loading data!",
                                backdrop: true
                            });
                            $scope.pageLoading = false;
                        });
                })
                    .error(function (err) {
                        delete err.StackTrace;
                        //$scope.error = 'An Error has occurred while deleting data!' + JSON.stringify(err);
                        bootbox.alert({
                            message: JSON.stringify(err),
                            title: "<img src=\"error.png\" style=\"width:30px;height:30px;\">  An Error has occurred while deleting data!",
                            backdrop: true
                        });
                        $scope.pageLoading = false;
                    });
            }

        });

        $scope.showAddSection = false;

    }

    $scope.saveRecord = function () {

        $scope.searchText = {};
        delete $scope.error;

        var row = [];
        for (var i = 0; i < $scope.columns.length; i++) {
            if ($scope.columns[i]['COLUMN_NAME'] !== 'Action') {
                if ($scope.columns[i]['DATA_TYPE'] === 'bit' && !$scope.columns[i]['data']) {
                    $scope.columns[i]['data'] = false;
                }
                if ($scope.columns[i]['DATA_TYPE'] == "datetime" && $scope.columns[i]['data'].length == 16) {
                    $scope.columns[i]['data'] = $scope.columns[i]['data'] + ":00";
                }
                row.push({
                    "Key": $scope.columns[i]['COLUMN_NAME'],
                    "Value": $scope.columns[i]['data']
                });
            }
        }
        var request = {
            tableName: $scope.table,
            row: row
        };
        //     var result = confirm('Want to Save?');

        // var result;
        bootbox.confirm("Want to Save Changes?", function (result) {
            if (result) {
                $scope.pageLoading = true;
                if ($scope.addMode) {
                    $http.post('/api/Data/InsertData?connectionString=' + $scope.connectionString + '&tableName=' + $scope.table, row).success(function (data) {
                        if (data == "1")
                            bootbox.alert("New Record successfully created", function () {
                                console.log("Alert Callback");
                            });
                        //    alert("New Record successfully created");
                        $http.get('/api/Data/GetData?connectionString=' + $scope.connectionString + '&tableName=' + $scope.table).success(function (data) {
                            $scope.records = JSON.parse(data.Data);
                            $scope.pageLoading = false;
                        })
                            .error(function (err) {
                                delete err.StackTrace;
                                //$scope.error = 'An Error has occurred while loading data!' + JSON.stringify(err);
                                bootbox.alert({
                                    message: JSON.stringify(err),
                                    title: "<img src=\"error.png\" style=\"width:30px;height:30px;\">  An Error has occurred while loading data!",
                                    backdrop: true
                                });
                                $scope.pageLoading = false;
                            });
                    })
                        .error(function (err) {
                            delete err.StackTrace;
                            //$scope.error = 'An Error has occurred while saving data!' + JSON.stringify(err);
                            bootbox.alert({
                                message: JSON.stringify(err),
                                title: "<img src=\"error.png\" style=\"width:30px;height:30px;\">  An Error has occurred while saving data!",
                                backdrop: true
                            });
                            $scope.pageLoading = false;
                        });
                } else {
                    $http.post('/api/Data/UpdateData?connectionString=' + $scope.connectionString + '&tableName=' + $scope.table, row).success(function (data) {
                        if (data == "1")
                            //   alert("Record successfully updated");
                            bootbox.alert("Record successfully updated", function () {
                                console.log("Alert Callback");
                            });
                        $http.get('/api/Data/GetData?connectionString=' + $scope.connectionString + '&tableName=' + $scope.table).success(function (data) {
                            $scope.records = JSON.parse(data.Data);
                            $scope.pageLoading = false;
                        })
                            .error(function (err) {
                                delete err.StackTrace;
                                //$scope.error = 'An Error has occurred while loading data!' + JSON.stringify(err);
                                bootbox.alert({
                                    message: JSON.stringify(err),
                                    title: "<img src=\"error.png\" style=\"width:30px;height:30px;\">  An Error has occurred while loading data!",
                                    backdrop: true
                                });
                                $scope.pageLoading = false;
                            });
                    })
                        .error(function (err) {
                            delete err.StackTrace;
                            //$scope.error = 'An Error has occurred while saving data!' + JSON.stringify(err); 
                            bootbox.alert({
                                message: JSON.stringify(err),
                                title: "<img src=\"error.png\" style=\"width:30px;height:30px;\">  An Error has occurred while saving data!",
                                backdrop: true
                            });
                            $scope.pageLoading = false;
                        });
                }
            }
        });
        $scope.showAddSection = false;
    };

    $scope.JSONToCSVConvertor = function (columns1, JSONData1, ReportTitle, ShowLabel) {
        var JSONData = angular.toJson(JSONData1);
        //If JSONData is not an object then JSON.parse will parse the JSON string in an Object
        var arrData = typeof JSONData != 'object' ? JSON.parse(JSONData) : JSONData;

        var columns = angular.toJson(columns1);
        //If JSONData is not an object then JSON.parse will parse the JSON string in an Object
        var arrcolumns = typeof columns != 'object' ? JSON.parse(columns) : columns;


        console.log(arrData);
        console.log(arrcolumns);
        var CSV = '';
        //Set Report title in first row or line

        // CSV += ReportTitle + '\r\n\n';

        //This condition will generate the Label/Header
        if (ShowLabel) {
            var row = "";

            //This loop will extract the label from 1st index of on array
            for (var c in arrcolumns) {
                console.log(c);
                //Now convert each value to string and comma-seprated
                //if (index != 'e_prime_contract_id') {
                // if (index == 'pc_name') {
                // index = "Prime Contract Name".toUpperCase();
                // }
                // if (index == 'pc_number') {
                // index = "Prime Contract Number".toUpperCase();
                // }
                // if (index == 'status') {
                // index = "Status".toUpperCase();
                // }
                // if (index == 'current_end_date') {
                // index = "Current End Date".toUpperCase();
                // }
                // if (index == 'owning_company') {
                // index = "Owning Company".toUpperCase();
                // }
                // if (index == 'operation_type') {
                // index = "Operation Type".toUpperCase();
                // }
                // if (index == 'client_type') {
                // index = "Client Type".toUpperCase();
                // }
                // if (index == 'contract_type') {
                // index = "Contract Type".toUpperCase();
                // }
                // row += index + ',';
                //}
                row += arrcolumns[c].COLUMN_NAME + ',';
            }

            row = row.slice(0, -1);
            console.log(row);
            //append Label row with line break
            CSV += row + '\r\n';
        }
        //console.log(arrData[0]);
        //1st loop is to extract each row
        for (var i = 0; i < arrData.length; i++) {
            var row = "";

            //2nd loop will extract each column and convert it in string comma-seprated
            for (var index in arrData[i]) {

                row += '"' + arrData[i][index] + '",';



            }

            row.slice(0, row.length - 1);

            //add a line break after each row
            CSV += row + '\r\n';
        }

        if (CSV == '') {
            alert("Invalid data");
            return;
        }

        //Generate a file name
        var fileName = "";
        //this will remove the blank-spaces from the title and replace it with an underscore
        // fileName += ReportTitle.replace(/ /g, "_");
        fileName += ReportTitle;
        //Initialize file format you want csv or xls
        var uri = 'data:text/csv;charset=utf-8,' + escape(CSV);

        // Now the little tricky part.
        // you can use either>> window.open(uri);
        // but this will not work in some browsers
        // or you will not get the correct file extension 

        //this trick will generate a temp <a /> tag
        var link = document.createElement("a");
        link.href = uri;

        //set the visibility hidden so it will not effect on your web-layout
        link.style = "visibility:hidden";
        link.download = fileName + ".csv";

        //this part will append the anchor tag and remove it after automatic click
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    }
}

// The $inject property of every controller (and pretty much every other type of object in Angular) needs to be a string array equal to the controllers arguments, only as strings
LandingPageController.$inject = ['$scope', '$http', '$filter'];
