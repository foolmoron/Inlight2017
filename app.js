const express = require('express')
const path = require('path')
const favicon = require('serve-favicon')
const logger = require('morgan')
const cookieParser = require('cookie-parser')
const bodyParser = require('body-parser')
const session = require('express-session')

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
app.use(session({
    secret: config.SECRET,
    resave: true,
    saveUninitialized: true,
}))

// auth
var adminAuth = (req, res, next) => {
    if (req.session && req.session.isAdmin) {
        next()
    }
    res.redirect('/login')
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
        res.redirect('/')
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