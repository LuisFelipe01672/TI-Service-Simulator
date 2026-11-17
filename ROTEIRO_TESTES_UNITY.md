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

## 4b. Locação física (Etapa 5)
- [ ] Contrato fechado: a instalação aparece na lista de chamados; a aba Contratos diz "Instalação agendada"
- [ ] No cliente a impressora está na caixa; abrir faz ela subir e a caixa vai para o chão (recolher antes de assinar)
- [ ] Fitas laranja seguram a tampa; toner da caixa com lacre; papel; cabo; ligar; relatório mostra IP por DHCP
- [ ] Fixar o IP no painel (visor mostra o endereço); adicionar a impressora pelo IP no computador; página de teste
      pelo computador → cliente confere e assina; contrato instalado e implantação + frete no Empresa
- [ ] "Agendar visita no cliente": ler o contador no visor, trocar toner baixo, troca preventiva (unidade de imagem
      animada), toner reserva aparece ao lado da impressora; contrato com toner cheio, reserva e leitura do mês

## 4c. Bancada e usados (Etapa 6)
- [ ] Comprar o lote na Loja: caixas "LOTE USADO" aparecem no escritório, perto da porta
- [ ] E abre a caixa (estilete), E pega o equipamento, E na bancada põe em cima; E no chão (sem nada mirado) larga
- [ ] Mirar o equipamento na bancada: ligar e testar (monitor de bancada: sem sinal / área de trabalho do antigo dono;
      folha com listra na impressora; luzes do roteador/switch; fumaça na sucata)
- [ ] Computador sem vídeo: placa POST (55), testador de fonte ou troca na bancada; trocar a peça (velha vai para a mochila)
- [ ] Limpar (poeira some), apagar dados (barra de progresso), etiqueta de patrimônio, anunciar (plaquinha "À VENDA")
- [ ] Dia seguinte: o comprador levou o anunciado; impressora etiquetada e sem dados vai para a frota (aba Contratos)
- [ ] Sucata na mão → lixo eletrônico; peças velhas da mochila também vão para lá

## 4d. Notebook e problemas de software (Etapa 7)
Atalhos: *TI Service/Teste (Play)/Notebook e software* (um chamado de cada problema, passos e fotos em `Builds/Capturas/notebook`).
- [ ] Chamado de notebook: o notebook aparece na mesa do computador, ao lado do monitor, num lugar livre, com a tela TI OS
- [ ] Bateria estufada: touchpad levantado e o notebook balançando; desligar, tirar da tomada, virar e abrir a tampa de baixo
      (o notebook vira de barriga para cima), tirar a bateria inchada (vai para a mochila), pôr a nova, fechar, ligar
- [ ] Tecla A: testar no bloco de notas (a letra some), tirar a capinha, soprar, encaixar (clique)
- [ ] Carregador: luz apagada e fita isolante no cabo; multímetro mostra 0 V; trocar pelo universal (luz verde)
- [ ] Superaquecimento: ar quente na lateral, desliga sozinho; abrir por baixo e limpar a ventoinha
- [ ] Dobradiça: a tela tomba para trás; soltar a moldura (fica na mesa), trocar as dobradiças, encaixar a moldura
- [ ] Software no monitor do cliente: tela de conta bloqueada, propagandas pulando, erro do sistema contábil, caixa de
      saída do e-mail, janela de salvar PDF, otimizador na inicialização — cada um some quando resolvido
- [ ] Na tela do cliente (TI OS): entrar como Administrador e redefinir/desbloquear; e-mail porta 587 + autenticação e
      Enviar; antivírus (verificação, quarentena), desinstalar a barra de ofertas, restaurar o navegador, reiniciar;
      Gerenciador de Tarefas » Inicialização; Atualizações (desinstalar e ocultar/pausar); impressora padrão
- [ ] Remoto: os de software resolvem de longe; senha bloqueada e defeitos físicos pedem visita

## 4e. Rede física (Etapa 8)
Atalhos: *TI Service/Teste (Play)/Rede física* (visita do rack, visita de cabeamento, passos, reset com clipe, trocar de
sala, fotos em `Builds/Capturas/rede`). Loja: punch-down e etiquetadora são ferramentas novas.
- [ ] Projeto de rack ou cabeamento com material entregue: o tablet (Negócios) mostra o andamento e "Ir ao cliente
      executar"; não tem mais o botão de trabalhar horas nesses dois
