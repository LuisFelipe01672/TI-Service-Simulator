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
1. 🔜 **Fechar o atendimento** — OS preenchida pelas ações, assinatura do cliente, estrelas e comentário, retrabalho
   quando a causa fica, prazo virando SLA em horas do jogo (fim do cronômetro em segundos). Logo depois: tutorial do
   primeiro dia e abas do tablet liberadas aos poucos.
2. ⬜ **Realismo visual da base** — luz pré-calculada nos interiores (light probes, reflection probes baked),
   texturas dos GLB extraídas e comprimidas (meta: build perto de 200 MB), móveis do cliente sem o kit simples,
   telas que refletem, close de câmera nas peças, pendências da casa (banheiro, abajur, rotina da manhã).
3. ⬜ **Três clientes com sala própria** (contabilidade, clínica, loja), o cliente leva o técnico até o equipamento.
4. ⬜ **Ferramentas de diagnóstico como progressão** (placa POST, testador de fonte, multímetro, pendrive) e saúde
   das peças; check-up vendável com laudo.
5. ⬜ **Locação física** — impressora de patrimônio na caixa, instalação e IP no painel, visita mensal no local
   (contador no visor, toner reserva, troca preventiva).
6. ⬜ **Bancada e usados como objetos** — testar, recuperar, apagar dados, etiquetar patrimônio, vender ou locar.
7. ⬜ **Notebook e problemas de software** (senha, e-mail, vírus, lentidão, impressora padrão). Antes: reorganizar
   o código para **uma definição por equipamento** (estado, ações, quebrar/resolvido e peças num lugar só).
8. ⬜ **Rede física** — rack, patch panel, cabeamento com antes/depois (Fase 5).

Regras desta ordem: **sistemas novos de gestão ficam congelados até a etapa 5** (o que existe precisa ganhar corpo
físico primeiro). A cidade (Fase 6) pode começar por uma rua pequena com 3–4 endereços — decisão do dono quando
chegar lá (❓). Ideia do dono: partir de uma **cidade já pronta** e adaptar — avaliar traçado real de um bairro brasileiro
(OpenStreetMap, com crédito), pacotes prontos (Asset Store fica fora do Git) e modelos CC0/CC-BY, sempre leve para 4 GB. Realismo nunca vira tédio: depois de dominar uma tarefa, oferecer "fazer rápido".

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
- ⬜ Tirar o tom marrom/amarelado da gradação de cor (cores mais neutras, como no teste HDRP)
- ⬜ Profundidade de campo só nos closes
- ⬜ Iluminação baked (lightmaps) nos interiores + reflection probes + light probes para personagens
      (validada no cômodo de prova: peças sobrepostas nas quinas, modelos recebem luz por light probes)
- ⬜ Materiais PBR (ambientCG/Poly Haven) em todo piso, parede, metal e plástico; sujeira/desgaste onde faz sentido
- ⬜ Modelos realistas (Poly Haven CC0 / Sketchfab CC-BY) substituindo as caixas, cômodo por cômodo
- ⬜ Opções gráficas (baixo/médio/alto/ultra)

**Sistema de diagnóstico**
- ⬜ Chamado vago → lista de causas possíveis com peso; cada causa deixa pistas (LED, bipe, mensagem, cheiro de
      queimado, poeira, código na placa POST, leitura do multímetro)
