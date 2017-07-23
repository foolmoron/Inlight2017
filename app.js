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

const PORT = 8000
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
const db = new loki('db/db.json', { autosave: true })
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
        dirNew: 'http://localhost:8000/img/drawings/new/', 
        dirApproved: 'http://localhost:8000/img/drawings/approved/' 
    })
})

// drawing
const drawingDirectory = __dirname + '/public/img/drawings/'
const drawingNew = drawingDirectory + 'new/'
if (!fs.existsSync(drawingNew)) {
    fs.mkdirSync(drawingNew)
}
const drawingApproved = drawingDirectory + 'approved/'
if (!fs.existsSync(drawingApproved)) {
    fs.mkdirSync(drawingApproved)
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
    json: '',
    status: STATUS.NEW,
    autoapprove: false,
}, init)

app.get('/drawing', (req, res, next) => {
    var newDrawing = Drawing()
    data.drawings.insert(newDrawing)
    var drawing = data.drawings.findOne({uuid: newDrawing.uuid})
    data.drawings.update(drawing)
    res.json({uuid: drawing.uuid})
})
app.get('/drawing/:uuid', (req, res, next) => {
    var drawing = data.drawings.findOne({uuid: req.params.uuid})
    if (!drawing) {
        return res.status(400).json({error : 'invalid drawing uuid'})
    }
    delete drawing.json
    res.json(drawing)
})
app.get('/drawingupdates/:sinceTime', (req, res, next) => {
    var sinceTime = parseInt(req.params.sinceTime) || 0
    var drawings = data.drawings.where(drawing => drawing.meta.updated >= sinceTime)
    res.json(drawings)
})
app.get('/drawingindex/:sinceTime', (req, res, next) => {
    var sinceTime = parseInt(req.params.sinceTime) || 0
    var drawings = data.drawings.where(drawing => drawing.meta.updated >= sinceTime)
    var drawingIndex = drawings.map(drawing => ({
        uuid: drawing.uuid,
        deleted: drawing.status == STATUS.DELETED,
    }))
    res.json(drawingIndex)
})

app.post('/drawing', (req, res, next) => {
    var drawing = data.drawings.findOne({uuid: req.body.uuid})
    if (!drawing) {
        drawing = Drawing({uuid: req.body.uuid})
        data.drawings.insert(drawing)
    }
    // update model
    drawing.json = req.body.json
    if (drawing.status == STATUS.UPDATED || drawing.status == STATUS.APPROVED) {
        drawing.status = STATUS.UPDATED
    }
    var autoapprove = drawing.status != STATUS.IGNORED && (GLOBAL.autoapprove ||drawing.autoapprove)
    if (autoapprove) {
        drawing.status = STATUS.APPROVED
    }
    data.drawings.update(drawing)
    // render to png
    var canvas = fabric.createCanvasForNode(500, 500)
    canvas.loadFromJSON(drawing.json, () => {
        canvas.renderAll()
        var destStream = fs.createWriteStream(drawingNew + drawing.uuid + '.png')
        canvas.createPNGStream().on('data', chunk => destStream.write(chunk))
        // autoapprove copy file
        if (autoapprove) {
            var destStream2 = fs.createWriteStream(drawingApproved + drawing.uuid + '.png')
            canvas.createPNGStream().on('data', chunk => destStream2.write(chunk))
        }
    })
    // return
    res.sendStatus(200)
})

app.get('/drawing/:uuid/approve', (req, res, next) => {
    var drawing = data.drawings.findOne({uuid: req.params.uuid})
    if (!drawing) {
        return res.status(400).json({error : 'invalid drawing uuid'})
    }
    // update
    drawing.status = STATUS.APPROVED
    data.drawings.update(drawing)
    // copy file
    try {
        fs.createReadStream(drawingNew + drawing.uuid + '.png')
        .on('error', (err) => { if (err) console.log('approve: ' + err) })
        .pipe(fs.createWriteStream(drawingApproved + drawing.uuid + '.png'))
    } catch (e) {
    }
    // return
    res.sendStatus(200)
})
app.get('/drawing/:uuid/autoapprove/:toggle', (req, res, next) => {
    var drawing = data.drawings.findOne({uuid: req.params.uuid})
    if (!drawing) {
        return res.status(400).json({error : 'invalid drawing uuid'})
    }
    // update
    drawing.autoapprove = req.params.toggle == 'true'
    if (drawing.autoapprove) {
        drawing.status = STATUS.APPROVED
        // copy file
        try {
            fs.createReadStream(drawingNew + drawing.uuid + '.png')
            .on('error', (err) => { if (err) console.log('autoapprove: ' + err) })
            .pipe(fs.createWriteStream(drawingApproved + drawing.uuid + '.png'))
        } catch (e) {
        }
    }
    data.drawings.update(drawing)
    // return
    res.sendStatus(200)
})
app.get('/drawing/:uuid/ignore/:toggle', (req, res, next) => {
    var drawing = data.drawings.findOne({uuid: req.params.uuid})
    if (!drawing) {
        return res.status(400).json({error : 'invalid drawing uuid'})
    }
    // update
    drawing.status = req.params.toggle == 'true' ? STATUS.IGNORED : STATUS.NEW
    if (drawing.status == STATUS.IGNORED) {
        drawing.autoapprove = false
    }
    data.drawings.update(drawing)
    // return
    res.sendStatus(200)
})
app.get('/drawing/:uuid/delete', (req, res, next) => {
    var drawing = data.drawings.findOne({uuid: req.params.uuid})
    if (!drawing) {
        return res.status(400).json({error : 'invalid drawing uuid'})
    }
    // update
    drawing.status = STATUS.DELETED
    drawing.autoapprove = false
    data.drawings.update(drawing)
    // delete file
    fs.unlink(drawingApproved + drawing.uuid + '.png', (err) => { if (err) console.log('delete: ' + err) })
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
app.listen(PORT, function () {
    console.log(`
************************************
** Started listening on port ${PORT} **
************************************`)
})
module.exports = app