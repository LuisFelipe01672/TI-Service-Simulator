# Plano — Fase 1: Impressora completa

Objetivo: o atendimento de impressora ficar **completo e realista**, do jeito que o técnico faz na vida real,
com animação em cada ação e visual caprichado. Depois repetimos o mesmo padrão para computador, roteador e cabos.

---

## 1. Visual da impressora (modelo e texturas)

Hoje ela é feita de caixas. Precisa parecer uma multifuncional de verdade, **com as peças separadas**
(tampa, gaveta de papel, bandeja de saída, painel, porta de rede atrás), porque cada uma vai se mexer.

| Caminho | Prós | Contras |
|---|---|---|
| **A. Modelagem por código + texturas reais** (plástico, grade de ventilação, adesivos, logotipo fictício) | Peças separadas e animáveis; zero custo; texturas CC0 do ambientCG que já usamos | Formas mais simples que um modelo profissional |
| **B. Baixar modelo 3D pronto** (Poly Pizza / Sketchfab, licença CC0 ou CC-BY) | Mais bonito de cara | Quase sempre vem "uma peça só" (não dá para abrir tampa); CC-BY exige crédito; precisa da sua aprovação para baixar |
| **C. Gerar com IA** (Tripo, plano pago ~US$ 20/mês) | Rápido | Malha bagunçada, peça única, custo |

**Recomendação:** A, com cara de multifuncional laser moderna (cinza-claro + grafite), e testar B só para
objetos que não se mexem (decoração). Detalhes que dão realismo:
- visor LCD com o **texto real** do estado (ex.: "Toner baixo", "Papel atolado", "Pronta") — e não só um LED;
- gaveta de papel com folhas visíveis, bandeja de saída, grade de ventilação, etiqueta com modelo e nº de série;
- porta de rede atrás com **LED verde/laranja piscando** de verdade;
- logotipo fictício (nada de marca real).

## 2. Interações com animação (mirar + E / segurar E)

Cada ação mostra o que acontece. Lista do que a impressora precisa ter:

**Parte de trás / cabo de rede**
- [ ] Olhar atrás: câmera aproxima da traseira (close) e mostra o plugue RJ45 na porta e os LEDs
- [ ] Desconectar: o plugue sai da porta e fica pendurado (clique de trava)
- [ ] Conectar: o plugue entra até dar o clique; LED acende se o cabo for bom
- [ ] Testar com o testador: testador aparece na mão, luzes 1–8 acendem em sequência (ou falham)
- [ ] Trocar o cabo por um da mochila / refazer a ponta (já existe a crimpagem animada)

**Painel / visor**
- [ ] Olhar o visor: close no LCD com a mensagem real
- [ ] Navegar no menu do painel (ex.: imprimir relatório de configuração com o IP)
- [ ] Botão de imprimir página de teste e botão liga/desliga com LED

**Toner**
- [ ] Abrir a tampa (já existe)
- [ ] Retirar o cartucho: ele sai da impressora e fica **na sua mão** (aparece na frente da câmera)
- [ ] Olhar o cartucho na mão (peso, se tem toner, se o lacre está lá)
- [ ] **Recolocar o mesmo cartucho** (nova ação) — ou deixar de lado e pegar um novo da mochila
- [ ] Chacoalhar (animação balançando na mão), puxar o lacre (fita saindo)
- [ ] Encaixar (desliza para dentro), fechar a tampa
- [ ] Cartucho velho vai para a mochila como "toner usado" (descarte/logística reversa)

**Papel e impressão**
- [ ] Gaveta de papel abre/fecha; pôr papel
- [ ] Imprimir página de teste: **a folha sai pela bandeja mostrando o resultado** (ok, falhada, em branco,
      manchada, com listras) — o resultado é a pista
- [ ] Papel atolado: abrir a tampa traseira e puxar a folha amassada

## 3. Novos problemas de impressora (além dos 4 de hoje)

Hoje: spooler travado, cabo, fila de impressão, toner.
Novos (todos com pista visual):
- Papel atolado (visor "Atolamento", folha amassada lá dentro)
- Sem papel / gaveta mal encaixada
- Lacre do toner novo não retirado (página em branco) — já existe a lógica, falta mostrar
- Toner falhado vs. cilindro riscado (listra vertical sempre no mesmo lugar → cilindro, não toner)
- IP mudou (DHCP) → imprimir relatório de configuração no painel para descobrir o IP novo
- Tampa mal fechada (visor "Feche a tampa")
- Driver errado no computador (página com caracteres estranhos)

## 4. Peças de sistema que servem para todo o jogo

- **Mão / item segurado:** mostrar na frente da câmera o que o técnico está segurando (cartucho, testador, cabo)
- **Close de câmera:** ao interagir com uma peça, a câmera aproxima suavemente e volta ao terminar
- **Folha impressa com textura gerada** conforme o resultado
- **Visor LCD com texto** (TextMeshPro no mundo)
- Sons específicos (puxar plugue, clique da trava, impressora puxando papel)

## 5. Ordem de execução (cada etapa testável no jogo)

1. Modelo novo da impressora (peças separadas + texturas + visor com texto real)
2. Mão/item segurado + close de câmera
3. Toner completo (retirar, olhar, recolocar o mesmo, novo, chacoalhar, lacre, encaixar)
4. Traseira e cabo de rede (desconectar/conectar animado, LEDs, testador)
5. Página de teste saindo com o resultado visível
6. Novos problemas (atolamento, papel, cilindro, IP, tampa, driver)
7. Teste completo de cada problema do começo ao fim + build

## Depois da impressora

Computador (gabinete, cabos atrás, tela com o Windows real no monitor), roteador (luzes, cabos, reset com clipe),
cabeamento/rack, e o visual geral (salas, rua, personagens fazendo coisas).
