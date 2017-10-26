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
var DRAWING_MIN_TIME = 10000

var drawingObj = JSON.parse(localStorage.getItem('drawing')) || {}
var readyToSetup = false
get(URL + (drawingObj.uuid ? "/" + drawingObj.uuid : ""), res => {
    var obj = JSON.parse(res.responseText)
    if (drawingObj.uuid != obj.uuid) {
        // read
        drawingObj.uuid = obj.uuid
        drawingObj.uuidTime = new Date().getTime()
        drawingObj.prompt = obj.prompt
        drawingObj.colors = obj.colors
        // save
        localStorage.setItem('drawing', JSON.stringify(drawingObj))
    }
    // setup
    drawingObj.json = obj.json
    readyToSetup = true
    console.log('UUID = ' + drawingObj.uuid + " withJSON = " + !!obj.json)
})

var push = throttleBounce(function(canvas) {
    if (!drawingObj.uuid) return
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
        uuid: drawingObj.uuid,
        json: data,
        dimensions: { width: Math.ceil(group.width), height: Math.ceil(group.height) },
    })
}, RATE_LIMIT)

function complete() {
    get(URL + '/' + drawingObj.uuid + '/complete')
}

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

        // push new drawings to server
        if (!window.loadingJSON) {
            push(canvas)
        }
    })

    // prompt text and animation
    function setupPrompt(prompt) {
        var promptDiv = document.querySelector('.prompt')

        var instructions = Array.from(document.querySelectorAll('.prompt .instructions'))
        var timeErrors = Array.from(document.querySelectorAll('.prompt .time-error'))
        var emptyErrors = Array.from(document.querySelectorAll('.prompt .empty-error'))
        var okayMessages = Array.from(document.querySelectorAll('.prompt .okay-message'))
        var submitMessages = Array.from(document.querySelectorAll('.prompt .submit-message'))
        var doneMessages = Array.from(document.querySelectorAll('.prompt .done-message'))
        var dots = Array.from(document.querySelectorAll('.prompt .dots'))
        var allMessages = [].concat([instructions, timeErrors, emptyErrors, okayMessages, submitMessages, doneMessages, dots])

        var SUBMIT_PRAISES = ['BRILLIANT', 'AMAZING', 'MAGNIFICENT', 'MARVELOUS', 'SPLENDID', 'AWESOME', 'BEAUTIFUL', 'FANTASTIC', 'UNIQUE', 'PHENOMENAL', 'GORGEOUS']
        var praiseTexts = Array.from(document.querySelectorAll('.prompt .praise-text'))

        function disableAll() {
            allMessages.forEach(list => list.forEach(item => item.classList.add('hidden')))
        }
        function enableByPrompt(list) {
            list.forEach(item => {
                item.classList.toggle('hidden', item.dataset.type != prompt && item.dataset.type != '*')
            })
        }
        function showOnly(list) {
            disableAll()
            enableByPrompt(list)
        }
        function chainTimeouts(funcsAndTimes) {
            function doFunc(i) {
                if (i < funcsAndTimes.length) {
                    setTimeout(() => { funcsAndTimes[i + 1](); doFunc(i + 2); }, funcsAndTimes[i])
                }
            }
            doFunc(0)
        }

        // show instructions
        showOnly(instructions)

        // try to submit on click
        var hadError = false
        var submitted = false
        promptDiv.onclick = e => {
            if (submitted) {
                return
            }

            if ((new Date().getTime() - drawingObj.uuidTime) <= DRAWING_MIN_TIME) {
                showOnly(timeErrors)
                hadError = true
            } else if (canvas._objects.length == 0) {
                showOnly(emptyErrors)
                hadError = true
            } else {
                // submit
                submitted = true
                document.body.style.pointerEvents = 'none' // hard freeze all input on screen

                praiseTexts.forEach(t => t.textContent = SUBMIT_PRAISES[Math.floor(Math.random() * SUBMIT_PRAISES.length)])
                dots.forEach(t => t.textContent = '')
                showOnly(submitMessages)
                enableByPrompt(dots)

                chainTimeouts([
                    200, () => dots.forEach(t => t.textContent = '.'),
                    200, () => dots.forEach(t => t.textContent = '..'),
                    200, () => dots.forEach(t => t.textContent = '...'),
                    400, () => dots.forEach(t => t.textContent = '.....'),
                    400, () => dots.forEach(t => t.textContent = '.......'),
                    400, () => { enableByPrompt(doneMessages); complete(); },
                    1600, () => { localStorage.clear(); location.href = location.href; },
                ])
            }
        }

        // check for error being resolved
        setInterval(() => {
            if (hadError) {
                if ((new Date().getTime() - drawingObj.uuidTime) <= DRAWING_MIN_TIME && canvas._objects.length == 0) {
                    return
                }
                hadError = false
            }
        }, 500)
    }

    // color controls
    function setColor(colorPicker, colorButtons) {
        // set color pickers
        var currentIndex = colorButtons.indexOf(colorPicker)
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

    function setupColors(colors) {
        var colorContainer = document.querySelector('.colors')
        
        // clear old buttons
        var colorButtons = Array.from(document.querySelectorAll('.colors > div'))
        for (var i = 0; i < colorButtons.length; i++) {
            colorButtons[i].remove()
        }

        // add new buttons
        colorButtons = []
        for (var i = 0; i < colors.length; i++) {
            var button = document.createElement('div')
            button.dataset.color = colors[i]
            button.style.backgroundColor = colors[i]
            button.onclick = e => setColor(e.target, colorButtons)

            colorContainer.appendChild(button)
            colorButtons.push(button)
        }

        setColor(colorButtons[0], colorButtons)
    }

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
    setTimeout(handleResize, 80)
    setTimeout(handleResize, 160)
    setTimeout(handleResize, 250)

    // update
    var d = new Date().getTime()
    function update() {
        // dt
        var dt = d - (d = new Date().getTime())
        if (readyToSetup && canvas) {
            // load colors
            setupColors(drawingObj.colors)

            // load prompt
            setupPrompt(drawingObj.prompt)

            // load json
            if (drawingObj.json) {
                window.loadingJSON = true
                canvas.loadFromJSON(drawingObj.json, () => {
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
                    // clear loading flag
                    window.loadingJSON = false
                })
            }

            readyToSetup = false
        }
        // toggle canvas buttons
        undoButton.classList.toggle('disabled', historyIndex <= 0 && canvas.isDrawingMode)
        redoButton.classList.toggle('disabled', historyIndex >= history.length && canvas.isDrawingMode)
        clearButton.classList.toggle('disabled', canvas._objects.length == 0 && canvas.isDrawingMode)
        // loop
        requestAnimationFrame(update)
    }
    requestAnimationFrame(update)

    // kickoff history
    doHistory(0)
}