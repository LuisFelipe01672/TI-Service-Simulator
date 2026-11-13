using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Parte da GameUI: manutenção com as mãos (ações no equipamento), minigame de crimpar e bancada da sede.
// À noite as ações também vão virar peças 3D (mirar e apertar/segurar E); a lógica é a mesma (AcoesFisicas).
public partial class GameUI
{
    // ---------- Manutenção ----------
    RectTransform manutencao, manAcoes, manLog, manMochila;
    TextMeshProUGUI txtManTitulo, txtManSub, txtSegurando;
    Equipamento equipamentoAtual;
    readonly List<string> logManutencao = new List<string>();
    bool segurando;
    Coroutine rotinaSegurar;

    // ---------- Crimpagem ----------
    RectTransform crimpagem, crimpFios, crimpSlots, crimpRef;
    TextMeshProUGUI txtCrimpTitulo, txtCrimpEtapa;
    Button botaoCrimpAvancar;
    TextMeshProUGUI txtCrimpAvancar;
    Crimpagem crimpAtual;
    System.Action<bool> aoTerminarCrimpagem;
    System.Action aoCancelarCrimpagem;
    Vector3 origemSujeira;

    // ---------- Bancada ----------
    RectTransform bancada, bancadaConteudo;

    void ConstruirManutencao(RectTransform pai)
    {
        manutencao = JanelaModal(pai, "Manutencao", new Vector2(1240, 0));
        UIFactory.Vertical(manutencao, 12, 30);
        UIFactory.AjustarAoConteudo(manutencao);
        txtManTitulo = UIFactory.Texto(manutencao, "", 34, UIFactory.CorTexto, FontStyles.Bold);
        txtManSub = UIFactory.Texto(manutencao, "", 19, UIFactory.CorSuave);

        var colunas = UIFactory.Vazio(manutencao, "Colunas");
        var h = UIFactory.Horizontal(colunas, 24, 0, TextAnchor.UpperLeft);
        h.childForceExpandHeight = false;

        manAcoes = UIFactory.Vazio(colunas, "Acoes");
        UIFactory.Vertical(manAcoes, 8);
        manAcoes.gameObject.AddComponent<LayoutElement>().preferredWidth = 560;

        var direita = UIFactory.Vazio(colunas, "Direita");
        UIFactory.Vertical(direita, 10);
        direita.gameObject.AddComponent<LayoutElement>().preferredWidth = 596;
        UIFactory.Texto(direita, "O QUE VOCÊ VIU E FEZ", 17, UIFactory.Destaque, FontStyles.Bold);
        manLog = UIFactory.Painel(direita, "Log", UIFactory.Cartao);
        UIFactory.Vertical(manLog, 6, 16);
        UIFactory.Texto(direita, "MOCHILA", 17, UIFactory.CorSuave, FontStyles.Bold);
        manMochila = UIFactory.Painel(direita, "Mochila", UIFactory.Cartao);
        UIFactory.Vertical(manMochila, 4, 14);

        txtSegurando = UIFactory.Texto(manutencao, "", 22, UIFactory.Alerta, FontStyles.Bold, TextAlignmentOptions.Center);
        UIFactory.Botao(manutencao, "Fechar (Esc)", FecharModal, UIFactory.CartaoClaro, 220, 48);

        ConstruirCrimpagem(pai);
        ConstruirBancada(pai);
    }

    public void AbrirManutencao(Equipamento eq)
    {
        if (equipamentoAtual != eq) logManutencao.Clear();
        equipamentoAtual = eq;
        AbrirModal(Modal.Manutencao, manutencao);
        MontarManutencao();
    }

    void Registrar(string texto)
    {
        if (string.IsNullOrEmpty(texto)) return;
        logManutencao.Insert(0, texto);
        if (logManutencao.Count > 7) logManutencao.RemoveAt(logManutencao.Count - 1);
    }

