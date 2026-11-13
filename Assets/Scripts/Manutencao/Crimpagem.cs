using System.Collections.Generic;
using UnityEngine;

// Minigame de refazer a ponta de um cabo de rede (crimpar um conector RJ45), etapa por etapa,
// como um técnico faz: cortar, descascar, ordenar os 8 fios no padrão T568B, aparar, inserir e apertar.
public class Crimpagem
{
    public enum Etapa { Cortar, Descascar, Ordenar, Aparar, Inserir, Crimpar, Pronto }

    // Padrão T568B (o mais usado no Brasil)
    public static readonly string[] T568B =
        { "Branco-laranja", "Laranja", "Branco-verde", "Azul", "Branco-azul", "Verde", "Branco-marrom", "Marrom" };
    public static readonly string[] CoresHex =
        { "#F7C9A0", "#F28C28", "#B9E3B0", "#2F6FD6", "#B5CCF2", "#2E9E4A", "#D9C2AE", "#7A4A2A" };
    public static readonly bool[] Listrado = { true, false, true, false, true, false, true, false };

    public Etapa etapa = Etapa.Cortar;
    public readonly List<int> fiosSoltos = new List<int>();   // na mão, embaralhados
    public readonly List<int> ordem = new List<int>();        // já colocados (posição 1 a 8)
    public bool sucesso;

    public Crimpagem()
    {
        for (int i = 0; i < 8; i++) fiosSoltos.Add(i);
        for (int i = 0; i < fiosSoltos.Count; i++)
        {
            int j = Random.Range(i, fiosSoltos.Count);
            (fiosSoltos[i], fiosSoltos[j]) = (fiosSoltos[j], fiosSoltos[i]);
        }
    }

    public bool OrdemCerta
    {
        get
        {
            if (ordem.Count != 8) return false;
            for (int i = 0; i < 8; i++) if (ordem[i] != i) return false;
            return true;
        }
    }

    public void ColocarFio(int fio)
    {
        if (etapa != Etapa.Ordenar || !fiosSoltos.Contains(fio)) return;
        fiosSoltos.Remove(fio);
        ordem.Add(fio);
    }

    public void TirarUltimo()
    {
        if (etapa != Etapa.Ordenar || ordem.Count == 0) return;
        int f = ordem[ordem.Count - 1];
        ordem.RemoveAt(ordem.Count - 1);
        fiosSoltos.Add(f);
    }

    // Avança para a próxima etapa. Devolve a mensagem (ou um erro, sem avançar).
    public string Avancar()
    {
        switch (etapa)
        {
            case Etapa.Cortar:
                etapa = Etapa.Descascar;
                return "Você cortou fora a ponta velha com o alicate.";
            case Etapa.Descascar:
                etapa = Etapa.Ordenar;
                return "Capa descascada (~2 cm). Agora destrance e coloque os 8 fios na ordem.";
            case Etapa.Ordenar:
                if (ordem.Count < 8) return "Coloque todos os 8 fios primeiro.";
                etapa = Etapa.Aparar;
                return "Fios alinhados. Hora de aparar no tamanho certo.";
            case Etapa.Aparar:
                etapa = Etapa.Inserir;
                return "Pontas aparadas retinhas (~1,3 cm).";
            case Etapa.Inserir:
                if (Mochila.Instance != null && !Mochila.Instance.Usar(Item.ConectorRJ45))
                    return "Você não tem conector RJ45! Compre no tablet.";
                etapa = Etapa.Crimpar;
                return "Fios empurrados até o fundo do conector, com a trava virada para baixo.";
            case Etapa.Crimpar:
                etapa = Etapa.Pronto;
                sucesso = OrdemCerta;   // ordem errada = ponta ruim (só o testador mostra)
                return "Crec! Conector crimpado. Teste o cabo para ter certeza.";
        }
        return "";
    }

    // Etapas que deixam sujeira no chão
    public static bool FazSujeira(Etapa e) => e == Etapa.Cortar || e == Etapa.Descascar || e == Etapa.Aparar;
}
