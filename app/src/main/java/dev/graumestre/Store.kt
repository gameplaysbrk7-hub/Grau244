package dev.graumestre

import android.content.Context
import dev.graumestre.core.Upgrades

/** Persistência local (SharedPreferences). Nenhum dado sai do aparelho. */
class Store(context: Context) {
    private val sp = context.getSharedPreferences("grau_mestre", Context.MODE_PRIVATE)

    var bestScore: Int
        get() = sp.getInt("best", 0)
        set(v) = sp.edit().putInt("best", v).apply()
    var bestWheelie: Float
        get() = sp.getFloat("best_wheelie", 0f)
        set(v) = sp.edit().putFloat("best_wheelie", v).apply()
    var coins: Int
        get() = sp.getInt("coins", 0)
        set(v) = sp.edit().putInt("coins", v).apply()
    var engine: Int
        get() = sp.getInt("engine", 0)
        set(v) = sp.edit().putInt("engine", v).apply()
    var balance: Int
        get() = sp.getInt("balance", 0)
        set(v) = sp.edit().putInt("balance", v).apply()
    /** Máscara de bits das cores compradas. Bit 0 (laranja) sempre liberado. */
    var skinsOwned: Int
        get() = sp.getInt("skins", 1)
        set(v) = sp.edit().putInt("skins", v or 1).apply()
    var skin: Int
        get() = sp.getInt("skin", 0)
        set(v) = sp.edit().putInt("skin", v).apply()

    val upgrades: Upgrades get() = Upgrades(engine, balance)
}