    void MontarManutencao()
    {
        var eq = equipamentoAtual;
        if (eq == null) return;
        var mm = MissionManager.Instance;
        var ch = mm != null ? mm.chamadoAtivo : null;
        var estado = mm != null ? mm.Estado : null;

        txtManTitulo.text = eq.Nome + (ch != null ? $"  <size=60%><color=#9AA6B8>{ch.nomeCliente}</color></size>" : "");
        txtSegurando.text = "";

        UIFactory.Limpar(manAcoes);
        UIFactory.Limpar(manLog);
        UIFactory.Limpar(manMochila);

        if (ch == null || estado == null)
        {
            txtManSub.text = "Nenhum chamado em atendimento. Aceite um chamado no tablet (Tab) primeiro.";
            MontarMochilaResumo();
            return;
        }
        txtManSub.text = $"\"{ch.DescricaoVisivel}\" — {ch.contato}   ·   Prazo: {Tempo(mm.TempoRestante)}";

        var mochila = Mochila.Instance;
        foreach (var acao in AcoesFisicas.Para(eq.tipoEquipamento, estado))
        {
            var a = acao;
            bool temItem = a.requer == null || (mochila != null && mochila.Tem(a.requer.Value));
            string rotulo = a.rotulo + (a.segurar > 0f ? $"  <size=75%>(segure {a.segurar:F0}s)</size>" : "");
            if (!temItem) rotulo = $"{a.rotulo}  <size=75%><color=#F2545B>precisa: {Mochila.Info(a.requer.Value).nome}</color></size>";
            var cor = a.observacao ? UIFactory.Cartao : a.abreComputador ? UIFactory.Destaque : UIFactory.CartaoClaro;
            var b = UIFactory.Botao(manAcoes, rotulo, () => ExecutarAcao(a), cor, 560, 46, 19);
            b.interactable = temItem && !segurando;
        }

        if (logManutencao.Count == 0)
            UIFactory.Texto(manLog, "Comece olhando o equipamento: visor, luzes, cabos...", 19, UIFactory.CorSuave);
        for (int i = 0; i < logManutencao.Count; i++)
            UIFactory.Texto(manLog, (i == 0 ? "▶ " : "• ") + logManutencao[i], i == 0 ? 20 : 18, i == 0 ? UIFactory.CorTexto : UIFactory.CorSuave);

        MontarMochilaResumo();
    }

    void MontarMochilaResumo()
    {
        var m = Mochila.Instance;
        if (m == null) return;
        var ferramentas = new List<string>();
        foreach (var info in Mochila.Catalogo)
            if (info.ferramenta && m.Tem(info.item)) ferramentas.Add(info.nome);
        UIFactory.Texto(manMochila, "Ferramentas: " + (ferramentas.Count == 0 ? "só o notebook" : "notebook, " + string.Join(", ", ferramentas)), 18, UIFactory.CorTexto);
        UIFactory.Texto(manMochila, $"Conectores RJ45: {m.Quantidade(Item.ConectorRJ45)}  ·  Toner: {m.Quantidade(Item.Toner)}  ·  Cabos prontos: {m.ListaCabos()}", 18, UIFactory.CorSuave);
    }

    void ExecutarAcao(AcaoFisica a)
    {
        if (segurando) return;
        if (a.abreComputador) { AbrirComputador(); return; }
        if (a.abreCrimpagem) { CrimparNoCliente(); return; }
        if (a.segurar > 0f) { rotinaSegurar = StartCoroutine(Segurar(a)); return; }
        Concluir(a);
    }

    IEnumerator Segurar(AcaoFisica a)
    {
        segurando = true;
        MontarManutencao();
        float t = 0f;
        while (t < a.segurar)
        {
            t += Time.unscaledDeltaTime;
            int barras = Mathf.RoundToInt(20f * t / a.segurar);
            txtSegurando.text = $"{a.rotulo}...  [{new string('|', barras)}{new string('.', 20 - barras)}]";
            yield return null;
        }
        segurando = false;
        Concluir(a);
    }

    void Concluir(AcaoFisica a)
    {
        string r = a.executar();
        if (!a.observacao) MissionManager.Instance?.RegistrarAcao();
        Registrar(r);
        // A ação pode ter fechado o chamado (página de teste ok, por exemplo)
        if (MissionManager.Instance == null || MissionManager.Instance.chamadoAtivo == null) { FecharModal(); return; }
        if (modal == Modal.Manutencao) MontarManutencao();
    }

    // ---------- Crimpagem ----------

