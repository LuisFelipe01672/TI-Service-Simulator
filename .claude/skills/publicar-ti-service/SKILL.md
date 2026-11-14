---
name: publicar-ti-service
description: Fazer commit e push do TI Service Simulator com segurança — sem vazar notas privadas, builds, saves ou arquivos do Mixamo no repositório público. Use antes de qualquer commit ou push deste projeto.
---

# Publicar o TI Service Simulator (commit / push)

Repositório: `LuisFelipe01672/TI-Service-Simulator` no GitHub — **público**, com Git LFS. Branch: `main`.

## Regras do dono
- **Push só quando o dono pedir** (cada vez). Commit local pode, ao fim de uma etapa testada.
- Mensagens de commit em português, no estilo `feat:` / `fix:` / `docs:` + lista do que mudou, terminando com a
  linha de coautoria pedida pelo sistema.
- Valores e marcas reais da empresa do dono **nunca** no repositório (use valores arredondados e nomes fictícios).

## Antes do commit
1. `git status --short` e leia a lista inteira.
2. **Nunca** podem entrar:
   - `TI-Service-Contexto/` e `TI-Service-Contexto.zip` (notas privadas, `privado/` com a tabela de preços) — já
     estão em `.git/info/exclude`; se aparecerem, pare.
   - `Builds/` (executável), `save*.json`, `Player.log`.
   - `Assets/Models/Mixamo/` (termos da Adobe proíbem redistribuir) — está no `.gitignore`.
3. Mudanças suspeitas que a Unity faz sozinha:
   - `ProjectSettings/ProjectSettings.asset` com `preloadedAssets: []` → efeito do build: `git checkout -- ProjectSettings/ProjectSettings.asset`.
   - `Assets/Scenes/SampleScene.unity` com diferença minúscula (arredondamento em RectTransform) → não commitar;
     só commite a cena quando ela foi remontada de propósito (menu TI Service/3).
4. Modelos/texturas novos: confira `git lfs status` (fbx/glb/png devem aparecer como LFS) e se `CREDITOS.md` foi
   atualizado.
5. Mensagem longa: escreva num arquivo da scratchpad e use `git commit -F arquivo` (heredoc/aspas no PowerShell
   quebram com acentos e várias linhas).

## Push (só com pedido)
```powershell
cd "C:\Projetos\TI Service Simulator"
git log origin/main..HEAD --name-only --pretty=format: | Select-String "Contexto|privado|Builds/|save\.json|Models/Mixamo/"
git push origin main
```
- O primeiro comando precisa voltar **vazio**. O PowerShell mostra o resultado do `git push` como "erro"
  (stderr); confira a linha `xxxxxxx..yyyyyyy  main -> main` e `git status -sb` sem "ahead".
- Depois do push, diga ao dono o hash novo do `origin/main`.
