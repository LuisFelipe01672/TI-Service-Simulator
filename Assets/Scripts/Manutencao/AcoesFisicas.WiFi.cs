using UnityEngine;

// Etapa 10: ações do projeto de Wi-Fi. No forro: marcar o lugar do AP onde o técnico mira, subir na escada e fixar
// o suporte, canaleta e cabo até o rack, crimpar, testar, encaixar o AP (e, se o sinal ficou fraco, tirar de novo).
// Na caixa do kit (no chão, ao lado do rack): abrir e olhar. A porta PoE do switch fica nas ações do rack.
// A configuração é no navegador do computador do cliente (GameUI.Computador) e o teste de visitante e a medição,
// no celular (app Wi-Fi).
public static partial class AcoesFisicas
{
    internal static void AccessPoint(EstadoEquipamentos e, ListaAcoes l)
    {
        if (!e.wifiServico) return;

        l.peca = "caixa";
        l.Add(new AcaoFisica("Olhar o kit do access point", () =>
            e.apInstalado ? "Caixa vazia: só o manual e o saquinho de parafusos que sobrou."
                          : $"Kit do {e.NomeAp}: access point de teto (PoE), suporte, buchas e parafusos. " +
                            "Antes de furar: meça a sala com o celular (app Wi-Fi) para ver onde o sinal é fraco.") { observacao = true, close = false });

        l.peca = "forro";
        l.Add(new AcaoFisica("Olhar o teto", () =>
            !e.apMarcado ? "Forro liso. O melhor lugar para o AP é no alto, longe de metal, de onde ele \"enxerga\" a sala toda por cima dos móveis." :
            !e.apSuporte ? $"Marca de lápis no forro: aqui vai o {e.NomeAp}." :
            !e.apInstalado ? $"Suporte fixado{(e.apCabo ? ", canaleta até o rack e o cabo pendurado esperando o AP" : "")}." :
            e.ApIniciando ? $"{e.NomeAp} no teto: luz laranja piscando (iniciando)." :
            e.ApLigado ? $"{e.NomeAp} no teto: luz verde acesa." : $"{e.NomeAp} no teto, apagado: sem energia (porta PoE?).") { observacao = true, close = false });

        if (!e.apSuporte)
            l.Add(new AcaoFisica(e.apMarcado ? "Marcar de novo onde estou mirando" : "Marcar aqui o lugar do access point (lápis)", () =>
            {
                if (!e.surveyAntes)
                    return "Antes de escolher o lugar, meça a sala (celular » Wi-Fi » Medir a sala): sem a medição você não sabe onde o sinal é fraco.";
                if (!PontoNoForro(out Vector3 p)) return "Mire no forro.";
                e.apPos = AccessPointCliente.AjustarNoForro(p);
                e.apMarcado = true;
                e.rackVersao++;
                Vector3 local = AccessPointCliente.Local(e.apPos);
                return $"Marca de lápis no forro ({DescreverLugar(local)}). Agora a escada aberta embaixo da marca.";
            }) { close = false });

        if (e.apMarcado && !e.apSuporte)
            l.Add(new AcaoFisica("Subir na escada e fixar o suporte no forro (furadeira)", () =>
            {
                if (!AccessPointCliente.EscadaEmbaixo(e.apPos))
                    return "Precisa da escada aberta embaixo da marca (ela fica no porta-malas do carro). Na cadeira de rodinha, nem pensar.";
                e.apSuporte = true;
                e.rackVersao++;
                Sujeira.Espalhar(new Vector3(e.apPos.x, e.apPos.y - AccessPointCliente.AlturaForro, e.apPos.z), 4, "fios");
                return "Você subiu na escada, furou o forro, pôs as buchas e parafusou o suporte. Caiu pó de gesso no chão.";
            }, 4f));

        if (e.apSuporte && !e.apCabo)
            l.Add(new AcaoFisica("Fixar a canaleta e passar o cabo Cat6 do suporte até o rack", () =>
            {
                if (!AccessPointCliente.EscadaEmbaixo(e.apPos)) return "Precisa da escada embaixo do suporte para começar a canaleta no teto.";
                e.apCabo = true;
                e.apCrimpado = e.apTestado = false;
                e.rackVersao++;
                MM?.CobrarPeca(18f, "Canaleta e cabo Cat6");
                return "Canaleta rente ao teto até a parede do rack e descendo até ele; cabo Cat6 passado por dentro, tampa encaixada. " +
                       "Sobra de 30 cm no suporte e uns 50 cm dentro do rack.";
            }, 5f));

        if (e.apCabo && !e.apInstalado)
        {
            bool refazer = e.apCrimpado && e.apTestado && !e.apPontaBoa;
            if (!e.apCrimpado || refazer)
                l.Add(new AcaoFisica(refazer ? "Refazer a ponta do cabo do AP (crimpar de novo)" : "Crimpar a ponta do cabo do AP (RJ45)", () => "")
                {
                    abreCrimpagem = true, requer = Item.AlicateCrimpagem,
                    aoCrimpar = ok => { e.apCrimpado = true; e.apPontaBoa = ok; e.apTestado = false; e.rackVersao++; }
                });
            if (e.apCrimpado)
                l.Add(new AcaoFisica("Testar o cabo do AP com o testador (remoto no rack)", () =>
                {
                    e.apTestado = true;
                    e.testesDeCabo++;
                    e.ultimoTesteCabo = e.apPontaBoa ? DefeitoCabo.Nenhum : DefeitoCabo.Conector;
                    e.rackVersao++;
                    return e.apPontaBoa ? "Luzes 1 a 8 em sequência nas duas pontas. Cabo do AP aprovado."
                                        : "Os fios 3 e 6 não acendem: ponta mal crimpada. Refaça antes de fechar o AP.";
                }, 2f, Item.TestadorCabo));
            if (e.apCrimpado && e.apTestado && e.apPontaBoa)
                l.Add(new AcaoFisica($"Ligar o cabo e encaixar o {e.NomeAp} no suporte", () =>
                {
                    if (!AccessPointCliente.EscadaEmbaixo(e.apPos)) return "Precisa da escada embaixo do suporte.";
                    string r = e.EncaixarAp();
                    Sujeira.Espalhar(new Vector3(e.apPos.x, e.apPos.y - AccessPointCliente.AlturaForro, e.apPos.z), 1, "caixa");
                    MM?.Verificar();
                    return r;
                }, 2.5f));
        }

        // Sinal fraco depois de instalado: só tirando o AP e furando em outro lugar
        if (e.apInstalado && e.surveyDepois && e.piorDepois < EstadoEquipamentos.MetaDbm)
            l.Add(new AcaoFisica($"Tirar o {e.NomeAp} do lugar para mudar de posição", () =>
            {
                if (!AccessPointCliente.EscadaEmbaixo(e.apPos)) return "Precisa da escada embaixo do AP.";
                MM?.RegistrarErro("Furo a mais no forro: o AP foi instalado num lugar que não cobria a sala.", 1f);
                return e.TirarAp();
            }, 4f));
    }

    // Onde a mira encontra o plano do forro (até 3 m)
    static bool PontoNoForro(out Vector3 p)
    {
        p = Vector3.zero;
        var cam = Camera.main;
        var t = AccessPointCliente.Transform;
        if (cam == null || t == null) return false;
        float y = t.position.y + AccessPointCliente.AlturaForro;
        var raio = new Ray(cam.transform.position, cam.transform.forward);
        if (raio.direction.y < 0.15f) return false;
        float dist = (y - raio.origin.y) / raio.direction.y;
        if (dist <= 0f || dist > 3f) return false;
        p = raio.GetPoint(dist);
        return true;
    }

    static string DescreverLugar(Vector3 local)
    {
        float fx = Mathf.Abs(local.x) / (AccessPointCliente.LarguraSala / 2f), fz = Mathf.Abs(local.z) / (AccessPointCliente.ProfundidadeSala / 2f);
        if (fx < 0.35f && fz < 0.35f) return "no meio da sala";
        if (fx > 0.7f && fz > 0.7f) return "num canto";
        return "entre o meio e a parede";
    }
}