    void ConstruirCrimpagem(RectTransform pai)
    {
        crimpagem = JanelaModal(pai, "Crimpagem", new Vector2(1180, 0));
        UIFactory.Vertical(crimpagem, 12, 30);
        UIFactory.AjustarAoConteudo(crimpagem);
        txtCrimpTitulo = UIFactory.Texto(crimpagem, "Crimpar conector RJ45", 32, UIFactory.CorTexto, FontStyles.Bold);

        UIFactory.Texto(crimpagem, "PADRÃO T568B (pino 1 → 8, trava do conector para baixo)", 16, UIFactory.CorSuave, FontStyles.Bold);
        crimpRef = UIFactory.Vazio(crimpagem, "Referencia");
        UIFactory.Horizontal(crimpRef, 6);
        for (int i = 0; i < 8; i++) Fio(crimpRef, i, $"{i + 1}. {Crimpagem.T568B[i]}", null, 136, 40);

        txtCrimpEtapa = UIFactory.Texto(crimpagem, "", 22, UIFactory.CorTexto);
        UIFactory.Texto(crimpagem, "NO CONECTOR", 16, UIFactory.CorSuave, FontStyles.Bold);
        crimpSlots = UIFactory.Vazio(crimpagem, "Slots");
        UIFactory.Horizontal(crimpSlots, 6);
        UIFactory.Texto(crimpagem, "FIOS NA SUA MÃO (clique na ordem)", 16, UIFactory.CorSuave, FontStyles.Bold);
        crimpFios = UIFactory.Vazio(crimpagem, "Fios");
        UIFactory.Horizontal(crimpFios, 6);

        var linha = UIFactory.Vazio(crimpagem, "Botoes");
        UIFactory.Horizontal(linha, 12);
        botaoCrimpAvancar = UIFactory.Botao(linha, "Avançar", AvancarCrimpagem, UIFactory.Destaque, 420, 54);
        txtCrimpAvancar = botaoCrimpAvancar.GetComponentInChildren<TextMeshProUGUI>();
        UIFactory.Botao(linha, "Desfazer último fio", () => { crimpAtual?.TirarUltimo(); MontarCrimpagem(); }, UIFactory.CartaoClaro, 260, 54, 20);
        UIFactory.Botao(linha, "Desistir", FecharModal, UIFactory.Perigo, 180, 54, 20);
    }

    Button Fio(RectTransform pai, int fio, string texto, System.Action aoClicar, float largura = 132, float altura = 46)
    {
        var cor = UIFactory.Hex(Crimpagem.CoresHex[fio]);
        var b = UIFactory.Botao(pai, texto, aoClicar, cor, largura, altura, 15);
        var t = b.GetComponentInChildren<TextMeshProUGUI>();
        t.color = Crimpagem.Listrado[fio] || fio == 0 ? new Color(0.1f, 0.1f, 0.12f) : Color.white;
        return b;
    }

    void IniciarCrimpagem(string titulo, Vector3 sujeiraEm, System.Action<bool> aoTerminar, System.Action aoCancelar = null)
    {
        crimpAtual = new Crimpagem();
        aoTerminarCrimpagem = aoTerminar;
        aoCancelarCrimpagem = aoCancelar;
        origemSujeira = sujeiraEm;
        txtCrimpTitulo.text = titulo;
        AbrirModal(Modal.Crimpagem, crimpagem);
        MontarCrimpagem();
    }

    void CancelarCrimpagem()
    {
        var c = aoCancelarCrimpagem;
        crimpAtual = null;
        aoTerminarCrimpagem = null;
        aoCancelarCrimpagem = null;
        c?.Invoke();
    }

