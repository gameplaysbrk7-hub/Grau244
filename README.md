# Grau Mestre

Jogo de moto de grau (wheelie) 2D para Android, **original**: código, arte e física próprios.
Sem anúncios, sem internet (o app nem pede a permissão INTERNET), dados salvos só no aparelho.

## Como jogar
- **Lado direito da tela**: acelera e ergue a roda da frente.
- **Lado esquerdo**: freia e abaixa a roda.
- Segure o ponteiro na **faixa verde** do medidor: multiplicador maior (até x5).
- **Cones**: só passe com a roda erguida. **Pórticos**: só passe com a roda baixa o bastante.
- Moedas só são coletadas durante o grau. Gaste na **Garagem**: motor, equilíbrio e cores.
- Cair para trás, bater no cone ou no pórtico encerra a corrida.

## Estrutura
```
app/src/main/java/dev/graumestre/
  core/Game.kt      física, pontuação, geração de pista (Kotlin puro, testável na JVM)
  ui/GameView.kt    renderização em Canvas, telas, toques, loja
  Store.kt          persistência (SharedPreferences)
  MainActivity.kt
app/src/test/.../GameTest.kt   testes da lógica
.github/workflows/build.yml    compila o APK no GitHub
```
Dependências: nenhuma além do Kotlin. Gradle 8.9 (wrapper), Android Gradle Plugin 8.7.3, Kotlin 2.0.21, compileSdk 35, minSdk 24.

## Compilar sem PC: GitHub Actions (recomendado no celular)
1. Crie uma conta e um repositório vazio no GitHub (ex.: `grau-mestre`).
2. Envie este projeto (veja "Termux" abaixo, ou use o botão *Add file → Upload files* no site).
3. Abra a aba **Actions** → *Build APK* → o build roda sozinho a cada push (ou clique *Run workflow*).
4. Ao terminar, abra a execução e baixe o artefato **GrauMestre-debug-apk** (um .zip com `app-debug.apk`).
5. Extraia, abra o APK no celular e permita "instalar apps desconhecidos" quando pedir.

## Termux (enviar o código ao GitHub)
```bash
pkg update && pkg install git openssh unzip
termux-setup-storage
unzip ~/storage/downloads/GrauMestre.zip -d ~ && cd ~/GrauMestre
git init -b main
git add . && git commit -m "Primeira versão"
git remote add origin https://github.com/SEU_USUARIO/grau-mestre.git
git push -u origin main      # peça um token em GitHub → Settings → Developer settings → Personal access tokens
```
Atualizações depois de editar qualquer arquivo:
```bash
git add . && git commit -m "Descreva a mudança" && git push
```

## Compilar localmente (PC com Android Studio ou JDK 17 + Android SDK)
```bash
./gradlew testDebugUnitTest      # testes
./gradlew assembleDebug          # gera app/build/outputs/apk/debug/app-debug.apk
adb install -r app/build/outputs/apk/debug/app-debug.apk
```
Compilar o Android direto no Termux não é prático (o SDK oficial não roda no ARM do celular); use o GitHub Actions.

## Lendo erros de compilação
No log do Actions, procure a primeira linha com `e: ` (erro Kotlin) — ela traz arquivo e linha. `Unresolved reference` = import/nome errado; `Type mismatch` = tipo errado. Cole a linha do erro aqui para eu corrigir.

## Ajustar o jogo
Todos os números de dificuldade estão em `core/Game.kt`: `Physics` (ângulos), `Tuning` (efeito das melhorias), `Upgrades.cost` (preços) e `generate()` (frequência de obstáculos).

## Status de validação
- Testado: lógica do jogo (9 testes JUnit na JVM, passando).
- Verificado só contra stubs: sintaxe/tipos da interface (sem `android.jar` disponível).
- **Não testado**: compilação real com Gradle/AGP, instalação e jogo em aparelho. Falta o primeiro build no GitHub Actions e uma partida de teste.
- Não incluído: áudio.
