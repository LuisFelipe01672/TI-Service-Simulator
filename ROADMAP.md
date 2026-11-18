# Roadmap — TI Service Simulator

Plano de longo prazo. Cada fase termina **testada na Unity e com build**. Regras que valem sempre:
realismo (animação em toda ação, pista visual para todo problema), marcas fictícias, valores calibrados com o dono
(sem publicar números exatos), push só com pedido. Skills do projeto: `testar-na-unity`, `baixar-modelo-3d`,
`publicar-ti-service`.

Legenda: ✅ feito · 🔜 próxima · ⬜ a fazer · ❓ decisão do dono

## Ordem de execução (aprovada em 15/11/2026)
Depois de uma revisão com agents (design, realismo visual e código), as fases abaixo continuam valendo como
catálogo, mas a **ordem de trabalho** passa a ser esta. Cada etapa puxa os itens ⬜ das fases citadas.

0. ✅ **Base segura** — save à prova de queda (arquivo temporário, cópia .bak, save estragado guardado à parte),
   versão do save com migração em cadeia; testes automáticos (EditMode) do catálogo de problemas; consertos rápidos
   (cor neutra e SMAA, sombra encostando nos objetos, material da cozinha, marcas reais, glifos fora da fonte).
1. ✅ **Fechar o atendimento** — OS aberta no aceite e preenchida pelas ações (relato, perguntas, ações, peças,
   tempo); resolvido o defeito, o técnico mostra a OS, o cliente confere o checklist (testado, equipamento fechado,
   tudo ligado, chão limpo) e **assina numa prancheta 3D** com caneta; **estrelas e comentário** mexem na reputação
   (média no tablet); **causa raiz** (papel úmido, cabo solto no caminho do armário) faz o cliente voltar em 1–3 dias,
   urgente e em garantia; prazo virou **SLA em horas do jogo** com prioridade (Baixa 9h a Urgente 2h, desde a
   abertura). Também: **chamado guiado de toner** no primeiro dia (guia passo a passo no card) e **abas do tablet**
   liberadas pela trilha "Primeiros passos". Save versão 4; 20 testes novos (`Editor/Testes/TestesOSeSLA.cs`);
   atalhos em *TI Service/Teste (Play)/Atendimento*.
2. ✅ **Realismo visual da base** (16/11/2026) — **executável de 633 MB para 211 MB** (GLB viraram glTF com imagens
   separadas e comprimidas; `Editor/TexturasModelos.cs` acerta sRGB/compressão/tamanho; build LZ4HC); **luz
   pré-calculada nos interiores** (casca dos cômodos no lightmap com AO, sondas de luz por cômodo, reflexos baked com
   projeção em caixa; lâmpadas em Baked Indirect para os interruptores seguirem valendo; sol em tempo real por causa
   do ciclo do dia; paredes em duas metades, a de fora com a luz do dia); **móveis reais** no cliente e no escritório
   (mesa de metal de 75 cm, cadeira de escritório, gaveteiro, mesinha, aparador, sofá, plantas, lixeira de plástico);
   **close de câmera** em toda ação de olhar (fundo desfocado; no visor a pista é o próprio visor); **abajur** e
   **rotina da manhã** com café da manhã. FPS igual (~360 no quarto). Banheiro ficou para depois (feito na Etapa 3, com
   modelos do Sketchfab). Também pendente: notebook do escritório ainda é do kit simples.
3. ✅ **Três clientes com sala própria** (16/11/2026) — cada empresa vai para a sala do ramo dela
   (`Missions/SalaCliente.cs`): **escritório de contabilidade** (três estações, arquivo morto), **clínica** (recepção com
   balcão alto, sala de espera com pacientes sentados, impressora num aparador nos fundos) e **loja** (prateleiras e
   gôndola com mercadoria, mesa de promoção, balcão do caixa com o computador, vitrines). Impressora, roteador e
   computador são um conjunto só que vai para os pontos da sala ao aceitar o chamado. O **contato espera na recepção e
   leva o técnico até o equipamento** ("vem comigo"; espera se ele ficar para trás; o confuso só depois da conversa).
   **Gente trabalhando** (digitando, sentada esperando, repondo prateleira) com os personagens do Mixamo; monitores de
   decoração com uma planilha acesa. **Banheiro da casa** (pendência da Etapa 2): azulejo e porcelanato (ambientCG),
   vaso com caixa acoplada e pia de coluna (Sketchfab CC-BY), box de vidro com perfil de alumínio e chuveiro elétrico
   com eletroduto e registro (feitos em código). Luz pré-calculada nas salas novas; atalhos em
   *TI Service/Teste (Play)/Salas* (chamado em cada sala, seguir o contato, fotos); testes `TestesSalas`.
4. ✅ **Ferramentas de diagnóstico como progressão** (16/11/2026) — quatro ferramentas novas na loja, com peso no
   inventário, que **aparecem na mão do técnico** e mostram o resultado no próprio aparelho: **placa POST** (o visor
   corre os códigos e para no do defeito: 55 memória, apagado sem energia, A0 normal), **testador de fonte** (LEDs
   acendendo um a um, vermelho na tensão fora da norma, visor com 12 V/5 V/3,3 V e Power Good), **multímetro** (seletor
   gira, o número procura e firma; tomada, 12 V da fonte, bateria da placa-mãe e continuidade do fusível com bipe) e
   **pendrive de diagnóstico** (espetado; teste de memória e S.M.A.R.T./sensores numa **tela própria no monitor**).
   **Saúde das peças em %** (memória, fonte, disco, cooler, bateria da placa-mãe, cilindro) sorteada pela idade do
   computador; os defeitos derrubam a peça culpada. **Problemas novos** que a ferramenta certa resolve de cara: pente
   solto sem vídeo e sem apito (POST), fonte fraca que reinicia sob carga (testador/multímetro), memória com tela azul
   de vez em quando (pendrive), bateria da placa-mãe que perde a data (multímetro, troca da CR2032). **Check-up com
   laudo**: serviço novo (só aparece para quem tem pendrive e multímetro), termina quando todas as partes foram
   medidas; o laudo com medições e recomendações vai na OS que o cliente assina. Testes `TestesDiagnostico`; atalhos em
   *TI Service/Teste (Play)/Diagnóstico*. Ferramentas feitas por código: no Sketchfab só havia multímetro e pendrive
   CC-BY (candidatos para trocar o corpo, com aprovação do dono).
5. ✅ **Locação física** (16/11/2026) — instalar e visitar deixaram de ser botões no tablet: viram **chamados no
   cliente** (prioridade baixa, sem sorteio no catálogo; `CatalogoProblemas.InstalacaoLocacao` e `VisitaLocacao`,
   campo `servicoLocacao` no chamado). **Instalação**: a impressora de patrimônio chega **lacrada na caixa** no móvel
   da sala (etiqueta de patrimônio); abrir com o estilete faz ela subir de dentro e a caixa desmontar no chão com o
   isopor (sujeira do checklist); **fitas laranja** de transporte prendem a tampa e a gaveta; toner inicial da caixa
   (com lacre), papel, cabo de rede, ligar; o relatório mostra o IP por DHCP e o técnico **fixa o IP no painel**
   (o visor mostra o endereço); no computador do cliente, **adicionar a impressora pelo IP** (ação na tela ou no
   "Windows" do cliente) e a **página de teste pelo computador** fecha o serviço. Na assinatura: contrato começa a
   contar e implantação + frete viram boleto. **Visita mensal**: entra sozinha no dia 15 sem leitura do mês, com
   toner baixo sem reserva ou com a unidade de imagem perto do fim (ou pelo botão "Agendar visita no cliente");
   no local: **ler o contador no visor**, trocar o toner se estiver baixo, **troca preventiva da unidade de imagem**
   (a velha sai, a nova entra) e **toner reserva** deixado ao lado da impressora (itens da mochila, sem cobrar o
   cliente). Janela de manutenção mostra o que falta. Testes `TestesLocacao`; atalhos em
   *TI Service/Teste (Play)/Locação física* (passo a passo, inteiro e fotos em `Builds/Capturas/locacao`).
