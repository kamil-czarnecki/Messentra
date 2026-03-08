window.messentra = window.messentra || {};

window.messentra.scrollRowNearTop = function (gridSelector, rowSelector, itemIndex, rowHeight, topOffsetRows) {
    const grid = document.querySelector(gridSelector);
    if (!grid) return;

    const container = grid.querySelector('.mud-table-container');
    if (!container) return;

    const row = container.querySelector(rowSelector);

    if (row) {
        // Row is in the DOM — position it topOffsetRows from the top
        const targetScrollTop = row.offsetTop - topOffsetRows * rowHeight;
        container.scrollTop = Math.max(0, targetScrollTop);
    } else {
        // Row is virtualized out — calculate scrollTop directly from index
        const targetScrollTop = itemIndex * rowHeight - topOffsetRows * rowHeight;
        container.scrollTop = Math.max(0, targetScrollTop);
    }
};

