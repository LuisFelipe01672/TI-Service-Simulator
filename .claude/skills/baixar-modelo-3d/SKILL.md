---
name: baixar-modelo-3d
description: Encontrar, escolher e trazer modelos 3D realistas e gratuitos (CC0/CC-BY) para o TI Service Simulator — impressoras, computadores, móveis da casa, ferramentas — com licença conferida, créditos e LFS. Use quando o dono pedir modelos/texturas mais realistas ou um objeto novo no cenário.
---

# Baixar modelo 3D para o TI Service Simulator

Regras do projeto: visual **realista**; **sem marcas reais** (troque logotipos por marca fictícia); repositório
GitHub **público** com Git LFS. Downloads exigem aprovação do dono (diga o que, de onde e o tamanho).

## 1. Onde procurar (nesta ordem)
1. **Poly Haven** (polyhaven.com/models) — fotorrealista, **CC0**, sem login, sem crédito obrigatório. Melhor para
   móveis e objetos de casa/escritório.
2. **Sketchfab** com licença **CC-BY** ou **CC0** e download liberado (precisa do dono logado no navegador do app).
3. **ambientCG** — texturas PBR CC0 (pisos, paredes, plástico, metal); já usado no projeto.
4. **Kenney** — CC0, estilo simples (já usado nos móveis).
5. **Mixamo** — personagens/animações: pode usar no jogo, **não pode** ir para o GitHub (fica no `.gitignore`).
- Evite: Poly Pizza (low-poly cartoon), cgaxis (pede créditos pagos), sites "free" sem licença clara, IA paga.

## 2. Buscar no Sketchfab de verdade (a busca do site mistura resultados)
Abra uma página do sketchfab.com no navegador do app e rode via javascript_tool:
```js
const r = await fetch(`https://api.sketchfab.com/v3/search?type=models&q=${encodeURIComponent('office printer')}&downloadable=true&license=by&count=24&sort_by=-likeCount`).then(x=>x.json());
r.results.map(m => `${m.name} | ${m.license.label} | ${m.faceCount}f | ${m.user.username} | ${m.uid}`)
```
- Repita com `license=cc0`. Filtre por nome; prefira modelos com mais likes e < ~150k faces.
- Para o dono escolher, monte uma grade de miniaturas: pegue `m.thumbnails.images` (largura ≤ 640) e injete um
  `<div>` fixo na própria página do sketchfab.com (a página da API bloqueia imagens). Cada item com link para
  `https://sketchfab.com/3d-models/<uid>`.

## 3. Baixar
- Página `https://sketchfab.com/3d-models/<uid>#download` abre a janela de download.
- Confira o bloco **CC Attribution / "Commercial use is allowed"**.
- Prefira **GLB** (o projeto tem o pacote `com.unity.cloud.gltfast`) ou **FBX**. Evite .blend/.max/.dae.
- Os botões ficam numa lista; títulos longos empurram a lista para baixo — tire screenshot antes de clicar.
- O arquivo vai para `%USERPROFILE%\Downloads` (às vezes num `.zip`: extraia na scratchpad e pegue o .glb/.fbx).

## 4. Trazer para o projeto
- Copie para `Assets/Models/Sketchfab/` (ou `Assets/Models/PolyHaven/`) com **nome em português**
  (ex.: `Impressora_MFP_Compacta.fbx`, `Cartucho_Toner.glb`).
- `.gitattributes` já manda `*.fbx`, `*.glb`, `*.png` para o LFS — confira com `git lfs status`.
- Atualize **`CREDITOS.md`** (arquivo, link do modelo, autor, alterações). CC-BY exige crédito também numa tela de
  créditos do jogo.
- Logotipo de marca real (HP, Xerox, TP-Link, D-Link...): trocar a textura/material por marca fictícia antes de usar.

## 5. Na Unity
- Abra pelo Hub (ver skill `testar-na-unity`). GLB importa via glTFast; FBX nativo.
- Confira escala (normalizar para o tamanho real), materiais (Standard, Built-in RP) e se as peças que precisam
  animar (tampas, gavetas, cartucho) são objetos separados. Se não forem, as peças móveis são criadas por código
  por cima (ver `EquipamentoPecas.cs` / `CenaCrimpagem.cs`).
- Interações de equipamento seguem o padrão de peças: `PecaEquipamento` (mirar + E/segurar E) com ações de
  `AcoesFisicas` filtradas por `peca`.
