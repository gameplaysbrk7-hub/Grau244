package dev.graumestre.ui

import android.content.Context
import android.graphics.Canvas
import android.graphics.Color
import android.graphics.LinearGradient
import android.graphics.Paint
import android.graphics.Path
import android.graphics.RectF
import android.graphics.Shader
import android.os.Build
import android.os.VibrationEffect
import android.os.Vibrator
import android.view.MotionEvent
import android.view.SurfaceHolder
import android.view.SurfaceView
import dev.graumestre.Store
import dev.graumestre.core.CrashReason
import dev.graumestre.core.Game
import dev.graumestre.core.ObstacleType
import dev.graumestre.core.Phase
import dev.graumestre.core.Physics
import dev.graumestre.core.Tuning
import dev.graumestre.core.Upgrades
import kotlin.math.PI
import kotlin.math.cos
import kotlin.math.max
import kotlin.math.min
import kotlin.math.sin

private enum class Screen { MENU, SHOP, PLAY, PAUSE, OVER }

private class Btn(val id: String, val r: RectF, val label: String, val color: Int, val enabled: Boolean = true)
private class Popup(val text: String, var y: Float, var life: Float, val color: Int)

private class Skin(val name: String, val color: Int, val cost: Int)

private val SKINS = listOf(
    Skin("Laranja", 0xFFFF6B1A.toInt(), 0),
    Skin("Azul", 0xFF2D7BFF.toInt(), 150),
    Skin("Verde", 0xFF22C55E.toInt(), 150),
    Skin("Vermelha", 0xFFE5233B.toInt(), 250),
    Skin("Dourada", 0xFFFFC21A.toInt(), 400),
)

// céu: topo / horizonte, para dia, entardecer e noite
private val SKY_TOP = intArrayOf(0xFF4FA3F7.toInt(), 0xFF5B3A8C.toInt(), 0xFF0B1030.toInt())
private val SKY_BOTTOM = intArrayOf(0xFFBFE4FF.toInt(), 0xFFFF9A5C.toInt(), 0xFF2B3A78.toInt())

class GameView(context: Context, private val store: Store) : SurfaceView(context), SurfaceHolder.Callback, Runnable {

    private val lock = Any()
    private var thread: Thread? = null
    @Volatile private var running = false
    private var surfaceReady = false

    private var screen = Screen.MENU
    private var game: Game? = null
    private var buttons = ArrayList<Btn>()

    private var w = 1f
    private var h = 1f
    private var u = 1f            // 1% da altura da tela
    private var ppm = 1f          // pixels por metro
    private var groundY = 0f
    private var bikeX = 0f

    // entrada
    private val touches = HashMap<Int, Pair<Float, Float>>()
    private var throttle = false
    private var brake = false

    // estado visual
    private var menuDist = 0f
    private var clock = 0f
    private var crashT = 0f
    private var playTime = 0f
    private var creditedThisRun = false
    private var newRecord = false
    private var lastPassed = 0
    private var lastWheelie = false
    private val popups = ArrayList<Popup>()
    private var toast: String? = null
    private var toastLife = 0f

    // tintas reutilizáveis
    private val p = Paint(Paint.ANTI_ALIAS_FLAG)
    private val tp = Paint(Paint.ANTI_ALIAS_FLAG).apply { typeface = android.graphics.Typeface.DEFAULT_BOLD }
    private val path = Path()
    private val rect = RectF()

    init { holder.addCallback(this); isFocusable = true }

    // ---------- ciclo de vida ----------
    override fun surfaceCreated(holder: SurfaceHolder) { surfaceReady = true; startLoop() }
    override fun surfaceChanged(holder: SurfaceHolder, format: Int, width: Int, height: Int) {
        synchronized(lock) {
            w = width.toFloat(); h = height.toFloat(); u = h / 100f
            ppm = h / 16f; groundY = h * 0.80f; bikeX = w * 0.30f
        }
    }
    override fun surfaceDestroyed(holder: SurfaceHolder) { surfaceReady = false; stopLoop() }

    fun onHostPause() { synchronized(lock) { if (screen == Screen.PLAY) screen = Screen.PAUSE; clearInput() }; stopLoop() }
    fun onHostResume() { if (surfaceReady) startLoop() }

    /** @return true se consumiu o botão voltar. */
    fun handleBack(): Boolean = synchronized(lock) {
        when (screen) {
            Screen.PLAY -> { screen = Screen.PAUSE; clearInput(); true }
            Screen.PAUSE -> { screen = Screen.PLAY; true }
            Screen.SHOP, Screen.OVER -> { screen = Screen.MENU; true }
            Screen.MENU -> false
        }
    }

    private fun startLoop() {
        if (running) return
        running = true
        thread = Thread(this, "game-loop").also { it.start() }
    }