6. ✅ **Bancada e usados como objetos** (16/11/2026) — o lote comprado no tablet **chega em caixas** encostadas na parede
   do escritório (etiqueta "LOTE USADO" com o tipo); **E abre a caixa** (o equipamento sobe e a caixa desmonta), **E pega**
   (um por vez, na frente da câmera), **E na bancada** põe em cima; mirar o equipamento mostra as ações (roda do mouse,
   segurar E, como nas peças dos clientes). **Testar**: ligar na tomada e ver a pista — **monitor de bancada** novo com
   "sem sinal" ou a **área de trabalho do antigo dono** (planilhas, fotos), bipes de memória, LED, fumaça e cheiro de
   queimado na sucata, **folha com listra** na impressora, monitor piscando, roteador sem luz ou com todas piscando.
   **Diagnóstico** do computador sem vídeo com a **placa POST** (código 55 = memória, apagada = fonte), o **testador de
   fonte** ou trocando peças de bancada (sem ferramenta, demora). **Recuperar**: a peça ruim sai voando para a mochila e a
   nova entra (memória, fonte e cilindro da mochila; capacitores, fonte externa e bateria de notebook comprados no balcão);
   **limpar** (a poeira some, vale 10% a mais). **Apagar dados** com **tela de progresso** no monitor (LGPD; impressora:
   padrão de fábrica). **Etiqueta de patrimônio** física com número (TIS-0001...). **Destinos**: anunciar (foto com flash,
   plaquinha "À VENDA", o comprador retira no dia seguinte; com dados ainda pode dar multa), **colocar na frota de
   locação** (impressora limpa de dados e etiquetada vira patrimônio recondicionado do `ContratosManager`) ou levar a
   sucata na mão até o **lixo eletrônico** (sucata de computador: aproveitar o pente de memória antes). **Peça velha dos
   consertos** dos chamados (memória, fonte, HD, cilindro, bateria CR2032) volta para a mochila como "Peça velha" e vai
   para o lixo eletrônico (ou a estante). O tablet só mostra o estado, onde está e o próximo passo. Modelos reaproveitados
   do projeto (impressora, roteador, switch; `Resources/Usados/ModelosUsados`), resto em primitivas. Testes
   `TestesUsados`; atalhos em *TI Service/Teste (Play)/Usados na bancada* (lote de teste, bancada, próximo, passo, ciclo
   inteiro, fotos em `Builds/Capturas/usados`).
7. ✅ **Notebook e problemas de software** (16/11/2026) — antes, o código virou **uma definição por equipamento**
   (`Manutencao/Equipamentos/`: nome, peças, ações, "está funcionando?" e checklist; tipo sem definição dá erro em vez
   de cair no roteador; `EstadoEquipamentos` num arquivo por equipamento; cada `Problema` do catálogo carrega o próprio
   defeito, causa raiz, ferramenta ideal e se dá para resolver de longe — acabaram as chaves de texto e os switches).
   **Notebook do cliente** como 4º equipamento: aparece na mesa do computador, num lugar livre ao lado do monitor, com
   a tela TI OS na tampa; **bateria estufada** (touchpad levantado, balança na mesa; desligar, tirar da tomada, virar de
   barriga para cima, abrir a tampa de baixo, trocar), **tecla falhando** (teste no bloco de notas, capinha, ar
   comprimido), **carregador** (luz apagada, fita isolante, multímetro com 0 V, carregador universal), **superaquecimento**
   (ar quente, desliga sozinho, limpar ventoinha por baixo) e **dobradiça** (tela tombando, moldura, dobradiças + epóxi);
   peças novas na loja. **Problemas de software** com pista no monitor 3D e solução na tela do cliente: **conta
   bloqueada** (entrar como administrador, redefinir e desbloquear; só presencial), **e-mail preso na caixa de saída**
   (porta 587 + autenticação), **vírus/propaganda** (propagandas pulando, varredura, quarentena, desinstalar a barra,
   restaurar o navegador, reiniciar), **lentidão pela inicialização** (Gerenciador de Tarefas » Inicialização sem
   desligar o antivírus), **atualização que quebrou o sistema contábil** (desinstalar e ocultar; sem ocultar ela volta:
   retrabalho) e **impressora padrão virando PDF** (definir a padrão e desligar a troca automática). Mais **seis casos
   de atendimento remoto**. Marcas reais tiradas dos textos (TI OS, buscador, pacote de escritório). Testes
   `TestesEquipamentos` e `TestesNotebookSoftware`; atalhos em *TI Service/Teste (Play)/Notebook e software*.
   Modelo do notebook: "Laptop" (Aullwen, CC-BY), com a tela girando na dobradiça por código.
8. ✅ **Rede física** (17/11/2026) — projetos de **organização de rack** e de **cabeamento** deixaram de ser "trabalhar X
   horas" no tablet: o tablet mostra o andamento e manda o técnico ao cliente (a visita vira um chamado com OS e
   assinatura; `Chamado.projetoId`). **Rack de parede 12U** feito em código (`Missions/RackCliente.cs`,
   `EquipamentoPecas.Rack.cs`), 5º equipamento do cliente: vai para um pedaço livre da parede atrás do roteador (sem
   remontar a cena), com porta de vidro fumê, patch panel de 24 portas, organizador com anéis, switch com LED de link por
   porta, modem e régua. **Rack bagunçado** (serviço vendável): 8 cordões de 2,5 m pendurados e cruzados (tubos em
   código), porta que não fecha; **foto do antes** (flash), anotar o mapa das portas (sem ele, desconectar é erro),
   desconectar, **cordões de 0,3 m** porta a porta (o velho de 2,5 m não passa no organizador), organizador, **velcro**,
   **etiqueta porta a porta** (etiquetadora), luzes de link, fechar a porta e **foto do depois** — as duas fotos vão na OS.
   **Cabeamento em 3D**: até 4 pontos por visita na mesma parede do rack (marca de lápis » caixa de sobrepor e
   **canaleta** até 2,25 m e até o rack » cabo Cat6 » **crimpagem** com o minigame » **punch-down** no patch panel »
   **testador** com luzes 1–8 (ponta ruim: fios 3 e 6, refazer) » **etiqueta** nas duas pontas); o projeto só é entregue
   com todos os pontos testados (a equipe também passa pontos). **Reset do roteador com clipe**: o clipe entra no furo de
   trás enquanto o técnico segura E e as luzes piscam juntas no fim. Ferramentas novas na loja: punch-down e etiquetadora.
   Testes `TestesRede`; atalhos em *TI Service/Teste (Play)/Rede física*. Modelos `Patch_Panel`/`Switch_Rack_24`
   ainda não usados (o rack, o patch e o switch são primitivas com as portas no lugar certo para os cordões).
9. ➜ Continua na **leva 2** (abaixo).

Regras desta ordem: **sistemas novos de gestão ficam congelados até a etapa 5** (o que existe precisa ganhar corpo
físico primeiro). A cidade (Fase 6) pode começar por uma rua pequena com 3–4 endereços — decisão do dono quando
chegar lá (❓). Ideia do dono: partir de uma **cidade já pronta** e adaptar — avaliar traçado real de um bairro brasileiro
(OpenStreetMap, com crédito), pacotes prontos (Asset Store fica fora do Git) e modelos CC0/CC-BY, sempre leve para 4 GB. Realismo nunca vira tédio: depois de dominar uma tarefa, oferecer "fazer rápido".

## Ordem de execução — leva 2 (planejada em 17/11/2026)
A primeira ordem (etapas 0–8) terminou e está no GitHub. Nesta leva, cada etapa continua em conversa própria,
testada no Play, com testes automáticos, commit local e exe; push só com pedido.

