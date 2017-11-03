const express = require('express')
const cors = require('cors')
const path = require('path')
const fs = require('fs')
const favicon = require('serve-favicon')
const logger = require('morgan')
const cookieParser = require('cookie-parser')
const bodyParser = require('body-parser')
const session = require('express-session')
const loki = require('lokijs')
const uuid = require('uuid/v4')
const fabric = require('fabric').fabric

const config = require('./config.js')

const app = express()

// view engine setup
app.set('views', path.join(__dirname, 'views'))
app.set('view engine', 'ejs')

app.use(favicon(path.join(__dirname, 'public', 'favicon.ico')))
app.use(logger('dev'))
app.use(bodyParser.json())
app.use(bodyParser.urlencoded({ extended: false }))
app.use(cookieParser())
app.use(express.static(path.join(__dirname, 'public')))
app.use(cors())
app.use(session({
    secret: config.SECRET,
    resave: true,
    saveUninitialized: true,
}))

// database
if (!fs.existsSync('db')) {
    fs.mkdirSync('db')
}
const db = new loki('db/db.json', { autosave: true, serializationMethod: 'pretty' })
const data = {}
function initCollection(container, name, opts) {
    var collection = db.getCollection(name)
    if (!collection) {
        collection = db.addCollection(name, opts)
    }
    container[name] = collection
}

db.loadDatabase({}, () => {
    initCollection(data, 'drawings', { unique: ['uuid'] })
})

const GLOBAL = {
    autoapprove: false,
    colors: {
        animal: 36,
        plant: 96,
    },
    hueshiftspeed: 1,
}

// auth
var adminAuth = (req, res, next) => {
    if (req.session && req.session.isAdmin) {
        next()
    } else {
        res.redirect('/login')
    }
}

app.get('/login', (req, res, next) => {
    if (req.session) {
        req.session.destroy()
    }
    res.render('login', { })
})
app.post('/login', (req, res, next) => {
    if (req.body.password.trim() === config.PASSWORD) {
        req.session.isAdmin = true
        res.redirect('/drawings')
    } else {
        res.render('login', { 
            previousInput: req.body.password, 
            error: `That's the incorrect incantation... are you sure you're in the right place?`,
        })
    }
})

// routes
app.get('/', adminAuth, (req, res, next) => {
    res.render('index', { })
})
app.get('/drawings', adminAuth, (req, res, next) => {
    res.render('drawings', { 
        drawings: data.drawings.data, 
        globalAutoApprove: GLOBAL.autoapprove, 
        dirNew: `${config.HOST}:${config.PORT}/img/drawings/new/`,
        dirApproved: `${config.HOST}:${config.PORT}/img/drawings/approved/`
    })
})

// colors
function getNewColorPalette(colorType) {
    var colors = []
    var hue = GLOBAL.colors[colorType] || 0
    var sat = 100
    var lit = 50

    // base color
    colors.push("hsl(" + hue + ", " + sat + "%, " + lit + "%)")
    // shifted colors
    for (var i = 0; i < 3; i++) {
        hue = (hue + 10 + Math.random() * 20) % 360
        lit = 35 + Math.random() * 30
        colors.push("hsl(" + hue + ", " + sat + "%, " + lit + "%)")
    }
    // random color
    colors.push("hsl(" + (hue + 90 + Math.random() * 180) + ", " + sat + "%, " + lit + "%)")

    return colors
}

// drawing
var canvas = fabric.createCanvasForNode(11, 13) // 1 canvas for whole app
const drawingDirs = ['/public','/img','/drawings/']
const drawingDirectory = drawingDirs.reduce((acc, dir) => {
    acc += dir
    if (!fs.existsSync(acc)) {
        fs.mkdirSync(acc)
    }
    return acc
}, __dirname)
const drawingDirNew = drawingDirectory + 'new/'
if (!fs.existsSync(drawingDirNew)) {
    fs.mkdirSync(drawingDirNew)
}
const drawingDirApproved = drawingDirectory + 'approved/'
if (!fs.existsSync(drawingDirApproved)) {
    fs.mkdirSync(drawingDirApproved)
}