    private fun stopLoop() {
        running = false
        thread?.join(500)
        thread = null
    }

    override fun run() {
        val step = 1f / 60f
        var last = System.nanoTime()
        var acc = 0f
        while (running) {
            val now = System.nanoTime()
            acc += min((now - last) / 1e9f, 0.1f)
            last = now
            synchronized(lock) {
                while (acc >= step) { update(step); acc -= step }
            }
            val c = try { holder.lockCanvas() } catch (e: Exception) { null }
            if (c != null) {
                try { synchronized(lock) { drawFrame(c) } } finally { holder.unlockCanvasAndPost(c) }
            }
            val spent = (System.nanoTime() - now) / 1_000_000
            if (spent < 14) try { Thread.sleep(14 - spent) } catch (_: InterruptedException) {}
        }
    }

    // ---------- lógica de tela ----------
    private fun startRun() {
        game = Game(Tuning(store.upgrades))
        screen = Screen.PLAY
        playTime = 0f; crashT = 0f; creditedThisRun = false; newRecord = false
        lastPassed = 0; lastWheelie = false; popups.clear(); clearInput()
    }

    private fun update(dt: Float) {
        clock += dt
        if (toastLife > 0f) toastLife -= dt
        when (screen) {
            Screen.MENU, Screen.SHOP -> menuDist += dt * 11f
            Screen.PLAY -> updatePlay(dt)
            Screen.OVER -> crashT += dt
            Screen.PAUSE -> {}
        }
        popups.removeAll { it.life <= 0f }
        for (pp in popups) { pp.life -= dt; pp.y -= 6f * u * dt }
    }

    private fun updatePlay(dt: Float) {
        val g = game ?: return
        playTime += dt
        g.update(dt, throttle, brake)
        if (g.obstaclesPassed != lastPassed) { lastPassed = g.obstaclesPassed; popups.add(Popup("+BÔNUS", h * 0.45f, 1f, 0xFF7CFC00.toInt())) }
        if (g.wheelieActive && !lastWheelie) popups.add(Popup("GRAU!", h * 0.38f, 0.9f, 0xFFFFD23F.toInt()))
        lastWheelie = g.wheelieActive
        if (g.phase == Phase.CRASHED) finishRun(g)
    }

    private fun finishRun(g: Game) {
        screen = Screen.OVER
        crashT = 0f
        clearInput()
        vibrate()
        if (!creditedThisRun) {
            creditedThisRun = true
            store.coins = store.coins + g.earnedCoins
            val sc = g.score.toInt()
            if (sc > store.bestScore) { store.bestScore = sc; newRecord = sc > 0 }
            if (g.bestWheelie > store.bestWheelie) store.bestWheelie = g.bestWheelie
        }
    }

    private fun vibrate() {
        try {
            val v = context.getSystemService(Context.VIBRATOR_SERVICE) as? Vibrator ?: return
            if (Build.VERSION.SDK_INT >= 26) v.vibrate(VibrationEffect.createOneShot(120, VibrationEffect.DEFAULT_AMPLITUDE))
            else @Suppress("DEPRECATION") v.vibrate(120)
        } catch (_: Exception) {}
    }

    private fun clearInput() { touches.clear(); throttle = false; brake = false }

    // ---------- entrada ----------
    override fun onTouchEvent(e: MotionEvent): Boolean {
        synchronized(lock) {
            when (e.actionMasked) {
                MotionEvent.ACTION_DOWN, MotionEvent.ACTION_POINTER_DOWN -> {
                    val i = e.actionIndex
                    val x = e.getX(i); val y = e.getY(i)
                    if (screen == Screen.PLAY) {
                        if (pauseHit(x, y)) { screen = Screen.PAUSE; clearInput() }
                        else touches[e.getPointerId(i)] = Pair(x, y)
                    }
                }
                MotionEvent.ACTION_MOVE -> if (screen == Screen.PLAY) {
                    for (i in 0 until e.pointerCount) {
                        val id = e.getPointerId(i)
                        if (touches.containsKey(id)) touches[id] = Pair(e.getX(i), e.getY(i))
                    }
                }
                MotionEvent.ACTION_UP, MotionEvent.ACTION_POINTER_UP -> {
                    val i = e.actionIndex
                    if (screen != Screen.PLAY) onTap(e.getX(i), e.getY(i))
                    touches.remove(e.getPointerId(i))
                }
                MotionEvent.ACTION_CANCEL -> clearInput()
            }
            throttle = touches.values.any { it.first >= w / 2f }
            brake = touches.values.any { it.first < w / 2f }
        }
        return true
    }

    private fun pauseHit(x: Float, y: Float): Boolean {
        val cx = w - 7f * u; val cy = 7f * u
        return (x - cx) * (x - cx) + (y - cy) * (y - cy) < (7f * u) * (7f * u)
    }

