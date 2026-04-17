window.localStorageHelper = {
    save: function (key, value) {
        localStorage.setItem(key, JSON.stringify(value));
    },
    load: function (key) {
        let item = localStorage.getItem(key);
        return item ? JSON.parse(item) : null;
    },
    remove: function (key) {
        localStorage.removeItem(key);
    },
    clear: function () {
        localStorage.clear();
    }
};