9. 🟡 **Cidade** (Fase 6), partindo de algo **pronto** (regra "pronto antes do zero"):
   - 9A ✅ Pesquisa e escolha (17/11/2026): comparados traçado do OpenStreetMap, pacote da Asset Store (favela
     low poly, fora do Git) e modelos CC-BY/CC0. **O dono escolheu os modelos CC-BY** (casas e sobrados brasileiros
     do Sketchfab + texturas CC0 do ambientCG), com ruas, postes e fios feitos por código.
   - 9B ✅ Bairro com endereços (17/11/2026), tudo a pé e **sem teleporte**:
     - Planta única em `Scripts/Cidade/Bairro.cs` (ruas, calçadas, lotes, endereços): Rua das Acácias (escritório,
       casa, contabilidade, clínica, loja) cortada pela Rua Ipê Amarelo (ecoponto, hospital) e pela Rua dos
       Jacarandás (distribuidora de peças). O cenário, o GPS e os testes leem dela.
     - Salas dos clientes viraram prédios com a porta na calçada (a loja do outro lado da rua, girada); portas
       abrem com E; fachada com platibanda, caixa d'água azul e placa. Clínica num prédio de dois andares.
     - 22 casas e sobrados do Sketchfab nos lotes, orelhão, hospital do bairro; postes de concreto com cruzeta,
       transformador, caixa de emenda e fios caindo em curva; muro com portão de grade na casa do jogador.
     - **GPS** (`Cidade/GPS.cs`): aceitar um chamado liga o GPS com o endereço do cliente (canto inferior esquerdo:
       distância, "vire à direita", rua atual) e uma coluna verde na porta; "Você chegou" a 3 m. App **Mapa** no
       celular com as ruas, os endereços, você e "Ir" em cada endereço.
     - Atalhos de teste continuam levando direto à porta do cliente (`MissionManager.TeleporteDeTeste`);
       *Teste (Play)/Bairro/1* anda de verdade da casa ao escritório e aos 3 clientes (59 s, sem travar).
     - Medido: exe 222 → 243 MB; FPS no editor com × sem o bairro novo: quarto 181 × 210, porta do escritório 180 ×
       184, esquina da Ipê 109 × 119. 240 testes passando.
     - Ainda é só fachada: distribuidora de peças, ecoponto e hospital (sem interior nem uso no jogo).
   - 9C ✅ Carro do técnico (17/11/2026), dirigindo de verdade (sem viagem rápida); dá para ir a pé também:
     - Hatch popular de 3 portas "Milano '95" (CC-BY, Sketchfab) sem emblemas nem placas originais; placa Mercosul
       fictícia; porta do motorista e tampa traseira recortadas da carroceria e com dobradiça; volante e rodas giram.
     - `Cidade/CarroTecnico.cs`: E na porta entra (porta abre, senta, bate) e sai; câmera no banco do motorista
       (olhar em volta com o mouse); WheelCollider, tração dianteira, câmbio automático com ré, freio de mão,
       faróis com luz de verdade, luz de freio e de ré, buzina, som do motor; painel com km/h, marcha e combustível.
     - Combustível e km na escala do relógio (1 m no bairro = 60 m na cidade): 11 km/L, tanque de 48 L, reserva;
       posto na Rua dos Jacarandás, 340 (guia rebaixada, cobertura, bomba com visor, conveniência): completa o
       tanque e lança em "Combustível e deslocamento" no livro-caixa; pane seca → galão de 5 L a pé.
     - GPS dirigindo guia até a **vaga em frente** ao endereço (retângulo verde junto ao meio-fio, na mão certa) e
       a pé até a porta. A OS soma os km e o tempo dirigindo; chamado avulso cobra R$ 1,50/km no fechamento.
     - Porta-malas (`PortaMalas.cs`, `CargaFisica.cs`): abre/fecha; 4 volumes + escada dobrada (CC-BY, abre no
       chão) + maleta grande. [G] pega a encomenda da porta de casa e a caixa do lote de usados; a impressora de
       locação (chamado de instalação) sai do escritório na caixa e é largada na sala do cliente. Tudo no save.
     - Fim das ruas com barreiras zebradas e limite invisível em volta do bairro.
     - Medido: exe 243 → 253 MB; FPS no editor (com × sem bairro) quarto 207 × 229, porta 165 × 190, esquina
       108 × 125 (igual à 9B); dirigindo de casa à loja 194 FPS. 248 testes passando.
     - Falta: velocímetro do painel com ponteiro (hoje o número fica no HUD), interior escuro à noite, o carro não
       tem seta nem limpador; trânsito e pedestres ficam para a 9D.
   - 9D ✅ Vida na rua (17/11/2026), tudo leve e sem física complexa:
     - **Trânsito** (`Cidade/RotasTransito.cs`, `CarroNPC.cs`, `VidaNaRua.cs`): carros dos vizinhos com o mesmo hatch do
       técnico (malha única, 6 cores comuns, placa Mercosul fictícia sorteada) entram por uma ponta da rua e saem por
       outra, sempre nascendo e sumindo fora da vista. Mão direita, curva nos cruzamentos, param atrás de outro carro,
       de pedestre e do técnico (buzinam se ele não sai), na faixa de pedestre ocupada, no **PARE** da Ipê (para de vez e
       espera brecha na Acácias) e no **semáforo** da Jacarandás (verde/amarelo/vermelho, nunca os dois eixos abertos);
       conversão à esquerda espera quem vem de frente. Carro do técnico parado no meio-fio: desviam por fora, rente,
       quando a contramão está livre.
     - **Pedestres** (`Pedestre.cs`) nas calçadas a 85 cm da fachada, pela direita, desviando do técnico, uns dos outros
       e do orelhão; só atravessam a Acácias na faixa e esperam os carros pararem. Personagens do Mixamo (fora do Git)
       andando; sem eles, boneco de cápsulas balançando pernas e braços.
     - **Dia e noite**: postes acendem às 17h30 (luz de verdade só nos 8 mais perto da câmera; a lâmpada de todos
       brilha); 84 janelas das casas, sobrados e do hospital acendem aos poucos ao anoitecer e apagam depois das 22h
       (`JanelasNoturnas.cs`, medidas pelas fotos ortográficas das fachadas); faróis e lanternas dos carros (farol que
       ilumina só nos 3 mais perto); movimento por hora (pico 7h–9h30 e 17h–19h30, cheio no comercial, pouco à noite,
       menos no fim de semana).
     - Testes `TestesTransito` (rotas no asfalto e na mão certa, paradas fora do cruzamento e da zebra, semáforo, vez
       no cruzamento, calçadas, carro parando diante de pedestre/faixa/fila) + poste e PARE na esquina
       (`TestesPlantaCenario`). Atalhos *Teste (Play)/Rua* 1–5 (fiscal de batidas, gente fora da calçada, técnico no
       meio da rua, carro estacionado, fotos de dia/noite, FPS).
     - Medido no editor (90 s de trânsito cheio): 0 batidas, ninguém fora do asfalto/calçada, folga mínima de 2,7 m até
       o técnico; 5 carros desviaram do carro estacionado em 70 s. FPS (sem × com movimento): porta 165 × 146, esquina
       da Ipê 116 × 108, Jacarandás 160 × 135; à noite 155 × 130, 106 × 97, 155 × 131 (antes do corte dos postes a
       noite parada era 72 na esquina da Ipê). Exe 254 → 255 MB. 260 testes passando.
     - Fica para depois: **clima** (chuva atrasando serviço externo, pista molhada, guarda-chuva) — não sai barato
       (partículas, materiais molhados, regra de atraso nas OS); seta e farol alto nos carros dos vizinhos;
       ônibus/moto; pedestres entrando nas casas e lojas; os carros não "veem" o técnico dirigindo na contramão.