- [ ] Rack de parede no cliente, perto do roteador, sem cobrir quadro/janela; porta de vidro abre e fecha
- [ ] Rack bagunçado: 8 cordões de 2,5 m pendurados e cruzados, porta não fecha; foto do antes (flash), anotar o mapa,
      desconectar (cordões velhos no chão), cordões de 0,3 m um por um, organizador, velcro, etiqueta porta a porta,
      luzes de link no switch, fechar a porta, foto do depois; a OS mostra as duas fotos
- [ ] Desconectar sem anotar o mapa conta erro; cordão velho de 2,5 m não passa no organizador
- [ ] Cabeamento: marca de lápis » caixa e canaleta (furadeira, canaleta cresce até 2,25 m e corre até o rack) » cabo
      (sobra pendurada) » crimpar (minigame) » punch-down no patch panel (a ferramenta bate duas vezes) » testador
      (luzes 1–8) » etiqueta na tomada e no patch; ponta mal crimpada: testador acusa 3 e 6, refazer
- [ ] Projeto só entrega quando todos os pontos foram testados e etiquetados (até 4 por visita; a equipe também faz)
- [ ] Reset do roteador: segurar E no reset com o clipe; o clipe entra no furo, perto dos 10 s as luzes piscam juntas

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
- [x] Postes acendem às 17h30 (conferido às 20h30)
- [x] Roteador e monitor do cliente com a frente para a sala; peças respondem à mira
- [ ] Resolver um chamado inteiro só pelas peças (roteador e computador)

## 7b. Bairro a pé e GPS (Etapa 9B, 17/11)
Atalhos: **Teste (Play) → Bairro** (1: anda sozinho casa → escritório → 3 clientes → casa; 2: fotos do GPS e do
mapa; 3: FPS com e sem o bairro). Fotos do cenário sem Play: **Teste gráfico → Fotos do bairro (cena aberta)**.
- [x] Andar (automático) da casa ao escritório, à contabilidade, à loja, à distribuidora, à clínica e de volta, sem travar
- [x] Aceitar chamado não teleporta: o GPS liga com o endereço do cliente e desliga ao chegar ("Você chegou")
- [x] Celular → Mapa: ruas, endereços, você; "Ir" liga o GPS para o endereço escolhido
- [ ] Jogando com teclado e mouse: chamado na clínica, ir a pé seguindo o GPS, resolver e voltar para casa
- [ ] À noite: postes acesos ao longo das três ruas, sem pesar (FPS)
- [ ] Conferir se alguma casa vizinha ficou torta, flutuando ou com a frente para o lado errado

## 8. Testado em 13/11 (tarde) — sistemas
- [x] Empresa: MEI, ordem das seções, empréstimo, sala 20 m², estações 1/3
- [x] Negócios: foco (recusa por 30 dias), parcerias (negociar/solicitar/aprovada), projeto inteiro (levantamento → orçamento → material → 8 h → entregue → NFS-e → boleto pago)
- [x] Licitação: proposta enviada, resultado na sessão (inabilitado por falta de parceria)
- [x] Contrato de impressora: compra, proposta, instalação, controle de impressão (com PrintControl)
- [x] Falência (avisos 10/20 dias, tela no 30º, recomeçar) e salvar/carregar
- [ ] Vencer uma licitação (precisa de 2 atestados); alertas de firewall/ERP na Central (precisa desses contratos)

## Jogar fora da Unity
**TI Service → 4. Gerar executável (Windows)** cria `Builds/Windows/TI Service Simulator.exe` (a pasta inteira é o jogo; não vai para o Git).

## Atalhos de teste (TI Service → Teste (Play))
Ir para porta/rua/casa/cama/equipamentos, mirar peça por peça, pular 1 ou 10 dias, gerar pedido de projeto, edital ou contato de locação.

## O que anotar
- Textos cortados ou que vazam do cartão (os botões mais largos: "Controle de impressão", "Entregar N impressoras")
- Caracteres que aparecem como quadrado (→, ·, —)
- Qualquer coisa chata ou fácil demais: ajusto os números
