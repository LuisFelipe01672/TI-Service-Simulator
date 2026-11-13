using System.Collections.Generic;
using UnityEngine;

// Uma decisão que o jogador precisa tomar (aba Empresa): duas opções, prazo para responder.
[System.Serializable]
public class Decisao
{
    public int id;
    public string tipo;          // "aumento", "proposta-concorrente"...
    public string titulo, texto, opcaoA, opcaoB;
    public string alvo;          // nome do funcionário/cliente envolvido
    public float valor;
    public int expiraNoDia;
}

// Imprevistos do dia a dia de uma empresa de TI: atrasos de pagamento, calote, reajuste de fornecedor,
// promoção de equipamento, indicação de cliente, funcionário pedindo aumento, notebook quebrado...
// No máximo um por dia útil, com intervalo entre eles.
public static class EventosAleatorios
{
    public static float chancePorDia = 0.12f;
    public const int IntervaloMinimo = 4;

    public static List<Decisao> decisoes = new List<Decisao>();
    public static int ultimoEvento = -99, proximoId = 1;
    public static int promocaoAteDia = -1, reajusteAteDia = -1;

    public const float DescontoPromocao = 0.15f, Reajuste = 0.08f;

    static int Hoje => DiaManager.Instance != null ? DiaManager.Instance.dia : 1;
    static void Avisar(string t, Color c, float d = 6f) => GameUI.Instance?.Toast(t, c, d);

    // Multiplicadores usados pelos outros sistemas
    public static float MultEquipamento => Hoje <= promocaoAteDia ? 1f - DescontoPromocao : 1f;
    public static float MultLicencas => Hoje <= reajusteAteDia ? 1f + Reajuste : 1f;

    public static void Iniciar() { decisoes = new List<Decisao>(); ultimoEvento = -99; proximoId = 1; promocaoAteDia = reajusteAteDia = -1; }

    public static void NovoDia(int dia)
    {
        // Decisões sem resposta: vale a opção B (a "pior" por omissão)
        foreach (var d in new List<Decisao>(decisoes))
            if (dia > d.expiraNoDia) { Avisar($"Você não respondeu: <b>{d.titulo}</b>.", UIFactory.Alerta, 4f); Decidir(d, false); }

        var data = DiaManager.DataDoDia(dia);
        if (data.DayOfWeek == System.DayOfWeek.Saturday || data.DayOfWeek == System.DayOfWeek.Sunday) return;
        if (dia - ultimoEvento < IntervaloMinimo || Random.value >= chancePorDia) return;

        var possiveis = new List<int>();
        var emp = EmpresaManager.Instance;
        if (emp != null && emp.recebiveis.Exists(r => !r.pago && r.forma == "Boleto")) { possiveis.Add(0); possiveis.Add(1); }
        if (emp != null && emp.TemCnpj) { possiveis.Add(2); possiveis.Add(3); possiveis.Add(4); }
        if (FuncionarioManager.Instance != null && FuncionarioManager.Instance.equipe.Count > 0) possiveis.Add(5);
        if (ServicosManager.Instance != null && ServicosManager.Instance.contratos.Exists(c => c.ativo && c.Implantado && c.tipo == TipoServico.Email)) possiveis.Add(6);
        float rep = ReputationManager.Instance != null ? ReputationManager.Instance.GetReputacao() : 0f;
        if (rep >= 60f) possiveis.Add(7);
        possiveis.Add(8);
        if (possiveis.Count == 0) return;

        ultimoEvento = dia;
        Disparar(possiveis[Random.Range(0, possiveis.Count)], dia);
    }