10. ✅ **Wi-Fi corporativo** (18/11/2026) — o projeto de Wi-Fi deixou de ser "trabalhar X horas" no tablet: virou visita
    no cliente, um access point por visita (projeto de 1 a 4 APs; a equipe também instala):
    - **Medição com o celular** (app Wi-Fi, `Missions/SurveyWiFi.cs`): o técnico anda pela sala e cada metro medido
      aparece colorido no chão (verde bom, laranja fraco, vermelho muito fraco); medidos 75% dos quadrados livres, o
      **mapa de calor** vai para a OS (antes e depois). O app também mostra o sinal onde ele está e as redes vizinhas
      com o canal de cada uma.
    - **Modelo de sinal** (`SinalWiFi`, em `Missions/AccessPointCliente.cs`): perda pela distância + móveis no caminho
      (metal pesa mais). O roteador da operadora numa mesinha deixa o fundo da sala abaixo de −67 dBm; AP no meio do
      teto cobre tudo; AP num canto deixa o canto oposto fraco (aí é tirar o AP, furo a mais no forro, e pôr em outro lugar).
    - **AP no teto** (criado em código, `EquipamentoPecas.WiFi`): marcar com lápis onde se mira no forro (só depois da
      medição), **escada aberta embaixo** (a do porta-malas), furar e fixar o suporte (pó de gesso no chão), canaleta
      do rack até o AP e cabo Cat6, crimpar (minigame), testar, encaixar o AP, ligar na **porta PoE 24 do switch**
      (luz laranja piscando 20 s, depois verde).
    - **Configuração** no navegador do computador do cliente (192.168.0.2): rede da empresa WPA2/WPA3, **rede de
      visitantes com isolamento**, canal fora do canal dos vizinhos; desligar o Wi-Fi do roteador da operadora; testar
      com o celular na rede de visitantes (sem isolamento ele enxerga a impressora e as pastas).
    - Chamados avulsos novos do roteador: **Wi-Fi lento por canal congestionado** e **visitantes usando a rede da
      empresa** (senha no quadro da recepção) — o painel do roteador ganhou canal, rede de visitantes e troca de senha;
      os dois dão para fazer por acesso remoto.
    - Testes `TestesWiFi` (276 passando); atalhos *Teste (Play)/Wi-Fi* 1–8; fotos em `Builds/Capturas/wifi`.
    - Medido no editor: antes −74 dBm (14 a 15 pontos fracos de 57), AP no meio −60 dBm, AP num canto −70 dBm (4 fracos).
    - Não testado jogando de verdade: a medição andando (o atalho mede sem andar), a crimpagem pelo minigame, o
      painel do AP clicando (o atalho liga as opções), a escada carregada do carro até a sala.
10B. ✅ **Consertos do teste do colega** (18/11/2026; o zip que ele testou era de 16/11, da Etapa 7):
    - **notebook** só vai para a mesa do cliente quando o chamado é dele (antes aparecia junto do computador em todo
      atendimento) — `NotebookCliente.Posicionar`;
    - **roteador**: não reproduzido nesta versão (fotos das 3 salas em `Builds/Capturas/consertos`: na mesinha, no
      lugar certo). Pedir ao colega para olhar de novo no zip novo;
    - **duas bancadas** no escritório: a de equipamentos (embaixo da janela, tampo livre para os usados) e a de cabos
      (depois da cafeteira, com a caixa de 305 m, o rolo e o alicate) — `Bancada.deCabos`, criadas pelo `DiaManager`;
    - **driver da impressora** na tela do computador (Impressoras e fila mostra "Driver: Impressora PCL genérica" e o
      botão de remover e reinstalar com o pendrive de drivers); a ação no monitor continua valendo;
    - aba **Negócios** virou um menu (Foco, Projetos, Licitações, Parcerias, Produtos próprios) e cada seção tem
      "« Voltar para Negócios".
    - Atalhos *Teste (Play)/Consertos* 1–3 (fotos). 276 testes passando.
10C. ✅ **Animação em toda troca de peça** (18/11/2026) — as peças andam **junto com a barra enquanto o técnico segura
    E** (`Manutencao/AnimacaoPecas.cs`: tabela rótulo » animação; soltou antes do fim, as peças voltam):
    - **computador**: os 2 parafusos da tampa giram e saem antes de o gabinete virar (e voltam ao fechar); a poeira
      encolhe enquanto o ar sopra; as travas brancas da memória abrem, o pente sai do slot, sobe e volta (o novo é mais
      verde); a fonte e o disco deslizam para fora e para dentro (o SSD entra no lugar do HD); os 4 parafusos do cooler
      soltam um de cada vez em X e o cooler sai; a pasta velha some com o pano e a nova cresce num grão;
    - **notebook** (antes não tinha nenhuma animação): fecha a tela, vira, os 10 parafusos da tampa de baixo saem um
      por um e a tampa sai (fechar: ao contrário); a bateria sobe e desliza para fora / entra; a poeira da ventoinha
      some; a capinha da tecla sai na espátula e volta; a moldura desgruda da tela; a dobradiça volta para o lugar; o
      carregador velho sai e o novo entra;
    - **impressora**: a folha amassada sai devagar pela tampa de trás; o cilindro velho sobe e o novo desce;
    - **ferramenta na mão** na frente da câmera: chave Phillips girando, lata de ar comprimido soltando poeira, pano
      esfregando, seringa de pasta com o êmbolo descendo, espátula alavancando;
    - mesa cheia no chamado de notebook: o cliente tira o telefone/bloquinhos de baixo do notebook (voltam depois).
    - Testes `TestesAnimacao`; atalhos *Teste (Play)/Animações* 1–4 (fotos em `Builds/Capturas/animacoes`). 278 passando.
    - Falta: os usados da bancada (outro visual, `UsadoVisual`) e ações instantâneas (tirar o cartucho, gaveta) seguem
      com as animações de antes; mãos de verdade (hoje só a ferramenta aparece).
10D. ✅ **Visual do tablet e do computador + fotos na loja** (18/11/2026):
    - **Fotos dos 27 itens** (`Editor/GerarIcones.cs`, menu *TI Service/Gerar ícones da loja*): notebook, multímetro,
      pendrive e cartucho são os modelos 3D do jogo (Sketchfab, CC-BY); o resto são miniaturas em primitivas (pente de
      memória com chips e contatos, fonte com ventoinha, testador com LEDs, alicate, SSD, placa POST...). Fundo
      transparente, em `Resources/Icones`.
    - **Loja** em vitrine de 3 colunas: foto num fundo claro, selo "VOCÊ TEM"/"A CAMINHO", preço em verde com a variação
      do dia, descrição curta e os botões; a mochila mostra as ferramentas em fotos e o que está a caminho também.
    - **Tudo com cantos arredondados** (`UIFactory.Arredondado`, 9-slice gerado em código) e sombra nos cartões;
      **tablet** com moldura escura, borda clara e câmera frontal.
    - **TI OS**: papel de parede em degradê, menu com ícone colorido e sigla em cada programa (o aberto em destaque),
      janela do programa clara com sombra e **barra de tarefas** (rede, contato e relógio).
    - Testes `TestesVisual` (todo item tem foto); atalho *Teste (Play)/Visual/1* (fotos em `Builds/Capturas/interface`).
      280 passando.
    - Falta: ícones nas abas do tablet e um tema claro/escuro; a janela de manutenção e a OS seguem com o visual de antes
      (só ganharam os cantos arredondados).
