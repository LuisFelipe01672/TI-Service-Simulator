using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum TipoProjeto { Cabeamento, WiFi, Rack, Firewall }

public class InfoProjeto
{
    public TipoProjeto tipo;
    public string nome, unidade, descricao;
    public float materialPorUnidade, materialFixo;   // custo de atacado (aprox. 2026)
    public float horasPorUnidade, horasFixas;
    public int minU, maxU;
    public bool comodato;                             // material fica com a empresa (firewall): cliente não paga o equipamento
}

[System.Serializable]
public class Projeto
{
    public int id;
    public TipoProjeto tipo;
    public string empresa;
    public int tamanhoEstimado, tamanhoReal, tamanhoOrcado;
    public bool levantado;
    public float disposicao;
    public int expiraNoDia;

    public int etapa;               // 0 contato, 2 aprovado (comprar material), 3 material a caminho, 4 em execução, 5 concluído
    public float valorMaterial, valorMaoDeObra, valorTotal;
    public float custoMaterial;
    public float horasTotais, horasFeitas;
    public int diaMaterial, prazoDia;
    public bool avisouAtraso;
    public bool semEntrada;         // órgão público: paga tudo na entrega

    public InfoProjeto Info => ProjetosManager.Info(tipo);
}

// Projetos de infraestrutura: cabeamento estruturado, Wi-Fi corporativo, organização de rack e firewall.
// Fluxo: contato → levantamento no local (opcional, 1 h) → orçamento (material × margem + horas) → entrada de 50%
// → compra do material → execução (você e a equipe) → entrega e restante. Firewall vira mensalidade (comodato).
public class ProjetosManager : MonoBehaviour
{
    public static ProjetosManager Instance;

    public const float ValorHoraProjeto = 90f;     // hora de projeto (mais barata que a hora técnica avulsa)
    public const float HorasPorDiaFuncionario = 3f;
    public const int MesesFirewall = 36;
    public static readonly float[] Margens = { 1.5f, 1.7f, 2.0f };

    public static readonly InfoProjeto[] Tipos =
    {
        new InfoProjeto { tipo = TipoProjeto.Cabeamento, nome = "Cabeamento estruturado", unidade = "ponto de rede",
            descricao = "Passar cabo CAT6, crimpar, montar tomadas e patch panel, testar ponto a ponto. Precisa de alicate e testador.",
            materialPorUnidade = 160f, materialFixo = 300f, horasPorUnidade = 0.75f, horasFixas = 2f, minU = 6, maxU = 40 },
        new InfoProjeto { tipo = TipoProjeto.WiFi, nome = "Wi-Fi corporativo", unidade = "access point",
            descricao = "Site survey, access points no teto com cabo e PoE, rede de visitantes separada.",
            materialPorUnidade = 860f, materialFixo = 900f, horasPorUnidade = 1.5f, horasFixas = 2f, minU = 2, maxU = 8 },
        new InfoProjeto { tipo = TipoProjeto.Rack, nome = "Organização de rack", unidade = "rack",
            descricao = "Rack 12U, patch panel, organizadores, switch gerenciável e identificação de todos os cabos.",
            materialPorUnidade = 2860f, materialFixo = 0f, horasPorUnidade = 6f, horasFixas = 2f, minU = 1, maxU = 2 },
        new InfoProjeto { tipo = TipoProjeto.Firewall, nome = "Firewall", unidade = "firewall",
            descricao = "Instalar o firewall, criar regras, VPN e filtro de conteúdo. O equipamento fica em comodato e o cliente paga mensalidade.",
            materialPorUnidade = 3800f, materialFixo = 0f, horasPorUnidade = 6f, horasFixas = 2f, minU = 1, maxU = 2, comodato = true },
    };

    public static InfoProjeto Info(TipoProjeto t) => Tipos[(int)t];

    public List<Projeto> projetos = new List<Projeto>();
    public int proximoId = 1;
    public int concluidos;

    [Header("Ajustes")]
    public float chanceContatoPorDia = 0.15f;

    EventBinding<DiaIniciadoEvent> bDia;

    // ---------- Criação automática ----------