    private fun onTap(x: Float, y: Float) {
        val b = buttons.firstOrNull { it.enabled && it.r.contains(x, y) } ?: return
        when (b.id) {
            "play" -> startRun()
            "shop" -> screen = Screen.SHOP
            "menu", "back" -> screen = Screen.MENU
            "resume" -> screen = Screen.PLAY
            "buy_engine" -> buyUpgrade(true)
            "buy_balance" -> buyUpgrade(false)
            else -> if (b.id.startsWith("skin_")) pickSkin(b.id.removePrefix("skin_").toInt())
        }
    }

    private fun say(msg: String) { toast = msg; toastLife = 2f }

    private fun buyUpgrade(engine: Boolean) {
        val lvl = if (engine) store.engine else store.balance
        if (lvl >= Upgrades.MAX_LEVEL) return
        val cost = Upgrades.cost(lvl)
        if (store.coins < cost) { say("Moedas insuficientes"); return }
        store.coins = store.coins - cost
        if (engine) store.engine = lvl + 1 else store.balance = lvl + 1
        say("Melhoria comprada!")
    }

    private fun pickSkin(i: Int) {
        if (i !in SKINS.indices) return
        val owned = store.skinsOwned and (1 shl i) != 0
        if (owned) { store.skin = i; return }
        val cost = SKINS[i].cost
        if (store.coins < cost) { say("Moedas insuficientes"); return }
        store.coins = store.coins - cost
        store.skinsOwned = store.skinsOwned or (1 shl i)
        store.skin = i
        say("Cor desbloqueada!")
    }

    // ---------- desenho ----------
    private fun drawFrame(c: Canvas) {
        buttons = ArrayList()
        when (screen) {
            Screen.MENU -> { drawWorld(c, menuDist, null); drawMenu(c) }
            Screen.SHOP -> { drawWorld(c, menuDist, null); drawShop(c) }
            Screen.PLAY -> { drawWorld(c, game?.distance ?: 0f, game); drawHud(c) }
            Screen.PAUSE -> { drawWorld(c, game?.distance ?: 0f, game); drawHud(c); drawPause(c) }
            Screen.OVER -> { drawWorld(c, game?.distance ?: 0f, game); drawOver(c) }
        }
        toast?.let { if (toastLife > 0f) text(c, it, w / 2f, h * 0.92f, 4.5f * u, Color.WHITE, Paint.Align.CENTER) }
    }

    private fun lerpColor(a: Int, b: Int, t: Float): Int {
        val r = (Color.red(a) + (Color.red(b) - Color.red(a)) * t).toInt()
        val g = (Color.green(a) + (Color.green(b) - Color.green(a)) * t).toInt()
        val bl = (Color.blue(a) + (Color.blue(b) - Color.blue(a)) * t).toInt()
        return Color.rgb(r, g, bl)
    }

    private fun hash(i: Int, salt: Int): Int {
        var x = i * 374761393 + salt * 668265263
        x = (x xor (x ushr 13)) * 1274126177
        return (x xor (x ushr 16)) and 0x7fffffff
    }

    private fun drawWorld(c: Canvas, dist: Float, g: Game?) {
        // céu: dia → entardecer → noite, a cada 500 m
        val seg = dist / 500f
        val a = seg.toInt() % 3
        val b = (a + 1) % 3
        val f = seg - seg.toInt()
        val t = if (f < 0.85f) 0f else (f - 0.85f) / 0.15f
        val top = lerpColor(SKY_TOP[a], SKY_TOP[b], t)
        val bot = lerpColor(SKY_BOTTOM[a], SKY_BOTTOM[b], t)
        p.shader = LinearGradient(0f, 0f, 0f, groundY, top, bot, Shader.TileMode.CLAMP)
        c.drawRect(0f, 0f, w, groundY, p)
        p.shader = null
        val night = if (a == 2) 1f - t else if (b == 2) t else 0f

        if (night > 0.4f) { // estrelas
            p.color = Color.argb((255 * night).toInt(), 255, 255, 255)
            for (i in 0 until 40) {
                val sx = (hash(i, 1) % 1000) / 1000f * w
                val sy = (hash(i, 2) % 1000) / 1000f * groundY * 0.6f
                c.drawCircle(sx, sy, 0.25f * u, p)
            }
        }
        // sol / lua
        p.color = if (night > 0.5f) 0xFFF4F1DE.toInt() else 0xFFFFE08A.toInt()
        c.drawCircle(w * 0.80f, h * 0.20f, 7f * u, p)

        // prédios em duas camadas de paralaxe
        skyline(c, dist, 0.15f, 0.30f, 0.42f, lerpColor(0xFF7A8FB5.toInt(), 0xFF1A2347.toInt(), night), night, false)
        skyline(c, dist, 0.45f, 0.45f, 0.62f, lerpColor(0xFF4E5F86.toInt(), 0xFF10162E.toInt(), night), night, true)

        // asfalto
        p.color = 0xFF2B2D33.toInt()
        c.drawRect(0f, groundY, w, h, p)
        p.color = 0xFF565A66.toInt()
        c.drawRect(0f, groundY, w, groundY + 0.8f * u, p)
        p.color = 0xFFF2F2F2.toInt()
        val dashW = 2.5f * ppm; val period = 6f * ppm
        var x = -((dist * ppm) % period)
        while (x < w) { c.drawRect(x, groundY + 10f * u, x + dashW, groundY + 11f * u, p); x += period }

        // linhas de velocidade
        val sp = g?.speed ?: 14f
        if (sp > 13f) {
            p.color = Color.argb(70, 255, 255, 255); p.strokeWidth = 0.3f * u
            for (i in 0 until 8) {
                val ly = groundY - (hash(i, 9) % 600) / 100f * ppm - 1.2f * ppm
                val lx = ((hash(i, 5) % 1000) / 1000f * w - clock * sp * ppm * 2f) % w
                val xx = if (lx < 0) lx + w else lx
                c.drawLine(xx, ly, xx + sp * 0.9f * u, ly, p)
            }
        }

        if (g != null) drawTrack(c, dist, g)
        drawBike(c, dist, g)
    }