const STATUS = {
    NEW: 'new',
    UPDATED: 'updated',
    APPROVED: 'approved',
    IGNORED: 'ignored',
    DELETED: 'deleted',
}

const INITIAL_TYPES = ['a', 'p']

const Drawing = (init) => Object.assign({
    uuid: uuid(),
    type: 'a',
    facing: 'l',
    json: '',
    status: STATUS.NEW,
    empty: true,
    completedTime: null,
    autoapprove: false,
    colors: [],
}, init)

function checkDrawing(req, res, next) {
    var drawing = data.drawings.findOne({uuid: req.params.uuid})
    if (!drawing) {
        return res.status(400).json({error : 'invalid drawing uuid'})
    }
    req.drawing = drawing
    next()
}

app.get('/drawing', (req, res, next) => {
    var newDrawing = Drawing()
    newDrawing.type = INITIAL_TYPES[Math.floor(Math.random() * INITIAL_TYPES.length)]
    var colorType = newDrawing.type == 'a' ? 'animal' : 'plant'
    newDrawing.colors = getNewColorPalette(colorType)
    data.drawings.insert(newDrawing)

    GLOBAL.colors[colorType] = (GLOBAL.colors[colorType] + GLOBAL.hueshiftspeed) % 360

    var drawing = data.drawings.findOne({uuid: newDrawing.uuid})
    data.drawings.update(drawing)
    res.json(drawing)
})
app.get('/drawing/:uuid', checkDrawing, (req, res, next) => {
    var drawing = req.drawing
    res.json(drawing)
})
app.get('/drawingupdates/:sinceTime', (req, res, next) => {
    var sinceTime = parseInt(req.params.sinceTime) || 0
    var drawings = data.drawings.where(drawing => drawing.meta.updated >= sinceTime)
    res.json({time: new Date().getTime(), drawings})
})
app.get('/drawingindex/:sinceTime', (req, res, next) => {
    var sinceTime = parseInt(req.params.sinceTime) || 0
    var drawings = data.drawings.where(drawing => drawing.meta.updated >= sinceTime)
    var changes = drawings.map(drawing => ({
        uuid: drawing.uuid,
        type: drawing.type,
        facing: drawing.facing,
        deleted: drawing.status == STATUS.DELETED,
    }))
    res.json({time: new Date().getTime(), changes})
})

app.post('/drawing', (req, res, next) => {
    var drawing = data.drawings.findOne({uuid: req.body.uuid})
    if (!drawing) {
        drawing = Drawing({uuid: req.body.uuid})
        data.drawings.insert(drawing)
    }
    // update model
    drawing.json = req.body.json
    drawing.empty = ((drawing.json || {}).objects || []).length == 0
    drawing.dimensions = req.body.dimensions
    if (drawing.status == STATUS.UPDATED || drawing.status == STATUS.APPROVED) {
        drawing.status = STATUS.UPDATED
    }
    var autoapprove = drawing.status != STATUS.IGNORED && (GLOBAL.autoapprove || drawing.autoapprove)
    if (autoapprove) {
        drawing.status = STATUS.APPROVED
    }
    data.drawings.update(drawing)
    // save to png using minimally cropped dimensions provided by client
    canvas.setDimensions({ width: drawing.dimensions.width, height: drawing.dimensions.height })
    canvas.loadFromJSON(drawing.json, () => {
        // render objects
        canvas.renderAll()
        // save file
        var destStream = fs.createWriteStream(drawingDirNew + drawing.uuid + '.png')
        canvas.createPNGStream().on('data', chunk => destStream.write(chunk))
        // autoapprove copy file
        if (autoapprove) {
            var destStream2 = fs.createWriteStream(drawingDirApproved + drawing.uuid + '.png')
            canvas.createPNGStream().on('data', chunk => destStream2.write(chunk))
        }
    })
    // return
    res.sendStatus(200)
})

