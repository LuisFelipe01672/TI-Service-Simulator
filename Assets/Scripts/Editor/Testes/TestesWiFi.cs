using System.Linq;
using NUnit.Framework;
using UnityEngine;

// Etapa 10: Wi-Fi corporativo — modelo de sinal, visita do projeto (AP no teto, redes, visitantes, medição),
// defeitos de Wi-Fi do roteador e o andamento do projeto.
public class TestesWiFi
{
    static AcaoFisica Acao(TipoEquipamento t, EstadoEquipamentos e, string inicio) =>
        AcoesFisicas.Para(t, e).FirstOrDefault(a => a.rotulo.StartsWith(inicio));

    static EstadoEquipamentos Visita()
    {
        var e = new EstadoEquipamentos();
        CatalogoProblemas.Quebrar(CatalogoProblemas.InstalacaoWiFi, e);
        return e;
    }

    // Pior distância (do teto até o celular na mão) para um AP na posição (x, z) da sala de 10 x 8 m
    static float PiorSinalAp(float x, float z)
    {
        float pior = 0f;
        float dy = 2.8f - SinalWiFi.AlturaMedicao;
        for (int l = 0; l < SurveyWiFi.Linhas; l++)
            for (int c = 0; c < SurveyWiFi.Colunas; c++)
            {
                Vector2 p = SurveyWiFi.CentroLocal(c, l);
                float d = new Vector3(p.x - x, dy, p.y - z).magnitude;
                pior = Mathf.Min(pior, SinalWiFi.Calcular(SinalWiFi.TxAp, d, 0f));
            }
        return pior;
    }

    [Test]
    public void Sinal_PerdeComADistancia()
    {
        Assert.Greater(SinalWiFi.Calcular(SinalWiFi.TxAp, 1f, 0f), SinalWiFi.Calcular(SinalWiFi.TxAp, 4f, 0f));
        Assert.Greater(SinalWiFi.Calcular(SinalWiFi.TxAp, 4f, 0f), SinalWiFi.Calcular(SinalWiFi.TxAp, 4f, 6f), "móvel no caminho tira sinal");
        Assert.AreEqual("bom", SinalWiFi.Qualidade(-60f));
        Assert.AreEqual("fraco", SinalWiFi.Qualidade(-70f));
    }

    [Test]
    public void Sinal_ApNoMeioCobreECantoNao()
    {
        Assert.GreaterOrEqual(PiorSinalAp(0f, 0f), EstadoEquipamentos.MetaDbm + 3f, "AP no meio do teto cobre a sala com folga");
        float cantoX = AccessPointCliente.LarguraSala / 2f - AccessPointCliente.FolgaParede;
        float cantoZ = AccessPointCliente.ProfundidadeSala / 2f - AccessPointCliente.FolgaParede;
        Assert.Less(PiorSinalAp(cantoX, cantoZ), EstadoEquipamentos.MetaDbm, "AP num canto deixa o canto oposto fraco");
        // O roteador da operadora numa mesinha encostada na parede não chega bem no canto oposto
        float dRot = new Vector3(9f, 0.6f, 6f).magnitude;
        Assert.Less(SinalWiFi.Calcular(SinalWiFi.TxRoteador, dRot, 0f), EstadoEquipamentos.MetaDbm);
    }

    [Test]
    public void Visita_ComecaSemResolverEComMedicao()
    {
        var e = Visita();
        Assert.IsTrue(e.wifiServico);
        Assert.IsFalse(CatalogoProblemas.Resolvido(CatalogoProblemas.InstalacaoWiFi, e));
        Assert.Contains("medir a sala antes (celular » Wi-Fi)", e.PendenciasWiFi());
        // Sem a medição antes não se escolhe o lugar
        StringAssert.Contains("meça a sala", Acao(TipoEquipamento.AccessPoint, e, "Marcar aqui").executar());
        Assert.IsFalse(e.apMarcado);
        Assert.IsTrue(new[] { 1, 6, 11 }.Contains(e.canalVizinhos));
    }