    static void Disparar(int tipo, int dia)
    {
        var emp = EmpresaManager.Instance;
        switch (tipo)
        {
            case 0:   // atraso de boleto
            {
                var r = emp.recebiveis.Find(x => !x.pago && x.forma == "Boleto");
                r.diaPrevisto += 15;
                Avisar($"<b>{r.cliente}</b> avisou que vai atrasar o boleto de {UIFactory.Dinheiro(r.valor)}: paga em {DiaManager.DataDoDia(r.diaPrevisto):dd/MM}.", UIFactory.Alerta);
                break;
            }
            case 1:   // calote (só valores pequenos)
            {
                var r = emp.recebiveis.Find(x => !x.pago && x.forma == "Boleto" && x.valor <= 800f);
                if (r == null) goto case 0;
                emp.recebiveis.Remove(r);
                Avisar($"<b>Calote:</b> {r.cliente} fechou as portas e não vai pagar {UIFactory.Dinheiro(r.valor)}. Faz parte do risco do boleto.", UIFactory.Perigo);
                break;
            }
            case 2:   // promoção na distribuidora
                promocaoAteDia = dia + 3;
                Avisar($"<b>Promoção na distribuidora!</b> Impressoras, notebooks e mini PCs {DescontoPromocao * 100:0}% mais baratos até {DiaManager.DataDoDia(promocaoAteDia):dd/MM}.", UIFactory.Sucesso);
                break;
            case 3:   // reajuste de licenças
                reajusteAteDia = dia + 60;
                Avisar($"<b>Reajuste:</b> os fornecedores de licenças e nuvem subiram {Reajuste * 100:0}% (dólar alto) até {DiaManager.DataDoDia(reajusteAteDia):dd/MM}. Sua margem nos serviços cai.", UIFactory.Alerta);
                break;
            case 4:   // indicação
            {
                var cm = ContratosManager.Instance;
                if (cm != null && cm.Liberado && Random.value < 0.5f)
                {
                    cm.leads.Add(new Lead
                    {
                        id = cm.proximoId++, empresa = CatalogoLocacao.SortearEmpresa(), paginasMes = Random.Range(10, 60) * 50,
                        fracCor = Random.value < 0.5f ? 0f : 0.15f, disposicao = 1.35f, expiraNoDia = dia + 7
                    });
                    Avisar("<b>Indicação!</b> Um cliente satisfeito indicou você para um conhecido que quer alugar impressora (aba Contratos).", UIFactory.Sucesso);
                }
                else Avisar("<b>Indicação!</b> Um cliente falou bem de você num grupo de empresários: mais gente vai procurar a empresa.", UIFactory.Sucesso);
                ReputationManager.Instance?.AlterarReputacao(1f);
                break;
            }
            case 5:   // funcionário pede aumento
            {
                var f = FuncionarioManager.Instance.equipe[Random.Range(0, FuncionarioManager.Instance.equipe.Count)];
                float aumento = Mathf.Round(f.salarioMensal * 0.1f);
                decisoes.Add(new Decisao
                {
                    id = proximoId++, tipo = "aumento", alvo = f.nome, valor = aumento, expiraNoDia = dia + 3,
                    titulo = $"{f.nome} pediu aumento",
                    texto = $"Recebeu proposta de outra empresa e pede +{UIFactory.Dinheiro(aumento)}/mês. Se você negar, pode pedir demissão.",
                    opcaoA = $"Dar aumento (+{UIFactory.Dinheiro(aumento)})", opcaoB = "Negar"
                });
                Avisar($"<b>{f.nome}</b> quer conversar sobre salário (aba Empresa, decisões).", UIFactory.Alerta);
                break;
            }
            case 6:   // phishing no cliente de e-mail
            {
                var c = ServicosManager.Instance.contratos.Find(x => x.ativo && x.Implantado && x.tipo == TipoServico.Email);
                ServicosManager.Instance.AdicionarAlerta(c, "Golpe de phishing", "Um usuário clicou num e-mail falso. Trocar a senha, revisar regras da caixa e avisar a equipe do cliente.", 40, critico: true);
                Avisar($"<b>Phishing na {c.empresa}!</b> Alerta crítico na central.", UIFactory.Perigo);
                break;
            }
            case 7:   // reportagem
                ReputationManager.Instance?.AlterarReputacao(3f);
                Avisar("<b>Saiu no jornal!</b> Uma reportagem sobre empresas de tecnologia da cidade citou a sua (+3 reputação).", UIFactory.Sucesso);
                break;
            default:  // notebook do técnico quebrou
            {
                float conserto = 600f;
                GameEconomy.Instance?.Gastar(conserto, LivroCaixa.Consertos);
                Avisar($"Seu notebook de trabalho caiu da mochila e a tela trincou: conserto de {UIFactory.Dinheiro(conserto)}.", UIFactory.Perigo);
                break;
            }
        }
    }

    public static string Decidir(Decisao d, bool opcaoA)
    {
        decisoes.Remove(d);
        if (d.tipo == "aumento")
        {
            var fm = FuncionarioManager.Instance;
            var f = fm != null ? fm.equipe.Find(x => x.nome == d.alvo) : null;
            if (f == null) return "Essa pessoa não está mais na equipe.";
            if (opcaoA) { f.salarioMensal += d.valor; return $"{f.nome} ficou feliz e continua no time. Novo salário: {UIFactory.Dinheiro(f.salarioMensal)}."; }
            if (Random.value < 0.4f)
            {
                fm.equipe.Remove(f);
                Avisar($"<b>{f.nome} pediu demissão</b> e foi para a concorrência.", UIFactory.Perigo);
                return $"{f.nome} pediu demissão.";
            }
            return $"{f.nome} ficou chateado, mas continua no time.";
        }
        return "";
    }

    [System.Serializable]
    public class Dados
    {
        public List<Decisao> decisoes = new List<Decisao>();
        public int ultimoEvento = -99, proximoId = 1, promocaoAteDia = -1, reajusteAteDia = -1;
    }

    public static Dados Exportar() => new Dados { decisoes = new List<Decisao>(decisoes), ultimoEvento = ultimoEvento, proximoId = proximoId, promocaoAteDia = promocaoAteDia, reajusteAteDia = reajusteAteDia };
    public static void Carregar(Dados d)
    {
        if (d == null) { Iniciar(); return; }
        decisoes = d.decisoes ?? new List<Decisao>(); ultimoEvento = d.ultimoEvento; proximoId = Mathf.Max(1, d.proximoId);
        promocaoAteDia = d.promocaoAteDia; reajusteAteDia = d.reajusteAteDia;
    }
}
