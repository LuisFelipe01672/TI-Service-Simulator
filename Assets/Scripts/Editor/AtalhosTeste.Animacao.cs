using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Atalhos de teste da Etapa 10C (animação das peças enquanto o técnico segura E). O atalho "segura" a ação pela
// janela de manutenção (os mesmos campos que a barra usa) e fotografa em vários pontos da barra.
// Menu "TI Service/Teste (Play)/Animações".
public static partial class AtalhosTeste
{
    const string MenuAnim = Menu + "Animações/";
    static AcaoFisica acaoAnim;

    static void DefinirSegurar(AcaoFisica a, Equipamento eq, float p)
    {
        var ui = GameUI.Instance;
        if (ui == null) return;
        var bf = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        typeof(GameUI).GetField("acaoSeguradaJanela", bf).SetValue(ui, a);
        typeof(GameUI).GetField("progressoJanela", bf).SetValue(ui, a != null ? a.segurar * p : 0f);
        typeof(GameUI).GetField("equipamentoAtual", bf).SetValue(ui, eq);
    }

    static Equipamento Equip(TipoEquipamento t)
    {
        foreach (var e in Object.FindObjectsByType<Equipamento>()) if (e.tipoEquipamento == t) return e;
        return null;
    }

    // Passos: segurar a ação em cada ponto da barra (foto) e depois concluir
    static void PassosSegurar(List<(System.Action, float, string)> l, TipoEquipamento t, string prefixo, string foto, params float[] pontos)
    {
        l.Add((() =>
        {
            acaoAnim = null;
            var e = MissionManager.Instance?.Estado;
            if (e == null) return;
            foreach (var a in AcoesFisicas.Para(t, e)) if (a.rotulo.StartsWith(prefixo)) { acaoAnim = a; break; }
            if (acaoAnim == null) Debug.LogWarning($"[Teste] Ação não disponível: {prefixo}");
            else Debug.Log($"[Teste] Segurando: {acaoAnim.rotulo} (animação {acaoAnim.animacao})");
        }, 0.1f, null));
        for (int i = 0; i < pontos.Length; i++)
        {
            float p = pontos[i];
            l.Add((() => { if (acaoAnim != null) DefinirSegurar(acaoAnim, Equip(t), p); }, 0.7f, $"{foto}_{(int)(p * 100):00}"));
        }
        l.Add((() =>
        {
            DefinirSegurar(null, null, 0f);
            if (acaoAnim == null) return;
            if (acaoAnim.aoCrimpar != null) acaoAnim.aoCrimpar(true);
            else Debug.Log($"[Teste] {acaoAnim.rotulo} → {acaoAnim.executar()}");
            MissionManager.Instance?.RegistrarAcao(acaoAnim.rotulo);
        }, 1.2f, null));
    }

    // Câmera só das fotos (o jogador fica onde está): d < 0 olha por trás (parafusos da tampa), d > 0 pelo lado aberto
    static Camera camFoto;
    static void FotoCam(Vector3 pos, Vector3 alvo)
    {
        if (camFoto == null)
        {
            camFoto = new GameObject("CameraFotoAnimacao").AddComponent<Camera>();
            if (Camera.main != null) { camFoto.CopyFrom(Camera.main); camFoto.depth = Camera.main.depth + 5; }
            camFoto.nearClipPlane = 0.02f;
            camFoto.fieldOfView = 55f;
        }
        camFoto.transform.position = pos;
        camFoto.transform.LookAt(alvo);
    }

    // Escolhe, entre as direções, a primeira sem parede/móvel entre a peça e a câmera
    static Vector3 DirLivre(Vector3 alvo, float dist, Transform ignorar, params Vector3[] dirs)
    {
        foreach (var d0 in dirs)
        {
            Vector3 d = d0; d.y = 0f; d.Normalize();
            Vector3 cam = alvo + d * dist + Vector3.up * 0.28f;
            bool livre = true;
            foreach (var h in Physics.RaycastAll(alvo, (cam - alvo).normalized, (cam - alvo).magnitude, ~0, QueryTriggerInteraction.Ignore))
                if (ignorar == null || !h.collider.transform.IsChildOf(ignorar)) { livre = false; break; }
            if (livre) return d;
        }
        Vector3 r = dirs[0]; r.y = 0f; return r.normalized;
    }

    static void SemFotoCam() { if (camFoto != null) Object.Destroy(camFoto.gameObject); camFoto = null; }

