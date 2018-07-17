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
        var args = arguments
        if (!firedThisInterval) {
            // throttle
            func.apply(null, args)
            firedThisInterval = true
            setTimeout(function() { firedThisInterval = false }, interval)
        } else {
            // debounce
            clearTimeout(timeoutId)
            timeoutId = setTimeout(function() { func.apply(null, args) }, interval)
        }
    }
}
function ajax(method, sync, url, form, callback, error) {
    var xhr = new XMLHttpRequest()
    xhr.onreadystatechange = function() { 
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

function resetStorage() {
    localStorage.removeItem('drawing')
    localStorage.removeItem('clearStorageFixAttempt')
    localStorage.removeItem('objectShiftLeft')
    localStorage.removeItem('objectShiftTop')
}

function hasAllSameColors(canvas) {
    return canvas._objects.reduce((acc, item) => acc.stroke == item.stroke ? item : false)
}

// server
var BASE_URL = 'http://localhost:8000'
var DRAWING_URL = BASE_URL + '/drawing'
var RATE_LIMIT = 1000
var DRAWING_MIN_TIME = 10000

var drawingObj = JSON.parse(localStorage.getItem('drawing')) || {}
var readyToSetup = false
get(DRAWING_URL + (drawingObj.uuid ? "/" + drawingObj.uuid : ""), function(res) {
    if (!res.responseText) {
        return
    }
    document.body.classList.remove('loading')

    var obj = JSON.parse(res.responseText)
    if (drawingObj.uuid != obj.uuid) {
        // read
        drawingObj.uuid = obj.uuid
        drawingObj.uuidTime = new Date().getTime()
        drawingObj.colors = obj.colors
        // save
        localStorage.setItem('drawing', JSON.stringify(drawingObj))
    }
    // setup
    drawingObj.prompt = obj.type == 'a' ? 'animal' : 'plant'
    drawingObj.json = obj.json
    readyToSetup = true
    localStorage.setItem('clearStorageFixAttempt', 0)
}, function(error) {
    if (!parseInt(localStorage.getItem('clearStorageFixAttempt')) && (error.responseText || '').indexOf('invalid drawing uuid') >= 0) {
        resetStorage()
        localStorage.setItem('clearStorageFixAttempt', 1)
        location.reload(true)
    }
})

var push = throttleBounce(function(canvas) {
    if (!drawingObj.uuid) return
    // calculate bounds
    var group = new fabric.Group(canvas._objects, null, true)
    group._calcBounds()
    // get data and shift objects to top-left
    var data = JSON.parse(JSON.stringify(canvas.toObject()))
    data.objects.forEach(function(obj) {
        obj.left = obj.left - group.left
        obj.top = obj.top - group.top
    })
    // save object shifts
    localStorage.setItem('objectShiftLeft', group.left)
    localStorage.setItem('objectShiftTop', group.top)
    // post data and dimensions
    post(DRAWING_URL, {
        uuid: drawingObj.uuid,
        json: data,
        dimensions: { width: Math.ceil(group.width), height: Math.ceil(group.height) },
    })
}, RATE_LIMIT)

function complete() {
    get(DRAWING_URL + '/' + drawingObj.uuid + '/complete')
}

// past drawings
var PAST_DRAWING_MAX_HEIGHT = 250
function updatePastDrawings(container) {
    var pastDrawings = JSON.parse(localStorage.getItem('pastdrawings')) || []
    container.parentElement.classList.toggle('hidden', !pastDrawings.length)

    // clear div
    container.innerHTML = ''
    // populate div
    var tpl = document.querySelector('#tpl-past-drawing')
    for (var i = 0; i < pastDrawings.length; i++) {
        // base node
        var node = document.importNode(tpl.content.children[0], true)
        node.dataset.uuid = pastDrawings[i].uuid
        container.appendChild(node)
        // elements
        var canvasEl = node.querySelector('canvas')
        var timeEl = node.querySelector('.past-time')
        timeEl.textContent = new Date(pastDrawings[i].completionTime || 0).toLocaleString()
        // canvas
        var canvas = new fabric.StaticCanvas(canvasEl)
        canvas.loadFromJSON(pastDrawings[i].json, function() {
            // calculate bounds
            var group = new fabric.Group(canvas._objects, null, true)
            group._calcBounds()
            var width = Math.ceil(group.width)
            var height = Math.ceil(group.height)
            var aspect = width / height
            // canvas size
            canvas.setWidth(width)
            canvas.setHeight(height)
            // div size
            var h = Math.min(height, PAST_DRAWING_MAX_HEIGHT)
            var w = h * aspect
            canvasEl.style.width = w + 'px'
            canvasEl.style.height = h + 'px'
            // shift to top right
            canvas._objects.forEach(function(obj) {
                obj.setLeft(obj.left - group.left)
                obj.setTop(obj.top - group.top)
                obj.setCoords()
            })
            canvas.renderAll()
        })
    }
}

// commands
var WIGGLE_COOLDOWN = 30*1000 // WARNING: Make sure to change this in the [.past-drawing .commands .btn-wiggle:disabled .cooldown] CSS anim as well
function handleWiggle(el) {
    // cooldown
    el.setAttribute('disabled', 'disabled') 
    setTimeout(() => el.removeAttribute('disabled'), WIGGLE_COOLDOWN)
    // send command
    var uuid = el.parentElement.parentElement.dataset.uuid
    get(BASE_URL + '/command/wiggle/' + uuid + '/add/1')
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
    var justCleared = false
    function doHistory(index, forcePush) {
        justCleared = false

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
    undoButton.onclick = function(e) { 
        if (justCleared) {
            historyIndex = history.length
            doHistory(historyIndex, true)
        } else if (historyIndex > 0) {
            historyIndex = Math.max(historyIndex - 1, 0)
            doHistory(historyIndex, true)
        }
    }
    redoButton.onclick = function(e) {
        if (justCleared) {
            historyIndex = history.length
            doHistory(historyIndex, true)
        } else if (historyIndex < history.length) {
            historyIndex = Math.min(historyIndex + 1, history.length)
            doHistory(historyIndex, true)
        }
    }
    clearButton.onclick = function(e) {
        if (historyIndex > 0) {
            historyIndex = 0
            doHistory(historyIndex, true)
            justCleared = true
            // flash canvas red
            var canvasContainer = document.querySelector('.canvas-container')
            var prevTransition = canvasContainer.style.transition
            var prevColor = canvasContainer.style.backgroundColor
            canvasContainer.style.transition = 'none'
            canvasContainer.style.backgroundColor = '#ff5151'
            setTimeout(() => {
                canvasContainer.style.transition = prevTransition
                canvasContainer.style.backgroundColor = prevColor
            }, 0)
        }
    }

    // new path drawn
    canvas.on('object:added', function(e) {
        if (doingHistory) return

        justCleared = false
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

    // intro
    var intro = document.querySelector('.intro')
    var intros = Array.from(document.querySelectorAll('.intro > span'))
    var fadeIntro = function(e) {
        intro.classList.add('fade')
        drawingObj.uuidTime = new Date().getTime()
        setTimeout(function() { intro.classList.add('hidden') }, 700)
    }
    intro.addEventListener('mousedown', fadeIntro)
    intro.addEventListener('touchstart', fadeIntro)

    // prompt text and animation
    function setupPrompt(prompt) {
        var promptDiv = document.querySelector('.prompt')

        var instructions = Array.from(document.querySelectorAll('.prompt .instructions'))
        var timeErrors = Array.from(document.querySelectorAll('.prompt .time-error'))
        var emptyErrors = Array.from(document.querySelectorAll('.prompt .empty-error'))
        var colorErrors = Array.from(document.querySelectorAll('.prompt .color-error'))
        var okayMessages = Array.from(document.querySelectorAll('.prompt .okay-message'))
        var submitMessages = Array.from(document.querySelectorAll('.prompt .submit-message'))
        var doneMessages = Array.from(document.querySelectorAll('.prompt .done-message'))
        var dots = Array.from(document.querySelectorAll('.prompt .dots'))
        var allMessages = [].concat([instructions, timeErrors, colorErrors, emptyErrors, okayMessages, submitMessages, doneMessages, dots])

        var SUBMIT_PRAISES = ['BRILLIANT', 'AMAZING', 'MAGNIFICENT', 'MARVELOUS', 'SPLENDID', 'AWESOME', 'BEAUTIFUL', 'FANTASTIC', 'UNIQUE', 'PHENOMENAL', 'GORGEOUS']
        var praiseTexts = Array.from(document.querySelectorAll('.prompt .praise-text'))

        function disableAll() {
            allMessages.forEach(function(list) { list.forEach(function(item) { item.classList.add('hidden') }) })
        }
        function enableByPrompt(list) {
            list.forEach(function(item) {
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
                    setTimeout(function() { funcsAndTimes[i + 1](); doFunc(i + 2); }, funcsAndTimes[i])
                }
            }
            doFunc(0)
        }

        // set up color texts
        var colorTexts = [1,2,3,4,5].map(i => Array.from(document.querySelectorAll('.prompt .color' + i)))
        for (var i = 0; i < drawingObj.colors.length && i < colorTexts.length; i++) {
            for (var c = 0; c < colorTexts[i].length; c++) {
                colorTexts[i][c].style.color = drawingObj.colors[i]
            }
        }

        // show intro
        showOnly(intros)

        // show instructions
        showOnly(instructions)

        // try to submit on click
        var hadError = false
        var submitted = false
        promptDiv.onclick = function(e) {
            if (submitted) {
                return
            }

            if ((new Date().getTime() - drawingObj.uuidTime) <= DRAWING_MIN_TIME) {
                drawingObj.uuidTime = Math.max(drawingObj.uuidTime, new Date().getTime() - (DRAWING_MIN_TIME - 5000))
                showOnly(timeErrors)
                hadError = true
            } else if (canvas._objects.length == 0) {
                showOnly(emptyErrors)
                hadError = true
            } else if (canvas._objects.length > 0 && hasAllSameColors(canvas)) {
                showOnly(colorErrors)
                hadError = true
            } else {
                // submit
                submitted = true
                document.body.style.pointerEvents = 'none' // hard freeze all input on screen

                praiseTexts.forEach(function(t) { t.textContent = SUBMIT_PRAISES[Math.floor(Math.random() * SUBMIT_PRAISES.length)] })
                dots.forEach(function(t) { t.textContent = '' })
                showOnly(submitMessages)
                enableByPrompt(dots)

                chainTimeouts([
                    200, function() { dots.forEach(function(t) { t.textContent = '.' }) },
                    200, function() { dots.forEach(function(t) { t.textContent = '..' }) },
                    200, function() { dots.forEach(function(t) { t.textContent = '...' }) },
                    400, function() { dots.forEach(function(t) { t.textContent = '.....' }) },
                    400, function() { dots.forEach(function(t) { t.textContent = '.......' }) },
                    400, function() {
                        // complete drawing and wait a while for request to go through
                        enableByPrompt(doneMessages)
                        complete()
                    },
                    1600, function() {
                        // finish drawing and reload
                        var pastDrawings = JSON.parse(localStorage.getItem('pastdrawings')) || []
                        pastDrawings.unshift({
                            uuid: drawingObj.uuid,
                            json: JSON.parse(JSON.stringify(canvas.toObject())),
                            completionTime: new Date().getTime(),
                        })
                        localStorage.setItem('pastdrawings', JSON.stringify(pastDrawings))
                        resetStorage()
                        location.reload()
                    },
                ])
            }
        }

        // check for error being resolved
        setInterval(function() {
            if (hadError) {
                if ((new Date().getTime() - drawingObj.uuidTime) <= DRAWING_MIN_TIME || canvas._objects.length == 0 || hasAllSameColors(canvas)) {
                    return
                }
                hadError = false
                showOnly(okayMessages)
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
            button.onclick = function(e) { setColor(e.target, colorButtons) }

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
        widthButtons[i].onclick = function(e) { setWidth(e.target) }
    }
    setWidth(widthButtons[1])

    // resizing
    var offsettingElements = ['.prompt', '.colors', '.controls']
        .map(function(s) { return Array.from(document.querySelectorAll(s)) })
        .reduce(function(acc, arr) { return acc.concat(arr) }, [])
    var prevElementOffset = 0
    function resizerSentinel() {
        var elementOffset = offsettingElements.reduce(function(acc, item) { return acc + item.offsetHeight }, 0)
        if (elementOffset != prevElementOffset) {
            var extraOffset = 0
            canvas.wrapperEl.style.top = (extraOffset + padding) + 'px'
            canvas.wrapperEl.style.left = padding + 'px'
            canvas.setHeight(window.innerHeight - (elementOffset + extraOffset + 2*padding))
            canvas.setWidth(window.innerWidth - 2*padding)
            canvas.renderAll()
            prevElementOffset = elementOffset
        }
        requestAnimationFrame(resizerSentinel)
    }
    requestAnimationFrame(resizerSentinel)
    window.addEventListener('resize', debounce(function() { prevElementOffset = 0 }, 100))

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
                canvas.loadFromJSON(drawingObj.json, function() {
                    // shift objects to original position
                    var shiftLeft = parseInt(localStorage.getItem('objectShiftLeft'))
                    var shiftTop = parseInt(localStorage.getItem('objectShiftTop'))
                    canvas._objects.forEach(function(obj) {
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

                // disable intro instantly
                intro.classList.add('hidden')
            }

            readyToSetup = false
        }
        // toggle canvas buttons
        undoButton.classList.toggle('disabled', !justCleared && historyIndex <= 0 && canvas.isDrawingMode)
        redoButton.classList.toggle('disabled', !justCleared && historyIndex >= history.length && canvas.isDrawingMode)
        clearButton.classList.toggle('disabled', canvas._objects.length == 0 && canvas.isDrawingMode)
        // loop
        requestAnimationFrame(update)
    }
    requestAnimationFrame(update)

    // kickoff history
    doHistory(0)

    // load past drawings
    var pastDrawingContainer = document.querySelector('.past-drawings .content')
    updatePastDrawings(pastDrawingContainer)
}