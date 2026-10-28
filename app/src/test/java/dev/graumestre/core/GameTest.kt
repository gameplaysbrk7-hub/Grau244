package dev.graumestre.core

import org.junit.Assert.*
import org.junit.Test

class GameTest {
    private fun newGame(up: Upgrades = Upgrades(), seed: Long = 1L) = Game(Tuning(up), seed)

    /** Piloto simples: acelera quando abaixo do ponto de equilíbrio, solta quando acima. */
    private fun runBot(g: Game, seconds: Float) {
        var t = 0f
        while (t < seconds && g.phase == Phase.RUNNING) {
            val target = Physics.BALANCE_ANGLE - 0.04f
            val throttle = g.angle < target || (g.angle < target + 0.05f && g.omega < 0.2f)
            val brake = g.angle > Physics.BALANCE_ANGLE + 0.2f
            g.update(1f / 60f, throttle, brake)
            t += 1f / 60f
        }
    }

    @Test fun idleBikeStaysOnGroundAndScoresNothing() {
        val g = newGame()
        repeat(600) { g.update(1f / 60f, false, false) }
        assertEquals(Phase.RUNNING, g.phase)
        assertEquals(0f, g.angle, 0f)
        assertEquals(0f, g.score, 0f)
    }

    @Test fun holdingThrottleForeverFlipsBike() {
        val g = newGame()
        repeat(60 * 20) { g.update(1f / 60f, true, false) }
        assertEquals(Phase.CRASHED, g.phase)
        assertEquals(CrashReason.FLIPPED, g.crashReason)
    }

    @Test fun throttleLiftsFrontWheelAndStartsWheelie() {
        val g = newGame()
        repeat(60 * 3) { if (!g.wheelieActive) g.update(1f / 60f, true, false) }
        assertTrue(g.wheelieActive)
    }

    @Test fun balancingScoresPoints() {
        val g = newGame()
        runBot(g, 20f)
        assertTrue("bot should reach a wheelie and score", g.score > 50f)
        assertTrue(g.multiplier > 1f || g.phase == Phase.CRASHED)
    }

    @Test fun conesCrashWhenFrontWheelIsDown() {
        val g = newGame()
        // roda no chão, só ligeiro gás para andar
        var n = 0
        while (g.phase == Phase.RUNNING && n++ < 60 * 60) g.update(1f / 60f, n % 6 == 0, false)
        assertEquals(Phase.CRASHED, g.phase)
        assertTrue(g.crashReason == CrashReason.CONE || g.crashReason == CrashReason.GATE)
    }

    @Test fun sameSeedIsDeterministic() {
        val a = newGame(seed = 7); val b = newGame(seed = 7)
        assertEquals(a.obstacles.map { it.x to it.type }, b.obstacles.map { it.x to it.type })
    }

    @Test fun upgradesMakeBikeEasierAndFaster() {
        val base = Tuning(Upgrades()); val max = Tuning(Upgrades(5, 5))
        assertTrue(max.maxSpeed > base.maxSpeed)
        assertTrue(max.gravity < base.gravity)
        assertTrue(max.damping > base.damping)
    }

    @Test fun upgradeCostsIncrease() {
        assertTrue(Upgrades.cost(1) > Upgrades.cost(0))
        assertTrue(Upgrades.cost(4) > Upgrades.cost(3))
    }

    @Test fun earnedCoinsIncludeScoreBonus() {
        val g = newGame()
        runBot(g, 15f)
        assertEquals(g.coins + (g.score / 60f).toInt(), g.earnedCoins)
    }
}
