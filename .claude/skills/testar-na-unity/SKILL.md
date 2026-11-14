---
name: testar-na-unity
description: Testar o TI Service Simulator dentro da Unity (compilar, ler o Console, dar Play, usar os atalhos de teste, gerar o .exe) sem estragar o save do dono. Use sempre que for conferir uma mudança no jogo, rodar o roteiro de testes, ver algo na tela ou gerar o executável.
---

# Testar o TI Service Simulator na Unity

Projeto: `C:\Projetos\TI Service Simulator` (Unity 6000.4.10f1, Built-in RP). Windows, PowerShell.

## 1. Abrir a Unity (licença)
- O Unity Hub é a versão da **Microsoft Store** (fica em `C:\Program Files\WindowsApps\UnityTechnologies.UnityHub_*`).
- A licença só vale abrindo **pelo Hub**. Abrir `Unity.exe` direto (ou `open_application "Unity"` com o Editor fechado)
  dá "No valid Unity Editor license found" ou abre uma janela "Install Unity Hub". Não faça isso.
- Para abrir o Hub: `Start-Process "shell:AppsFolder\UnityTechnologies.UnityHub_2vrhnee42bhxm!UnityHub"` e peça ao dono
  para clicar no projeto (o Hub não está na lista de apps controláveis).
- Se a janela do Editor estiver minimizada, restaure com `ShowWindow(handle, 3)` + `SetForegroundWindow` (user32) usando
  o processo `Unity` cujo `MainWindowTitle` contém "TI Service".

## 2. Compilar e ler erros
- Na Unity: clique no Console e `Ctrl+R` (refresh). Espere ~20–25 s.
- Log completo: `$env:LOCALAPPDATA\Unity\Editor\Editor.log` — procure `error CS`, `Exception`, `[Build]`.
- Checagem rápida sem a Unity (mesmo compilador e referências):
  `Editor\Data\NetCoreRuntime\dotnet.exe Editor\Data\DotNetSdkRoslyn\csc.dll @arquivo.rsp`, montando o .rsp a partir
  de `Assembly-CSharp.csproj` (DefineConstants + HintPaths) e de todos os `.cs` de `Assets` (Editor à parte).
- **Nunca recompile com o Play rodando**: gera centenas de NullReference do pacote IngameDebugConsole (falso alarme)
  e pode salvar estado lixo. Sempre confira que o Play parou (zoom nos botões Play/Pause no topo) antes de `Ctrl+R`.
- Use `Ctrl+P` para Play/Stop (clicar no botão às vezes não pega).

## 3. Save do jogo
- Pasta: `%USERPROFILE%\AppData\LocalLow\DefaultCompany\TI Service Simulator`.
- O editor usa `save_editor.json`; o `.exe` usa `save.json` (o do dono). Mesmo assim, antes de testes que mexem em
  dinheiro/mochila, faça backup do save que for usar na pasta scratchpad e restaure no fim.
- Jogo novo: apague `save_editor.json`.

## 4. Atalhos de teste (menu TI Service → Teste (Play))
Ir para: porta do escritório, rua, porta/dentro da casa, cama, bancada, impressora/roteador/computador do cliente,
olhar o cliente. Também: mirar a próxima peça do equipamento (com diagnóstico do raio no log), pular 1 ou 10 dias
(para na falência), gerar pedido de projeto / edital / contato de locação, encher a mochila.
- **O menu da Unity só abre com o cursor solto**: aperte `Esc` (pausa do jogo) antes de clicar no menu, e `Esc` de novo
  depois para voltar ao jogo.
- Antes de despausar, mova o mouse para o centro da aba Game: senão o recapture do cursor gira a câmera e a mira sai
  do alvo (efeito do mouse simulado, não é bug do jogo).
- Para dinheiro/reputação: Inspector do `GameManager` (GameEconomy → Saldo; botões "Teste: +R$ 1.000",
  "Teste: +20 reputação"). Busque na Hierarchy com `t:GameEconomy` / `t:ReputationManager`.
- Maximize a aba Game (duplo clique na aba) para ver a interface no tamanho real; restaure antes de usar a Hierarchy.

## 5. Gerar o executável
- Menu **TI Service → 4. Gerar executável (Windows)** → `Builds/Windows/TI Service Simulator.exe` (pasta ignorada no Git).
- A Unity pergunta "Scene(s) Have Been Modified": responda **Don't Save** (o build usa a cena salva; a marcação de
  modificada é falsa).
- O build às vezes zera `preloadedAssets` em `ProjectSettings/ProjectSettings.asset`: restaure com
  `git checkout -- ProjectSettings/ProjectSettings.asset` antes de commitar.
- Feche a janela do Explorer que o build abre (`Shell.Application` → `Quit()` na janela com "Builds").
- Teste rápido: abrir o .exe em janela (`-screen-fullscreen 0 -screen-width 1280 -screen-height 720`), esperar 15 s,
  ler `Player.log` na pasta do save procurando `Exception|Error`, fechar.

## 6. Menus do projeto
- `TI Service/3. Montar cenário` — remonta e salva a cena (escritório, cliente, rua, casa). Rode depois de mudar
  `MontarCenario*.cs`.
- `TI Service/5. Configurar personagens (Mixamo)` — humanoide, loops, texturas e `BancoPersonagens`.

## 7. Roteiro
O checklist está em `ROTEIRO_TESTES_UNITY.md` (raiz). Marque o que foi testado e anote o que falta.