    private fun skyline(c: Canvas, dist: Float, factor: Float, minH: Float, maxH: Float, color: Int, night: Float, windows: Boolean) {
        val slotW = 11f * ppm * (0.6f + factor)
        val off = dist * factor * ppm
        val first = (off / slotW).toInt() - 1
        val count = (w / slotW).toInt() + 3
        for (i in first until first + count) {
            val bh = (minH + (hash(i, (factor * 100).toInt()) % 1000) / 1000f * (maxH - minH)) * h
            val bw = slotW * (0.7f + (hash(i, 3) % 100) / 400f)
            val bx = i * slotW - off
            p.color = color
            c.drawRect(bx, groundY - bh, bx + bw, groundY, p)
            if (windows) {
                val lit = lerpColor(0xFF8793B5.toInt(), 0xFFFFD86B.toInt(), night)
                p.color = lit
                var wy = groundY - bh + 2f * u
                var row = 0
                while (wy < groundY - 3f * u) {
                    var wx = bx + 1.5f * u
                    var col = 0
                    while (wx < bx + bw - 2.5f * u) {
                        if (hash(i * 31 + row, col + 7) % 3 != 0) c.drawRect(wx, wy, wx + 1.4f * u, wy + 1.8f * u, p)
                        wx += 3f * u; col++
                    }
                    wy += 4.2f * u; row++
                }
            }
        }
    }

    private fun sx(worldX: Float, dist: Float) = bikeX + (worldX - dist) * ppm

    private fun drawTrack(c: Canvas, dist: Float, g: Game) {
        for (o in g.obstacles) {
            val x = sx(o.x, dist)
            if (x < -3f * ppm || x > w + 3f * ppm) continue
            when (o.type) {
                ObstacleType.CONE -> drawCone(c, x)
                ObstacleType.GATE -> drawGate(c, x)
            }
        }
        // dica sobre o próximo obstáculo
        g.obstacles.firstOrNull { it.x > g.distance }?.let { o ->
            val x = sx(o.x, dist)
            if (x < w) {
                val msg = if (o.type == ObstacleType.CONE) "↑ LEVANTE" else "↓ ABAIXE"
                text(c, msg, x, groundY - 4.3f * ppm, 3.2f * u, Color.WHITE, Paint.Align.CENTER)
            }
        }
        for (co in g.coinList) {
            if (co.taken) continue
            val x = sx(co.x, dist)
            if (x < -ppm || x > w + ppm) continue
            val bob = sin(clock * 5f + co.x) * 0.1f * ppm
            val on = g.wheelieActive
            p.color = if (on) 0xFFFFC21A.toInt() else 0x88FFC21A.toInt()
            c.drawCircle(x, groundY - 1.5f * ppm + bob, 0.38f * ppm, p)
            p.color = if (on) 0xFFFFE680.toInt() else 0x66FFE680.toInt()
            c.drawCircle(x, groundY - 1.5f * ppm + bob, 0.22f * ppm, p)
        }
    }

