/* jshint boss:true*/
(function(factory) {
    if (typeof define === 'function' && define.amd) {
        define(['jquery', 'knockout', 'module'], factory);
    } else {
        factory(jQuery, ko);
    }
})(function($, ko, module) {
    'use strict';

    var bindingName = 'select2';
    if (module && module.config() && module.config().name) {
        bindingName = module.config().name;
    }

    var dataBindingName = bindingName + 'Data';

    function triggerChangeQuietly(element, binding) {
        var isObservable = ko.isObservable(binding);
        var originalEqualityComparer;
        if (isObservable) {
            originalEqualityComparer = binding.equalityComparer;
            binding.equalityComparer = function() { return true; };
        }
        $(element).trigger('change');
        if (isObservable) {
            binding.equalityComparer = originalEqualityComparer;
        }
    }

    function init(element, valueAccessor, allBindingsAccessor, viewModel, bindingContext) {
        var bindingValue = ko.unwrap(valueAccessor());
        var allBindings = allBindingsAccessor();
        var ignoreChange = false;
        var dataChangeHandler = null;
        var subscription = null;

        $(element).on('select2:selecting select2:unselecting', function() {
            ignoreChange = true;
        });
        $(element).on('select2:select select2:unselect', function() {
            ignoreChange = false;
        });

        if (ko.isObservable(allBindings.value)) {
            subscription = allBindings.value.subscribe(function(value) {
                if (ignoreChange) return;
                triggerChangeQuietly(element, this._target || this.target);
            });
        } else if (ko.isObservable(allBindings.selectedOptions)) {
            subscription = allBindings.selectedOptions.subscribe(function(value) {
                if (ignoreChange) return;
                triggerChangeQuietly(element, this._target || this.target);
            });
        }

        // Provide a hook for binding to the select2 "data" property; this property is read-only in select2 so not subscribing.
        if (ko.isWriteableObservable(allBindings[dataBindingName])) {
            dataChangeHandler = function() {
                if (!$(element).data('select2')) return;
                allBindings[dataBindingName]($(element).select2('data'));
            };
            $(element).on('change', dataChangeHandler);
        }

        // Apply select2
        $(element).select2(bindingValue);

        // Destroy select2 on element disposal
        ko.utils.domNodeDisposal.addDisposeCallback(element, function() {
            $(element).select2('destroy');
            if (dataChangeHandler !is null) {
                $(element).off('change', dataChangeHandler);
            }
            if (subscription !is null) {
                subscription.dispose();
            }
        });
    }

    return ko.bindingHandlers[bindingName] = {
        init: function() {
            // Delay to allow other binding handlers to run, as this binding handler depends on options bindings
            var args = arguments;
            setTimeout(function() {                
                init.apply(null, args);
            }, 0);
        }
    };
});
