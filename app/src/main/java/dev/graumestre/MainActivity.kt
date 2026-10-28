package dev.graumestre

import android.app.Activity
import android.os.Bundle
import android.view.View
import android.view.WindowManager
import dev.graumestre.ui.GameView

class MainActivity : Activity() {
    private lateinit var view: GameView

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        window.addFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON)
        view = GameView(this, Store(this))
        setContentView(view)
    }

    override fun onWindowFocusChanged(hasFocus: Boolean) {
        super.onWindowFocusChanged(hasFocus)
        if (hasFocus) {
            @Suppress("DEPRECATION")
            window.decorView.systemUiVisibility = View.SYSTEM_UI_FLAG_FULLSCREEN or
                View.SYSTEM_UI_FLAG_HIDE_NAVIGATION or View.SYSTEM_UI_FLAG_IMMERSIVE_STICKY or
                View.SYSTEM_UI_FLAG_LAYOUT_STABLE or View.SYSTEM_UI_FLAG_LAYOUT_FULLSCREEN or
                View.SYSTEM_UI_FLAG_LAYOUT_HIDE_NAVIGATION
        }
    }

    override fun onPause() { super.onPause(); view.onHostPause() }
    override fun onResume() { super.onResume(); view.onHostResume() }

    @Deprecated("Deprecated in Java")
    override fun onBackPressed() {
        if (!view.handleBack()) @Suppress("DEPRECATION") super.onBackPressed()
    }
}
