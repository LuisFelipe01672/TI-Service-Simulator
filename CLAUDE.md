# TI Service Simulator — guia do projeto

Simulador de técnico de TI brasileiro. Unity **6000.4.10f1**, **Built-in Render Pipeline**, C#, Windows.
Plano de longo prazo: `ROADMAP.md`. Plano detalhado da impressora: `PLANO_FASE_IMPRESSORA.md`.
Roteiro de testes manuais: `ROTEIRO_TESTES_UNITY.md`. Créditos de assets: `CREDITOS.md`.

## Regras do dono
- Tudo em **português** (código, textos do jogo, commits).
- **Realismo**: toda ação de trabalho tem animação; todo problema deixa pista visual.
- **Sem marcas reais** e **sem valores exatos** da empresa do dono no repositório (é público). Notas privadas
  ficam em `TI-Service-Contexto/` (fora do Git, só para calibrar).
- **Pronto antes do zero**: antes de criar algo (modelo, cenário, cidade, som, textura, animação, sistema), procurar
  algo pronto para adaptar (Sketchfab, Poly Haven, ambientCG, OpenStreetMap, pacotes e skills). Filtros: licença
  (no Git só CC0/CC-BY/MIT; Asset Store e Mixamo ficam fora do Git), peso (placa de 4 GB) e cara de Brasil/estilo coerente.
- **Push só quando o dono pedir.** Commit local ao fim de cada etapa testada.
- Skills do projeto em `.claude/skills/`: `testar-na-unity`, `baixar-modelo-3d`, `publicar-ti-service`.

## Como continuar (leia primeiro numa conversa nova)
1. **Onde estamos**: `ROADMAP.md` → seções "Ordem de execução" (etapas 0–8, feitas) e "Ordem de execução — leva 2"
   (9 cidade, 10 Wi-Fi, 11 CFTV, 12 nobreak, 13 pente-fino de bugs). Pegue a primeira etapa ⬜ dessa lista.
   Resumo de passagem com o estado do dia: `TI-Service-Contexto/conversas/LEIA-PRIMEIRO.md` (privado, fora do Git).
   Confira sempre com `git log --oneline -5` e `git status -sb` (o que está "ahead" ainda não foi enviado).
2. **Uma etapa por conversa**: a conversa principal planeja e propõe um cartão de sessão nova por etapa, com um
   briefing completo (o que existe, o que fazer, regras, quando está pronto). A sessão da etapa implementa, testa e
   faz commit local; a principal confere (git log, ROADMAP, data do exe, fotos em `Builds/Capturas/`) e conta ao dono.
3. **Ciclo de cada etapa**: ler o código antes → implementar → `Assets/Refresh` (compilar) → testes automáticos
   → testar no Play pelos atalhos (*TI Service/Teste (Play)*, criar atalhos novos para o que fez) → fotos →
   marcar no `ROADMAP.md` → commit local → gerar o exe (*TI Service/4*) → contar ao dono o que **não** foi testado.
4. **Push** só quando o dono pedir (skill `publicar-ti-service`): antes, conferir que nada de `TI-Service-Contexto/`,
   `Builds/`, saves, Mixamo, `.mcp.json` ou `Plugins/NuGet` vai junto e procurar marcas reais no diff
   (`git diff origin/main..HEAD`). Push pelo **PowerShell** (`git push origin main`): o Git do Bash não tem o
   gerenciador de credenciais; se pedir login, rodar num terminal do app para o dono entrar no GitHub pelo navegador.
5. **Zip para testadores**: compactar `Builds/Windows` numa pasta `TI-Service-Simulator` dentro de
   `Builds/TI-Service-Simulator-teste.zip` (PowerShell `Compress-Archive`); o colega extrai e abre o .exe
   (SmartScreen: "Mais informações → Executar assim mesmo"). Os arquivos do motor ficam com data antiga: normal.
6. **Downloads** (modelos, texturas): sempre pedir aprovação ao dono antes, com nome, autor, licença e tamanho.
   Sketchfab usa a sessão do dono no navegador do app (skill `baixar-modelo-3d`); se der 401, pedir para ele logar.

## Erros que já aconteceram (não repetir)
- **Play ligado**: nunca remontar o cenário, calcular a luz nem recompilar com o Play rodando — o bake não roda e a
  montagem se perde. Depois de testar no Play, mande `stop` e confirme (a Unity pode ter ficado em Play/pausa).
- **Ordem entre arquivos partial**: posição que depende de valor de outro arquivo do `MontarCenario`/`Bairro` deve
  ser propriedade (`=>`), nunca `static readonly` (o banheiro nasceu na frente do escritório por isso). Ao criar
  cômodo ou rua, atualizar `Editor/Testes/TestesPlantaCenario.cs`.
- **Unity-MCP no exe**: o gerador de exe tira o plugin do build; não mexer nisso.
- **Escrever arquivos**: `sed` com \n e heredoc com código quebram fácil no Bash do Windows — para edições grandes,
  escrever um script Node na scratchpad e rodar com `node`, ou usar a ferramenta de edição.
