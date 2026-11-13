# Roteiro de testes na Unity — hoje à noite

Tudo abaixo já passou nos testes de lógica fora da Unity. O que falta conferir é **compilar na Unity, a interface (layout) e a sensação de jogo**.

## 0. Preparar (eu faço pelo app, com o PC ligado)
- [ ] Copiar os commits do dia para `C:\Projetos\TI Service Simulator` (a partir do `01d852d`)
- [ ] Abrir a Unity e esperar compilar: **zero erros no Console**
- [ ] Apagar o save antigo (menu Pausa → ou SaveManager → "Apagar save") e começar jogo novo
- [ ] Você faz o push pelo Git (LFS)

## 1. Começo de jogo (10 min)
- [ ] Painel "PRIMEIROS PASSOS" aparece no canto superior direito e some ao abrir o tablet
- [ ] Tablet tem 7 abas cabendo na largura: Chamados, Contratos, Negócios, Central, Empresa, Equipe, Loja
- [ ] Chamado fácil paga ~R$ 115 como autônomo (65% da hora de empresa)
- [ ] Resolver 1 chamado → objetivo concluído (aviso verde) e conquista "Primeiro chamado" (aviso dourado)
- [ ] Reputação sobe devagar (~1 por chamado bem feito)

## 2. Empresa (aba Empresa)
- [ ] Abrir MEI → chamados passam a pagar 85%
- [ ] Seções aparecem nesta ordem: Situação, Abrir CNPJ, Recibos/Notas, Impostos, Banco, Sede, Mercado e concorrentes, Conquistas
- [ ] Banco: botões de R$ 1.000 / 3.000 em 6x e 12x; pegar um e ver a parcela na virada do mês
- [ ] Sede: alugar sala 20 m² (caução), comprar mesa + cadeira + computador → "Estações 1/3"

## 3. Negócios (aba nova)
- [ ] Foco da empresa: escolher um (grátis); tentar trocar logo depois → recusa por 30 dias
- [ ] Parcerias: "Negociar adesão" e "Solicitar"; depois de X dias vira "Parceiro Registrado"
- [ ] Projeto: levantamento (1 h, relógio anda), orçamento, comprar material, trabalhar 2 h
- [ ] Licitação: escolher desconto, enviar; no dia da sessão sai o resultado
- [ ] Produtos próprios aparecem bloqueados até virar ME

## 4. Contratos e Central
- [ ] Serviço bloqueado mostra "Precisa da parceria com a ..."
- [ ] Impressora instalada mostra botão "Controle de impressão" (com a parceria PrintControl)
- [ ] Central: alertas de firewall e de ERP aparecem quando houver esses contratos

## 5. Fim de jogo
- [ ] Forçar saldo negativo (Inspector do GameEconomy) e dormir vários dias: avisos em 10 e 20 dias, tela "Sua empresa faliu" no 30º
- [ ] "Recomeçar do zero" recarrega a cena com jogo novo

## 6. Salvar e carregar
- [ ] Salvar no meio do jogo, sair, voltar: sede, parcerias, projetos, licitações, foco, conquistas e objetivos continuam iguais

## 7. Parte 3D (rua, casa, cama e peças)
Depois de mudar o MontarCenario: **TI Service → 3. Montar cenário** (remonta e salva a cena).
Atalhos no Play: **TI Service → Teste (Play)** (aperte Esc antes, para o menu abrir com o cursor solto).
- [x] Porta do escritório abre com E; dá para sair a pé, atravessar a rua e subir a calçada
- [x] Porta da casa abre com E e o jogador passa (cápsula de 1,80 m)
- [x] Cama: "[E] Dormir..." → resumo "Você foi dormir às..." → acorda em casa olhando para a porta
- [x] Impressora e roteador com a frente virada para a sala
- [x] Peças da impressora: tampa abre de verdade, segurar E retira o cartucho, LED fica laranja sem toner
- [x] Parte de trás alcançável mirando por cima (equipamento encostado na parede)
- [ ] Postes acendem às 17h30 (conferir à noite)
- [ ] Roteador e computador: mirar em todas as peças e resolver um chamado inteiro só pelas peças

## O que anotar
- Textos cortados ou que vazam do cartão (os botões mais largos: "Controle de impressão", "Entregar N impressoras")
- Caracteres que aparecem como quadrado (→, ·, —)
- Qualquer coisa chata ou fácil demais: ajusto os números
