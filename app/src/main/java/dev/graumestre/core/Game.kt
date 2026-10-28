package dev.graumestre.core

import kotlin.math.abs
import kotlin.math.max
import kotlin.math.min
import kotlin.math.sin

/** Constantes de física. Ângulo em radianos: 0 = moto no chão, positivo = roda da frente erguida. */
object Physics {
    const val BALANCE_ANGLE = 0.62f      // ponto de equilíbrio (instável)
    const val SWEET_ZONE = 0.14f         // faixa "perfeita" em torno do equilíbrio
    const val FLIP_ANGLE = 1.30f         // passou disso: a moto cai para trás
    const val WHEELIE_ON = 0.22f         // acima disso conta como grau
    const val WHEELIE_OFF = 0.10f        // abaixo disso o grau acaba
    const val CONE_MIN_ANGLE = 0.38f     // para passar por cone, a roda da frente precisa estar erguida
    const val GATE_MAX_ANGLE = 0.95f     // para passar pelo pórtico, não pode estar muito inclinada
    const val MIN_SPEED = 4f
    const val WORLD_AHEAD = 140f         // metros gerados à frente
}

/** Níveis de melhoria comprados na loja. */
data class Upgrades(val engine: Int = 0, val balance: Int = 0) {
    companion object {
        const val MAX_LEVEL = 5
        fun cost(currentLevel: Int): Int = 120 * (currentLevel + 1) * (currentLevel + 1)
    }
}

class Tuning(up: Upgrades) {
    val maxSpeed = 20f + 2.5f * up.engine
    val accel = 7f + 0.8f * up.engine
    val lift = 5.6f + 0.25f * up.engine
    val gravity = 3.4f * (1f - 0.08f * up.balance)
    val damping = 1.1f + 0.15f * up.balance
}

enum class Phase { RUNNING, CRASHED }
enum class CrashReason { FLIPPED, CONE, GATE }
enum class ObstacleType { CONE, GATE }

class Obstacle(val type: ObstacleType, val x: Float)
class Coin(val x: Float) { var taken = false }

class Game(private val tuning: Tuning, seed: Long = System.nanoTime()) {
    private val rnd = java.util.Random(seed)

    var phase = Phase.RUNNING; private set
    var crashReason: CrashReason? = null; private set

    var angle = 0f; private set
    var omega = 0f; private set
    var speed = Physics.MIN_SPEED; private set
    var distance = 0f; private set

    var score = 0f; private set
    var coins = 0; private set
    var multiplier = 1f; private set
    var wheelieActive = false; private set
    var wheelieTime = 0f; private set
    var bestWheelie = 0f; private set
    var wheelieDistance = 0f; private set
    var perfectTime = 0f; private set
    var obstaclesPassed = 0; private set

    val obstacles = ArrayList<Obstacle>()
    val coinList = ArrayList<Coin>()
    private var spawnedUntil = 40f

    init { generate() }

    val inSweetSpot: Boolean
        get() = abs(angle - Physics.BALANCE_ANGLE) < Physics.SWEET_ZONE

    /** Moedas ganhas ao terminar a corrida (coletadas + bônus pela pontuação). */
    val earnedCoins: Int get() = coins + (score / 60f).toInt()

    fun update(dt: Float, throttle: Boolean, brake: Boolean) {
        if (phase != Phase.RUNNING) return
        updateSpeed(dt, throttle, brake)
        updateAngle(dt, throttle, brake)
        if (phase != Phase.RUNNING) return

        val prev = distance
        distance += speed * dt
        updateWheelie(dt)
        checkWorld(prev, distance)
        generate()
    }

    private fun updateSpeed(dt: Float, throttle: Boolean, brake: Boolean) {
        if (throttle) speed += tuning.accel * dt * (1f - speed / (tuning.maxSpeed * 1.05f))
        else speed -= 1.5f * dt
        if (brake) speed -= 6f * dt
        speed = speed.coerceIn(Physics.MIN_SPEED, tuning.maxSpeed)
    }

    private fun updateAngle(dt: Float, throttle: Boolean, brake: Boolean) {
        val speedFactor = 0.5f + 0.5f * (speed / tuning.maxSpeed)
        var alpha = tuning.gravity * sin(angle - Physics.BALANCE_ANGLE)
        if (throttle) alpha += tuning.lift * speedFactor
        if (brake) alpha -= 4.2f
        alpha -= tuning.damping * omega

        omega += alpha * dt
        angle += omega * dt

        if (angle <= 0f) { // encostou a roda da frente no chão
            angle = 0f
            if (omega < 0f) omega = 0f
        }
        if (angle >= Physics.FLIP_ANGLE) crash(CrashReason.FLIPPED)
    }

    private fun updateWheelie(dt: Float) {
        if (!wheelieActive && angle > Physics.WHEELIE_ON) {
            wheelieActive = true
            wheelieTime = 0f
            wheelieDistance = 0f
        } else if (wheelieActive && angle < Physics.WHEELIE_OFF) {
            wheelieActive = false
            multiplier = 1f
        }
        if (wheelieActive) {
            wheelieTime += dt
            wheelieDistance += speed * dt
            bestWheelie = max(bestWheelie, wheelieTime)
            val sweet = if (inSweetSpot) 0.5f else 0f
            if (inSweetSpot) perfectTime += dt
            multiplier = 1f + min(wheelieTime / 4f, 3.5f) + sweet
            score += speed * dt * multiplier
        }
    }

    private fun checkWorld(from: Float, to: Float) {
        for (o in obstacles) {
            if (o.x > from && o.x <= to) {
                val ok = when (o.type) {
                    ObstacleType.CONE -> angle >= Physics.CONE_MIN_ANGLE
                    ObstacleType.GATE -> angle <= Physics.GATE_MAX_ANGLE
                }
                if (ok) { obstaclesPassed++; score += 25f * multiplier }
                else { crash(if (o.type == ObstacleType.CONE) CrashReason.CONE else CrashReason.GATE); return }
            }
        }
        if (wheelieActive) for (c in coinList) {
            if (!c.taken && c.x > from && c.x <= to) { c.taken = true; coins++ }
        }
        obstacles.removeAll { it.x < to - 30f }
        coinList.removeAll { it.x < to - 30f }
    }

    private fun crash(reason: CrashReason) {
        phase = Phase.CRASHED
        crashReason = reason
        wheelieActive = false
        multiplier = 1f
    }

    private fun generate() {
        while (spawnedUntil < distance + Physics.WORLD_AHEAD) {
            val difficulty = min(distance / 1500f, 1f)
            val gap = 55f - 20f * difficulty + rnd.nextFloat() * 25f
            spawnedUntil += gap
            val type = if (rnd.nextFloat() < 0.55f) ObstacleType.CONE else ObstacleType.GATE
            obstacles.add(Obstacle(type, spawnedUntil))
            var cx = spawnedUntil - gap + 10f
            repeat(3 + rnd.nextInt(3)) { coinList.add(Coin(cx)); cx += 4f }
        }
    }
}