- ⬜ **Conversa de triagem**: por ligação/mensagem você escolhe perguntas ("o que mudou?", "aparece alguma
      mensagem?", "só nesse computador?"); o cliente responde mal, esquece ou mente ("não mexi em nada")
- ⬜ **Saúde das peças** em % (HD 34%, bateria do nobreak 10%, cilindro 80%) vista com a ferramenta certa;
      desgaste com o tempo de uso
- ⬜ Ferramentas compradas liberam ou aceleram pistas (ver Fases 4 e 5)

**Ordem de serviço (OS) e checklist**
- ⬜ Todo atendimento abre uma **OS**: problema relatado, diagnóstico, o que foi feito, peças, horas, deslocamento
- ⬜ **Checklist do serviço** na prancheta/celular (ex.: "testar todos os pontos", "etiquetar", "limpar o local");
      o cliente confere no fim — item esquecido vira retrabalho ou estrela a menos
- ⬜ **Cliente assina a OS**; sem assinatura ele pode contestar a cobrança
- ⬜ **Teste de prova** no fim (temperatura e desempenho antes/depois, página de teste, teste de cabo) anexado à OS
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
- ⬜ Abajur no criado-mudo e luz pré-calculada (baked) na casa
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
- ⬜ Celular: ligações de triagem (perguntas por telefone), orçamentos, GPS (Fase 6), fotos do problema
- ✅ **Energia do técnico** no HUD: cai com as horas e o trabalho; cansado anda/trabalha mais devagar; dormir cedo
      recupera tudo (tarde ou no escritório, menos); café na cozinha (+20, até 3 por dia) — salva no save
- ✅ Comer (geladeira) como outra forma de recuperar energia
- ⬜ Rotina da manhã: acordar → café (bônus de energia) → ver chamados no celular/PC → sair de casa

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
- ⬜ Usados como objetos no escritório e opção de colocar em locação — antes: lote de empresa que fechou e equipamentos
      descartados para testar, recuperar e revender ou colocar em locação
- ✅ **Logística reversa**: caixa no escritório recebe os toners usados (R$ 8 cada da recicladora + reputação)
- ⬜ Descarte de equipamentos: apagar dados de verdade (LGPD) antes de revender; lixo eletrônico no ponto de coleta

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
- ⬜ Placa de vídeo trocável, bateria da placa-mãe
- ✅ **Limpeza**: manta de poeira visível por dentro; pincel e ar comprimido (ferramenta) limpam; poeira = superaquecimento
- ✅ PC superaquecendo desliga sozinho depois de ~25 s ligado; a ventoinha gira mais rápido
- ⬜ Nuvem de poeira saindo na limpeza
- ✅ **Pasta térmica**: tirar o cooler (ele sai e mostra a pasta seca), limpar com álcool isopropílico, aplicar um grão
      de pasta nova, recolocar; recolocar sem pasta piora; superaquecimento com o gabinete limpo = pasta
- ⬜ Organização de cabos dentro do gabinete
- ⬜ Cabos atrás: rede, energia, vídeo (monitor sem imagem por cabo solto)
- ⬜ Monitor mostrando o "Windows" do jogo na própria tela 3D (o computador simulado que já existe) — a textura
      original do Computador_Mesa mostra um Windows real: ✅ coberta pela tela do jogo (TelaComputador)
- ⬜ **Ferramentas de diagnóstico**: testador de fonte, multímetro, **placa POST**, pendrive de diagnóstico (teste
      de memória, saúde do disco, antivírus), testador de cabo
- ⬜ **Check-up completo** (serviço vendável): inspeção de todas as peças com laudo de saúde em %
- ⬜ **Compatibilidade de peças**: memória do tipo errado não encaixa, fonte fraca desliga sob carga
- ✅ Problemas de hardware: superaquecimento por poeira, memória ruim (3 bipes, sem vídeo), fonte queimada (não liga)
- ⬜ Problemas: lentidão, vírus/ransomware, Windows não inicia, HD com setor
      ruim (backup antes de trocar), atualização do Windows quebrou o sistema da contabilidade
- ⬜ Peças trocadas fisicamente; peça velha volta para a mochila (sucata ou RMA)
- ⬜ Montagem de PC sob encomenda (orçamento → peças → montagem → teste de desempenho)
- ⬜ **Notebook**: tela, teclado, bateria estufada, dobradiça, limpeza

## Fase 5 — Rede, infraestrutura, CFTV e energia
- ✅ Roteador realista (Roteador.glb) com 4 LEDs por estado, plugue da fonte e furo de reset atrás (âncoras)
- ⬜ Reset com clipe animado (segurar o clipe no furo)
- ⬜ Switch de mesa e de rack, rack com patch panel (modelos já baixados)
- ⬜ **Projeto de cabeamento jogável em 3D**: passar cabo por canaleta, crimpar, conectar no patch panel, testar
      ponto a ponto, identificar com etiqueta
- ⬜ **Trabalho em altura e forro**: escada, abrir placa de forro, poeira; risco de cair se for com pressa
- ⬜ **Organizar rack bagunçado** (antes/depois) — serviço vendável
- ⬜ Wi-Fi: posicionar access point, sinal fraco nos cantos (mapa de calor), **rede de visitantes separada**
- ⬜ Firewall em comodato no rack do cliente; **VPN** para quem trabalha de casa
- ⬜ **Servidor/NAS** no escritório do cliente; **backup 3-2-1** (HD externo + NAS + nuvem) e **teste de restauração**
- ⬜ **Queda do link da operadora**: provar que não é sua culpa, abrir chamado com a operadora; vender link de backup
- ⬜ **CFTV**: câmeras, DVR/NVR, cabo, fonte; posicionar vendo a área certa, acesso pelo celular do cliente
- ⬜ **Nobreak**: bateria morta, apitando, dimensionar e instalar; queda de energia e **raio** queimando
      switch/fonte de quem não tem proteção
- ⬜ Ferramentas: alicate de crimpar, testador de cabo, punch-down, escada, furadeira, passa-fio, detector de tensão
- ⬜ **Fiscalização** em obras de cabeamento maiores (documentação, ART/responsável técnico) ❓ calibrar com o dono

## Fase 6 — Cidade e deslocamento (dirigindo de verdade)
Decisão do dono: **dirigir de verdade** (sem viagem rápida por mapa).
- ⬜ Carro do técnico dirigível (entrar/sair, volante, câmera em 1ª pessoa, física simples com WheelCollider)
- ⬜ Cidade com ruas ligando casa, sede, fornecedores, ponto de coleta de lixo eletrônico e clientes; GPS no celular
- ⬜ Combustível (posto), estacionar na frente do cliente, carregar caixas/equipamentos/escada no porta-malas
- ⬜ Vários endereços (clínica, contabilidade, loja, escola, prefeitura, condomínio) — cada cliente com sala própria
- ⬜ Trânsito simples (carros NPC, semáforos); **clima**: chuva atrasa e impede serviço externo, temporal traz raios
      e chamados
- ⬜ Cobrança de deslocamento/km no orçamento
- ⬜ Mais tarde: trocar de veículo (moto → carro → van da empresa adesivada)

## Fase 7 — Clientes e pessoas vivas
- ✅ Clientes Mixamo animados (parado / conversando)
- ⬜ Funcionários do cliente sentados digitando, andando, atendendo telefone (animações já baixadas)
- ⬜ Cliente leva você até o equipamento; reage ao seu trabalho (impaciência, alívio)
- ⬜ **Personalidades**: o que jura que "não mexeu em nada", o da senha no post-it, o apressado, o que pechincha, o
      que **não paga**, o fiel que indica outros
- ⬜ **Horário comercial do cliente**: certos serviços só com o escritório vazio (parar servidor/rede) — fim de
      tarde, noite ou fim de semana
- ⬜ **Avaliação por estrelas** depois de cada atendimento; **retrabalho/garantia** quando o serviço foi mal feito
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
