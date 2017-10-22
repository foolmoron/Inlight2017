// shim
window.requestAnimationFrame = window.requestAnimationFrame || window.webkitRequestAnimationFrame || window.mozRequestAnimationFrame || function(callback) {
    window.setTimeout(callback, 1000 / 60)
}
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
function ajax(method, sync, url, form, callback, error) {
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
    xhr.open(method, url, !sync)
    if (form) {
        xhr.setRequestHeader('Content-type', 'application/json')
    }
    xhr.send(form)
}
function get(url, callback, error) { return ajax('GET', false, url, null, callback, error) }
function post(url, form, callback, error) { return ajax('POST', false, url, JSON.stringify(form), callback, error) }
function getSync(url, callback, error) { return ajax('GET', true, url, null, callback, error) }
function postSync(url, form, callback, error) { return ajax('POST', true, url, JSON.stringify(form), callback, error) }

// server
var URL = 'http://localhost:8000/drawing'
var RATE_LIMIT = 1000
var DRAWING_MIN_TIME = 5000

var uuid = localStorage.getItem('uuid')
var uuidTime = new Date(parseInt(localStorage.getItem('uuidTime'))).getTime()
get(URL + (uuid ? "/" + uuid : ""), res => {
    var obj = JSON.parse(res.responseText)
    if (uuid != obj.uuid) {
        uuid = obj.uuid
        localStorage.setItem('uuid', uuid = uuid)
        localStorage.setItem('uuidTime', uuidTime = new Date().getTime())
    }
    window.jsonToLoad = obj.json
    console.log('UUID = ' + uuid + " withJSON = " + !!obj.json)
})

var push = throttleBounce(function(canvas) {
    if (!uuid) return
    // calculate bounds
    var group = new fabric.Group(canvas._objects, null, true)
    group._calcBounds()
    // get data and shift objects to top-left
    var data = JSON.parse(JSON.stringify(canvas.toObject()))
    data.objects.forEach(obj => {
        obj.left = obj.left - group.left
        obj.top = obj.top - group.top
    })
    // post data and dimensions
    post(URL, {
        uuid: uuid,
        json: data,
        dimensions: { width: group.width, height: group.height },
    })
}, RATE_LIMIT)

function complete() {
    getSync(URL + '/' + uuid + '/complete')
}

// events
window.addEventListener("beforeunload", complete)

// main
window.onload = function() {
    var padding = 10
    var canvas = new fabric.Canvas('canvas', {
        isDrawingMode: true,
    })

    // misc
    var doneButton = document.querySelector('.button.done')

    // undo/redo/clear
    var history = []
    var historyIndex = 0
    var doingHistory = false
    function doHistory(index, forcePush) {
        // redraw
        canvas.clear()
        doingHistory = true
        for (var i = 0; i < index && i < history.length; i++) {
            canvas.add(history[i])
        }
        doingHistory = false
        canvas.renderAll()

        // push to server
        if (canvas._objects.length > 0 || forcePush) {
            push(canvas)
        }
    }

    var undoButton = document.querySelector('.undo')
    var redoButton = document.querySelector('.redo')
    var clearButton = document.querySelector('.clear')
    undoButton.onclick = e => {
        historyIndex = Math.max(historyIndex - 1, 0)
        doHistory(historyIndex, true)
    }
    redoButton.onclick = e => {
        historyIndex = Math.min(historyIndex + 1, history.length)
        doHistory(historyIndex, true)
    }
    clearButton.onclick = e => {
        historyIndex = 0
        doHistory(historyIndex, true)
    }

    // new path drawn
    canvas.on('object:added', e => {
        if (doingHistory) return
        if (window.loadingJSON) return

        if (historyIndex < history.length) {
            history = history.slice(0, historyIndex)
        }
        history.push(e.target)
        historyIndex++
        undoButton.disabled = false
        redoButton.disabled = true
        clearButton.disabled = false

        // push to server
        push(canvas)
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

    // update
    var d = new Date().getTime()
    function update() {
        // dt
        var dt = d - (d = new Date().getTime())
        // load json
        if (window.jsonToLoad && canvas) {
            window.loadingJSON = true
            canvas.loadFromJSON(window.jsonToLoad, () => {
                canvas.renderAll()
                push(canvas)
                window.jsonToLoad = null
                window.loadingJSON = false
            })
        }
        // toggle done button based on min drawing time
        doneButton.disabled = (d - uuidTime) <= DRAWING_MIN_TIME || canvas._objects.length == 0
        // toggle canvas buttons
        undoButton.disabled = historyIndex <= 0
        redoButton.disabled = historyIndex >= history.length
        clearButton.disabled = canvas._objects.length == 0
        // loop
        requestAnimationFrame(update)
    }
    requestAnimationFrame(update)

    // kickoff history
    doHistory(0)
}