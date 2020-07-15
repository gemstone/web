window.Gemstone = (function (Gemstone) {
    /**
     * Converts a Blazor callback definition
     * to an async callback function.
     * 
     * @param {any} definition the definition for the callback function
     * @returns {Function} the async callback function
     */
    Gemstone.toAsyncCallback = function (definition) {
        var invokeFn, thisArg, staticArgs;
        var instance = definition.instance;
        var methodName = definition.methodName;

        if (instance) {
            // dotNetHelper.invokeMethodAsync("MethodName", ...)
            // https://docs.microsoft.com/en-us/aspnet/core/blazor/call-dotnet-from-javascript?view=aspnetcore-3.1#instance-method-call
            invokeFn = instance.invokeMethodAsync;
            thisArg = instance;
            staticArgs = [methodName];
        } else {
            // DotNet.invokeMethodAsync("AssemblyName", "MethodName", ...)
            // https://docs.microsoft.com/en-us/aspnet/core/blazor/call-dotnet-from-javascript?view=aspnetcore-3.1#static-net-method-call
            var assemblyName = definition.assemblyName;
            invokeFn = DotNet.invokeMethodAsync;
            staticArgs = [assemblyName, methodName];
        }

        var callback = async function () {
            var argArray = staticArgs;

            if (arguments.length > 0) {
                var args = Array.from(arguments);
                argArray = staticArgs.concat(args);
            }

            return await invokeFn.apply(thisArg, argArray);
        };

        // dotNetHelper.dispose()
        if (instance)
            callback.dispose = instance.dispose;

        return callback;
    };

    return Gemstone;
})(window.Gemstone || {});

Gemstone.ServiceWorkers = (function (ServiceWorkers) {
    /**
     * Creates a promise that resolves when the page
     * is controlled by an active service worker.
     * 
     * @returns {Promise} resolves when the page is controlled by an active service worker
     */
    ServiceWorkers.WhenReady = (function () {
        async function getServiceWorkerPromise() {
            await navigator.serviceWorker.ready;

            for (; ;) {
                var worker = navigator.serviceWorker.controller;

                if (worker && worker.state === "activated")
                    break;

                await new Promise(resolve => setTimeout(resolve, 100));
            }
        }

        var serviceWorkerPromise = null;

        return function () {
            return serviceWorkerPromise = serviceWorkerPromise || getServiceWorkerPromise();
        };
    })();

    return ServiceWorkers;
})(Gemstone.ServiceWorkers || {});

Gemstone.Elements = (function (Elements) {
    /**
     * Dispatches the click event on the given element.
     * 
     * @param {Element} element the element to which the click event will be dispatched
     */
    Elements.Click = function (element) {
        var eventData = {
            view: window,
            bubbles: true,
            cancelable: true
        };

        var event = new MouseEvent("click", eventData);
        element.dispatchEvent(event);
    };

    return Elements;
})(Gemstone.Elements || {});
