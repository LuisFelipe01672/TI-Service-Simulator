// Etapa 8: ações no rack de parede do cliente (organizar o rack bagunçado) e nos pontos de rede do projeto de
// cabeamento (canaleta, cabo, crimpagem, punch-down, testador e etiqueta). Visual: EquipamentoPecas.Rack.
public static partial class AcoesFisicas
{
    internal static void Rack(EstadoEquipamentos e, ListaAcoes l)
    {
        l.peca = "rack";
        l.Add(new AcaoFisica("Olhar o rack", () => e.OlharRack()) { observacao = true });
        if (e.rackServico)
        {
            if (!e.rackFotoAntes && !e.rackMexeu)
                l.Add(new AcaoFisica("Tirar a foto do antes (vai na OS)", () =>
                {
                    e.fotoAntes = RackCliente.Fotografar();
                    e.rackFotoAntes = true;
                    return "Foto do antes tirada: o bolo de cabo vai na OS para o cliente ver a diferença.";
                }, 1f) { observacao = true, close = false });
            if (!e.rackFotoDepois && e.rackMexeu)
                l.Add(new AcaoFisica("Tirar a foto do depois (vai na OS)", () =>
                {
                    var falta = e.PendenciasRack();
                    falta.Remove("foto do depois");
                    falta.Remove("foto do antes");
                    falta.Remove("anotar o mapa das portas");
                    if (falta.Count > 0) return $"Ainda não dá para a foto do depois. Falta: {string.Join(" · ", falta)}.";
                    e.fotoDepois = RackCliente.Fotografar();
                    e.rackFotoDepois = true;
                    MM?.Verificar();
                    return "Foto do depois tirada: cordões curtos, etiquetados e presos. Vai na OS junto com a do antes.";
                }, 1f) { observacao = true, close = false });
        }

        l.peca = "porta";
        l.Add(new AcaoFisica(e.rackPortaAberta ? "Fechar a porta de vidro do rack" : "Abrir a porta de vidro do rack", () =>
        {
            if (e.rackPortaAberta && e.rackServico && e.rackCordoes.Exists(c => !c.Curto))
                return "A porta bate no bolo de cabo pendurado e não fecha.";
            e.rackPortaAberta = !e.rackPortaAberta;
            e.rackVersao++;
            MM?.Verificar();
            return e.rackPortaAberta ? "Porta do rack aberta (a chavinha fica na fechadura)." : "Porta do rack fechada e trancada.";
        }));

        l.peca = "switch";
        l.Add(new AcaoFisica("Olhar as luzes de link do switch", () =>
        {
            string r = e.ConferirLinks();
            MM?.Verificar();
            return r;
        }) { observacao = true, pistaVisual = "Conte as luzes de link acesas no switch." });

        l.peca = "patch";
        if (e.rackServico) AcoesCordoes(e, l);
        foreach (var p in e.pontos)
        {
            var pt = p;
            if (pt.caboPassado && !pt.noPatch)
                l.Add(new AcaoFisica($"Inserir o ponto {pt.Nome} na porta {pt.portaPatch} do patch panel (punch-down)", () =>
                {
                    if (!e.rackPortaAberta) return "Abra a porta do rack primeiro.";
                    pt.noPatch = true;
                    pt.testado = false;
                    e.rackVersao++;
                    Sujeira.Espalhar(MM != null ? MM.PosicaoEquipamentoAtual : UnityEngine.Vector3.zero, 3, "fios");
                    return $"Os 8 fios do {pt.Nome} entraram nos contatos da porta {pt.portaPatch} (clack, clack: a ferramenta corta a sobra).";
                }, 3f, Item.PunchDown));
        }

        l.peca = "organizador";
        if (e.rackServico)
        {
            if (!e.rackNoOrganizador)
                l.Add(new AcaoFisica("Passar os cordões pelo organizador", () => e.PassarNoOrganizador(), 3f));
            else if (!e.rackVelcro)
                l.Add(new AcaoFisica("Prender o feixe com abraçadeira de velcro", () => e.PrenderVelcro(), 2f));
            else
                l.Add(new AcaoFisica("Olhar o organizador", () => "Feixe reto, preso com velcro, sem cabo forçando a porta.") { observacao = true });
        }

        for (int i = 0; i < e.pontos.Count && i < 4; i++)
        {
            l.peca = "ponto" + (i + 1);
            AcoesPonto(e, e.pontos[i], l);
        }
    }

