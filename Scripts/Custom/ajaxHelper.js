function callAjax(options) {
    /*
    options = {
        url: '/Controller/Action',
        method: 'POST', // or 'GET'
        data: { key: value },
        onSuccess: function(response) {},
        onError: function(error) {}
    }
    */

    $.ajax({
        url: options.url,
        type: options.method || 'POST',
        data: options.data || {},
        success: function (response) {
            if (options.onSuccess) options.onSuccess(response);
        },
        error: function (xhr, status, error) {
            if (options.onError) options.onError(xhr, status, error);
            else console.error('AJAX error:', error);
        }
    });
}