    static void IrAoGabinete(float d)
    {
        var pc = Equip(TipoEquipamento.Computador);
        if (pc == null) return;
        var gab = pc.transform.Find("Visual/Gabinete");
        if (gab == null) return;
        Vector3 alvo = gab.position + Vector3.up * 0.2f;
        Vector3 dir = d < 0f
            ? DirLivre(alvo, Mathf.Abs(d), pc.transform, gab.TransformDirection(new Vector3(-1f, 0f, 0.45f)), gab.TransformDirection(new Vector3(-1f, 0f, -0.45f)), gab.TransformDirection(Vector3.back))
            : DirLivre(alvo, Mathf.Abs(d), pc.transform, gab.TransformDirection(Vector3.left), gab.TransformDirection(new Vector3(-1f, 0f, -0.7f)), gab.TransformDirection(new Vector3(-1f, 0f, 0.7f)));
        FotoCam(alvo + dir * Mathf.Abs(d) + Vector3.up * 0.28f, alvo);
    }

    // Close numa peça de dentro do gabinete (pelo lado aberto)
    static void FocoGabinete(string peca, float d)
    {
        var pc = Equip(TipoEquipamento.Computador);
        var gab = pc != null ? pc.transform.Find("Visual/Gabinete") : null;
        var t = gab != null ? gab.Find(peca) : null;
        if (t == null) return;
        Vector3 dir = DirLivre(t.position, d, pc.transform, gab.TransformDirection(Vector3.left), gab.TransformDirection(new Vector3(-1f, 0f, -0.7f)), gab.TransformDirection(new Vector3(-1f, 0f, 0.7f)));
        FotoCam(t.position + dir * d + Vector3.up * 0.12f, t.position);
    }

    static void FotoNotebook()
    {
        var nb = Equip(TipoEquipamento.Notebook);
        if (nb == null) { Debug.LogWarning("[Teste] Notebook não está ativo na cena."); LogNotebook(); return; }
        Vector3 alvo = nb.transform.position + Vector3.up * 0.05f;
        FotoCam(alvo - nb.transform.forward * 0.45f + Vector3.up * 0.4f, alvo);
    }

    static void FotoImpressora()
    {
        var imp = Equip(TipoEquipamento.Impressora);
        if (imp == null) return;
        Vector3 alvo = imp.transform.position + Vector3.up * 0.2f;
        Vector3 dir = DirLivre(alvo, 0.7f, imp.transform, -imp.transform.forward - imp.transform.right, -imp.transform.forward + imp.transform.right, -imp.transform.forward);
        FotoCam(alvo + dir * 0.7f + Vector3.up * 0.35f, alvo);
    }

    [MenuItem(MenuAnim + "1. Computador: abrir, limpar, memória, cooler e pasta (fotos)", false, 1300)]
    static void AnimComputador()
    {
        if (Jogador() == null) return;
        EncherMochila();
        var l = new List<(System.Action, float, string)>
        {
            (() => { TestePasta(); }, 2.5f, null),
            (() => { var e = MissionManager.Instance?.Estado; if (e != null) { e.pcLigado = false; e.pcPastaSeca = true; e.pcPoeira = 1f; } IrPerto(TipoEquipamento.Computador, 1.0f); IrAoGabinete(-0.75f); }, 1.0f, "pc_0_fechado"),
        };
        PassosSegurar(l, TipoEquipamento.Computador, "Virar o gabinete e tirar a tampa", "pc_1_parafusos", 0.3f, 0.55f, 0.85f);
        l.Add((() => IrAoGabinete(0.42f), 1.2f, "pc_2_aberto"));
        PassosSegurar(l, TipoEquipamento.Computador, "Limpar com pincel", "pc_3_limpando", 0.2f, 0.7f);
        l.Add((() => FocoGabinete("Pente1", 0.3f), 0.5f, null));
        PassosSegurar(l, TipoEquipamento.Computador, "Tirar os pentes de memória", "pc_4_memoria", 0.1f, 0.35f, 0.5f, 0.75f);
        l.Add((() => FocoGabinete("Dissipador", 0.3f), 0.5f, null));
        PassosSegurar(l, TipoEquipamento.Computador, "Tirar o cooler", "pc_5_cooler", 0.3f, 0.7f, 0.95f);
        PassosSegurar(l, TipoEquipamento.Computador, "Limpar a pasta velha", "pc_6_pasta_limpa", 0.5f);
        PassosSegurar(l, TipoEquipamento.Computador, "Aplicar pasta térmica", "pc_7_pasta_nova", 0.6f);
        PassosSegurar(l, TipoEquipamento.Computador, "Recolocar o cooler", "pc_8_cooler_volta", 0.3f, 0.8f);
        l.Add((() => IrAoGabinete(0.42f), 0.5f, null));
        PassosSegurar(l, TipoEquipamento.Computador, "Colocar a tampa", "pc_9_fechando", 0.4f, 0.8f);
        l.Add((() => SemFotoCam(), 0.2f, null));
        SequenciaFotos("animacoes", l);
    }

