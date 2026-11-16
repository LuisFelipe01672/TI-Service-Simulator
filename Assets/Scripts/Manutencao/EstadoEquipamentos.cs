using System.Collections.Generic;
using UnityEngine;

// Um documento na fila de impressão
[System.Serializable]
public class TrabalhoImpressao
{
    public string nome;
    public int paginas;
    public bool travado;
    public TrabalhoImpressao(string nome, int paginas, bool travado = false) { this.nome = nome; this.paginas = paginas; this.travado = travado; }
}

public enum DefeitoCabo { Nenhum, Conector, Rompido }

// Um cabo de rede ligando um equipamento à tomada de rede da parede
[System.Serializable]
public class CaboRede
{
    public bool conectado = true;
    public DefeitoCabo defeito;
    public float metrosNecessarios = 2f;   // distância do equipamento até a tomada de rede
    public bool Bom => defeito == DefeitoCabo.Nenhum;
    public bool Funciona => conectado && Bom;

    // O que o testador de cabo mostra (luzes 1 a 8)
    public string Testar()
    {
        switch (defeito)
        {
            case DefeitoCabo.Conector: return "Luzes: 1 2 · 4 5 · 7 8  » os fios 3 e 6 não passam sinal: mau contato no conector.";
            case DefeitoCabo.Rompido:  return "Luzes: nenhuma acende » o cabo está rompido no meio.";
            default:                   return "Luzes: 1 2 3 4 5 6 7 8 em sequência » cabo perfeito.";
        }
    }
}

// "Simulação" dos equipamentos do escritório do cliente durante um chamado:
// cada problema começa quebrando alguma coisa aqui, e o jogador conserta mexendo nas peças e no PC.
// Tudo é calculado a partir desse estado (LEDs, mensagens, ping, impressão), como num equipamento de verdade.
[System.Serializable]
public enum ResultadoPagina { Nenhuma, Perfeita, Branca, Clara, Listra, Lixo, Relatorio }

// Um arquivo por equipamento: EstadoEquipamentos.Impressora/.Computador/.Roteador/.Notebook/.Software.cs
// (a definição de cada equipamento, com ações e "está funcionando?", fica em Manutencao/Equipamentos/).
[System.Serializable]
public partial class EstadoEquipamentos
{
}
