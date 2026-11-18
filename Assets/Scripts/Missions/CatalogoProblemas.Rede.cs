// Etapa 8: serviços de rede física feitos no local, vindos dos projetos (ProjetosManager). Ficam FORA das listas
// sorteáveis: só o projeto cria o chamado (organizar o rack, passar os pontos de rede de uma visita).
public static partial class CatalogoProblemas
{
    public const string OrganizarRack = "Organizar o rack";
    public const string PassarPontos = "Passar os pontos de rede";
    public const string InstalarAccessPoint = "Instalar o access point";   // Etapa 10
    public static bool EhServicoProjeto(Problema p) =>
        p != null && (p.solucao == OrganizarRack || p.solucao == PassarPontos || p.solucao == InstalarAccessPoint);

    public static readonly Problema OrganizacaoRack = new Problema(TipoEquipamento.Rack, OrganizarRack,
        "Rack com cabo embolado e nenhuma etiqueta: quando um ponto cai, ninguém sabe qual cabo é.",
        "Aquele armarinho dos cabos virou um ninho de rato.",
        new[] {
            "Faz anos que é assim, foi crescendo.",
            "Cada técnico que veio puxou um cabo novo por cima.",
            "Às vezes alguém esbarra e cai a internet de uma mesa.",
            "Todo mundo do escritório depende dele.",
            "Já desligamos e ligamos o switch, mas o problema é a bagunça." },
        Pergunta.MexeramEmAlgo, "Ninguém mexe ali, juro.")
        .Conclui(e => e.RackOrganizado)
        .Quebra(e => e.MontarRackBaguncado());

    public static readonly Problema CabeamentoPontos = new Problema(TipoEquipamento.Rack, PassarPontos,
        "Projeto de cabeamento: passar os pontos de rede novos até o rack, testar e etiquetar.",
        "Vieram passar os cabos novos, né?",
        new[] {
            "Fechamos o projeto com vocês na semana passada.",
            "Só marcamos com lápis onde vão as tomadas.",
            "Hoje a turma usa extensão de cabo pelo chão.",
            "As mesas novas do escritório.",
            "Não tem o que reiniciar, ainda não existe." },
        Pergunta.MexeramEmAlgo, "Ninguém mexeu em nada.")
        .Conclui(e => e.PontosConcluidos)
        .Quebra(e => { if (e.pontos.Count == 0) e.PrepararPontos(1, 2); });

    // Etapa 10: visita do projeto de Wi-Fi corporativo (um access point por visita)
    public static readonly Problema InstalacaoWiFi = new Problema(TipoEquipamento.AccessPoint, InstalarAccessPoint,
        "Projeto de Wi-Fi: instalar o access point no teto, rede de visitantes separada e sinal bom na sala toda.",
        "Vieram arrumar o Wi-Fi, né? Lá no fundo não pega nada.",
        new[] {
            "Desde sempre: o roteador da operadora não alcança o fundo da sala.",
            "Já mudamos o roteador de lugar umas três vezes.",
            "No canto o celular mostra uma barrinha e a chamada cai.",
            "Quem senta nos cantos sofre; e os clientes da recepção pedem a senha da empresa.",
            "Reiniciar não adianta, o problema é o alcance." },
        Pergunta.MexeramEmAlgo, "Nunca mexemos no roteador.")
        .Conclui(e => e.WiFiConcluido)
        .Quebra(e => { if (!e.wifiServico) e.PrepararWiFi(1); });
}