    [Test]
    public void Visita_CompletaResolve()
    {
        var e = Visita();
        e.RegistrarSurvey(new float[80], -74f, 12, 60);
        Assert.IsTrue(e.surveyAntes);
        Assert.IsFalse(e.surveyDepois);
        e.apMarcado = e.apSuporte = e.apCabo = true;
        Assert.AreEqual(Item.AlicateCrimpagem, Acao(TipoEquipamento.AccessPoint, e, "Crimpar a ponta").requer);
        e.apCrimpado = true; e.apPontaBoa = false;
        StringAssert.Contains("3 e 6", Acao(TipoEquipamento.AccessPoint, e, "Testar o cabo do AP").executar());
        Assert.IsNotNull(Acao(TipoEquipamento.AccessPoint, e, "Refazer a ponta"), "ponta ruim: refazer");
        StringAssert.Contains("Crimpe e teste", e.EncaixarAp());
        e.apPontaBoa = true;
        Acao(TipoEquipamento.AccessPoint, e, "Testar o cabo do AP").executar();
        e.EncaixarAp();
        Assert.IsTrue(e.apInstalado);

        // PoE no switch do rack: a porta do rack precisa abrir
        var poe = Acao(TipoEquipamento.Rack, e, "Ligar o cabo do AP-01");
        Assert.IsNotNull(poe, "a porta PoE fica nas ações do rack");
        StringAssert.Contains("Abra a porta", poe.executar());
        e.rackPortaAberta = true;
        poe.executar();
        Assert.IsTrue(e.apNoSwitch);
        e.apLigaEm = 0f;   // terminou de iniciar
        Assert.IsTrue(e.ApLigado);

        // Visitantes sem isolamento: o teste do celular denuncia
        e.apRedeEmpresa = e.apRedeVisitantes = true;
        StringAssert.Contains("não está isolada", e.TestarComoVisitante());
        Assert.IsFalse(e.apVisitanteTestado);
        e.apVisitantesIsolada = true;
        e.TestarComoVisitante();
        Assert.IsTrue(e.apVisitanteTestado);
        e.apCanal = e.canalVizinhos == 1 ? 11 : 1;
        Assert.IsTrue(e.ApConfigurado);

        e.RegistrarSurvey(new float[80], -61f, 0, 62);
        Assert.IsTrue(e.surveyDepois, "com o AP no teto a medição é a do depois");
        Assert.IsFalse(e.WiFiConcluido, "Wi-Fi da operadora ainda ligado e rack aberto");
        e.wifiAtivo = false;
        e.rackPortaAberta = false;
        Assert.IsTrue(e.WiFiConcluido);
        Assert.IsTrue(CatalogoProblemas.Resolvido(CatalogoProblemas.InstalacaoWiFi, e));
        Assert.IsEmpty(e.PendenciasWiFi());
        Assert.IsTrue(RegistroEquipamentos.De(TipoEquipamento.AccessPoint).Fechado(e));
        Assert.IsTrue(RegistroEquipamentos.De(TipoEquipamento.AccessPoint).Ligado(e));
    }

    [Test]
    public void Visita_SinalFracoDepoisObrigaAMudarOAp()
    {
        var e = Visita();
        e.surveyAntes = true;
        e.apMarcado = e.apSuporte = e.apCabo = e.apCrimpado = e.apPontaBoa = e.apTestado = e.apInstalado = e.apNoSwitch = true;
        e.RegistrarSurvey(new float[80], -71f, 3, 60);
        Assert.IsFalse(e.WiFiConcluido);
        var tirar = Acao(TipoEquipamento.AccessPoint, e, "Tirar o AP-01");
        Assert.IsNotNull(tirar, "sinal fraco depois: dá para tirar o AP e mudar de lugar");
        e.TirarAp();
        Assert.IsFalse(e.apMarcado || e.apSuporte || e.apInstalado || e.surveyDepois);
        Assert.AreEqual(1, e.apRemocoes);
    }

    [Test]
    public void Visita_AcoesNasPecasDoAp()
    {
        var def = RegistroEquipamentos.De(TipoEquipamento.AccessPoint);
        var e = Visita();
        void Conferir()
        {
            foreach (var a in AcoesFisicas.Para(TipoEquipamento.AccessPoint, e))
                Assert.Contains(a.peca, def.Pecas.ToList(), $"\"{a.rotulo}\" numa peça que o AP não tem");
        }
        Conferir();
        e.surveyAntes = e.apMarcado = true; Conferir();
        e.apSuporte = e.apCabo = true; Conferir();
        e.apCrimpado = e.apPontaBoa = e.apTestado = true; Conferir();
        e.apInstalado = true; e.surveyDepois = true; e.piorDepois = -80f; Conferir();
        Assert.IsEmpty(AcoesFisicas.Para(TipoEquipamento.AccessPoint, new EstadoEquipamentos()), "fora da visita o AP não tem ações");
        Assert.IsTrue(def.Funcionando(new EstadoEquipamentos()));
    }

    [Test]
    public void Servico_ForaDoSorteioEPresencial()
    {
        var p = CatalogoProblemas.InstalacaoWiFi;
        Assert.AreSame(p, CatalogoProblemas.Buscar(TipoEquipamento.AccessPoint, p.solucao));
        Assert.IsTrue(CatalogoProblemas.EhServicoProjeto(p));
        Assert.IsFalse(MissionManager.SolucaoRemota(p));
        foreach (var lista in new[] { CatalogoProblemas.Faceis, CatalogoProblemas.Medios, CatalogoProblemas.Dificeis })
            Assert.IsFalse(lista.Contains(p));
    }

