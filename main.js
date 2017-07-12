// util
function debounce(func, time, context) {
    var timeoutId
    return function() {
        clearTimeout(timeoutId)
        var args = arguments
        timeoutId = setTimeout(function() { func.apply(context, args) }, time)
    }
}

// main
window.onload = function() {
    var canvas = new fabric.Canvas('canvas', {
        isDrawingMode: true,
    })

    // resizing
    var offsettingElements = ['.prompt']
        .map(s => Array.from(document.querySelectorAll(s)))
        .reduce((acc, arr) => acc.concat(arr), [])
    function handleResize() {
        var elementOffset = offsettingElements.reduce((acc, item) => acc + item.offsetHeight, 0)
        var extraOffset = window.innerHeight * 0.1
        canvas.wrapperEl.style.top = extraOffset + 'px'
        canvas.setHeight(window.innerHeight - (elementOffset + extraOffset))
        canvas.setWidth(window.innerWidth)
        canvas.renderAll()
    }
    window.addEventListener('resize', debounce(handleResize, 100))
    handleResize()
}