- Unity fechou sozinha uma vez no meio do trabalho: reabrir pelo Hub e continuar.

## Ferramentas instaladas para este projeto
- Skills do usuário (`~/.claude/skills`): `find-skills` (procurar skills novas), `unity-build-size`,
  `camera-systems`, `game-feel`, `performance-optimization`, `save-systems`, `unity-csharp-scripting`.
  Python 3.13 instalado (scripts dessas skills).
- Agents úteis para revisões: Game Designer, Technical Artist, Unity Architect (rodar em paralelo e juntar num plano).
- Unity-MCP: ver "Testar e gerar build" abaixo.

## Arquitetura
- **Managers** se criam sozinhos com `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]` e conversam por
  **EventBus** (structs de evento). Evite referências diretas entre managers quando um evento resolve.
- **UI feita por código** (`UI/UIFactory.cs` + TextMeshPro). `GameUI` é `partial` (um arquivo por aba/janela:
  `GameUI.Contratos.cs`, `GameUI.Manutencao.cs`...), com enum `Modal` e a lista `janelas`.
  Evite glifos fora da fonte (▶ ● ⚠): use », •, (!).
- **Cenário montado por código no editor**: `Editor/MontarCenario*.cs`, menu *TI Service/3. Montar cenário*.
  Mudou o script → rode o menu de novo (ele salva a cena). Não edite a cena à mão.
  Ao fim ele calcula a luz dos interiores (lightmap + sondas, `MontarCenario.Luz.cs`; espere `[Luz] Luz calculada` no log).
  Sol e lâmpadas mudam em tempo real (dia/noite, interruptores): só o rebote das lâmpadas é pré-calculado.
- **Peças de equipamento**: `Manutencao/PecaEquipamento.cs` (mirar + E / segurar E, raycast com
  `QueryTriggerInteraction.Collide`), ações em `Manutencao/AcoesFisicas.cs` filtradas por `peca`, montagem em
  `Manutencao/EquipamentoPecas.cs`.
- **Animações** com **PrimeTween** (`Tween.LocalPosition`, `Sequence.Create()...`).
- **Personagens**: Mixamo em `Assets/Models/Mixamo/` (**fora do Git** — termos da Adobe), configurados pelo menu
  *TI Service/5*; carregados via `Resources/Personagens/BancoPersonagens.asset`. O jogo precisa funcionar sem eles
  (fallback `MontarSimples()`).
- **Modelos 3D** em `Assets/Models/Sketchfab/<Nome>/<Nome>.gltf` (CC-BY, crédito obrigatório) — glTF com imagens
  separadas via glTFast (GLB embutido vai sem compressão: converta com `gltf-transform copy`); `Editor/TexturasModelos.cs`
  configura as imagens. Texturas
  acima de 2048 podem falhar no import (DX12): reduza para 2048.
- **Sons**: gerados por código (`Audio/SonsGerados.cs`); um arquivo em `Resources/Sons/<nome>` substitui o gerado.
- **Configurações** em PlayerPrefs (`Configuracoes.cs`, janela `UI/JanelaConfiguracoes.cs`).
- **Save**: `save_editor.json` no editor e `save.json` no executável
  (`%USERPROFILE%\AppData\LocalLow\DefaultCompany\TI Service Simulator`).

## Testar e gerar build
- Abra a Unity **pelo Unity Hub** (licença). Não recompile com o Play rodando.
- Atalhos de teste: menu *TI Service/Teste (Play)*. Executável: *TI Service/4. Gerar executável (Windows)* →
  `Builds/Windows/` (fora do Git). Depois do build: `git checkout -- ProjectSettings/ProjectSettings.asset`.
- **Rodar menus sem clicar**: escreva o caminho do menu (ex.: `Assets/Refresh`,
  `TI Service/Teste gráfico/1. Montar cômodo de prova`) em `Temp/comando_unity.txt`; o editor executa e apaga
  (`Editor/ComandoExterno.cs`). Leia o resultado no `Editor.log`.
- **Testes automáticos** (EditMode, `Editor/Testes/`): menu *TI Service/Testes/Rodar testes automáticos* (resumo
  `[Testes] Fim: ...` no Editor.log). Problema novo no catálogo já entra nos testes sozinho.
- **Unity-MCP** (pacote `com.ivanmurzak.unity.mcp`, janela *Window ▸ AI Game Dev*) no modo **Custom/local**; o
  `.mcp.json` é local (fora do Git). Nunca usar o modo Cloud.
- **Save**: grava em `.tmp` e troca (anterior vira `.bak`); save que não abre vira `.corrompido-*`. Campo novo no
  save precisa de valor inicial; mudança de formato = subir `SaveManager.VersaoAtual` e um passo em `Migrar()`.
- **Pipeline gráfico: Built-in** (decidido após teste contra HDRP — ver ROADMAP, Pilar gráfico).
- Detalhes na skill `testar-na-unity`.
