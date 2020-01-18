const express = require('express')
const http = require('http')
const https = require('https')
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
    initCollection(data, 'commands', { unique: ['key'] })
})

const GLOBAL = {
    autoapprove: false,
    prevDrawingType: 'a',
    typeToName: {
        'a': 'animal',
        'p': 'plant',
        'r': 'bird',
        'd': 'building',
    },
    nextDrawingChances: {
        'a': {
            'a': 0.0,
            'p': 0.8,
            'r': 0.1,
            'd': 0.1,
        },
        'p': {
            'a': 0.25,
            'p': 0.35,
            'r': 0.15,
            'd': 0.25,
        },
        'r': {
            'a': 0.2,
            'p': 0.4,
            'r': 0.0,
            'd': 0.4,
        },
        'd': {
            'a': 0.3,
            'p': 0.3,
            'r': 0.3,
            'd': 0.1,
        },
    },
    colors: {
        animal: 36,
        plant: 96,
        bird: 180,
        building: 282,
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
        dirNew: `${config.HOST}:${config.HTTPS_PORT}/img/drawings/new/`,
        dirApproved: `${config.HOST}:${config.HTTPS_PORT}/img/drawings/approved/`
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
var canvas = new fabric.StaticCanvas(null, { width: 11, height: 13 }) // 1 canvas for whole app
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

const Drawing = (init) => Object.assign({
    uuid: uuid(),
    type: 'p',
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

    var chances = GLOBAL.nextDrawingChances[GLOBAL.prevDrawingType]
    var r = Math.random()
    var totalChance = 0
    for (var type in chances) {
        totalChance += chances[type]
        if (r <= totalChance) {
            newDrawing.type = type;
            break;
        }
    }
    GLOBAL.prevDrawingType = newDrawing.type

    var colorType = GLOBAL.typeToName[newDrawing.type]
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
        completed: drawing.completedTime != null,
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
app.get('/drawing/:uuid/incomplete', checkDrawing, (req, res, next) => {
    var drawing = req.drawing
    // update
    drawing.completedTime = null
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

const COMMAND_TYPE = {
    WIGGLE: 'wiggle',
    SPAWN: 'spawn',
}

const Command = (init) => Object.assign({
    key: '',
    uuid: '',
    type: '',
    num: 0,
}, init)

function checkCommand(req, res, next) {
    var upperType = (req.params.type + '').toUpperCase()
    if (!(upperType in COMMAND_TYPE)) {
        return res.status(400).json({error : 'invalid command type'})
    }
    var key = req.params.uuid + ' ' + upperType
    var command = data.commands.findOne({key})
    if (!command) {
        command = Command({key, uuid: req.params.uuid, type: upperType})
        data.commands.insert(command)
        command = data.commands.findOne({key})
        data.commands.update(command)
    }
    req.command = command
    next()
}

app.get('/command/:type/:uuid/add/:num', checkDrawing, checkCommand, (req, res, next) => {
    var command = req.command
    // update
    var num = parseInt(req.params.num) || 0
    if (num) {
        command.num += num
        data.commands.update(command)
    }
    // return
    res.sendStatus(200)
})
app.get('/command/:type/:uuid/clear', checkDrawing, checkCommand, (req, res, next) => {
    var command = req.command
    // update
    command.num = 0
    data.commands.update(command)
    // return
    res.sendStatus(200)
})
app.get('/command/:type?/:uuid?/:sincetime', (req, res, next) => {
    // pull
    var sinceTime = parseInt(req.params.sincetime) || 0
    var upperType = (req.params.type + '').toUpperCase()
    var commands = data.commands
        .where(command =>
            (req.params.type == null || upperType === command.type) &&
            (req.params.uuid == null || req.params.uuid === command.uuid) &&
            (command.meta.updated >= sinceTime)
        )
        .map(command => ({
            uuid: command.uuid,
            type: command.type,
            num: command.num,
        }))
    // return
    res.json({time: new Date().getTime(), commands})
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

    // never cache error pages
    res.header('Cache-Control', 'private, no-cache, max-age=0')
    res.header('Expires', '-1')
    res.header('Pragma', 'no-cache')

    // render the error page
    res.status(err.status || 500)
    res.render('error')
})

// start listening on http and https
http.createServer(app).listen(config.HTTP_PORT, function () {
    console.log(`
************************************
** Started listening on port ${config.HTTP_PORT} **
************************************`)
})
try {
    https.createServer({ key: fs.readFileSync(config.KEY_PATH), cert: fs.readFileSync(config.CERT_PATH) }, app).listen(config.HTTPS_PORT, function () {
        console.log(`
    ++++++++++++++++++++++++++++++++++++
    ++ Started listening on port ${config.HTTPS_PORT} ++
    ++++++++++++++++++++++++++++++++++++`)
    })
} catch (e) {

}

module.exports = app