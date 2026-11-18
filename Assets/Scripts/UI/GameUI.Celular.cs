using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Celular do técnico (tecla C): smartphone no canto da tela com barra de status, Mensagens (cada chamado chega
// como conversa do contato do cliente, com "Vou aí") e Banco (saldo e últimas movimentações).
// Avisa (vibra) quando chega mensagem nova.
public partial class GameUI
{
    RectTransform celular, celConteudo;
    TextMeshProUGUI celHora, celBadge;
    enum AppCel { Inicio, Mensagens, Conversa, Banco, Mapa, WiFi }
    AppCel appCel = AppCel.Inicio;
    Chamado celConversa;
    readonly HashSet<Chamado> celVistos = new HashSet<Chamado>();    // já avisados (notificação)
    readonly HashSet<Chamado> celLidos = new HashSet<Chamado>();     // abertos pelo jogador
    static readonly Color CorTelaCel = UIFactory.Hex("#0F141B");
    static readonly Color CorBolhaDeles = UIFactory.Hex("#2A3342");
    static readonly Color CorBolhaMinha = UIFactory.Hex("#1F6F4A");

    void ConstruirCelular(RectTransform pai)
    {
        celular = JanelaModal(pai, "Celular", new Vector2(430, 800));
        // no canto inferior direito, como quem tira o celular do bolso
        celular.anchorMin = celular.anchorMax = new Vector2(1f, 0f);
        celular.pivot = new Vector2(1f, 0f);
        celular.anchoredPosition = new Vector2(-150f, 40f);   // fora do indicador de alertas da direita
        celular.GetComponent<Image>().color = UIFactory.Hex("#05070A");   // moldura

        var tela = UIFactory.Painel(celular, "Tela", CorTelaCel);
        UIFactory.Esticar(tela, 14);

        // Barra de status
        var status = UIFactory.Painel(tela, "Status", new Color(0, 0, 0, 0.35f));
        status.anchorMin = new Vector2(0, 1); status.anchorMax = new Vector2(1, 1); status.pivot = new Vector2(0.5f, 1);
        status.sizeDelta = new Vector2(0, 40);
        celHora = UIFactory.Texto(status, "08:00", 20, UIFactory.CorTexto, FontStyles.Bold);
        UIFactory.Esticar(celHora.rectTransform);
        celHora.margin = new Vector4(16, 0, 0, 0);
        celHora.alignment = TextAlignmentOptions.Left;
        var sinal = UIFactory.Texto(status, "4G  ||||  87%", 18, UIFactory.CorSuave, FontStyles.Normal, TextAlignmentOptions.Right);
        UIFactory.Esticar(sinal.rectTransform);
        sinal.margin = new Vector4(0, 0, 16, 0);

        // Conteúdo do app (lista com rolagem)
        var area = UIFactory.Vazio(tela, "Area");
        area.anchorMin = Vector2.zero; area.anchorMax = Vector2.one;
        area.offsetMin = new Vector2(0, 64); area.offsetMax = new Vector2(0, -44);
        celConteudo = UIFactory.ListaComRolagem(area, "App");
        UIFactory.Esticar((RectTransform)celConteudo.parent.parent);

        // Barra de navegação (Início / Voltar)
        var nav = UIFactory.Vazio(tela, "Nav");
        nav.anchorMin = new Vector2(0, 0); nav.anchorMax = new Vector2(1, 0); nav.pivot = new Vector2(0.5f, 0);
        nav.sizeDelta = new Vector2(0, 56);
        UIFactory.Horizontal(nav, 10, 6, TextAnchor.MiddleCenter);
        UIFactory.Botao(nav, "Voltar", VoltarCelular, UIFactory.CartaoClaro, 130, 42, 18);
        UIFactory.Botao(nav, "Início", () => { appCel = AppCel.Inicio; AtualizarCelular(); }, UIFactory.CartaoClaro, 130, 42, 18);
        UIFactory.Botao(nav, "Guardar", FecharModal, UIFactory.CartaoClaro, 130, 42, 18);
    }

    public void AbrirCelular()
    {
        AbrirModal(Modal.Celular, celular);
        AtualizarCelular();
    }

    // Abre o celular direto no app Mapa (GPS)
    public void AbrirMapa()
    {
        AbrirCelular();
        appCel = AppCel.Mapa;
        AtualizarCelular();
    }

    // Abre o celular direto no app Wi-Fi (atalhos de teste)
    public void AbrirAppWiFi()
    {
        AbrirCelular();
        appCel = AppCel.WiFi;
        AtualizarCelular();
    }

    void VoltarCelular()
    {
        appCel = appCel == AppCel.Conversa ? AppCel.Mensagens : AppCel.Inicio;
        AtualizarCelular();
    }

    void AtualizarCelular()
    {
        if (celular == null || !celular.gameObject.activeSelf) return;
        UIFactory.Limpar(celConteudo);
        ((RectTransform)celConteudo).anchoredPosition = Vector2.zero;
        switch (appCel)
        {
            case AppCel.Inicio: CelInicio(); break;
            case AppCel.Mensagens: CelMensagens(); break;
            case AppCel.Conversa: CelConversa(); break;
            case AppCel.Banco: CelBanco(); break;
            case AppCel.Mapa: CelMapa(); break;
            case AppCel.WiFi: CelWiFi(); break;
        }
    }

    // Tela inicial: relógio grande e os apps
    void CelInicio()
    {
        var d = DiaManager.Instance;
        var hora = UIFactory.Texto(celConteudo, d != null ? d.HoraTexto() : "", 72, UIFactory.CorTexto, FontStyles.Bold, TextAlignmentOptions.Center);
        hora.gameObject.AddComponent<LayoutElement>().preferredHeight = 100;
        if (d != null) UIFactory.Texto(celConteudo, $"{d.DiaSemana}, {d.DataTexto}", 20, UIFactory.CorSuave, FontStyles.Normal, TextAlignmentOptions.Center);
        var esp = UIFactory.Vazio(celConteudo, "Espaco");
        esp.gameObject.AddComponent<LayoutElement>().preferredHeight = 30;

        int naoLidas = MissionManager.Instance != null ? MissionManager.Instance.chamadosDisponiveis.Count(c => !celLidos.Contains(c)) : 0;
        UIFactory.Botao(celConteudo, naoLidas > 0 ? $"Mensagens  ({naoLidas} nova{(naoLidas > 1 ? "s" : "")})" : "Mensagens",
            () => { appCel = AppCel.Mensagens; AtualizarCelular(); }, naoLidas > 0 ? UIFactory.Sucesso : UIFactory.CartaoClaro, 360, 64, 22);
        UIFactory.Botao(celConteudo, "Banco", () => { appCel = AppCel.Banco; AtualizarCelular(); }, UIFactory.CartaoClaro, 360, 64, 22);
        if (GPS.Instance != null)
            UIFactory.Botao(celConteudo, GPS.Instance.Ativo ? "Mapa  (GPS ligado)" : "Mapa", () => { appCel = AppCel.Mapa; AtualizarCelular(); },
                GPS.Instance.Ativo ? UIFactory.Destaque : UIFactory.CartaoClaro, 360, 64, 22);
        if (MissionManager.Instance?.Estado != null)
            UIFactory.Botao(celConteudo, SurveyWiFi.Medindo ? "Wi-Fi  (medindo)" : "Wi-Fi (analisador)", () => { appCel = AppCel.WiFi; AtualizarCelular(); },
                SurveyWiFi.Medindo ? UIFactory.Destaque : UIFactory.CartaoClaro, 360, 64, 22);
        UIFactory.Texto(celConteudo, "<size=80%>Tab: tablet  ·  C: guardar o celular</size>", 18, UIFactory.CorSuave, FontStyles.Normal, TextAlignmentOptions.Center);
    }

    // Lista de conversas: um chamado aberto = uma conversa
    void CelMensagens()
    {
        var mm = MissionManager.Instance;
        UIFactory.Texto(celConteudo, "Mensagens", 30, UIFactory.CorTexto, FontStyles.Bold);
        if (mm == null) return;
        var lista = new List<Chamado>();
        if (mm.chamadoAtivo != null) lista.Add(mm.chamadoAtivo);
        lista.AddRange(mm.chamadosDisponiveis);
        if (lista.Count == 0) { UIFactory.Texto(celConteudo, "Nenhuma mensagem nova.", 20, UIFactory.CorSuave); return; }

        foreach (var ch in lista)
        {
            var c = ch;
            bool nova = !celLidos.Contains(c) && c != mm.chamadoAtivo;
            var cartao = UIFactory.Painel(celConteudo, "Conversa", nova ? UIFactory.Hex("#1B3A2E") : UIFactory.Cartao);
            UIFactory.Vertical(cartao, 2, 12);
            UIFactory.AjustarAoConteudo(cartao);
            UIFactory.Texto(cartao, $"<b>{c.contato}</b>  <color=#9AA6B8><size=85%>{c.nomeCliente}</size></color>" +
                (nova ? "  <color=#3CCB7F><size=80%>• nova</size></color>" : "") +
                (c == mm.chamadoAtivo ? "  <color=#3FA9F5><size=80%>em atendimento</size></color>" : ""), 20, UIFactory.CorTexto);
            UIFactory.Texto(cartao, Resumir(c.DescricaoVisivel, 46), 17, UIFactory.CorSuave);
            var bt = cartao.gameObject.AddComponent<Button>();
            bt.onClick.AddListener(() => { SomManager.Tocar("clique", 0.6f); celConversa = c; celLidos.Add(c); appCel = AppCel.Conversa; AtualizarCelular(); });
        }
    }

    static string Resumir(string s, int max) => string.IsNullOrEmpty(s) || s.Length <= max ? s : s.Substring(0, max - 1) + "…";

    // Conversa com o contato do cliente: o pedido em bolhas e os botões de resposta
    void CelConversa()
    {
        var mm = MissionManager.Instance;
        var c = celConversa;
        if (mm == null || c == null) { appCel = AppCel.Mensagens; CelMensagens(); return; }
        UIFactory.Texto(celConteudo, $"<b>{c.contato}</b>\n<size=80%><color=#9AA6B8>{c.nomeCliente}</color></size>", 24, UIFactory.CorTexto);

        Bolha($"Oi, aqui é {c.contato}, da {c.nomeCliente}. {c.DescricaoVisivel}", false);
        string pagamento = c.contratoId > 0 ? "É do contrato de locação." : $"Pagamos {UIFactory.Dinheiro(c.recompensa)}" +
                           (c.exigeNota ? ", com nota fiscal." : ".");
        Bolha($"Consegue vir hoje? {pagamento}", false);

        if (c == mm.chamadoAtivo) { Bolha("Combinado, já estou indo!", true); return; }
        if (c.falhou || c.concluido) return;

        bool fora = DiaManager.Instance != null && DiaManager.Instance.ForaDoExpediente;
        string motivo = null;
        bool liberado = EmpresaManager.Instance == null || EmpresaManager.Instance.PodeAtender(c, out motivo);
        if (!liberado) UIFactory.Texto(celConteudo, $"<color=#F2545B>{motivo}</color>", 17, UIFactory.CorTexto);
        if (fora) UIFactory.Texto(celConteudo, "<color=#F5B83F>Fora do expediente: responda amanhã.</color>", 17, UIFactory.CorTexto);

        var vou = UIFactory.Botao(celConteudo, "Responder: \"Vou aí!\"", () =>
        {
            mm.AceitarChamado(c);
            FecharModal();
        }, UIFactory.Sucesso, 360, 56, 20);
        vou.interactable = mm.chamadoAtivo == null && !fora && liberado;
        UIFactory.Botao(celConteudo, "Ver depois", VoltarCelular, UIFactory.CartaoClaro, 360, 48, 18);
    }

    void Bolha(string texto, bool minha)
    {
        var linha = UIFactory.Vazio(celConteudo, "Linha");
        var h = UIFactory.Horizontal(linha, 0, 0, minha ? TextAnchor.UpperRight : TextAnchor.UpperLeft);
        h.childForceExpandWidth = false;
        UIFactory.AjustarAoConteudo(linha);
        var bolha = UIFactory.Painel(linha, "Bolha", minha ? CorBolhaMinha : CorBolhaDeles);
        UIFactory.Vertical(bolha, 0, 12);
        var le = bolha.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = 300;
        UIFactory.AjustarAoConteudo(bolha);
        UIFactory.Texto(bolha, texto, 19, UIFactory.CorTexto);
    }

    // Mapa do bairro (Etapa 9B): ruas, endereços e você; cada endereço tem "Ir" (liga o GPS)
    void CelMapa()
    {
        UIFactory.Texto(celConteudo, "Mapa do bairro", 30, UIFactory.CorTexto, FontStyles.Bold);
        var jogador = GameObject.FindWithTag("Player");
        var gps = GPS.Instance;

        // Desenho: x de -LimiteX a +LimiteX, z de LimiteSul a LimiteNorte (norte para cima)
        const float largura = 380f;
        float w = 2f * Bairro.LimiteX, h = Bairro.LimiteNorte - Bairro.LimiteSul;
        float altura = largura * h / w;
        var mapa = UIFactory.Painel(celConteudo, "Mapa", UIFactory.Hex("#3E5A34"));   // terrenos (verde)
        var le = mapa.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = altura; le.preferredWidth = largura;
        Vector2 Norm(float x, float z) => new Vector2((x + Bairro.LimiteX) / w, (z - Bairro.LimiteSul) / h);
        RectTransform Retangulo(string nome, Rect area, Color cor)
        {
            var r = UIFactory.Painel(mapa, nome, cor);
            r.anchorMin = Norm(area.xMin, area.yMin);
            r.anchorMax = Norm(area.xMax, area.yMax);
            r.offsetMin = r.offsetMax = Vector2.zero;
            r.GetComponent<Image>().raycastTarget = false;
            return r;
        }
        foreach (var p in Bairro.Pedacos())
            Retangulo(p.tipo == TipoPiso.Asfalto ? "Asfalto" : "Calcada", p.area,
                p.tipo == TipoPiso.Asfalto ? UIFactory.Hex("#2B2F36") : UIFactory.Hex("#9A968E"));
        Retangulo("Posto", Bairro.TerrenoPosto, UIFactory.Hex("#4A4F57"));
        void Ponto(Vector3 pos, Color cor, float tam, string rotulo)
        {
            var r = Retangulo("Ponto", new Rect(pos.x, pos.z, 0f, 0f), cor);
            r.sizeDelta = new Vector2(tam, tam);
            if (rotulo == null) return;
            var t = UIFactory.Texto(r, rotulo, 13, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
            t.rectTransform.sizeDelta = new Vector2(90, 18);
            t.rectTransform.anchoredPosition = new Vector2(0, 14);
            t.raycastTarget = false;
        }
        foreach (var e in Bairro.Enderecos)
        {
            bool destino = gps != null && gps.Ativo && gps.Destino.tipo == e.tipo;
            Ponto(e.porta, destino ? UIFactory.Sucesso : UIFactory.Alerta, destino ? 14 : 10, RotuloCurto(e.tipo));
        }
        var carro = CarroTecnico.Instance;
        if (carro != null && !CarroTecnico.NoCarro) Ponto(carro.transform.position, UIFactory.Hex("#3FA9F5"), 10, "carro");
        if (jogador != null) Ponto(jogador.transform.position, UIFactory.Destaque, 12, "você");

        if (gps != null && gps.Ativo && jogador != null)
        {
            UIFactory.Texto(celConteudo, $"GPS: <b>{gps.Destino.nome}</b>\n{gps.Destino.Linha} · {gps.Distancia(jogador.transform.position):0} m", 18, UIFactory.CorTexto);
            UIFactory.Botao(celConteudo, "Desligar o GPS", () => { gps.Desligar(); AtualizarCelular(); }, UIFactory.CartaoClaro, 360, 44, 18);
        }
        UIFactory.Texto(celConteudo, "Endereços", 20, UIFactory.CorSuave, FontStyles.Bold);
        foreach (var e in Bairro.Enderecos)
        {
            var end = e;
            float d = jogador != null ? Vector2.Distance(new Vector2(jogador.transform.position.x, jogador.transform.position.z), new Vector2(e.porta.x, e.porta.z)) : 0f;
            UIFactory.Botao(celConteudo, $"{end.nome}\n<size=80%><color=#9AA6B8>{end.Linha} · {d:0} m</color></size>", () =>
            {
                gps?.Definir(end);
                AtualizarCelular();
            }, UIFactory.Cartao, 360, 60, 17);
        }
    }

    static string RotuloCurto(TipoEndereco t)
    {
        switch (t)
        {
            case TipoEndereco.Casa: return "casa";
            case TipoEndereco.Escritorio: return "escritório";
            case TipoEndereco.Fornecedor: return "peças";
            case TipoEndereco.Coleta: return "coleta";
            case TipoEndereco.Contabilidade: return "contábil";
            case TipoEndereco.Clinica: return "clínica";
            case TipoEndereco.Loja: return "loja";
            default: return "posto";
        }
    }

    // Banco: saldo e últimas movimentações do livro-caixa
    void CelBanco()
    {
        UIFactory.Texto(celConteudo, "Banco", 30, UIFactory.CorTexto, FontStyles.Bold);
        float saldo = GameEconomy.Instance != null ? GameEconomy.Instance.saldo : 0f;
        UIFactory.Texto(celConteudo, "Saldo disponível", 18, UIFactory.CorSuave);
        UIFactory.Texto(celConteudo, UIFactory.Dinheiro(saldo), 40, saldo >= 0 ? UIFactory.Sucesso : UIFactory.Perigo, FontStyles.Bold);
        UIFactory.Texto(celConteudo, "Últimas movimentações", 18, UIFactory.CorSuave, FontStyles.Bold);
        var lancs = LivroCaixa.lancamentos;
        if (lancs.Count == 0) { UIFactory.Texto(celConteudo, "Nenhuma movimentação ainda.", 18, UIFactory.CorSuave); return; }
        for (int i = lancs.Count - 1, n = 0; i >= 0 && n < 10; i--, n++)
        {
            var l = lancs[i];
            string cor = l.valor >= 0 ? "#3CCB7F" : "#F2545B";
            UIFactory.Texto(celConteudo, $"{l.categoria}  <color={cor}><b>{(l.valor >= 0 ? "+" : "")}{UIFactory.Dinheiro(l.valor)}</b></color>", 18, UIFactory.CorTexto);
        }
    }

    // A cada quadro: relógio da barra de status e notificação de mensagem nova (vibra)
    void AtualizarCelularFundo()
    {
        if (celHora != null && DiaManager.Instance != null) celHora.text = DiaManager.Instance.HoraTexto();
        var mm = MissionManager.Instance;
        if (mm == null) return;
        foreach (var c in mm.chamadosDisponiveis)
        {
            if (celVistos.Contains(c)) continue;
            celVistos.Add(c);
            if (Time.timeSinceLevelLoad < 3f) continue;   // os do começo do dia não fazem barulho todos de uma vez
            SomManager.Tocar("bip", 0.4f, 1.3f);
            Toast($"Celular: nova mensagem de {c.contato} ({c.nomeCliente}). Aperte <b>C</b>.", UIFactory.Destaque, 3.5f);
        }
    }

    // Etapa 10: analisador de Wi-Fi — sinal aqui, redes vizinhas e canais, medição da sala (mapa de calor) e teste
    // de visitante
    void CelWiFi()
    {
        UIFactory.Texto(celConteudo, "Wi-Fi", 30, UIFactory.CorTexto, FontStyles.Bold);
        var mm = MissionManager.Instance;
        var e = mm?.Estado;
        if (e == null) { UIFactory.Texto(celConteudo, "Nenhum atendimento em andamento.", 18, UIFactory.CorSuave); return; }
        var jog = GameObject.FindWithTag("Player");
        var ap = AccessPointCliente.Transform;
        bool naSala = jog != null && ap != null && SalaCliente.Atual != null &&
                      Mathf.Abs(AccessPointCliente.Local(jog.transform.position).x) < AccessPointCliente.LarguraSala / 2f &&
                      Mathf.Abs(AccessPointCliente.Local(jog.transform.position).z) < AccessPointCliente.ProfundidadeSala / 2f;
        float aqui = SinalWiFi.SemSinal;
        if (naSala)
        {
            Vector3 p = jog.transform.position;
            aqui = SinalWiFi.Em(new Vector3(p.x, ap.position.y + SinalWiFi.AlturaMedicao, p.z), e);
            var cor = SinalWiFi.Cor(aqui);
            UIFactory.Texto(celConteudo, $"Sinal aqui: <b>{aqui:0} dBm</b>  <color=#{ColorUtility.ToHtmlStringRGB(cor)}>({SinalWiFi.Qualidade(aqui)})</color>", 22, UIFactory.CorTexto);
            UIFactory.Texto(celConteudo, $"<size=80%>Meta para voz e vídeo: {EstadoEquipamentos.MetaDbm:0} dBm ou mais em toda a sala.</size>", 16, UIFactory.CorSuave);
        }
        else UIFactory.Texto(celConteudo, "Fora da sala do cliente.", 18, UIFactory.CorSuave);

        if (SurveyWiFi.Medindo)
            UIFactory.Botao(celConteudo, "Parar a medição", () => { SurveyWiFi.Instance.Parar(); AtualizarCelular(); }, UIFactory.Perigo, 360, 54, 19);
        else
            UIFactory.Botao(celConteudo, "Medir a sala (mapa de calor)", () =>
            {
                string r = SurveyWiFi.Garantir().Comecar();
                if (SurveyWiFi.Medindo) FecharModal();
                Toast(r, SurveyWiFi.Medindo ? UIFactory.Destaque : UIFactory.Alerta, 5f);
            }, UIFactory.Destaque, 360, 54, 19);

        foreach (var (tex, nome) in new[] { (e.fotoAntes, "Antes"), (e.fotoDepois, "Depois") })
        {
            if (tex == null || tex.name != "MapaDeCalor") continue;   // a foto do rack não entra aqui
            UIFactory.Texto(celConteudo, $"Mapa de calor — {nome}  <size=80%>(branco: roteador · azul: AP)</size>", 17, UIFactory.CorSuave, FontStyles.Bold);
            var img = new GameObject("Mapa", typeof(RectTransform), typeof(RawImage));
            img.transform.SetParent(celConteudo, false);
            img.GetComponent<RawImage>().texture = tex;
            var le = img.AddComponent<LayoutElement>();
            le.preferredWidth = 360; le.preferredHeight = 288;
        }

        bool temVisitantes = e.wifiServico ? e.apRedeVisitantes : e.WiFiNoRoteador && e.rotRedeVisitantes;
        if (temVisitantes)
            UIFactory.Botao(celConteudo, "Entrar na rede de visitantes e tentar abrir a impressora", () =>
            {
                string r = e.TestarComoVisitante();
                mm.RegistrarAcao("Teste na rede de visitantes");
                Registrar(r);
                Toast(r, UIFactory.CorTexto, 6f);
                mm.Verificar();
                AtualizarCelular();
            }, UIFactory.CartaoClaro, 360, 64, 17);

        UIFactory.Texto(celConteudo, "Redes por perto", 20, UIFactory.CorSuave, FontStyles.Bold);
        foreach (var r in e.RedesVisiveis(aqui, mm.chamadoAtivo?.nomeCliente))
            UIFactory.Texto(celConteudo, r, 16, UIFactory.CorTexto);
    }
}
