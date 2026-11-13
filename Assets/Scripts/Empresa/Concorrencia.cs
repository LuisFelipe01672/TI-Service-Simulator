using System.Collections.Generic;
using UnityEngine;

// Uma empresa concorrente (fictícia)
[System.Serializable]
public class Concorrente
{
    public string nome;
    public string perfil;
    public float precoFator;    // preço em relação à tabela (0,85 = 15% mais barato)
    public float qualidade;     // 0 a 1: clientes de concorrente ruim voltam a procurar você
    public float fatia;         // fatia de mercado (peso para aparecer nas disputas)
    public int vitoriasSobreVoce, derrotasParaVoce;
}

// O mercado: cada proposta disputa com um concorrente sorteado pelo tamanho dele.
// Todo mês os concorrentes mudam de preço; quem perde muito encolhe, quem ganha cresce. Alguns quebram e outros abrem.
public static class Concorrencia
{
    public static List<Concorrente> lista = new List<Concorrente>();
    public static float fatiaJogador = 0.02f;
    public static int ganhas, perdidas;
    public static int ultimoMesAtualizado = -1;

    static readonly string[] novos = { "Byte Certo Informática", "NetPro Serviços", "Suporte Total TI", "CloudMinas", "Conecta Já" };

    public static void Iniciar()
    {
        lista = new List<Concorrente>
        {
            new Concorrente { nome = "TechMais Soluções", perfil = "Barata e meio bagunçada", precoFator = 0.88f, qualidade = 0.45f, fatia = 0.30f },
            new Concorrente { nome = "Infinity TI", perfil = "Grande, cara e eficiente", precoFator = 1.12f, qualidade = 0.85f, fatia = 0.35f },
            new Concorrente { nome = "PrintFácil Locações", perfil = "Especialista em impressoras", precoFator = 0.95f, qualidade = 0.70f, fatia = 0.20f },
            new Concorrente { nome = "Rede Já Informática", perfil = "Forte em redes e suporte", precoFator = 1.0f, qualidade = 0.65f, fatia = 0.13f },
        };
        fatiaJogador = 0.02f; ganhas = perdidas = 0; ultimoMesAtualizado = -1;
    }

    static void Garantir() { if (lista == null || lista.Count == 0) Iniciar(); }

    // Sorteia quem disputa a proposta, pelo tamanho de cada um
    public static Concorrente Sortear()
    {
        Garantir();
        float total = 0f;
        foreach (var c in lista) total += c.fatia;
        float r = Random.value * total;
        foreach (var c in lista) { r -= c.fatia; if (r <= 0f) return c; }
        return lista[lista.Count - 1];
    }

    // Preço que o concorrente oferece para um "preço justo"
    public static float Oferta(Concorrente c, float justo) => justo * c.precoFator * Random.Range(0.95f, 1.08f);

    // Peso da qualidade: o cliente aceita pagar um pouco mais por quem tem boa reputação/imagem
    public static float VantagemJogador(float reputacao)
    {
        float imagem = SedeManager.Instance != null ? SedeManager.Instance.BonusImagem : 0f;
        return 1f + (reputacao - 50f) / 400f + imagem;
    }

    // Quantos clientes procuram você: cresce com a reputação e com a imagem da sede (fachada, recepção)
    public static float FatorDemanda()
    {
        float rep = ReputationManager.Instance != null ? ReputationManager.Instance.GetReputacao() : 50f;
        float imagem = SedeManager.Instance != null ? SedeManager.Instance.BonusImagem : 0f;
        return (0.4f + 0.8f * rep / 100f) * (1f + imagem * 2.5f);
    }

    public static void Registrar(Concorrente c, bool jogadorGanhou)
    {
        if (c == null) return;
        if (jogadorGanhou) { ganhas++; c.derrotasParaVoce++; c.fatia = Mathf.Max(0.02f, c.fatia - 0.005f); fatiaJogador += 0.005f; }
        else { perdidas++; c.vitoriasSobreVoce++; c.fatia += 0.003f; }
    }

    // Uma vez por mês: preços mudam, um concorrente pode quebrar e outro abrir
    public static string AtualizarMes(int mes)
    {
        Garantir();
        if (mes <= ultimoMesAtualizado) return null;
        ultimoMesAtualizado = mes;
        string noticia = null;
        foreach (var c in lista)
            c.precoFator = Mathf.Clamp(c.precoFator + Random.Range(-0.04f, 0.04f), 0.75f, 1.3f);

        var menor = lista.Find(c => c.fatia < 0.05f);
        if (menor != null && Random.value < 0.5f)
        {
            lista.Remove(menor);
            noticia = $"A {menor.nome} fechou as portas. Os clientes dela estão procurando outra empresa!";
        }
        else if (lista.Count < 5 && Random.value < 0.15f)
        {
            var nome = novos[Random.Range(0, novos.Length)];
            if (!lista.Exists(c => c.nome == nome))
            {
                lista.Add(new Concorrente { nome = nome, perfil = "Nova no mercado, preço agressivo", precoFator = 0.82f, qualidade = 0.5f, fatia = 0.06f });
                noticia = $"Abriu uma concorrente nova: {nome}, com preços bem agressivos.";
            }
        }
        return noticia;
    }

    [System.Serializable]
    public class Dados
    {
        public List<Concorrente> lista = new List<Concorrente>();
        public float fatiaJogador;
        public int ganhas, perdidas, ultimoMesAtualizado = -1;
    }

    public static Dados Exportar() => new Dados { lista = new List<Concorrente>(lista), fatiaJogador = fatiaJogador, ganhas = ganhas, perdidas = perdidas, ultimoMesAtualizado = ultimoMesAtualizado };

    public static void Carregar(Dados d)
    {
        if (d == null || d.lista == null || d.lista.Count == 0) { Iniciar(); return; }
        lista = d.lista; fatiaJogador = d.fatiaJogador; ganhas = d.ganhas; perdidas = d.perdidas; ultimoMesAtualizado = d.ultimoMesAtualizado;
    }
}
