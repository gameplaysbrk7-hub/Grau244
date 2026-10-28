# Projeto Moto 3D (Android) — Fase 1

Jogo 3D original de motos para Android, inspirado no estilo de pilotagem em terceira pessoa com
grau e manobras. Esta é a **Fase 1: protótipo jogável do núcleo** (moto, física, grau, câmera,
controles touch) em um estacionamento de testes, com build Android automático pelo GitHub Actions.

> **Importante — nada aqui foi compilado nem executado ainda.** O código foi escrito sem acesso à
> Unity. O primeiro build no GitHub Actions pode falhar por erro de compilação ou configuração;
> envie o log para corrigirmos. A física (grau, aderência, câmera) também **precisa de ajuste fino
> em um aparelho real**.

## Versões
- Unity **2022.3.45f1** (LTS) — registrada em `ProjectSettings/ProjectVersion.txt` e no workflow
- Pipeline de render: **Built-in** (URP fica para uma fase futura)
- Pacotes: `com.unity.ugui 1.0.0` + módulos embutidos (`Packages/manifest.json`)
- Entrada: Input Manager clássico + uGUI. Android: minSdk 24, IL2CPP, ARM64, OpenGL ES 3

## O que está implementado (Fase 1)
- Moto com suspensão por raycast, aderência lateral, tração traseira, freios dianteiro/traseiro independentes
- Grau por física (torque no Rigidbody, a roda dianteira realmente sai do chão) com assistência de equilíbrio e queda
- Stoppie e borrachão (freio traseiro + acelerador), derrapagem (botão), queda e recuperação
- Câmera em 3ª pessoa (suave, arrastar para girar, pinça para zoom, anti-colisão, recentralizar) e 1ª pessoa
- Piloto provisório com tronco que inclina; mãos no guidão e pés nos pedais
- Controles touch reais + menu de pausa com opacidade, tamanho, sensibilidade e vibração (salvos)
- Mundo de teste: asfalto, prédios, rampas, lombadas, cones e caixotes

## O que ainda NÃO existe (próximas fases)
Cidade conectada, trânsito e pedestres, minimapa/GPS, dia/noite/chuva, áudio, garagem e personalização,
missões e economia, salvamento de progresso do jogo (só as configurações são salvas), sair/subir na moto
e caminhar, outras motos, LOD/níveis de qualidade, URP, reposicionar botões.

## Controles
| Botão | Ação |
|---|---|
| `<` `>` | Direção |
| ACELERAR | Acelerador (segure) |
| FREIO DIANT. / TRAS. | Freios independentes (segure) |
| GRAU | Segure junto com ACELERAR acima de ~9 km/h. Acelerar sobe o ângulo; FREIO TRAS. desce; soltar GRAU pousa |
| DERRAPAR | Solta a traseira (use em curva). FREIO TRAS. + ACELERAR = borrachão |
| CÂMERA / CENTRAR | Alterna 1ª/3ª pessoa / recentraliza |
| LEVANTAR | Recupera a moto (também volta sozinha 3 s após cair) |
| II | Pausa e configurações |

Arrastar o dedo fora dos botões gira a câmera; pinça faz zoom. No Editor (PC): W/S/Espaço = acelerar/freio diant./freio tras.,
A/D = direção, Shift = grau, Ctrl = derrapar, botão direito do mouse = câmera.

## Ajustando a física
Os parâmetros ficam públicos em `MotorcycleController` (massa, mola, aderência, torque, freios) e
`WheelieController` (`kp`, `kd`, `popTorque`, ângulos). Se o grau for difícil demais ou fácil demais,
comece por `popTorque`, `maxTargetAngle` e `kp`.

## Licença da Unity (o passo mais difícil)
O GitHub Actions precisa de uma licença Unity. São 3 segredos no repositório:
`UNITY_LICENSE` (conteúdo do arquivo `.ulf`), `UNITY_EMAIL` e `UNITY_PASSWORD`.

Segundo a documentação do GameCI, para a licença **Personal (gratuita)** o `.ulf` é obtido ativando a licença no Unity Hub de um
computador (no Windows costuma ficar em `C:\ProgramData\Unity\Unity_lic.ulf`). O fluxo antigo de ativação manual
pelo site da Unity deixou de ser oficialmente suportado para Personal, então **é provável que você precise de um
PC (ou de alguém com um) uma única vez** para gerar esse arquivo. O workflow `Pedir arquivo de ativação Unity`
(aba Actions) existe para o método antigo e pode não funcionar. Confira sempre: https://game.ci/docs/github/activation