    private fun drawCone(c: Canvas, x: Float) {
        p.color = 0xFFFF7A1A.toInt()
        path.reset()
        path.moveTo(x - 0.45f * ppm, groundY); path.lineTo(x + 0.45f * ppm, groundY)
        path.lineTo(x + 0.12f * ppm, groundY - 1.05f * ppm); path.lineTo(x - 0.12f * ppm, groundY - 1.05f * ppm); path.close()
        c.drawPath(path, p)
        p.color = Color.WHITE
        c.drawRect(x - 0.27f * ppm, groundY - 0.62f * ppm, x + 0.27f * ppm, groundY - 0.40f * ppm, p)
        p.color = 0xFF3A3A3A.toInt()
        c.drawRect(x - 0.55f * ppm, groundY - 0.08f * ppm, x + 0.55f * ppm, groundY, p)
    }

    private fun drawGate(c: Canvas, x: Float) {
        val half = 1.6f * ppm
        p.color = 0xFF9AA0AD.toInt()
        c.drawRect(x - half - 0.18f * ppm, groundY - 3.4f * ppm, x - half + 0.18f * ppm, groundY, p)
        c.drawRect(x + half - 0.18f * ppm, groundY - 3.4f * ppm, x + half + 0.18f * ppm, groundY, p)
        // barra listrada amarelo/preto
        val top = groundY - 2.7f * ppm; val bot = groundY - 2.0f * ppm
        p.color = 0xFFFFD23F.toInt(); c.drawRect(x - half, top, x + half, bot, p)
        p.color = 0xFF1A1A1A.toInt()
        var sxp = x - half
        while (sxp < x + half) { c.drawRect(sxp, top, min(sxp + 0.35f * ppm, x + half), bot, p); sxp += 0.7f * ppm }
    }

    private fun drawBike(c: Canvas, dist: Float, g: Game?) {
        val angle: Float; val dx: Float; var dy = 0f
        val skinColor = SKINS[store.skin.coerceIn(SKINS.indices)].color
        when {
            g == null -> { angle = 0.55f + 0.05f * sin(clock * 2f); dx = 0f }
            g.phase == Phase.CRASHED -> {
                val tt = min(crashT, 1.2f)
                angle = g.angle + if (g.crashReason == CrashReason.FLIPPED) tt * 4f else tt * -1.2f
                dx = if (g.crashReason == CrashReason.FLIPPED) -tt * 3f * ppm else tt * 2f * ppm
                dy = 0f
            }
            else -> { angle = g.angle; dx = 0f }
        }
        val R = 0.38f * ppm
        val L = 1.45f * ppm
        val k = ppm // metro -> px
        c.save()
        c.translate(bikeX + dx, groundY - R + dy)
        c.rotate(-angle * 180f / PI.toFloat())
        val spin = (dist * ppm / R) * 180f / PI.toFloat()

        fun wheel(cx: Float) {
            p.style = Paint.Style.STROKE; p.strokeWidth = 0.17f * k; p.color = 0xFF121212.toInt()
            c.drawCircle(cx, 0f, R, p)
            p.strokeWidth = 0.05f * k; p.color = 0xFFB8BCC8.toInt()
            c.drawCircle(cx, 0f, R * 0.72f, p)
            c.save(); c.rotate(spin, cx, 0f)
            for (i in 0 until 3) {
                val a = i * PI.toFloat() / 3f
                c.drawLine(cx - cos(a) * R * 0.7f, -sin(a) * R * 0.7f, cx + cos(a) * R * 0.7f, sin(a) * R * 0.7f, p)
            }
            c.restore()
            p.style = Paint.Style.FILL
        }
        fun line(x1: Float, y1: Float, x2: Float, y2: Float, wd: Float, col: Int) {
            p.style = Paint.Style.STROKE; p.strokeCap = Paint.Cap.ROUND
            p.strokeWidth = wd * k; p.color = col
            c.drawLine(x1 * k, y1 * k, x2 * k, y2 * k, p)
            p.style = Paint.Style.FILL; p.strokeCap = Paint.Cap.BUTT
        }
        val dark = 0xFF22242B.toInt()
        wheel(0f); wheel(L)
        // balança, motor, escapamento
        line(0f, 0f, 0.5f / 1f, -0.3f, 0.12f, dark)
        rect.set(0.45f * k, -0.55f * k, 1.0f * k, -0.12f * k); p.color = 0xFF50545F.toInt(); c.drawRoundRect(rect, 0.06f * k, 0.06f * k, p)
        line(0.35f, -0.12f, -0.25f, -0.1f, 0.1f, 0xFF8A8F9C.toInt())
        // garfo e guidão
        val head = Pair((L / k) - 0.3f, -0.9f)
        line(L / k, 0f, head.first, head.second, 0.1f, 0xFFB8BCC8.toInt())
        line(head.first, head.second, head.first - 0.2f, -1.08f, 0.09f, dark)
        // quadro, tanque, banco
        line(0.5f, -0.3f, head.first, head.second, 0.12f, dark)
        rect.set(0.5f * k, -1.0f * k, 1.0f * k, -0.66f * k); p.color = skinColor; c.drawRoundRect(rect, 0.12f * k, 0.12f * k, p)
        rect.set(-0.1f * k, -0.76f * k, 0.55f * k, -0.62f * k); p.color = dark; c.drawRoundRect(rect, 0.07f * k, 0.07f * k, p)
        // farol
        p.color = 0xFFFFF2B3.toInt(); c.drawCircle((L / k - 0.12f) * k, -0.98f * k, 0.13f * k, p)
        // piloto
        val jacket = 0xFF2A2F4A.toInt()
        line(0.25f, -0.78f, 0.7f, -1.45f, 0.3f, jacket)           // tronco
        line(0.7f, -1.4f, head.first - 0.2f, -1.08f, 0.13f, jacket)  // braço
        line(0.25f, -0.72f, 0.65f, -0.3f, 0.17f, 0xFF3B4F80.toInt()) // perna
        p.color = skinColor; c.drawCircle(0.82f * k, -1.72f * k, 0.22f * k, p) // capacete
        p.color = 0xFF111111.toInt(); c.drawRect(0.86f * k, -1.78f * k, 1.04f * k, -1.66f * k, p) // viseira
        c.restore()
    }