    void MontarCrimpagem()
    {
        var c = crimpAtual;
        if (c == null) return;
        UIFactory.Limpar(crimpSlots);
        UIFactory.Limpar(crimpFios);

        bool ordenando = c.etapa == Crimpagem.Etapa.Ordenar;
        for (int i = 0; i < 8; i++)
        {
            if (i < c.ordem.Count) Fio(crimpSlots, c.ordem[i], $"{i + 1}", null);
            else
            {
                var vazio = UIFactory.Botao(crimpSlots, $"{i + 1}", null, UIFactory.Cartao, 132, 46, 15);
                vazio.interactable = false;
            }
        }
        if (c.etapa >= Crimpagem.Etapa.Ordenar)
            foreach (var f in c.fiosSoltos)
            {
                int fio = f;
                var b = Fio(crimpFios, fio, Crimpagem.T568B[fio], () => { crimpAtual?.ColocarFio(fio); MontarCrimpagem(); });
                b.interactable = ordenando;
            }
        else
            UIFactory.Texto(crimpFios, "(os fios aparecem depois de descascar a capa)", 18, UIFactory.CorSuave);

        string[] dicas =
        {
            "1. Corte fora a ponta velha com o alicate.",
            "2. Descasque uns 2 cm da capa (segure).",
            "3. Destrance os pares e coloque os 8 fios na ordem do padrão.",
            "4. Apare as pontas retinhas.",
            "5. Empurre os fios até o fundo do conector (gasta 1 RJ45).",
            "6. Aperte o alicate com força (segure)."
        };
        string[] botoes = { "Cortar a ponta", "Descascar (segure)", "Fios na ordem, continuar", "Aparar os fios", "Inserir no conector", "Crimpar! (segure)" };
        int idx = Mathf.Min((int)c.etapa, 5);
        txtCrimpEtapa.text = dicas[idx];
        txtCrimpAvancar.text = botoes[idx];
        botaoCrimpAvancar.interactable = !segurando && (!ordenando || c.ordem.Count == 8);
    }

    void AvancarCrimpagem()
    {
        var c = crimpAtual;
        if (c == null || segurando) return;
        float segurar = c.etapa == Crimpagem.Etapa.Descascar ? 1.5f : c.etapa == Crimpagem.Etapa.Crimpar ? 2f : 0f;
        if (segurar > 0f) rotinaSegurar = StartCoroutine(SegurarCrimpagem(segurar));
        else PassoCrimpagem();
    }

    IEnumerator SegurarCrimpagem(float duracao)
    {
        segurando = true;
        botaoCrimpAvancar.interactable = false;
        float t = 0f;
        while (t < duracao)
        {
            t += Time.unscaledDeltaTime;
            int barras = Mathf.RoundToInt(16f * t / duracao);
            txtCrimpAvancar.text = $"[{new string('|', barras)}{new string('.', 16 - barras)}]";
            yield return null;
        }
        segurando = false;
        PassoCrimpagem();
    }

    void PassoCrimpagem()
    {
        var c = crimpAtual;
        if (c == null) return;
        var etapa = c.etapa;
        string msg = c.Avancar();
        if (c.etapa != etapa && Crimpagem.FazSujeira(etapa))
            Sujeira.Espalhar(origemSujeira, etapa == Crimpagem.Etapa.Aparar ? 6 : 3, etapa == Crimpagem.Etapa.Cortar ? "conector" : etapa == Crimpagem.Etapa.Aparar ? "fios" : "capa");

        if (c.etapa == Crimpagem.Etapa.Pronto)
        {
            var fim = aoTerminarCrimpagem;
            bool ok = c.sucesso;
            crimpAtual = null;
            aoTerminarCrimpagem = null;
            aoCancelarCrimpagem = null;
            Toast(msg, UIFactory.Destaque, 2f);
            fim?.Invoke(ok);
            return;
        }
        if (c.etapa == etapa) Toast(msg, UIFactory.Alerta, 2f);   // não avançou (faltou algo)
        MontarCrimpagem();
    }

    // Refazer a ponta do cabo do equipamento do cliente (uma ponta)
    void CrimparNoCliente()
    {
        var mm = MissionManager.Instance;
        var e = mm?.Estado;
        var eq = equipamentoAtual;
        if (e == null || eq == null) return;
        var cabo = eq.tipoEquipamento == TipoEquipamento.Impressora ? e.impCabo : e.pcCabo;
        if (Mochila.Instance != null && !Mochila.Instance.Tem(Item.ConectorRJ45))
        {
            Registrar("<color=#F2545B>Você não tem conectores RJ45.</color> Compre no tablet (Loja).");
            MontarManutencao();
            return;
        }
        mm.RegistrarAcao();
        IniciarCrimpagem($"Refazer a ponta do cabo: {eq.Nome}", eq.transform.position, ok =>
        {
            if (ok) { if (cabo.defeito == DefeitoCabo.Conector) cabo.defeito = DefeitoCabo.Nenhum; }
            else if (cabo.defeito != DefeitoCabo.Rompido) cabo.defeito = DefeitoCabo.Conector;
            mm.CobrarPeca(2f);
            Registrar("Você refez a ponta do cabo. Teste com o testador para ter certeza.");
            AbrirManutencao(eq);
        }, () => { if (eq != null) AbrirManutencao(eq); });
    }