    [MenuItem(MenuAnim + "2. Computador: fonte e SSD (fotos)", false, 1301)]
    static void AnimFonteSsd()
    {
        if (Jogador() == null) return;
        EncherMochila();
        var l = new List<(System.Action, float, string)>
        {
            (() => { ChamadoTeste("Trocar fonte"); }, 2.5f, null),
            (() => { var e = MissionManager.Instance?.Estado; if (e != null) { e.pcLigado = false; e.pcGabineteAberto = true; } IrPerto(TipoEquipamento.Computador, 1.0f); IrAoGabinete(0.42f); }, 1.5f, null),
        };
        PassosSegurar(l, TipoEquipamento.Computador, "Trocar a fonte", "fonte", 0.3f, 0.5f, 0.8f);
        l.Add((() => { ChamadoTeste("Trocar HD"); }, 2.5f, null));
        l.Add((() => { var e = MissionManager.Instance?.Estado; if (e != null) { e.pcLigado = false; e.pcGabineteAberto = true; e.pcBackupFeito = true; } IrAoGabinete(0.6f); }, 1.5f, null));
        PassosSegurar(l, TipoEquipamento.Computador, "Trocar o HD por um SSD", "ssd", 0.3f, 0.55f, 0.85f);
        l.Add((() => SemFotoCam(), 0.2f, null));
        SequenciaFotos("animacoes", l);
    }

    [MenuItem(MenuAnim + "3. Notebook: parafusos, bateria e tampa (fotos)", false, 1302)]
    static void AnimNotebook()
    {
        if (Jogador() == null) return;
        EncherMochila();
        var l = new List<(System.Action, float, string)>
        {
            (() => NbBateria(), 2.5f, null),
            (() => { var e = MissionManager.Instance?.Estado; if (e != null) { e.nbLigado = false; e.nbNaTomada = false; } IrPerto(TipoEquipamento.Notebook, 0.5f); FotoNotebook(); }, 1.0f, null),
        };
        PassosSegurar(l, TipoEquipamento.Notebook, "Virar e tirar os 10 parafusos", "nb_1_abrindo", 0.2f, 0.6f, 0.95f);
        PassosSegurar(l, TipoEquipamento.Notebook, "Desconectar e retirar a bateria", "nb_2_bateria_sai", 0.4f, 0.85f);
        PassosSegurar(l, TipoEquipamento.Notebook, "Instalar a bateria nova", "nb_3_bateria_entra", 0.5f);
        PassosSegurar(l, TipoEquipamento.Notebook, "Encaixar a tampa de baixo", "nb_4_fechando", 0.15f, 0.5f, 0.92f);
        l.Add((() => NbDobradica(), 2.5f, null));
        l.Add((() => { var e = MissionManager.Instance?.Estado; if (e != null) e.nbLigado = false; IrPerto(TipoEquipamento.Notebook, 0.55f); FotoNotebook(); }, 1.0f, null));
        PassosSegurar(l, TipoEquipamento.Notebook, "Soltar a moldura", "nb_5_moldura", 0.6f);
        PassosSegurar(l, TipoEquipamento.Notebook, "Trocar as dobradiças", "nb_6_dobradica", 0.5f);
        l.Add((() => SemFotoCam(), 0.2f, null));
        SequenciaFotos("animacoes", l);
    }

    [MenuItem(MenuAnim + "4. Impressora: papel atolado (fotos)", false, 1303)]
    static void AnimImpressora()
    {
        if (Jogador() == null) return;
        var l = new List<(System.Action, float, string)>
        {
            (() => TestePapelAtolado(), 2.5f, null),
            (() => { IrPerto(TipoEquipamento.Impressora, 1.0f); FotoImpressora(); }, 1.0f, null),
        };
        PassosSegurar(l, TipoEquipamento.Impressora, "Abrir a tampa traseira e puxar", "imp_papel", 0.4f, 0.9f);
        l.Add((() => SemFotoCam(), 0.2f, null));
        SequenciaFotos("animacoes", l);
    }

    [MenuItem(MenuAnim + "9. Log: onde está o notebook", false, 1309)]
    static void LogNotebook()
    {
        var nb = NotebookCliente.Garantir();
        var ch = MissionManager.Instance?.chamadoAtivo;
        Debug.Log($"[Teste] Notebook: ativo {nb.gameObject.activeInHierarchy} em {nb.transform.position} · chamado {ch?.equipamento} · sala {SalaCliente.Atual?.Nome}");
    }
}
