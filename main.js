// util
function debounce(func, time, context) {
    var timeoutId
    return function() {
        clearTimeout(timeoutId)
        var args = arguments
        timeoutId = setTimeout(function() { func.apply(context, args) }, time)
    }
}
function lerp(a, b, t) {
    return a + t * (b - a)
}
function ilerp(x, a, b) {
    return (x - a) / (b - a)
}

// main
window.onload = function() {
    var padding = 10
    var canvas = new fabric.Canvas('canvas', {
        isDrawingMode: true,
        left: padding,
        top: padding,
    })

    // color controls
    var colorContainer = document.querySelector('.colors')
    var colorButtons = Array.from(document.querySelectorAll('.colors > div'))
    var widthCircle = document.querySelector('.width-circle.main')
    var widthCircleOutline = document.querySelector('.width-circle.outline')
    var widthCircleOutlineInner = document.querySelector('.width-circle.outline-inner')

    function setColor(color) {
        canvas.freeDrawingBrush.color = color
        document.body.style.backgroundColor = color
        widthCircle.style.backgroundColor = color
        widthCircleOutline.style.backgroundColor = color
    }

    for (var i = 0; i < colorButtons.length; i++) {
        var colorButton = colorButtons[i]
        colorButton.style.backgroundColor = colorButton.dataset.color
        colorButton.onclick = e => setColor(e.target.dataset.color)
    }
    setColor(colorButtons[0].dataset.color)

    // width controls
    var widthInput = document.querySelector('.width-input')
    
    function setWidth(width, minWidth, maxWidth) {
        canvas.freeDrawingBrush.width = width
        var parentHeight = widthCircle.parentElement.offsetHeight
        var percHeight = ilerp(width, minWidth, maxWidth) * 0.9 + 0.1
        widthCircle.style.width = parentHeight*percHeight + 'px'
        widthCircle.style.height = parentHeight*percHeight + 'px'
        widthCircleOutline.style.width = parentHeight+2 + 'px'
        widthCircleOutline.style.height = parentHeight+2 + 'px'
        widthCircleOutlineInner.style.width = parentHeight + 'px'
        widthCircleOutlineInner.style.height = parentHeight + 'px'
    }

    widthInput.oninput = e => setWidth(e.target.valueAsNumber, parseFloat(e.target.min), parseFloat(e.target.max))
    setWidth(widthInput.valueAsNumber, parseFloat(widthInput.min), parseFloat(widthInput.max))

    // resizing
    var offsettingElements = ['.prompt', '.colors']
        .map(s => Array.from(document.querySelectorAll(s)))
        .reduce((acc, arr) => acc.concat(arr), [])
    function handleResize() {
        // controls
        setWidth(widthInput.valueAsNumber, parseFloat(widthInput.min), parseFloat(widthInput.max))
        // canvas
        var elementOffset = offsettingElements.reduce((acc, item) => acc + item.offsetHeight, 0)
        var extraOffset = 0
        canvas.wrapperEl.style.top = (extraOffset + padding) + 'px'
        canvas.wrapperEl.style.left = padding + 'px'
        canvas.setHeight(window.innerHeight - (elementOffset + extraOffset + 2*padding))
        canvas.setWidth(window.innerWidth - 2*padding)
        canvas.renderAll()
    }
    window.addEventListener('resize', debounce(handleResize, 100))
    handleResize()
}