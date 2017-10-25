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
    // save object shifts
    localStorage.setItem('objectShiftLeft', group.left)
    localStorage.setItem('objectShiftTop', group.top)
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
        if (historyIndex > 0) {
            historyIndex = Math.max(historyIndex - 1, 0)
            doHistory(historyIndex, true)
        }
    }
    redoButton.onclick = e => {
        if (historyIndex < history.length) {
            historyIndex = Math.min(historyIndex + 1, history.length)
            doHistory(historyIndex, true)
        }
    }
    clearButton.onclick = e => {
        if (historyIndex > 0) {
            historyIndex = 0
            doHistory(historyIndex, true)
        }
    }

    // new path drawn
    canvas.on('object:added', e => {
        if (doingHistory) return

        if (historyIndex < history.length) {
            history = history.slice(0, historyIndex)
        }
        history.push(e.target)
        historyIndex++
        undoButton.classList.toggle('disabled', false)
        redoButton.classList.toggle('disabled', true)
        clearButton.classList.toggle('disabled', false)

        // push new drawings to server
        if (!window.loadingJSON) {
            push(canvas)
        }
    })

    // color controls
    var colorContainer = document.querySelector('.colors')
    var colorButtons = Array.from(document.querySelectorAll('.colors > div'))

    function setColor(colorPicker) {
        // set color pickers
        var currentIndex = colorButtons.indexOf(colorPicker)
        console.log(currentIndex)
        for (var i = 0; i < colorButtons.length; i++) {
            var button = colorButtons[i]
            button.classList.remove('bottom-left-radius')
            button.classList.remove('bottom-right-radius')
            if (i == currentIndex) {
                // leave alone
            } else if (i == (currentIndex - 1)) {
                button.classList.add('bottom-right-radius')
            } else if (i == (currentIndex + 1)) {
                button.classList.add('bottom-left-radius')
            }
        }
        // set color
        var color = colorPicker.dataset.color
        canvas.freeDrawingBrush.color = color
        document.body.style.backgroundColor = color
    }

    for (var i = 0; i < colorButtons.length; i++) {
        var colorButton = colorButtons[i]
        colorButton.style.backgroundColor = colorButton.dataset.color
        colorButton.onclick = e => setColor(e.target)
    }
    setColor(colorButtons[0])

    // width controls
    var widthButtons = Array.from(document.querySelectorAll('.width'))
    
    function setWidth(widthButton) {
        // set width pickers
        for (var i = 0; i < widthButtons.length; i++) {
            widthButtons[i].classList.remove('selected')
        }
        widthButton.classList.add('selected')
        // set width
        canvas.freeDrawingBrush.width = parseFloat(widthButton.dataset.size)
    }

    for (var i = 0; i < widthButtons.length; i++) {
        widthButtons[i].onclick = e => setWidth(e.target)
    }
    setWidth(widthButtons[1])

    // resizing
    var offsettingElements = ['.prompt', '.colors', '.controls']
        .map(s => Array.from(document.querySelectorAll(s)))
        .reduce((acc, arr) => acc.concat(arr), [])
    function handleResize() {
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
                // shift objects to original position
                var shiftLeft = parseInt(localStorage.getItem('objectShiftLeft'))
                var shiftTop = parseInt(localStorage.getItem('objectShiftTop'))
                canvas._objects.forEach(obj => {
                    obj.setLeft(obj.left + shiftLeft)
                    obj.setTop(obj.top + shiftTop)
                    obj.setCoords()
                })
                // render
                canvas.renderAll()
                // save
                push(canvas)
                // clear loading flags
                window.jsonToLoad = null
                window.loadingJSON = false
            })
        }
        // toggle done button based on min drawing time
        doneButton.disabled = (d - uuidTime) <= DRAWING_MIN_TIME || canvas._objects.length == 0
        // toggle canvas buttons
        undoButton.classList.toggle('disabled', historyIndex <= 0)
        redoButton.classList.toggle('disabled', historyIndex >= history.length)
        clearButton.classList.toggle('disabled', canvas._objects.length == 0)
        // loop
        requestAnimationFrame(update)
    }
    requestAnimationFrame(update)

    // kickoff history
    doHistory(0)
}