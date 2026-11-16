using System.Collections.Generic;
using UnityEngine;

// Estado do roteador do cliente (campos rot*, Wi-Fi, DHCP) e o que ele mostra/faz.
public partial class EstadoEquipamentos
{
    // ---------- Roteador ----------
    public bool rotNaTomada = true;
    public float rotDesligadoDesde = -1f;      // Time.time em que foi tirado da tomada
    public float rotLigaEm = -1f;              // Time.time em que termina de iniciar (luzes piscando antes)
    public bool rotTravado;                    // memória cheia: a internet cai sozinha
    public bool rotConfigOk = true;            // configuração da internet/Wi-Fi certa
    public bool rotSenhaDesconhecida;          // alguém trocou a senha do painel
    public bool wifiAtivo = true;
    public string wifiNome = "Escritorio";
    public bool dhcpDuplo;                     // o modem da operadora também está entregando IP
    public bool modemEmBridge = true;

    public const string SenhaPadrao = "admin";
    public const float SegundosParaReiniciarRoteador = 10f;   // tempo mínimo fora da tomada
    public const float SegundosIniciandoRoteador = 15f;

    // ---------- Consultas ----------

    public bool RoteadorIniciando => rotNaTomada && Time.time < rotLigaEm;
    public bool RoteadorFuncionando => rotNaTomada && !RoteadorIniciando;
    public bool InternetOk => RoteadorFuncionando && rotConfigOk && !rotTravado && !dhcpDuplo;

    // As 4 luzes do roteador: Power, Internet, Wi-Fi, LAN
    public string LuzesRoteador()
    {
        if (!rotNaTomada) return "Todas apagadas (fora da tomada)";
        if (RoteadorIniciando) return "Power: piscando · Internet: apagada · Wi-Fi: apagada · LAN: piscando (iniciando...)";
        string internet = !rotConfigOk || dhcpDuplo ? "laranja" : rotTravado ? "piscando laranja de vez em quando" : "verde";
        string wifi = wifiAtivo && rotConfigOk ? "verde" : "apagada";
        return $"Power: verde · Internet: {internet} · Wi-Fi: {wifi} · LAN: verde";
    }

    // ---------- Ações ----------

    // Tirar/colocar o roteador na tomada. Só "destrava" se ficou fora o tempo suficiente.
    public string TomadaRoteador()
    {
        if (rotNaTomada)
        {
            rotNaTomada = false;
            rotDesligadoDesde = Time.time;
            return "Você tirou o roteador da tomada. Todas as luzes apagaram.";
        }
        float fora = Time.time - rotDesligadoDesde;
        rotNaTomada = true;
        rotLigaEm = Time.time + SegundosIniciandoRoteador;
        if (fora >= SegundosParaReiniciarRoteador)
        {
            rotTravado = false;
            return $"Você ligou o roteador de volta depois de {fora:F0}s. Ele está iniciando...";
        }
        return $"Você religou rápido demais ({fora:F0}s). O ideal é esperar uns {SegundosParaReiniciarRoteador:F0}s.";
    }

    // Reset de fábrica (botão com clipe): apaga tudo
    public void ResetFabrica()
    {
        rotConfigOk = false;            // precisa configurar a internet/Wi-Fi de novo
        rotSenhaDesconhecida = false;   // volta para a senha da etiqueta
        rotTravado = false;
        wifiAtivo = true;
        wifiNome = "ROTEADOR_5G_A1B2";
        rotNaTomada = true;
        rotLigaEm = Time.time + SegundosIniciandoRoteador;
    }

    public void ConfigurarRoteador(string nomeWifi)
    {
        rotConfigOk = true;
        wifiNome = string.IsNullOrEmpty(nomeWifi) ? "Escritorio" : nomeWifi;
    }
}
