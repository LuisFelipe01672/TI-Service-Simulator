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

## Arquitetura
- **Managers** se criam sozinhos com `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]` e conversam por
  **EventBus** (structs de evento). Evite referências diretas entre managers quando um evento resolve.
- **UI feita por código** (`UI/UIFactory.cs` + TextMeshPro). `GameUI` é `partial` (um arquivo por aba/janela:
  `GameUI.Contratos.cs`, `GameUI.Manutencao.cs`...), com enum `Modal` e a lista `janelas`.
  Evite glifos fora da fonte (▶ ● ⚠): use », •, (!).
- **Cenário montado por código no editor**: `Editor/MontarCenario*.cs`, menu *TI Service/3. Montar cenário*.
  Mudou o script → rode o menu de novo (ele salva a cena). Não edite a cena à mão.
- **Peças de equipamento**: `Manutencao/PecaEquipamento.cs` (mirar + E / segurar E, raycast com
  `QueryTriggerInteraction.Collide`), ações em `Manutencao/AcoesFisicas.cs` filtradas por `peca`, montagem em
  `Manutencao/EquipamentoPecas.cs`.
- **Animações** com **PrimeTween** (`Tween.LocalPosition`, `Sequence.Create()...`).
- **Personagens**: Mixamo em `Assets/Models/Mixamo/` (**fora do Git** — termos da Adobe), configurados pelo menu
  *TI Service/5*; carregados via `Resources/Personagens/BancoPersonagens.asset`. O jogo precisa funcionar sem eles
  (fallback `MontarSimples()`).
- **Modelos 3D** em `Assets/Models/Sketchfab/` (CC-BY, crédito obrigatório) — GLB via pacote glTFast. Texturas
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