10E. ⬜ **Cidade maior** (dono: 6 a 10 vezes maior e com mais clientes). Hoje o bairro tem 124 x 89 m (~1,1 ha) e é um
    "H deitado" escrito à mão em `Cidade/Bairro.cs` (3 ruas fixas: Acácias, Ipê Amarelo, Jacarandás), com o trânsito
    (`RotasTransito`, 6 pontas fixas), o GPS, o semáforo/PARE, os pedestres e os testes presos a essas 3 ruas. Crescer
    6–10x pede trocar o "H" por uma **malha de ruas** genérica. Dividido em três, cada uma testada e com exe:
    - 10E-1 ✅ **Malha de ruas genérica, mesmo tamanho** (18/11/2026): `Cidade/MalhaRuas.cs` — lista de ruas
      (leste-oeste/norte-sul, eixo, começo/fim, largura do asfalto e de cada calçada, postes), cruzamentos calculados
      (PARE ou semáforo, rua principal), pontas, faixas de pedestre e guias rebaixadas. `Bairro.Malha` monta as 3 ruas
      de hoje nas mesmas posições; `Bairro.Pedacos()` sai da malha. Lendo da malha: rotas dos carros (caminho mais
      curto entre duas pontas no grafo das ruas), paradas/vez/semáforo/PARE por cruzamento, faixas, calçadas dos
      pedestres (em volta de cada quarteirão), e a montagem (faixa central, zebras, postes, semáforos, PAREs, linhas de
      retenção, barreiras do fim das ruas, frente das casas para as janelas noturnas, gramado, mapa do celular).
      **GPS** agora vai pelo caminho mais curto nas ruas: aponta para o próximo cruzamento ("vire à esquerda" na
      esquina) e mostra a distância pelo caminho.
    - Conferido igual: foto antes × depois (mesmo código de comparação) dos 21 pedaços de rua, das 30 rotas (pontos,
      limites e paradas) e da rede de calçadas — idênticos; cena remontada igual objeto por objeto, menos 4 traços da
      faixa central (saíram 2 que ficavam em cima das zebras perto da Ipê e da Jacarandás; entrou 1 em cada transversal
      logo ao norte da Acácias).
    - Testes: `TestesTransito` e `TestesPlantaCenario` sobre a malha, cada teste de rua rodando no bairro **e numa
      grade 3 x 3** (`MalhaRuas.Grade`: 6 ruas, 9 cruzamentos, 12 pontas, 132 rotas); novos: rotas pelo caminho mais
      curto, ruas cobertas de ponta a ponta, carro atravessando a grade parando no PARE, GPS no bairro e na grade,
      fachadas. 299 passando. Atalho novo *Teste (Play)/Bairro/4* (GPS pelo caminho: 5 de 5 direções certas, fotos
      `Builds/Capturas/bairro/gps_caminho_*`). Fiscal do trânsito (80 s cheio): 0 batidas, ninguém fora do
      asfalto/calçada.
    - FPS no editor, igual à 9D (sem × com movimento): porta 171 × 151, esquina da Ipê 116 × 106, Jacarandás
      156 × 157; à noite 158 × 136, 106 × 97, 157 × 129. Com × sem o bairro: quarto 175 × 194, porta 157 × 170,
      esquina 110 × 121.
    - Limites (para a 10E-2): rua termina na borda do mapa (sem "T" no meio do mapa); os pedestres só atravessam nas
      faixas; um relógio só para todos os semáforos.
    - 10E-2 ⬜ **Cidade ~6x** (~330 x 230 m, grade de quarteirões em volta do bairro atual): ruas novas, ~100 casas e
      sobrados com os modelos CC-BY já baixados (variações de cor, muro e portão), praça, comércios de fachada; desempenho
      para placa de 4 GB: distância de corte por camada, occlusion culling, luz de poste só perto; medir FPS nos pontos
      de sempre e em 3 novos (meta: não cair abaixo de 60 no editor).
    - 10E-3 ⬜ **Mais clientes**: 6 a 9 prédios de cliente com interior (cópias das 3 salas com outro layout/cor/móveis),
      cada cliente com o seu endereço (`SalaCliente.Para` por endereço), vagas, GPS, atalhos e fotos.
    Depois, se o FPS deixar, subir para ~10x.
11. ⬜ **CFTV**: câmeras, DVR/NVR, cabo e fonte; posicionar vendo a área certa; acesso pelo celular do cliente.
12. ⬜ **Nobreak e energia**: bateria morta apitando, dimensionar e instalar; queda de energia e raio queimando
    switch/fonte de quem não tem proteção.
13. ⬜ **Verificação geral (pente-fino de bugs)** — depois da cidade e das etapas acima, antes de novas funções:
   - Roteiro passando por todos os lugares, equipamentos e interações (casa, escritório, rua/cidade, clientes,
     bancada, rack), com fotos; + o **feedback do colega** que está testando o zip.
   - Checagens automáticas no editor: dois pontos de [E] perto demais um do outro; objetos atravessando móveis ou
     paredes; cômodos sobrepostos (já existe teste da planta); porta que leva a lugar errado.
   - **Bugs já vistos pelo dono** (anotados em 17/11/2026):
     - [E] muito colado: abrir a porta e acender a luz disputam o mesmo ponto (interruptor ao lado da porta);
     - mochila aparecendo **dentro da mesa**;
     - **roteador bugado** (detalhar ao reproduzir);
     - (resolvido em 16/11) banheiro na frente da porta do escritório.
     - carro (fotos da 9C): painel muito escuro; brilho forte no canto de cima dentro do carro; tampa do
       porta-malas aberta aparecendo no lugar errado e a caixa não aparece dentro.
     - a conferir jogando (9C): dirigir no teclado, caixa da locação e lote de usados no porta-malas, galão.
     - bairro (fotos da 9B): prédios como caixas lisas (fachada da clínica), brilho estranho nas janelas escuras,
       ruas terminando de repente com a borda do mapa visível.
     - rua (9D): carro dos vizinhos parado no fim da rua à vista espera até 8 s antes de sumir; o orelhão fica
       colado no caminho de quem anda pela calçada norte (dá para passar, mas apertado).

Ideias para a leva 3 (avaliar depois): servidor/NAS e backup 3-2-1 com teste de restauração; firewall/VPN;
queda do link da operadora; trabalho em altura e forro; ferramentas de rede (punch-down, escada, passa-fio);
funcionários visíveis saindo para atender (Fase 8).

## Visão
Simulador de técnico de TI brasileiro, do MEI em casa até uma empresa com sede, equipe e contratos. O prazer vem de
quatro coisas (inspiração em simuladores de trabalho do gênero):
1. **Trabalho físico satisfatório** — cada tarefa é feita com as mãos: abrir caixa, crimpar, tirar poeira, trocar
   peça, organizar rack (antes/depois).
2. **Diagnóstico de verdade** — o cliente descreve o problema de forma vaga ("tá lento", "a internet caiu"); você
   pergunta, mede, testa e descobre a causa.
3. **Crescimento visível** — o dinheiro vira coisas que você vê: ferramentas, carro, sede maior, prateleiras cheias,
   funcionários fazendo o que antes era você.
4. **Você melhora como técnico** — experiência por tipo de serviço, certificações, reputação na cidade.

E o tempero da **TI real**: "não mexi em nada", senha no post-it, backup nunca testado, toner acabando sexta às 17h,
certificado digital vencido, domínio vencido derrubando o e-mail, operadora que não resolve, cliente que não paga.

## Sistemas transversais (servem a todas as fases)
Construídos uma vez; cada equipamento novo só pluga as suas peças, causas e ações.

**Pilar gráfico** — o mais realista possível sem travar PCs médios
Decisão do dono (13/11/2026): **fica no Built-in RP**. Teste do cômodo de prova (RX 6500 XT 4 GB): Built-in 549 FPS
× HDRP 314 FPS, com ganho visual pequeno; o que mais pesa no realismo são os **modelos**, não o pipeline.
Ferramentas: menu *TI Service/Teste gráfico* (cena `Assets/Scenes/Testes/TesteGrafico_HomeOffice`, fotos em
`Builds/Capturas`). Reavaliar URP só se a Unity abandonar o Built-in.
- ✅ Post-processing v2 no jogo (ACES, bloom, AO, gradação de cor, vinheta)
- ✅ Tirar o tom marrom/amarelado da gradação de cor (cores mais neutras, como no teste HDRP) — Etapa 0
- ✅ Profundidade de campo só nos closes (CloseDePeca, Etapa 2)
- ✅ Iluminação baked (lightmaps) nos interiores + reflection probes + light probes para personagens (Etapa 2)
      (validada no cômodo de prova: peças sobrepostas nas quinas, modelos recebem luz por light probes)
- ⬜ Materiais PBR (ambientCG/Poly Haven) em todo piso, parede, metal e plástico; sujeira/desgaste onde faz sentido
- ⬜ Modelos realistas (Poly Haven CC0 / Sketchfab CC-BY) substituindo as caixas, cômodo por cômodo
- ⬜ Opções gráficas (baixo/médio/alto/ultra)