    // ---------- HUD ----------
    private fun drawHud(c: Canvas) {
        val g = game ?: return
        text(c, g.score.toInt().toString(), 3f * u, 9f * u, 8f * u, Color.WHITE, Paint.Align.LEFT)
        if (g.wheelieActive) {
            val col = if (g.inSweetSpot) 0xFF7CFC00.toInt() else 0xFFFFD23F.toInt()
            text(c, "x" + String.format("%.1f", g.multiplier), 3f * u, 16f * u, 5.5f * u, col, Paint.Align.LEFT)
        }
        // moedas
        p.color = 0xFFFFC21A.toInt(); c.drawCircle(w / 2f - 6f * u, 6f * u, 2.2f * u, p)
        text(c, (g.coins).toString(), w / 2f - 3f * u, 8f * u, 5.5f * u, Color.WHITE, Paint.Align.LEFT)
        // velocímetro
        text(c, "${(g.speed * 5f).toInt()} km/h", 3f * u, h - 3f * u, 4.5f * u, 0xFFCCD2E6.toInt(), Paint.Align.LEFT)
        text(c, "${g.distance.toInt()} m", w - 3f * u, h - 3f * u, 4.5f * u, 0xFFCCD2E6.toInt(), Paint.Align.RIGHT)
        drawGauge(c, g)
        // pausa
        val cx = w - 7f * u; val cy = 7f * u
        p.color = 0x66000000; c.drawCircle(cx, cy, 5f * u, p)
        p.color = Color.WHITE
        c.drawRect(cx - 1.8f * u, cy - 2.2f * u, cx - 0.6f * u, cy + 2.2f * u, p)
        c.drawRect(cx + 0.6f * u, cy - 2.2f * u, cx + 1.8f * u, cy + 2.2f * u, p)
        // popups
        for (pp in popups) {
            val a = (255 * min(1f, max(0f, pp.life) * 2f)).toInt()
            text(c, pp.text, bikeX + 8f * u, pp.y, 6f * u, (pp.color and 0xFFFFFF) or (a shl 24), Paint.Align.LEFT)
        }
        // dica de controles
        if (playTime < 5f) {
            p.color = 0x55000000; c.drawRoundRect(RectF(w * 0.12f, h * 0.30f, w * 0.88f, h * 0.43f), 2f * u, 2f * u, p)
            text(c, "DIREITA: acelera e ergue a roda  •  ESQUERDA: freia e abaixa", w / 2f, h * 0.375f, 3.6f * u, Color.WHITE, Paint.Align.CENTER)
            text(c, "Segure o ponteiro na faixa verde para pontuar mais!", w / 2f, h * 0.415f, 3f * u, 0xFFCCD2E6.toInt(), Paint.Align.CENTER)
        }
    }