app.get('/drawing/:uuid/complete', checkDrawing, (req, res, next) => {
    var drawing = req.drawing
    // update
    drawing.completedTime = new Date().getTime()
    data.drawings.update(drawing)
    // return
    res.sendStatus(200)
})
app.get('/drawing/:uuid/approve', checkDrawing, (req, res, next) => {
    var drawing = req.drawing
    // update
    drawing.status = STATUS.APPROVED
    data.drawings.update(drawing)
    // copy file
    try {
        fs.createReadStream(drawingDirNew + drawing.uuid + '.png')
        .on('error', (err) => { if (err) console.log('approve: ' + err) })
        .pipe(fs.createWriteStream(drawingDirApproved + drawing.uuid + '.png'))
    } catch (e) {
    }
    // return
    res.sendStatus(200)
})
app.get('/drawing/:uuid/autoapprove/:toggle', checkDrawing, (req, res, next) => {
    var drawing = req.drawing
    // update
    drawing.autoapprove = req.params.toggle == 'true'
    if (drawing.autoapprove) {
        drawing.status = STATUS.APPROVED
        // copy file
        try {
            fs.createReadStream(drawingDirNew + drawing.uuid + '.png')
            .on('error', (err) => { if (err) console.log('autoapprove: ' + err) })
            .pipe(fs.createWriteStream(drawingDirApproved + drawing.uuid + '.png'))
        } catch (e) {
        }
    }
    data.drawings.update(drawing)
    // return
    res.sendStatus(200)
})
app.get('/drawing/:uuid/ignore/:toggle', checkDrawing, (req, res, next) => {
    var drawing = req.drawing
    // update
    drawing.status = req.params.toggle == 'true' ? STATUS.IGNORED : STATUS.NEW
    if (drawing.status == STATUS.IGNORED) {
        drawing.autoapprove = false
    }
    data.drawings.update(drawing)
    // return
    res.sendStatus(200)
})
app.get('/drawing/:uuid/delete', checkDrawing, (req, res, next) => {
    var drawing = req.drawing
    // update
    drawing.status = STATUS.DELETED
    drawing.autoapprove = false
    data.drawings.update(drawing)
    // delete file
    fs.unlink(drawingDirApproved + drawing.uuid + '.png', (err) => { if (err) console.log('delete: ' + err) })
    // return
    res.sendStatus(200)
})
app.get('/drawing/:uuid/type/:type', checkDrawing, (req, res, next) => {
    var drawing = req.drawing
    // update
    drawing.type = req.params.type
    data.drawings.update(drawing)
    // return
    res.sendStatus(200)
})
app.get('/drawing/:uuid/facing/:facing', checkDrawing, (req, res, next) => {
    var drawing = req.drawing
    // update
    drawing.facing = req.params.facing
    data.drawings.update(drawing)
    // return
    res.sendStatus(200)
})

app.get('/globalautoapprove', (req, res, next) => {
    res.send(GLOBAL.autoapprove)
})
app.get('/globalautoapprove/:toggle', (req, res, next) => {
    // update
    GLOBAL.autoapprove = req.params.toggle == 'true'
    // return
    res.sendStatus(200)
})

app.get('/hueshiftspeed', (req, res, next) => {
    res.json(GLOBAL.hueshiftspeed)
})
app.get('/hueshiftspeed/:val', (req, res, next) => {
    // update
    GLOBAL.hueshiftspeed = parseFloat(req.params.val)
    // return
    res.json(GLOBAL.hueshiftspeed)
})

app.get('/color/:type', (req, res, next) => {
    res.json(GLOBAL.colors[req.params.type] || 0)
})
app.get('/color/:type/:hue', (req, res, next) => {
    // update
    GLOBAL.colors[req.params.type] = parseInt(req.params.hue)
    // return
    res.json(GLOBAL.colors[req.params.type] || 0)
})

// catch 404 and forward to error handler
app.use((req, res, next) => {
    var err = new Error('Not Found')
    err.status = 404
    next(err)
})

// error handler
app.use((err, req, res, next) => {
    // set locals, only providing error in development
    res.locals.message = err.message
    res.locals.error = req.app.get('env') === 'development' ? err : {}

    // render the error page
    res.status(err.status || 500)
    res.render('error')
})

// start listening
app.listen(config.PORT, function () {
    console.log(`
************************************
** Started listening on port ${config.PORT} **
************************************`)
})
module.exports = app