**Sistema de diagnóstico**
- ⬜ Chamado vago → lista de causas possíveis com peso; cada causa deixa pistas (LED, bipe, mensagem, cheiro de
      queimado, poeira, código na placa POST, leitura do multímetro)
- ⬜ **Conversa de triagem**: por ligação/mensagem você escolhe perguntas ("o que mudou?", "aparece alguma
      mensagem?", "só nesse computador?"); o cliente responde mal, esquece ou mente ("não mexi em nada")
- ✅ **Saúde das peças** em % vista com a ferramenta certa (Etapa 4: memória, fonte, disco, cooler, bateria da
      placa-mãe; desgaste pela idade do computador) (⬜ bateria do nobreak, cilindro visto pelo menu da impressora)
- ✅ Ferramentas compradas liberam ou aceleram pistas (Etapa 4: placa POST, testador de fonte, multímetro, pendrive)

**Ordem de serviço (OS) e checklist**
- ✅ Todo atendimento abre uma **OS**: problema relatado, diagnóstico, o que foi feito, peças, horas
      (⬜ deslocamento separado na OS)
- ✅ **Checklist do serviço** que o cliente confere no fim — item esquecido vira estrela a menos
      (⬜ itens por tipo de serviço: "testar todos os pontos", "etiquetar")
- ✅ **Cliente assina a OS** na prancheta (⬜ sem assinatura ele pode contestar a cobrança)
- ✅ **Teste de prova** anexado à OS: as medições das ferramentas viram o laudo da OS (Etapa 4)
      (⬜ antes/depois de temperatura e desempenho, página de teste)
- ⬜ **Fotos de antes/depois** anexadas à OS vão para o **portfólio** no celular e atraem clientes

**O técnico (personagem)**
- ⬜ **Energia/cansaço**: trabalhar até tarde rende hoje e cobra amanhã (mais lento, mais erro); dormir e comer
      recuperam; café dá um empurrão curto
- ⬜ **Experiência por tipo de serviço** (cabeamento, impressoras, computadores, redes, CFTV): mais rápido e menos
      erro com a prática; ligada às certificações
- ⬜ **Reputação na cidade** além das estrelas: clientes grandes só chamam quem tem nome
- ⬜ **Pressa tem risco**: subir a escada correndo, mexer na fonte sem desligar, crimpar sem conferir a ordem

**Celular e PC de casa**
- ⬜ Celular: mensagens fictícias com clientes (pedidos, fotos, cobrança), ligações de triagem, orçamentos, OS,
      GPS, banco, portfólio, alertas de monitoramento
- ⬜ PC de casa: sistema de chamados, loja, banco, **atendimento remoto** e **painel de monitoramento**

---

## Fase 0 — Fundação
- ✅ glTFast instalado e os 11 modelos importados (Switch_Rack_24: texturas 4K→2K e UVs unificadas para importar)
- ✅ Commit e push dos modelos + `CREDITOS.md` + skills + este roadmap (1f03f7f)
- ✅ `CLAUDE.md` na raiz com as convenções do projeto (português, UI por código, managers por evento, saves)
- ✅ Tela de **Créditos** no menu principal (autores do `CREDITOS.md`; exigido pelo CC-BY)
- ⬜ Corrigir a marcação falsa de "cena modificada" ao gerar o executável
- ✅ **Teste gráfico decisivo**: cômodo de prova (home office) com os mesmos modelos e texturas no
      Built-in + post-processing e numa cópia do projeto em **HDRP** (e/ou URP); comparar visual e FPS
- ✅ Pipeline gráfico escolhido: **Built-in** (ver Pilar gráfico)

## Fase 1A — Casa jogável (quarto + home office)
- ✅ O jogo novo **começa em casa** (ao lado da cama), não no escritório
- ✅ Quarto realista: cama de casal, 2 criados-mudos, despertador, porta-retrato, quadro, guarda-roupa, persianas,
      tapete, planta, teto de gesso (modelos Poly Haven + Sketchfab; persianas feitas por código)
- ✅ Abajur no criado-mudo e luz pré-calculada (baked) na casa
- ✅ Home office (visual): mesa de metal, cadeira presidente, PC, luminária, blocos de notas, relógio, estante de aço
      com caixa de ferramentas e caixas, mochila
- ✅ **PC do jogador** funcionando: E senta na cadeira (câmera desliza até o monitor) e abre o sistema "TI OS";
      o monitor mostra a tela do jogo (papel de parede, ícones, relógio) no lugar da textura original
- ✅ Mochila física: cada dia começa com ela no home office; E pega; sem ela, no cliente, ferramentas, peças e
      cabos não estão à mão (aviso ao chegar e lembrete ao aceitar chamado)
- ✅ Interruptores (quarto e sala) que apagam/acendem as luzes, relógio de parede com a hora do jogo
      (ponteiros de verdade; segundos em tempo real), sol entrando pelas persianas
- ✅ **Atendimento remoto** pelo PC de casa: botão "Acesso remoto" nos chamados (só sentado no PC); liga para o
      cliente, conecta e resolve software (spooler, fila, driver, IP do PC, reiniciar, config. do roteador); físico
      (cabo, toner, roteador fora = sem internet) pede visita; cliente às vezes ausente; paga 75%, gasta 30 min
- ⬜ Remoto: mais casos de software (senha, e-mail, impressora padrão) quando entrarem no catálogo
- ✅ **Celular do técnico** (tecla C): barra de status, relógio, Mensagens (cada chamado chega como conversa do
      contato, com "Vou aí!" que aceita), Banco (saldo e movimentações), aviso de mensagem nova
- ✅ Celular: GPS e mapa do bairro (Etapa 9B)
- ⬜ Celular: ligações de triagem (perguntas por telefone), orçamentos, fotos do problema
- ✅ **Energia do técnico** no HUD: cai com as horas e o trabalho; cansado anda/trabalha mais devagar; dormir cedo
      recupera tudo (tarde ou no escritório, menos); café na cozinha (+20, até 3 por dia) — salva no save
- ✅ Comer (geladeira) como outra forma de recuperar energia
- ✅ Rotina da manhã: acordar → café (bônus de energia) → ver chamados no celular/PC → sair de casa

## Fase 1B — Casa completa (cenário)
- ✅ Sala realista: sofá de couro, mesa de centro, tapete, rack de madeira com TV de tela plana (feita por código),
      planta, mesa redonda de jantar com cadeiras estofadas (Poly Haven)
- ✅ Cozinha realista: bancada compacta com pia, cooktop e forno sob a janela, geladeira inox (comer), micro-ondas em
      cima dela e cafeteira espresso (café) — Sketchfab CC-BY
- ⬜ Banheiro
- ⬜ Pisos/azulejos/paredes PBR em todos os cômodos
- ✅ Comer na geladeira: café da manhã/almoço/lanche conforme a hora, +30 de energia, R$ 12 (Alimentação no
      livro-caixa), intervalo de 3 h, até 3 por dia
- ❓ Fontes: Poly Haven (CC0) primeiro; Sketchfab CC-BY para o que faltar

## Fase 2 — Compras físicas, estoque e fornecedores
- ✅ Tudo que é comprado chega como **caixa de papelão na porta de casa** (interfone avisa), fica salvo até você
      buscar; E abre e guarda na mochila
- ✅ **Caixa de cabo de rede 305 m** realista: etiqueta "Cat6 · 305 m · 23 AWG", cabo azul saindo pelo furo; vai
      para a bancada do escritório e mostra quantos metros restam
- ✅ Saquinho de RJ45, caixas de toner e de cilindro como objetos na estante de estoque
- ✅ **Inventário com limite** (tecla I): a mochila carrega até 10 kg, cada item tem peso; o que não cabe fica na
      estante do escritório (E na estante ou I para guardar/pegar); encomenda que não cabe fica na porta
- ⬜ Ferramentas e equipamentos de locação como objetos
- ✅ Cabos prontos enrolados na estante, cada rolo com o tamanho na etiqueta (ponta ruim em laranja)
- ✅ **Estante de estoque** no escritório ligada ao estoque do sistema: mostra o que você tem e esvazia ao usar
- ✅ 3 fornecedores fictícios (Distribuidora Centro TI, Loja do Bairro, MegaShop Online) com preço, prazo, expressa e
      confiabilidade diferentes: **entrega atrasada** (o fornecedor avisa), **peça errada**, **peça com defeito**
- ✅ **Preço de mercado oscilando** por dia e por item (−15% a +25%), mostrado na loja ("+8% hoje")
- ✅ **Garantia/RMA**: peça errada ou com defeito descoberta ao abrir a caixa; troca automática sem custo, chega dias depois
- ✅ **Equipamentos usados**: a cada 4 dias aparece um lote de empresa que fechou (aba Loja); testar, recuperar,
      apagar os dados (LGPD: vender sem apagar pode dar multa), vender ou levar a sucata ao ponto de coleta
- ✅ Usados como objetos no escritório e opção de colocar em locação (Etapa 6): caixas do lote, bancada com teste,
      diagnóstico, troca de peça, limpeza, etiqueta de patrimônio, anúncio ou frota de locação
- ✅ **Logística reversa**: caixa no escritório recebe os toners usados (R$ 8 cada da recicladora + reputação)
- ✅ Descarte de equipamentos (Etapa 6): apagamento com tela de progresso (LGPD) antes de revender; sucata levada na
      mão até o lixo eletrônico; peças velhas dos consertos também

## Fase 3 — Impressora completa e locação  (detalhe em `PLANO_FASE_IMPRESSORA.md`)
- ✅ Peças com mira + E, tampa animada, crimpagem animada em 3D
- ✅ Modelo realista no cliente (Impressora_MFP_Compacta, materiais próprios sem logotipo): a parte de cima
      (scanner) levanta na dobradiça e mostra o toner realista; peças de mira encaixadas por âncoras
- ✅ Visor LCD com texto real conforme o estado ("Pronta", "Tampa aberta", "Instale o toner", "Toner baixo",
      "Toner esgotado", "Sem rede"; apagado com a impressora desligada)
- ✅ Cartucho na mão do técnico (sai da impressora até a frente da câmera, balança ao chacoalhar, fita do lacre sai)
- ⬜ Close de câmera nas peças
- ✅ Toner completo: retirar (vai para a mão), olhar (peso/lacre), chacoalhar, recolocar o MESMO, guardar o usado na
      mochila (descarte), pegar novo, puxar o lacre, encaixar
- ✅ Traseira: plugue RJ45 entra/sai animado (pendurado quando solto) e LED da porta piscando com link
- ✅ Testador com luzes 1–8 na mão (duas unidades, fios com defeito ficam apagados)
- ✅ Página de teste sai pela frente com o resultado na folha (perfeita, em branco, clara com falhas, listra vertical)
- ✅ Problemas novos: papel atolado (puxar pela tampa traseira), sem papel (gaveta), cilindro riscado (listra na folha;
      trocar a unidade de imagem — item novo na loja)
- ✅ Mais problemas: IP mudou (relatório de configuração), tampa mal fechada, driver errado — via sistema de
      diagnóstico ("a impressora não imprime" pode ser qualquer um)
- ✅ **Franquia de páginas + excedente**: todo mês ler o contador (no visor, em chamado, ou na visita) e cobrar o excedente
- ✅ **Peças por tempo de uso** (unidade de imagem; fusor e rolos ainda não): cilindro, fusor e rolos duram X mil páginas; trocar antes evita chamado, tarde
      demais gera chamado e cliente bravo
- ✅ **Toner reserva no cliente** (estoque deixado lá) para evitar visita urgente; controlar quanto sobrou

## Fase 4 — Computador completo
- ✅ Modelo realista no cliente (Computador_Mesa): monitor, teclado e mouse; tela viva "TI OS" que apaga com o PC
      desligado (película preta cobre a textura original com marca real); peças de mira por âncoras
- ✅ Gabinete (torre) ao lado do monitor: o técnico vira o gabinete, tira a tampa lateral e vê placa-mãe, pentes,
      cooler, fonte, HD e a poeira; só abre com o PC desligado
- ✅ Peças internas animadas: o pente de memória e a fonte saem e voltam na troca
- ✅ HD com setores ruins: teste de saúde do disco (pendrive), backup antes, troca por SSD (o disco sai e entra o
      SSD), instalar o sistema e restaurar; trocar sem backup = cliente perde os arquivos (erro grave)
- ⬜ Placa de vídeo trocável (✅ bateria da placa-mãe: medir com multímetro e trocar a CR2032, Etapa 4)
- ✅ **Limpeza**: manta de poeira visível por dentro; pincel e ar comprimido (ferramenta) limpam; poeira = superaquecimento
- ✅ PC superaquecendo desliga sozinho depois de ~25 s ligado; a ventoinha gira mais rápido
- ⬜ Nuvem de poeira saindo na limpeza
- ✅ **Pasta térmica**: tirar o cooler (ele sai e mostra a pasta seca), limpar com álcool isopropílico, aplicar um grão
      de pasta nova, recolocar; recolocar sem pasta piora; superaquecimento com o gabinete limpo = pasta
- ⬜ Organização de cabos dentro do gabinete
- ⬜ Cabos atrás: rede, energia, vídeo (monitor sem imagem por cabo solto)
- ⬜ Monitor mostrando o "Windows" do jogo na própria tela 3D (o computador simulado que já existe) — a textura
      original do Computador_Mesa mostra um Windows real: ✅ coberta pela tela do jogo (TelaComputador)
- ✅ **Ferramentas de diagnóstico**: testador de fonte, multímetro, **placa POST**, pendrive de diagnóstico (teste
      de memória, saúde do disco), testador de cabo (Etapa 4) (⬜ antivírus no pendrive; modelos 3D do Sketchfab)
- ✅ **Check-up completo** (serviço vendável): inspeção de todas as peças com laudo de saúde em % (Etapa 4)
      (⬜ recomendações do laudo virarem orçamento)
- ⬜ **Compatibilidade de peças**: memória do tipo errado não encaixa, fonte fraca desliga sob carga
- ✅ Problemas de hardware: superaquecimento por poeira, memória ruim (3 bipes, sem vídeo), fonte queimada (não liga)
- ⬜ Problemas: lentidão, vírus/ransomware, Windows não inicia, HD com setor
      ruim (backup antes de trocar), atualização do Windows quebrou o sistema da contabilidade
- ✅ Peça velha volta para a mochila (Etapa 6) e vai para o lixo eletrônico do escritório (RMA ainda não)
- ⬜ Montagem de PC sob encomenda (orçamento → peças → montagem → teste de desempenho)
- ⬜ **Notebook**: tela, teclado, bateria estufada, dobradiça, limpeza

## Fase 5 — Rede, infraestrutura, CFTV e energia
- ✅ Roteador realista (Roteador.glb) com 4 LEDs por estado, plugue da fonte e furo de reset atrás (âncoras)
- ✅ Reset com clipe animado (segurar o clipe no furo) — Etapa 8
- ✅ Rack de parede com patch panel, organizador e switch de rack (em código; Etapa 8) · ⬜ switch de mesa e usar os
      modelos `Patch_Panel`/`Switch_Rack_24` já baixados
- ✅ **Projeto de cabeamento jogável em 3D**: passar cabo por canaleta, crimpar, conectar no patch panel, testar
      ponto a ponto, identificar com etiqueta — Etapa 8
- ⬜ **Trabalho em altura e forro**: escada, abrir placa de forro, poeira; risco de cair se for com pressa
- ✅ **Organizar rack bagunçado** (antes/depois) — serviço vendável — Etapa 8
- ✅ Wi-Fi: posicionar access point, sinal fraco nos cantos (mapa de calor), **rede de visitantes separada** — Etapa 10
- ⬜ Firewall em comodato no rack do cliente; **VPN** para quem trabalha de casa
- ⬜ **Servidor/NAS** no escritório do cliente; **backup 3-2-1** (HD externo + NAS + nuvem) e **teste de restauração**
- ⬜ **Queda do link da operadora**: provar que não é sua culpa, abrir chamado com a operadora; vender link de backup
- ⬜ **CFTV**: câmeras, DVR/NVR, cabo, fonte; posicionar vendo a área certa, acesso pelo celular do cliente
- ⬜ **Nobreak**: bateria morta, apitando, dimensionar e instalar; queda de energia e **raio** queimando
      switch/fonte de quem não tem proteção
- ⬜ Ferramentas: alicate de crimpar, testador de cabo, punch-down e etiquetadora (✅ Etapa 8), escada, furadeira,
      passa-fio, detector de tensão
- ⬜ **Fiscalização** em obras de cabeamento maiores (documentação, ART/responsável técnico) ❓ calibrar com o dono

## Fase 6 — Cidade e deslocamento (dirigindo de verdade)
Decisão do dono: **dirigir de verdade** (sem viagem rápida por mapa).
- ✅ Carro do técnico dirigível (entrar/sair, volante, câmera em 1ª pessoa, física simples com WheelCollider) — Etapa 9C
- ✅ Cidade com ruas ligando casa, sede, fornecedores, ponto de coleta de lixo eletrônico e clientes; GPS no celular
      (Etapa 9B: bairro a pé; fornecedor e ecoponto ainda só fachada)
- ✅ Combustível (posto), estacionar na frente do cliente, carregar caixas/equipamentos/escada no porta-malas — Etapa 9C
- ⬜ Vários endereços (clínica, contabilidade, loja, escola, prefeitura, condomínio) — cada cliente com sala própria
- ⬜ Trânsito simples (carros NPC, semáforos); **clima**: chuva atrasa e impede serviço externo, temporal traz raios
      e chamados
- ✅ Cobrança de deslocamento/km no orçamento (R$ 1,50/km no chamado avulso) — Etapa 9C
- ⬜ Mais tarde: trocar de veículo (moto → carro → van da empresa adesivada)

## Fase 7 — Clientes e pessoas vivas
- ✅ Clientes Mixamo animados (parado / conversando)
- ⬜ Funcionários do cliente sentados digitando, andando, atendendo telefone (animações já baixadas)
- ⬜ Cliente leva você até o equipamento; reage ao seu trabalho (impaciência, alívio)
- ⬜ **Personalidades**: o que jura que "não mexeu em nada", o da senha no post-it, o apressado, o que pechincha, o
      que **não paga**, o fiel que indica outros
- ⬜ **Horário comercial do cliente**: certos serviços só com o escritório vazio (parar servidor/rede) — fim de
      tarde, noite ou fim de semana
- ✅ **Avaliação por estrelas** depois de cada atendimento; **retrabalho/garantia** quando a causa do problema ficou
      (⬜ mais causas raiz além do papel úmido e do cabo solto)
- ⬜ **Golpes e engenharia social** nos clientes (ligação do "banco", e-mail falso, phishing): limpar o estrago e
      vender treinamento
- ⬜ Mais variedade de personagens (Mixamo) e escritórios com cara própria por tipo de cliente
- ⬜ Seus funcionários visíveis na sede trabalhando e saindo para atender

## Fase 8 — Empresa, contratos e progressão
- ⬜ Terminar testes: vencer licitação, alertas de firewall/ERP na Central, serviços mensais, equipe e folha,
      virar ME, produto próprio (ERP/bot)
- ⬜ **Orçamento negociável** (peças + horas + deslocamento): cliente aprova, pechincha ou some
- ⬜ **Você define seus preços** (visita, hora técnica, formatação, contrato): caro demais = cliente vai embora;
      barato demais = você quebra
- ⬜ **Contrato mensal de suporte** (pacote de horas) com **prioridade e prazo (SLA)**: "servidor parado" é urgente,
      "trocar mouse" pode esperar; atraso gera multa
- ⬜ **Monitoramento remoto** dos clientes de contrato (agente instalado): alertas de disco cheio, backup que falhou,
      nobreak na bateria, antivírus desatualizado — resolver antes do cliente ligar vale estrelas
- ⬜ **Visita preventiva mensal** (limpeza, atualização, conferência de backup, leitura de contadores)
- ⬜ **Funcionário novo no cliente**: criar e-mail/conta/licença, preparar notebook, configurar impressora
- ⬜ **Funcionário que sai**: bloquear acessos, trocar senhas, recolher equipamento — esquecer gera problema
      (ex-funcionário apagando arquivos)
- ⬜ **Domínio, e-mail e site** do cliente: domínio vencido derruba o e-mail da empresa inteira; renovação, DNS,
      migração de e-mail
- ⬜ **Documentação da rede do cliente** (IPs, senhas, mapa de pontos, equipamentos com **etiqueta de patrimônio**):
      documentado = chamado resolvido mais rápido
- ⬜ **Certificações/cursos** que liberam linhas de serviço (CFTV, redes avançadas, firewall, servidores, backup)
- ⬜ **Funcionários com especialidade** (auxiliar, técnico, comercial, limpeza) assumem tarefas que eram suas —
      você vê eles fazendo
- ⬜ **Contas do mês**: aluguel, luz, internet, contador, impostos (DAS/Simples), combustível, folha
- ⬜ Problemas brasileiros: **certificado digital A1/A3 vencido** (emissor de NF-e parado), maquininha/Pix sem
      internet, sistema da contabilidade, LGPD (vazamento por senha fraca)
- ⬜ **Plantão e serviço fora do horário** (trocar servidor no domingo) pago mais caro; sobreaviso
- ⬜ **Sazonalidade**: volta às aulas, Imposto de Renda e fim de ano (contadores lotados), Black Friday (lojas),
      começo do ano (licitações)
- ⬜ **Concorrência visível**: concorrente que baixa o preço e tenta roubar cliente (já existe no sistema; mostrar
      no mundo e nas mensagens)
- ⬜ **Escolhas tentadoras**: programa pirata (rápido e barato, mas risco de fiscalização e de perder clientes
      grandes), "esquecer" de emitir nota
- ⬜ **Sede física** evoluindo (sala → sala maior → galpão), com móveis comprados aparecendo; **sede suja ou
      bagunçada** derruba a impressão de quem te visita
- ⬜ Eventos com cena: cliente furioso na porta, queda de energia, rato no forro roendo cabo, ransomware num cliente
      sem backup, backup "que nunca foi testado"
- ⬜ Balanceamento com o dono (valores calibrados com as notas privadas, fora do repositório; M365, ERP e controle
      de impressão a confirmar ❓)

## Fase 9 — Polimento
- ⬜ Sons gravados (Kenney Audio CC0 / Freesound CC0) no lugar dos gerados; música ambiente
- ⬜ Tutorial guiado no primeiro dia (com os objetivos "Primeiros passos")
- ⬜ Configurações gráficas completas, otimização (texturas 4K do Mixamo → 2K, LOD, oclusão)
- ⬜ Vários saves (slots), autosave
- ⬜ Menu principal 3D (câmera passeando pela casa/escritório)
- ⬜ Conquistas ligadas a situações reais ("Salvou a contabilidade em pleno IR", "Rack impecável", "Backup salvou o dia")
- ⬜ Revisão de textos, acentuação e glifos

## Fase 10 — Lançamento
- ⬜ Build estável sem console de debug por padrão
- ⬜ Página (itch.io / Steam), capturas e trailer
- ⬜ Revisão de licenças (CREDITOS.md completo, sem marcas reais, Mixamo embutido só no build)

---

### Ideias soltas (avaliar depois)
- Modo "dia livre" sem pressão de prazo
- Modo carreira com metas (ex.: "ter 10 contratos mensais em 1 ano")

### Como executar
Uma fase por vez, na ordem; os sistemas transversais nascem na primeira fase que precisa deles e crescem depois.
Em cada sessão: pegar o próximo ⬜, implementar, testar na Unity (skill `testar-na-unity`), marcar ✅ aqui,
commit local; push quando o dono pedir. Itens ❓ precisam da resposta do dono antes de começar.