    static bool registrado;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (!registrado)
        {
            SceneManager.sceneLoaded += (_, __) => CriarSePrecisar();
            registrado = true;
        }
        CriarSePrecisar();
    }

    static void CriarSePrecisar()
    {
        if (Instance != null) return;
        if (FindAnyObjectByType<MissionManager>() == null) return;
        new GameObject("ProjetosManager").AddComponent<ProjetosManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (dadosSalvos != null) { Carregar(dadosSalvos); dadosSalvos = null; }
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void OnEnable()
    {
        bDia = new EventBinding<DiaIniciadoEvent>(e => NovoDia(e.dia));
        EventBus<DiaIniciadoEvent>.Register(bDia);
    }

    void OnDisable() => EventBus<DiaIniciadoEvent>.Deregister(bDia);

    static int Hoje => DiaManager.Instance != null ? DiaManager.Instance.dia : 1;
    static void Avisar(string texto, Color cor, float dur = 4f) => GameUI.Instance?.Toast(texto, cor, dur);

    // ---------- Liberação ----------

    public static bool Liberado(TipoProjeto t, out string motivo)
    {
        motivo = null;
        var emp = EmpresaManager.Instance;
        if (emp != null && !emp.TemCnpj) { motivo = "Projetos são para empresas: precisa de CNPJ."; return false; }
        if (t == TipoProjeto.Firewall && !Parcerias.Ativa(Parcerias.Firewall))
        { motivo = $"Precisa da parceria com a {Parcerias.Lista[Parcerias.Firewall].nome}."; return false; }
        return true;
    }

    // ---------- Contas do orçamento ----------

    public static float CustoMaterialBase(TipoProjeto t, int tamanho) { var i = Info(t); return i.materialFixo + i.materialPorUnidade * tamanho; }
    public static float Horas(TipoProjeto t, int tamanho) { var i = Info(t); return i.horasFixas + i.horasPorUnidade * tamanho; }

    // O que você paga pelo material agora (varejo/atacado/nível de parceria)
    public static float CustoMaterialReal(TipoProjeto t, int tamanho)
    {
        float mult = t == TipoProjeto.Firewall ? 1f - Parcerias.Desconto(Parcerias.Firewall) : Parcerias.MultMaterialInfra;
        return Mathf.Round(CustoMaterialBase(t, tamanho) * mult);
    }

    public int TamanhoParaOrcar(Projeto p) => p.levantado ? p.tamanhoReal : p.tamanhoEstimado;

    public static void Orcamento(TipoProjeto t, int tamanho, int margem, out float material, out float maoDeObra, out float total)
    {
        material = Info(t).comodato ? 0f : Mathf.Round(CustoMaterialBase(t, tamanho) * Margens[Mathf.Clamp(margem, 0, Margens.Length - 1)]);
        maoDeObra = Mathf.Round(Horas(t, tamanho) * ValorHoraProjeto);
        total = material + maoDeObra;
    }

    // Preço que o mercado considera justo (margem padrão de 1,7)
    public static float PrecoJusto(TipoProjeto t, int tamanho)
    {
        Orcamento(t, tamanho, 1, out _, out _, out float total);
        return total;
    }

    // ---------- Ações ----------

    public string Levantar(Projeto p)
    {
        if (p.etapa != 0) return "O levantamento é antes do orçamento.";
        if (p.levantado) return "Você já fez o levantamento.";
        if (!ServicosManager.GastarTempo(60f, out string motivo)) return motivo;
        p.levantado = true;
        return $"Levantamento na <b>{p.empresa}</b>: são <b>{p.tamanhoReal} {Plural(p.Info.unidade, p.tamanhoReal)}</b>" +
               (p.tamanhoReal != p.tamanhoEstimado ? $" (o cliente tinha falado em {p.tamanhoEstimado})." : ", como o cliente disse.");
    }

    public string EnviarOrcamento(Projeto p, int margem)
    {
        if (p.etapa != 0) return "Orçamento já enviado.";
        if (!Liberado(p.tipo, out string motivo)) return motivo;
        int tam = TamanhoParaOrcar(p);
        Orcamento(p.tipo, tam, margem, out float material, out float mao, out float total);

        float justo = PrecoJusto(p.tipo, p.tamanhoReal);
        float rep = ReputationManager.Instance != null ? ReputationManager.Instance.GetReputacao() : 30f;
        float limite = justo * p.disposicao * (1f + (rep - 50f) / 500f);
        var rival = Concorrencia.Sortear(AreaNegocio.Infraestrutura);
        float oferta = Concorrencia.Oferta(rival, justo);

        if (total > limite)
        {
            Concorrencia.Registrar(rival, false);
            projetos.Remove(p);
            return $"<b>{p.empresa}</b> achou o orçamento caro ({UIFactory.Dinheiro(total)}) e fechou com a {rival.nome}.";
        }
        if (total > oferta * (Concorrencia.VantagemJogador(rep) + FocoEmpresa.Vantagem(AreaNegocio.Infraestrutura)))
        {
            Concorrencia.Registrar(rival, false);
            projetos.Remove(p);
            return $"<b>{p.empresa}</b> fechou com a {rival.nome} por {UIFactory.Dinheiro(oferta)} (o seu saiu {UIFactory.Dinheiro(total)}).";
        }
        Concorrencia.Registrar(rival, true);

        p.etapa = 2;
        p.tamanhoOrcado = tam;
        p.valorMaterial = material; p.valorMaoDeObra = mao; p.valorTotal = total;
        p.horasTotais = Horas(p.tipo, tam) * FocoEmpresa.MultHorasProjeto;
        p.prazoDia = Hoje + Mathf.CeilToInt(p.horasTotais / 6f) + 5;
        Cobrar(p, Mathf.Round(total * 0.5f), "Entrada (50%)");
        return $"Aprovado! <b>{p.empresa}</b> — {p.Info.nome}, {UIFactory.Dinheiro(total)}. Entrada de 50% faturada. " +
               $"Compre o material; prazo de entrega {DiaManager.DataDoDia(p.prazoDia):dd/MM}.";
    }

    public string ComprarMaterial(Projeto p)
    {
        if (p.etapa != 2) return "Material já comprado.";
        float custo = CustoMaterialReal(p.tipo, p.tamanhoReal);
        var eco = GameEconomy.Instance;
        if (eco != null && eco.saldo < custo) return $"Saldo insuficiente: o material custa {UIFactory.Dinheiro(custo)}.";
        eco?.Gastar(custo, LivroCaixa.Material);
        p.custoMaterial = custo;
        int dias = p.tipo == TipoProjeto.Firewall ? 3 : Parcerias.Ativa(Parcerias.Infra) ? 1 : 2;
        p.diaMaterial = Hoje + dias;
        p.etapa = 3;

        // Sem levantamento, a medição real aparece agora: o que faltar sai do seu bolso
        string surpresa = "";
        p.horasTotais = Horas(p.tipo, p.tamanhoReal) * FocoEmpresa.MultHorasProjeto;
        if (p.tamanhoReal > p.tamanhoOrcado)
            surpresa = $" <color=#F5B83F>Medindo no local: são {p.tamanhoReal}, não {p.tamanhoOrcado}. Material e horas a mais por sua conta!</color>";
        return $"Material comprado ({UIFactory.Dinheiro(custo)}). Chega em {DiaManager.DataDoDia(p.diaMaterial):dd/MM}." + surpresa;
    }

    // Você trabalha 2 horas no projeto
    public string Trabalhar(Projeto p)
    {
        if (p.etapa != 4) return p.etapa == 3 ? "O material ainda não chegou." : "Nada para fazer agora.";
        if (p.tipo == TipoProjeto.Cabeamento && Mochila.Instance != null &&
            (Mochila.Instance.Quantidade(Item.AlicateCrimpagem) <= 0 || Mochila.Instance.Quantidade(Item.TestadorCabo) <= 0))
            return "Para cabeamento você precisa do alicate de crimpagem e do testador de cabo na mochila (Loja).";
        if (!ServicosManager.GastarTempo(120f, out string motivo)) return motivo;
        p.horasFeitas += 2f;
        if (p.horasFeitas >= p.horasTotais) return Concluir(p);
        return $"Mais 2 h na <b>{p.empresa}</b>: {Progresso(p) * 100:0}% pronto.";
    }

    // Projeto já contratado (licitação): pula o orçamento, vai direto para a compra do material
    public Projeto CriarProjetoContratado(TipoProjeto t, string empresa, int tamanho, float valor)
    {
        var p = new Projeto
        {
            id = proximoId++, tipo = t, empresa = empresa, tamanhoEstimado = tamanho, tamanhoReal = tamanho, tamanhoOrcado = tamanho,
            levantado = true, etapa = 2, valorTotal = valor, semEntrada = true,
            horasTotais = Horas(t, tamanho) * FocoEmpresa.MultHorasProjeto
        };
        p.prazoDia = Hoje + Mathf.CeilToInt(p.horasTotais / 6f) + 10;
        projetos.Add(p);
        return p;
    }

    public static float Progresso(Projeto p) => p.horasTotais <= 0f ? 0f : Mathf.Clamp01(p.horasFeitas / p.horasTotais);

    string Concluir(Projeto p)
    {
        p.etapa = 5;
        concluidos++;
        if (p.semEntrada) Cobrar(p, p.valorTotal, "Entrega e medição final");
        else Cobrar(p, p.valorTotal - Mathf.Round(p.valorTotal * 0.5f), "Entrega (50%)");
        bool noPrazo = Hoje <= p.prazoDia;
        ReputationManager.Instance?.AlterarReputacao(noPrazo ? 2f : -1f);
        Conquistas.Desbloquear("projeto");

        string extra = "";
        if (p.tipo == TipoProjeto.Firewall && ServicosManager.Instance != null)
        {
            ServicosManager.Instance.CriarContratoImplantado(TipoServico.Firewall, p.empresa, p.tamanhoReal, MesesFirewall,
                CatalogoServicos.MensalFirewall * p.tamanhoReal);
            Conquistas.Desbloquear("firewall");
            extra = $" Firewall gerenciado ativo: {UIFactory.Dinheiro(CatalogoServicos.MensalFirewall * p.tamanhoReal)}/mês por {MesesFirewall} meses.";
        }
        else if (Random.value < 0.5f && ServicosManager.Instance != null && ServicosManager.Instance.Liberado(TipoServico.Suporte, out _))
        {
            // Cliente satisfeito com a rede nova pede suporte mensal
            ServicosManager.Instance.leads.Add(new LeadServico
            {
                id = ServicosManager.Instance.proximoId++, tipo = TipoServico.Suporte, empresa = p.empresa,
                unidades = Mathf.Clamp(p.tamanhoReal, 3, 25), disposicao = 1.2f, expiraNoDia = Hoje + 7
            });
            extra = " Gostaram tanto que pediram proposta de suporte mensal (aba Contratos)!";
        }

        float lucro = p.valorTotal - p.custoMaterial;
        return $"<b>Projeto entregue na {p.empresa}!</b> {(noPrazo ? "No prazo (+2 reputação)." : "Atrasado (-1 reputação).")} " +
               $"Recebido no total {UIFactory.Dinheiro(p.valorTotal)}, material {UIFactory.Dinheiro(p.custoMaterial)} → sobra {UIFactory.Dinheiro(lucro)} pela mão de obra.{extra}";
    }

    void Cobrar(Projeto p, float valor, string parcela)
    {
        if (valor <= 0f) return;
        var ch = new Chamado(p.empresa, $"{p.Info.nome} — {parcela}", TipoChamado.Rede, DificuldadeChamado.Facil, valor, 0f) { exigeNota = true };
        if (EmpresaManager.Instance != null) EmpresaManager.Instance.RegistrarServico(ch, valor, true);
        else GameEconomy.Instance?.ReceberPagamento(valor);
    }

    static string Plural(string unidade, int n)
    {
        if (n == 1) return unidade;
        if (unidade.StartsWith("ponto")) return "pontos de rede";
        if (unidade.StartsWith("access")) return "access points";
        return unidade + "s";
    }

    public static string Descrever(TipoProjeto t, int n) => $"{n} {Plural(Info(t).unidade, n)}";

    // ---------- Dia a dia ----------

    void NovoContato()
    {
        var possiveis = new List<TipoProjeto>();
        foreach (var i in Tipos) if (Liberado(i.tipo, out _)) possiveis.Add(i.tipo);
        if (possiveis.Count == 0) return;
        var t = possiveis[Random.Range(0, possiveis.Count)];
        var info = Info(t);
        int real = Random.Range(info.minU, info.maxU + 1);
        // O cliente costuma errar a conta (para menos, na maioria das vezes)
        int estimado = info.maxU <= 2 ? real : Mathf.Max(info.minU, Mathf.RoundToInt(real * Random.Range(0.65f, 1.1f)));
        var p = new Projeto
        {
            id = proximoId++, tipo = t, empresa = CatalogoServicos.SortearEmpresa(),
            tamanhoEstimado = estimado, tamanhoReal = real, disposicao = Random.Range(0.92f, 1.3f), expiraNoDia = Hoje + 7
        };
        projetos.Add(p);
        Avisar($"Pedido de orçamento: <b>{p.empresa}</b> quer {info.nome.ToLower()} (uns {Descrever(t, estimado)}). Aba Negócios.", UIFactory.Destaque);
    }

    void NovoDia(int dia)
    {
        Parcerias.NovoDia(dia);
        ProdutosProprios.NovoDia(dia);
        Licitacoes.NovoDia(dia);

        projetos.RemoveAll(p => p.etapa == 0 && p.expiraNoDia < dia);
        projetos.RemoveAll(p => p.etapa == 5 && dia - p.prazoDia > 15);

        var emp = EmpresaManager.Instance;
        int abertos = projetos.FindAll(p => p.etapa == 0).Count;
        if ((emp == null || emp.TemCnpj) && abertos < 3 && Random.value < chanceContatoPorDia * Concorrencia.FatorDemanda() * FocoEmpresa.MultLeads(AreaNegocio.Infraestrutura)) NovoContato();

        var data = DiaManager.DataDoDia(dia);
        bool util = data.DayOfWeek != System.DayOfWeek.Saturday && data.DayOfWeek != System.DayOfWeek.Sunday;
        int equipe = FuncionarioManager.Instance != null ? FuncionarioManager.Instance.equipe.Count : 0;
        var emExecucao = projetos.FindAll(p => p.etapa == 4);

        foreach (var p in projetos)
        {
            if (p.etapa == 3 && dia >= p.diaMaterial)
            {
                p.etapa = 4;
                Avisar($"Material da <b>{p.empresa}</b> chegou. Mão na massa!", UIFactory.Sucesso);
            }
            if (p.etapa >= 2 && p.etapa < 5 && dia > p.prazoDia && !p.avisouAtraso)
            {
                p.avisouAtraso = true;
                Avisar($"<b>{p.empresa}</b> está cobrando: o projeto passou do prazo!", UIFactory.Perigo);
            }
        }

        // A equipe toca os projetos em execução nos dias úteis
        if (util && equipe > 0 && emExecucao.Count > 0)
        {
            float horas = equipe * HorasPorDiaFuncionario / emExecucao.Count;
            foreach (var p in emExecucao)
            {
                p.horasFeitas += horas;
                if (p.horasFeitas >= p.horasTotais) Avisar(Concluir(p), UIFactory.Sucesso, 6f);
            }
        }
    }

    // ---------- Save ----------

    [System.Serializable]
    public class Dados
    {
        public List<Projeto> projetos = new List<Projeto>();
        public int proximoId = 1, concluidos;
    }

    public static Dados dadosSalvos;

    public Dados Exportar() => new Dados { projetos = new List<Projeto>(projetos), proximoId = proximoId, concluidos = concluidos };

    public void Carregar(Dados d)
    {
        if (d == null) { NovoJogo(); return; }
        projetos = d.projetos ?? new List<Projeto>();
        proximoId = Mathf.Max(1, d.proximoId); concluidos = d.concluidos;
    }

    public void NovoJogo() { projetos.Clear(); proximoId = 1; concluidos = 0; }
}
