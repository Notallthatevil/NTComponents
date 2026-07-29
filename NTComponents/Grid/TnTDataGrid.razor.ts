function tntInitResizable(tableElement) {
    const createResizableTable = function (table) {
        const cols = table.querySelectorAll('th');
        [].forEach.call(cols, function (col) {
            if (col.classList.contains('tnt-interactable-placeholder')) {
                return; // Skip placeholder columns
            }

            // Add a resizer element to the column
            let resizer = col.querySelector('.tnt-resizeable');
            if (!resizer) {
                const resizer = document.createElement('div');
                resizer.classList.add('tnt-resizeable');

                // Set the height
                resizer.style.height = '100%';

                col.appendChild(resizer);

                const styles = window.getComputedStyle(col);
                col.style.width = styles.width;
                col.style.minWidth = styles.width;

                createResizableColumn(col, resizer);
            }
        });
    };

    const createResizableColumn = function (col, resizer) {
        let x = 0;
        let w = 0;

        const mouseDownHandler = function (e) {
            x = e.clientX;

            const styles = window.getComputedStyle(col);
            w = parseInt(styles.width, 10);

            document.addEventListener('mousemove', mouseMoveHandler);
            document.addEventListener('mouseup', mouseUpHandler);

            resizer.classList.add('tnt-resizing');
        };

        const mouseMoveHandler = function (e) {
            const dx = e.clientX - x;
            col.style.width = (w + dx) + 'px';
            col.style.minWidth = (w + dx) + 'px';
        };

        const mouseUpHandler = function () {
            resizer.classList.remove('tnt-resizing');
            document.removeEventListener('mousemove', mouseMoveHandler);
            document.removeEventListener('mouseup', mouseUpHandler);
        };

        resizer.addEventListener('mousedown', mouseDownHandler);
    };

    createResizableTable(tableElement);
}

export function getBodyHeight(element) {
    if (element) {
        const container = element.parentElement;
        if (container && container.getBoundingClientRect) {
            return Math.round(Math.min(window.innerHeight, container.getBoundingClientRect().height));
        }
    }

    return -1;
}

export function onLoad() {
}

export function onUpdate() {
    for (const table of [...document.querySelectorAll('table.tnt-resizable')]) {
        tntInitResizable(table);
    }
}

export function onDispose() { }
