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
function throttleBounce(func, interval) {
    var firedThisInterval = false
    var timeoutId = null
    return function() { // use explicit function syntax to get wrapped arguments context
        if (!firedThisInterval) {
            // throttle
            func.apply(null, arguments)
            firedThisInterval = true
            setTimeout(() => { firedThisInterval = false }, interval)
        } else {
            // debounce
            clearTimeout(timeoutId)
            timeoutId = setTimeout(() => { func.apply(null, arguments) }, interval)
        }
    }
}
function ajax(method, form, url, callback, error) {
    var xhr = new XMLHttpRequest()
    xhr.onreadystatechange = () => { 
        if (xhr.readyState === 4) {
            if (xhr.status < 300 && callback) {
                callback(xhr)
            } else if (xhr.status >= 300 && error) {
                error(xhr)
            }
        }
    }
    xhr.open(method, url, true)
    if (form) {
        xhr.setRequestHeader('Content-type', 'application/json')
    }
    xhr.send(form)
}
function get(url, callback, error) { return ajax('GET', null, url, callback, error) }
function post(url, form, callback, error) { return ajax('POST', JSON.stringify(form), url, callback, error) }

// server
var URL = 'http://localhost:8000/drawing'
var RATELIMIT = 1000

var uuid = null
get(URL, res => {
    uuid = JSON.parse(res.responseText).uuid
    console.log('UUID = ' + uuid)
})

var push = throttleBounce(function(data) {
    if (!uuid) return
    post(URL, {
        uuid: uuid,
        json: data,
    })
}, RATELIMIT)

// main
window.onload = function() {
    var padding = 10
    var canvas = new fabric.Canvas('canvas', {
        isDrawingMode: true,
    })

    // undo/redo/clear
    var history = []
    var historyIndex = 0
    var doingHistory = false
    function doHistory(index) {
        // redraw
        canvas.clear()
        doingHistory = true
        for (var i = 0; i < index && i < history.length; i++) {
            canvas.add(history[i])
        }
        doingHistory = false
        canvas.renderAll()
        // update buttons
        undoButton.disabled = index <= 0
        redoButton.disabled = index >= history.length
        clearButton.disabled = index <= 0

        // push to server
        push(canvas.toObject())
    }

    var undoButton = document.querySelector('.undo')
    var redoButton = document.querySelector('.redo')
    var clearButton = document.querySelector('.clear')
    undoButton.onclick = e => {
        historyIndex = Math.max(historyIndex - 1, 0)
        doHistory(historyIndex)
    }
    redoButton.onclick = e => {
        historyIndex = Math.min(historyIndex + 1, history.length)
        doHistory(historyIndex)
    }
    clearButton.onclick = e => {
        historyIndex = 0
        doHistory(historyIndex)
    }
    
    doHistory(0)

    // new path drawn
    canvas.on('object:added', e => {
        if (doingHistory) return

        if (historyIndex < history.length) {
            history = history.slice(0, historyIndex)
        }
        history.push(e.target)
        historyIndex++
        undoButton.disabled = false
        redoButton.disabled = true
        clearButton.disabled = false

        // push to server
        push(canvas.toObject())
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