Nunca coloque senhas ou o `.ulf` dentro dos arquivos do projeto: use somente os segredos do repositório.

## Passo a passo pelo celular
1. **Baixar e extrair** o ZIP (app Arquivos do Android → tocar no ZIP → Extrair).
2. **Criar o repositório**: em github.com (navegador, "Versão para computador") → `+` → New repository → privado ou público → Create.
3. **Enviar os arquivos**: no repositório, Add file → Upload files e selecione os arquivos e pastas extraídos.
   - Limitação: seletores de arquivo do Android costumam **esconder a pasta `.github`** e não enviam pastas vazias.
     Se `.github/workflows/android-build.yml` não aparecer no repositório, crie-o à mão: Add file → Create new file,
     digite `.github/workflows/android-build.yml` no nome (as barras criam as pastas), cole o conteúdo do arquivo e faça Commit.
     Faça o mesmo com `.gitignore` se faltar.
   - Alternativa com Git (Termux no Android):
     ```
     cd ProjetoMoto3D
     git init
     git add .
     git commit -m "Fase 1"
     git branch -M main
     git remote add origin https://github.com/SEU_USUARIO/SEU_REPO.git
     git push -u origin main
     ```
     (a senha do push é um *Personal Access Token* criado em GitHub → Settings → Developer settings.)
4. **Segredos**: repositório → Settings → Secrets and variables → Actions → New repository secret. Crie `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD`.
5. **Executar**: aba Actions → **Android Build** → Run workflow → Run workflow.
6. **Acompanhar**: toque na execução em andamento; a primeira costuma levar de 20 a 60 minutos.
7. **Baixar**: ao terminar com ✅, role até **Artifacts** e baixe `ProjetoMoto3D-Android-APK` (um ZIP com o `.apk`). Extraia.
8. **Instalar**: toque no `.apk` e permita "instalar apps desconhecidos" para o app que o abriu.
   O APK sai assinado com chave de debug (serve para testes, não para a Play Store).

## Erros comuns
- **"License activation strategy could not be determined" / erro de licença**: segredos ausentes ou `.ulf` inválido/expirado (veja a seção da licença).
- **Erro de compilação C# (`error CS...`)**: abra o log da etapa "Compilar com Unity" e me envie as linhas com `error CS`.
- **Workflow não aparece na aba Actions**: o arquivo não está em `.github/workflows/` na branch principal.
- **Falta de espaço no runner**: o workflow já libera espaço; tente rodar de novo.
- **"App não instalado"**: desinstale versão anterior assinada com outra chave, ou libere a instalação de fontes desconhecidas.
- **Tela preta/crash ao abrir**: me informe modelo do aparelho e Android; obtenha o log com `adb logcat -s Unity`.

## Estrutura
```
Assets/Scripts/Core      GameBootstrap, SaveSystem, GameSettings, Visuals
Assets/Scripts/Vehicle   MotorcycleController, WheelieController, MotorcycleInput, RiderAnimationController, BikeBuilder, BikeRig
Assets/Scripts/Camera    CameraManager, ThirdPersonCameraController, FirstPersonCameraController
Assets/Scripts/UI        TouchInputController, UIManager, UIFactory, HoldButton
Assets/Scripts/World     TestWorldBuilder
Assets/Editor            BuildScript (cria Assets/Scenes/Main.unity e compila o APK)
.github/workflows        android-build.yml, unity-activation.yml
```
A cena `Assets/Scenes/Main.unity` é gerada automaticamente pelo `BuildScript` (e pelo menu **Projeto Moto → Gerar cena de teste**
no Editor); ela contém só o `GameBootstrap`, que monta o resto em tempo de execução.

## Testes
| Item | Status |
|---|---|
| Verificação de chaves/parênteses balanceados nos .cs | feito |
| Compilação C# na Unity | **não realizado** |
| Build Android no GitHub Actions | **não realizado** |
| Física da moto, grau, câmera, toque, quedas | **não realizado** |
| Desempenho (30/60 FPS) em aparelhos | **não realizado** |