    private fun drawGauge(c: Canvas, g: Game) {
        val gw = w * 0.34f; val gh = 2.2f * u
        val x0 = (w - gw) / 2f; val y0 = h - 7f * u
        val maxA = Physics.FLIP_ANGLE
        fun gx(a: Float) = x0 + gw * (a / maxA)
        p.color = 0x88000000.toInt(); rect.set(x0 - u, y0 - u, x0 + gw + u, y0 + gh + u); c.drawRoundRect(rect, u, u, p)
        p.color = 0xFF4A4F63.toInt(); c.drawRect(x0, y0, gx(Physics.WHEELIE_ON), y0 + gh, p)
        p.color = 0xFFC9A227.toInt(); c.drawRect(gx(Physics.WHEELIE_ON), y0, gx(Physics.BALANCE_ANGLE - Physics.SWEET_ZONE), y0 + gh, p)
        p.color = 0xFF2ECC71.toInt(); c.drawRect(gx(Physics.BALANCE_ANGLE - Physics.SWEET_ZONE), y0, gx(Physics.BALANCE_ANGLE + Physics.SWEET_ZONE), y0 + gh, p)
        p.color = 0xFFC9A227.toInt(); c.drawRect(gx(Physics.BALANCE_ANGLE + Physics.SWEET_ZONE), y0, gx(1.0f), y0 + gh, p)
        p.color = 0xFFE5233B.toInt(); c.drawRect(gx(1.0f), y0, x0 + gw, y0 + gh, p)
        val mx = gx(g.angle.coerceIn(0f, maxA))
        p.color = Color.WHITE
        path.reset(); path.moveTo(mx, y0 - 0.2f * u); path.lineTo(mx - 1.4f * u, y0 - 2.4f * u); path.lineTo(mx + 1.4f * u, y0 - 2.4f * u); path.close()
        c.drawPath(path, p)
    }

    // ---------- menus ----------
    private fun panel(c: Canvas, l: Float, t: Float, r: Float, b: Float) {
        p.color = 0xCC10142A.toInt(); rect.set(l, t, r, b); c.drawRoundRect(rect, 3f * u, 3f * u, p)
    }

    private fun button(c: Canvas, id: String, label: String, cx: Float, cy: Float, bw: Float, bh: Float, color: Int, enabled: Boolean = true) {
        val r = RectF(cx - bw / 2f, cy - bh / 2f, cx + bw / 2f, cy + bh / 2f)
        buttons.add(Btn(id, r, label, color, enabled))
        p.color = if (enabled) color else 0xFF555A6B.toInt()
        c.drawRoundRect(r, 2.5f * u, 2.5f * u, p)
        text(c, label, cx, cy + bh * 0.17f, bh * 0.42f, Color.WHITE, Paint.Align.CENTER)
    }

    private fun drawMenu(c: Canvas) {
        text(c, "GRAU MESTRE", w / 2f, h * 0.22f, 13f * u, 0xFFFFD23F.toInt(), Paint.Align.CENTER)
        text(c, "Equilibre a moto. Empine mais. Pontue.", w / 2f, h * 0.30f, 4f * u, Color.WHITE, Paint.Align.CENTER)
        button(c, "play", "JOGAR", w / 2f, h * 0.48f, 34f * u, 11f * u, 0xFFFF6B1A.toInt())
        button(c, "shop", "GARAGEM", w / 2f, h * 0.64f, 34f * u, 11f * u, 0xFF2D7BFF.toInt())
        text(c, "Recorde: ${store.bestScore}   •   Maior grau: ${String.format("%.1f", store.bestWheelie)} s   •   Moedas: ${store.coins}",
            w / 2f, h * 0.84f, 4f * u, Color.WHITE, Paint.Align.CENTER)
        text(c, "Sem anúncios. Sem internet.", w / 2f, h * 0.93f, 3f * u, 0xFFCCD2E6.toInt(), Paint.Align.CENTER)
    }

    private fun drawShop(c: Canvas) {
        panel(c, w * 0.06f, h * 0.06f, w * 0.94f, h * 0.94f)
        text(c, "GARAGEM", w / 2f, h * 0.17f, 8f * u, 0xFFFFD23F.toInt(), Paint.Align.CENTER)
        text(c, "Moedas: ${store.coins}", w * 0.92f, h * 0.17f, 5f * u, 0xFFFFC21A.toInt(), Paint.Align.RIGHT)

        upgradeRow(c, "Motor", "Mais velocidade e força para erguer", store.engine, h * 0.32f, "buy_engine")
        upgradeRow(c, "Equilíbrio", "A moto cai mais devagar", store.balance, h * 0.50f, "buy_balance")

        text(c, "Cores da moto", w * 0.10f, h * 0.67f, 4.5f * u, Color.WHITE, Paint.Align.LEFT)
        val n = SKINS.size
        val cell = w * 0.80f / n
        for (i in 0 until n) {
            val cx = w * 0.10f + cell * (i + 0.5f); val cy = h * 0.76f
            val owned = store.skinsOwned and (1 shl i) != 0
            val r = RectF(cx - cell * 0.44f, cy - 6f * u, cx + cell * 0.44f, cy + 6f * u)
            buttons.add(Btn("skin_$i", r, SKINS[i].name, SKINS[i].color))
            p.color = if (store.skin == i) Color.WHITE else 0xFF30364F.toInt()
            c.drawRoundRect(r, 2f * u, 2f * u, p)
            p.color = SKINS[i].color; c.drawCircle(cx, cy - 1.5f * u, 3f * u, p)
            val label = if (owned) (if (store.skin == i) "Em uso" else SKINS[i].name) else "${SKINS[i].cost} moedas"
            text(c, label, cx, cy + 4.3f * u, 2.6f * u, if (store.skin == i) 0xFF10142A.toInt() else Color.WHITE, Paint.Align.CENTER)
        }
        button(c, "back", "VOLTAR", w / 2f, h * 0.89f, 24f * u, 8f * u, 0xFF4A4F63.toInt())
    }

