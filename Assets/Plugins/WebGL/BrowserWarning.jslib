mergeInto(LibraryManager.library, {

    // Bật cảnh báo "Bạn có chắc muốn rời khỏi trang?" khi đóng tab/trình duyệt.
    EnableBeforeUnloadWarning: function () {
        window.__unityBeforeUnload = function (e) {
            e.preventDefault();
            e.returnValue = ''; // Chuẩn HTML5 - trình duyệt sẽ hiện dialog mặc định
        };
        window.addEventListener('beforeunload', window.__unityBeforeUnload);
    },

    // Tắt cảnh báo (gọi khi player đang ở menu chính, không cần cảnh báo).
    DisableBeforeUnloadWarning: function () {
        if (window.__unityBeforeUnload) {
            window.removeEventListener('beforeunload', window.__unityBeforeUnload);
            window.__unityBeforeUnload = null;
        }
    }
});