    static void AcoesCordoes(EstadoEquipamentos e, ListaAcoes l)
    {
        bool longos = e.rackCordoes.Exists(c => !c.Curto);
        if (e.rackCordoes.Count > 0 && !e.rackMapaAnotado && longos)
            l.Add(new AcaoFisica("Anotar o mapa das portas (patch » switch)", () =>
            {
                e.rackMapaAnotado = true;
                return e.MapaPortas() + " — anotado no bloco.";
            }, 2f) { observacao = true, close = false });
        if (longos)
            l.Add(new AcaoFisica("Desconectar os cordões velhos", () =>
            {
                if (!e.rackPortaAberta) return "Abra a porta do rack primeiro.";
                string r = e.DesconectarCordoes(out bool semMapa);
                Sujeira.Espalhar(MM != null ? MM.PosicaoEquipamentoAtual : UnityEngine.Vector3.zero, 4, "cabo velho");
                if (semMapa) MM?.RegistrarErro("Desconectou o rack sem anotar o mapa: ninguém sabe mais qual ponto é de qual mesa.", 2f);
                return r + " Os cordões velhos ficaram no chão: recolha antes de ir embora.";
            }, 4f));
        int porta = e.ProximaPortaSemCordao();
        if (porta > 0 && e.rackMexeu)
        {
            l.Add(new AcaoFisica($"Ligar cordão de 0,3 m na porta {porta} (P{porta:00} » SW{porta:00})", () =>
            {
                if (!e.rackPortaAberta) return "Abra a porta do rack primeiro.";
                return e.ConectarCordao(true);
            }, 1.2f));
            l.Add(new AcaoFisica($"Reaproveitar um cordão velho de 2,5 m na porta {porta}", () =>
            {
                if (!e.rackPortaAberta) return "Abra a porta do rack primeiro.";
                return e.ConectarCordao(false);
            }, 1.2f));
        }
        int semEtiqueta = e.ProximaPortaSemEtiqueta();
        if (semEtiqueta > 0 && e.rackCordoes.Exists(c => c.portaPatch == semEtiqueta))
            l.Add(new AcaoFisica($"Etiquetar a porta {semEtiqueta} (P{semEtiqueta:00})", () => e.Etiquetar(), 0.8f, Item.Etiquetadora));
    }

    static void AcoesPonto(EstadoEquipamentos e, PontoRede p, ListaAcoes l)
    {
        l.Add(new AcaoFisica($"Olhar o ponto {p.Nome}", () =>
            !p.canaleta ? $"{p.Nome}: só a marca de lápis na parede. Daqui até o rack vai canaleta (sem forro nesta sala)." :
            !p.caboPassado ? $"{p.Nome}: caixa e canaleta fixadas, sem cabo ainda." :
            !p.crimpado ? $"{p.Nome}: o cabo sai da caixa com sobra, sem conector." :
            !p.noPatch ? $"{p.Nome}: tomada montada. Do outro lado, o cabo ainda não foi inserido no patch panel." :
            !p.testado ? $"{p.Nome}: montado nas duas pontas. Falta testar." :
            !p.Ok ? $"{p.Nome}: o último teste falhou. Refaça a ponta." :
            p.etiquetado ? $"{p.Nome}: testado e etiquetado." : $"{p.Nome}: testado, falta a etiqueta.") { observacao = true });

        if (!p.canaleta)
        {
            l.Add(new AcaoFisica($"Fixar a caixa do {p.Nome} e a canaleta até o rack", () =>
            {
                p.canaleta = true;
                e.rackVersao++;
                Sujeira.Espalhar(RackCliente.PosicaoPonto(e.pontos.IndexOf(p)), 3, "fios");
                return $"Furadeira, bucha e parafuso: caixa de sobrepor do {p.Nome} na parede e canaleta até o rack, com curvas nos cantos.";
            }, 4f));
            return;
        }
        if (!p.caboPassado)
        {
            l.Add(new AcaoFisica($"Passar o cabo Cat6 do {p.Nome} pela canaleta até o rack", () =>
            {
                p.caboPassado = true;
                e.rackVersao++;
                return $"Cabo passado pela canaleta, tampa encaixada por cima. Sobra de 30 cm na caixa e uns 50 cm dentro do rack.";
            }, 4f));
            return;
        }
        bool refazer = p.crimpado && p.testado && !p.pontaBoa;
        if (!p.crimpado || refazer)
            l.Add(new AcaoFisica(refazer ? $"Refazer a ponta do {p.Nome} (crimpar de novo)" : $"Crimpar a ponta do {p.Nome} (RJ45 na tomada)", () => "")
            {
                abreCrimpagem = true, requer = Item.AlicateCrimpagem,
                aoCrimpar = ok => { p.crimpado = true; p.pontaBoa = ok; p.testado = false; e.rackVersao++; }
            });
        if (p.crimpado)
            l.Add(new AcaoFisica($"Testar o {p.Nome} com o testador (remoto no patch panel)", () =>
            {
                if (!p.noPatch) return "O outro lado ainda não está no patch panel: não tem onde ligar o remoto do testador.";
                p.testado = true;
                e.testesDeCabo++;
                e.ultimoTesteCabo = p.pontaBoa ? DefeitoCabo.Nenhum : DefeitoCabo.Conector;
                e.rackVersao++;
                MM?.Verificar();
                return p.pontaBoa ? $"{p.Nome}: luzes 1 a 8 em sequência nas duas pontas. Ponto aprovado."
                                  : $"{p.Nome}: os fios 3 e 6 não acendem. Ponta mal crimpada: refaça.";
            }, 2f, Item.TestadorCabo));
        if (p.Ok && !p.etiquetado)
            l.Add(new AcaoFisica($"Etiquetar o {p.Nome} (tomada e porta {p.portaPatch} do patch)", () =>
            {
                p.etiquetado = true;
                e.rackVersao++;
                MM?.Verificar();
                return $"Etiqueta {p.Nome} na tomada e na porta {p.portaPatch} do patch panel. Igual nas duas pontas.";
            }, 1f, Item.Etiquetadora));
    }
}