    private fun upgradeRow(c: Canvas, title: String, desc: String, level: Int, cy: Float, id: String) {
        text(c, "$title  (nível $level/${Upgrades.MAX_LEVEL})", w * 0.10f, cy - 1f * u, 5f * u, Color.WHITE, Paint.Align.LEFT)
        text(c, desc, w * 0.10f, cy + 4f * u, 3.2f * u, 0xFFCCD2E6.toInt(), Paint.Align.LEFT)
        // barra de nível
        for (i in 0 until Upgrades.MAX_LEVEL) {
            p.color = if (i < level) 0xFF2ECC71.toInt() else 0xFF30364F.toInt()
            c.drawRect(w * 0.10f + i * 3.4f * u, cy + 6f * u, w * 0.10f + i * 3.4f * u + 2.8f * u, cy + 7.4f * u, p)
        }
        if (level >= Upgrades.MAX_LEVEL) button(c, id, "MÁXIMO", w * 0.80f, cy + 1.5f * u, 28f * u, 9f * u, 0xFF4A4F63.toInt(), enabled = false)
        else {
            val cost = Upgrades.cost(level)
            button(c, id, "$cost moedas", w * 0.80f, cy + 1.5f * u, 28f * u, 9f * u, 0xFFFF6B1A.toInt(), enabled = store.coins >= cost)
        }
    }

    private fun drawPause(c: Canvas) {
        p.color = 0x99000000.toInt(); c.drawRect(0f, 0f, w, h, p)
        text(c, "PAUSADO", w / 2f, h * 0.30f, 10f * u, Color.WHITE, Paint.Align.CENTER)
        button(c, "resume", "CONTINUAR", w / 2f, h * 0.50f, 34f * u, 11f * u, 0xFFFF6B1A.toInt())
        button(c, "menu", "MENU", w / 2f, h * 0.68f, 34f * u, 11f * u, 0xFF4A4F63.toInt())
    }

    private fun drawOver(c: Canvas) {
        val g = game ?: return
        p.color = 0x99000000.toInt(); c.drawRect(0f, 0f, w, h, p)
        panel(c, w * 0.22f, h * 0.08f, w * 0.78f, h * 0.92f)
        val why = when (g.crashReason) {
            CrashReason.FLIPPED -> "A moto virou para trás!"
            CrashReason.CONE -> "Bateu no cone! Erga a roda antes."
            CrashReason.GATE -> "Bateu no pórtico! Abaixe a roda."
            null -> ""
        }
        text(c, "CAIU!", w / 2f, h * 0.20f, 10f * u, 0xFFE5233B.toInt(), Paint.Align.CENTER)
        text(c, why, w / 2f, h * 0.27f, 3.8f * u, Color.WHITE, Paint.Align.CENTER)
        text(c, "${g.score.toInt()} pontos", w / 2f, h * 0.38f, 8f * u, Color.WHITE, Paint.Align.CENTER)
        if (newRecord) text(c, "NOVO RECORDE!", w / 2f, h * 0.45f, 5f * u, 0xFF7CFC00.toInt(), Paint.Align.CENTER)
        else text(c, "Recorde: ${store.bestScore}", w / 2f, h * 0.45f, 4f * u, 0xFFCCD2E6.toInt(), Paint.Align.CENTER)
        text(c, "Distância ${g.distance.toInt()} m  •  Maior grau ${String.format("%.1f", g.bestWheelie)} s", w / 2f, h * 0.52f, 3.6f * u, Color.WHITE, Paint.Align.CENTER)
        text(c, "+${g.earnedCoins} moedas (${g.coins} coletadas)", w / 2f, h * 0.58f, 4f * u, 0xFFFFC21A.toInt(), Paint.Align.CENTER)
        button(c, "play", "JOGAR DE NOVO", w / 2f, h * 0.71f, 40f * u, 10f * u, 0xFFFF6B1A.toInt())
        button(c, "shop", "GARAGEM", w / 2f - 14f * u, h * 0.84f, 26f * u, 8f * u, 0xFF2D7BFF.toInt())
        button(c, "menu", "MENU", w / 2f + 14f * u, h * 0.84f, 26f * u, 8f * u, 0xFF4A4F63.toInt())
    }

    private fun text(c: Canvas, s: String, x: Float, y: Float, size: Float, color: Int, align: Paint.Align) {
        tp.textSize = size; tp.color = color; tp.textAlign = align
        c.drawText(s, x, y, tp)
    }
}
