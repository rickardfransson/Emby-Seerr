define(['baseView', 'loading', 'emby-input', 'emby-button'], function (BaseView, loading) {
    'use strict';

    var pluginId = 'a3c5e0d2-7b41-4c8e-9f16-5d2b8e4a7c90';
    var textfalt = ['SeerrAdress', 'SeerrApiNyckel', 'PlatshallarMapp', 'EmbyAdress', 'Sprak', 'AldersgransLander', 'NotisRubrik', 'NotisText'];
    var talfalt = ['MinAntalRoster', 'MaxAntalPlatshallare', 'FilmerPerForslagslista', 'NotisSekunder'];
    var kryssfalt = ['NotisAktiv'];
    var decimalfalt = ['MinBetyg'];

    function View(view, params) {
        BaseView.apply(this, arguments);

        view.querySelector('.bestallForm').addEventListener('submit', function (e) {
            e.preventDefault();
            loading.show();
            ApiClient.getPluginConfiguration(pluginId).then(function (config) {
                textfalt.forEach(function (f) { config[f] = view.querySelector('#' + f).value.trim(); });
                talfalt.forEach(function (f) { config[f] = parseInt(view.querySelector('#' + f).value, 10) || 0; });
                decimalfalt.forEach(function (f) { config[f] = parseFloat(view.querySelector('#' + f).value.replace(',', '.')) || 0; });
                kryssfalt.forEach(function (f) { config[f] = view.querySelector('#' + f).checked; });
                return ApiClient.updatePluginConfiguration(pluginId, config);
            }).then(function (result) {
                Dashboard.processPluginConfigurationUpdateResult(result);
            });
            return false;
        });
    }

    Object.assign(View.prototype, BaseView.prototype);

    View.prototype.onResume = function (options) {
        BaseView.prototype.onResume.apply(this, arguments);
        var view = this.view;
        loading.show();
        ApiClient.getPluginConfiguration(pluginId).then(function (config) {
            textfalt.concat(talfalt, decimalfalt).forEach(function (f) {
                view.querySelector('#' + f).value = config[f] == null ? '' : config[f];
            });
            kryssfalt.forEach(function (f) { view.querySelector('#' + f).checked = config[f] !== false; });
            loading.hide();
        });
    };

    return View;
});