    // ---------- Roteador (chamados avulsos) ----------

    [Test]
    public void Roteador_CanalCongestionado()
    {
        var p = CatalogoProblemas.Buscar(TipoEquipamento.Roteador, CatalogoProblemas.TrocarCanalWiFi);
        Assert.IsNotNull(p);
        var e = new EstadoEquipamentos();
        CatalogoProblemas.Quebrar(p, e);
        Assert.IsFalse(CatalogoProblemas.Resolvido(p, e));
        // Pista: o analisador do celular mostra as redes dos vizinhos no mesmo canal do roteador
        var redes = e.RedesVisiveis(-60f, "Cliente");
        Assert.GreaterOrEqual(redes.Count(r => r.Contains($"canal {e.canalVizinhos} ")), 6);
        e.rotCanal = e.canalVizinhos == 11 ? 1 : 11;
        Assert.IsTrue(CatalogoProblemas.Resolvido(p, e));
        Assert.IsTrue(MissionManager.SolucaoRemota(p));
    }

    [Test]
    public void Roteador_RedeDeVisitantes()
    {
        var p = CatalogoProblemas.Buscar(TipoEquipamento.Roteador, CatalogoProblemas.CriarRedeVisitantes);
        Assert.IsNotNull(p);
        var e = new EstadoEquipamentos();
        CatalogoProblemas.Quebrar(p, e);
        Assert.IsFalse(CatalogoProblemas.Resolvido(p, e));
        StringAssert.Contains("Não existe rede de visitantes", e.TestarComoVisitante());
        e.rotRedeVisitantes = e.rotVisitantesIsolada = true;
        Assert.IsFalse(CatalogoProblemas.Resolvido(p, e), "a senha da empresa ainda está no quadro");
        e.rotSenhaVazada = false;
        Assert.IsTrue(CatalogoProblemas.Resolvido(p, e));
        StringAssert.Contains("Isolada", e.TestarComoVisitante());
    }

    // ---------- Projeto ----------

    static Projeto WiFi(int tamanho, int feitos) => new Projeto
    {
        id = 88, tipo = TipoProjeto.WiFi, empresa = "Clínica Teste", tamanhoReal = tamanho, tamanhoOrcado = tamanho,
        etapa = 4, unidadesFeitas = feitos, horasTotais = ProjetosManager.Horas(TipoProjeto.WiFi, tamanho)
    };

    [Test]
    public void Projeto_UmApPorVisita()
    {
        var go = new GameObject("PM_Teste");
        try
        {
            var pm = go.AddComponent<ProjetosManager>();
            var p = WiFi(2, 0);
            pm.projetos.Add(p);
            Assert.AreEqual(1, ProjetosManager.PontosNaVisita(p));
            var ch = new Chamado(p.empresa, "", TipoChamado.Rede, DificuldadeChamado.Medio, 0f, 600f)
            { projetoId = p.id, estado = new EstadoEquipamentos(), problema = CatalogoProblemas.InstalacaoWiFi };
            pm.PrepararEstado(ch);
            Assert.IsTrue(ch.estado.wifiServico);
            Assert.AreEqual("AP-01", ch.estado.NomeAp);
            pm.VisitaConcluida(ch);
            Assert.AreEqual(0, p.unidadesFeitas, "AP não concluído não conta");
            var e = ch.estado;
            e.surveyAntes = e.apMarcado = e.apSuporte = e.apCabo = e.apCrimpado = e.apPontaBoa = e.apTestado = e.apInstalado = e.apNoSwitch = true;
            e.apLigaEm = 0f;
            e.apRedeEmpresa = e.apRedeVisitantes = e.apVisitantesIsolada = e.apVisitanteTestado = true;
            e.apCanal = e.canalVizinhos == 1 ? 11 : 1;
            e.wifiAtivo = false;
            e.surveyDepois = true; e.piorDepois = -60f;
            string r = pm.VisitaConcluida(ch);
            Assert.AreEqual(1, p.unidadesFeitas);
            StringAssert.Contains("1 de 2", r);
            var ch2 = new Chamado(p.empresa, "", TipoChamado.Rede, DificuldadeChamado.Medio, 0f, 600f) { projetoId = p.id, estado = new EstadoEquipamentos() };
            pm.PrepararEstado(ch2);
            Assert.AreEqual("AP-02", ch2.estado.NomeAp);
            StringAssert.Contains("cliente", pm.Trabalhar(p), "o tablet não faz mais o trabalho do Wi-Fi");
        }
        finally { Object.DestroyImmediate(go); }
    }
}