    // ---------- Bancada da sede ----------

    void ConstruirBancada(RectTransform pai)
    {
        bancada = JanelaModal(pai, "Bancada", new Vector2(760, 0));
        UIFactory.Vertical(bancada, 12, 32);
        UIFactory.AjustarAoConteudo(bancada);
        UIFactory.Texto(bancada, "Bancada da sede", 34, UIFactory.CorTexto, FontStyles.Bold);
        bancadaConteudo = UIFactory.Vazio(bancada, "Conteudo");
        UIFactory.Vertical(bancadaConteudo, 10);
        UIFactory.Botao(bancada, "Fechar (Esc)", FecharModal, UIFactory.CartaoClaro, 220, 48);
    }

    public void AbrirBancada(Vector3 posicao)
    {
        origemSujeira = posicao;
        AbrirModal(Modal.Bancada, bancada);
        MontarBancada();
    }

    void MontarBancada()
    {
        var m = Mochila.Instance;
        UIFactory.Limpar(bancadaConteudo);
        if (m == null) return;

        UIFactory.Texto(bancadaConteudo, $"Caixa de cabo: <b>{m.MetrosNaCaixa} m</b> restantes  ·  Conectores RJ45: <b>{m.Quantidade(Item.ConectorRJ45)}</b>", 21, UIFactory.CorTexto);
        UIFactory.Texto(bancadaConteudo, $"Cabos prontos na mochila: {m.ListaCabos()}", 19, UIFactory.CorSuave);
        MontarPreparacaoNaBancada();

        string falta = !m.Tem(Item.AlicateCrimpagem) ? "Você precisa de um alicate de crimpagem." :
                       m.MetrosNaCaixa <= 0 ? "Você não tem caixa de cabo (compre no tablet)." :
                       m.Quantidade(Item.ConectorRJ45) < 2 ? "Você precisa de pelo menos 2 conectores RJ45." : null;
        if (falta != null)
        {
            UIFactory.Texto(bancadaConteudo, $"<color=#F5B83F>{falta}</color>", 20, UIFactory.CorTexto);
            return;
        }

        UIFactory.Texto(bancadaConteudo, "FAZER UM CABO NOVO (puxa da caixa e crimpa as duas pontas)", 17, UIFactory.Destaque, FontStyles.Bold);
        var linha = UIFactory.Vazio(bancadaConteudo, "Tamanhos");
        UIFactory.Horizontal(linha, 10);
        foreach (float metros in new[] { 1f, 2f, 3f, 5f, 10f })
        {
            float mt = metros;
            var b = UIFactory.Botao(linha, $"{mt:0} m", () => FazerCabo(mt), UIFactory.CartaoClaro, 120, 52, 22);
            b.interactable = m.MetrosNaCaixa >= mt;
        }
        UIFactory.Texto(bancadaConteudo, "Dica: meça no cliente antes. Cabo curto não alcança a tomada; cabo comprido demais fica sobrando.", 18, UIFactory.CorSuave);
    }

    void FazerCabo(float metros)
    {
        var m = Mochila.Instance;
        if (m == null || !m.TirarDaCaixa(metros)) return;
        Sujeira.Espalhar(origemSujeira, 2, "capa");
        IniciarCrimpagem($"Cabo de {metros:0} m — ponta 1 de 2", origemSujeira, pontaA =>
        {
            IniciarCrimpagem($"Cabo de {metros:0} m — ponta 2 de 2", origemSujeira, pontaB =>
            {
                m.cabos.Add(new CaboPronto(metros, pontaA, pontaB));
                Toast($"Cabo de {metros:0} m pronto e guardado na mochila." + (pontaA && pontaB ? "" : " (teste antes de usar...)"), UIFactory.Sucesso, 3f);
                AbrirBancada(origemSujeira);
            }, () => AbrirBancada(origemSujeira));
        }, () => AbrirBancada(origemSujeira));
    }
}
