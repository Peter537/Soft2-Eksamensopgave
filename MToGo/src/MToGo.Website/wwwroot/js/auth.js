
window.authStorage = {
    setToken: function (token) {
        localStorage.setItem('token', token);
    },
    getToken: function () {
        return localStorage.getItem('token');
    },
    removeToken: function () {
        localStorage.removeItem('token');
    },
    hasToken: function () {
        return localStorage.getItem('token') !== null;
    }
};
