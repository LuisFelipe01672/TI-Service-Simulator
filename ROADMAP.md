# Roadmap — TI Service Simulator

Plano de longo prazo. Cada fase termina **testada na Unity e com build**. Regras que valem sempre:
realismo (animação em toda ação, pista visual para todo problema), marcas fictícias, valores calibrados com o dono
(sem publicar números exatos), push só com pedido. Skills do projeto: `testar-na-unity`, `baixar-modelo-3d`,
`publicar-ti-service`.

Legenda: ✅ feito · 🔜 próxima · ⬜ a fazer · ❓ decisão do dono

## Visão
Simulador de técnico de TI brasileiro, do MEI em casa até uma empresa com sede, equipe e contratos. O prazer vem de
três coisas (inspiração em simuladores de trabalho do gênero):
1. **Trabalho físico satisfatório** — cada tarefa é feita com as mãos: abrir caixa, crimpar, tirar poeira, trocar
   peça, organizar rack (antes/depois).
2. **Diagnóstico de verdade** — o cliente descreve o problema de forma vaga ("tá lento", "a internet caiu"); você
   investiga com ferramentas e descobre a causa.
3. **Crescimento visível** — o dinheiro vira coisas que você vê: ferramentas, carro, sede maior, prateleiras cheias,
   funcionários fazendo o que antes era você.

E o tempero da **TI real**: "não mexi em nada", senha no post-it, backup nunca testado, toner acabando sexta às 17h,
certificado digital vencido, operadora que não resolve, cliente que não paga.

## Pilar gráfico (vale para todas as fases)
Meta: o mais realista possível dentro do Built-in RP, sem travar PCs médios.
- ⬜ Post-processing (pacote Post Processing v2): tonemapping ACES, bloom leve, oclusão de ambiente (SSAO),
      gradação de cor, profundidade de campo só nos closes
- ⬜ Iluminação: luz baked (lightmaps) nos interiores + reflection probes + light probes para os personagens
- ⬜ Materiais PBR (ambientCG/Poly Haven) em todo piso, parede, metal e plástico; sujeira/desgaste onde faz sentido
- ⬜ Modelos realistas (Poly Haven CC0 / Sketchfab CC-BY) substituindo as caixas, cômodo por cômodo
- ⬜ Opções gráficas (baixo/médio/alto/ultra) para manter o FPS
- ❓ Avaliar migrar para **HDRP** (máximo realismo, mais pesado) ou **URP** depois da Fase 1, comparando o mesmo
      cômodo nos dois. Migração custa caro — decidir cedo.

---

## Fase 0 — Fundação (próxima sessão, rápido)
- 🔜 Abrir a Unity pelo Hub → instalar glTFast e importar os 11 modelos de `Assets/Models/Sketchfab`
- 🔜 Commit dos modelos + `CREDITOS.md` + skills (`.claude/skills`) + este roadmap
- ⬜ `CLAUDE.md` na raiz com as convenções do projeto (português, UI por código, managers por evento, saves)
- ⬜ Tela de **Créditos** no menu principal (autores do `CREDITOS.md`; exigido pelo CC-BY)
- ⬜ Corrigir a marcação falsa de "cena modificada" ao gerar o executável
- ⬜ Instalar Post Processing v2 e montar um perfil base (primeiro passo do pilar gráfico)

## Fase 1 — Casa realista e começo do jogo
O jogo passa a **começar em casa** (acordando na cama), não no escritório.
- ⬜ Quarto: cama de casal, criado-mudo, abajur, guarda-roupa, cortina, tapete
- ⬜ Home office: mesa, cadeira, **PC do jogador**, prateleira de ferramentas, mochila física
- ⬜ Sala (sofá, TV, rack, quadros), cozinha (geladeira, fogão, pia, micro-ondas, cafeteira), banheiro
- ⬜ Acabamento: pisos/azulejos/paredes PBR, interruptores que acendem luzes, relógio de parede com a hora do jogo,
      sol pela janela, luz baked
- ⬜ Rotina da manhã: acordar → (café opcional = pequeno bônus) → ver chamados no PC/celular → sair de casa
- ⬜ **PC de casa como central de trabalho**: sistema de chamados, loja, banco e **atendimento remoto**
      (programa de acesso remoto com nome fictício): resolver de casa parte dos chamados (senha, impressora
      padrão, driver, configurar e-mail) — mais rápido e mais barato, mas nem tudo dá para fazer de longe
- ⬜ **Celular do técnico**: app de mensagens fictício com os clientes (pedidos, fotos do problema, cobrança),
      orçamentos para aprovar, GPS, banco; substitui parte do tablet de forma mais natural
- ❓ Fontes: Poly Haven (CC0) primeiro; Sketchfab CC-BY para o que faltar

## Fase 2 — Compras físicas, estoque e fornecedores
- ⬜ Tudo que é comprado chega como **caixa na porta** (casa ou sede), no horário de entrega; pegar e levar
- ⬜ **Caixa de cabo de rede 305 m** realista (marca fictícia, "Cat6 · 305 m"), com o cabo saindo pelo furo;
      na bancada o cabo sai da caixa ao fazer um cabo
- ⬜ Saquinho de RJ45, caixa de toner, ferramentas e equipamentos de locação como objetos
- ⬜ Cabos prontos enrolados (com o tamanho escrito) na mochila/bancada
- ⬜ **Prateleiras de estoque** na sede: você guarda cada item e vê o que está acabando (ligado ao estoque do sistema)
- ⬜ Fornecedores com preço, prazo e confiabilidade diferentes: **entrega atrasada**, **peça errada**, peça com
      defeito
- ⬜ **Garantia/RMA**: peça defeituosa volta ao fabricante e demora a voltar (ou você paga outra)
- ⬜ **Peças usadas e recondicionadas**: lote de equipamentos de empresa que fechou (leilão) para testar,
      recondicionar e revender ou colocar em locação

## Fase 3 — Impressora completa  (detalhe em `PLANO_FASE_IMPRESSORA.md`)
- ✅ Peças com mira + E, tampa animada, crimpagem animada em 3D
- ⬜ Modelo realista (Impressora_MFP_Compacta / MFP_Escritorio) com logotipo fictício e visor com texto real
- ⬜ Mão/item segurado + close de câmera
- ⬜ Toner completo (tirar, olhar, recolocar o mesmo, novo, chacoalhar, lacre, encaixar; toner usado na mochila)
- ⬜ Traseira: plugue RJ45 animado, LEDs, testador com luzes 1–8
- ⬜ Página de teste saindo com o resultado (ok, falhada, branca, listras)
- ⬜ Problemas novos: atolamento, sem papel, cilindro riscado, IP mudou, tampa, driver
- ⬜ **Chamado vago** desde já: "a impressora não imprime" pode ser toner, cabo, IP, driver ou fila travada

## Fase 4 — Computador completo + diagnóstico
- ⬜ Modelo realista (Computador_Mesa) + gabinete que abre (tampa lateral)
- ⬜ Peças internas animáveis: memória, SSD/HD, fonte, cooler, placa de vídeo
- ⬜ **Limpeza satisfatória**: gabinete imundo, poeira saindo com ar comprimido e pincel (antes/depois visível);
      cooler sujo = superaquecimento
- ⬜ Cabos atrás: rede, energia, vídeo (monitor sem imagem por cabo solto)
- ⬜ Monitor mostrando o "Windows" do jogo na própria tela 3D (o computador simulado que já existe)
- ⬜ **Ferramentas de diagnóstico** (compradas, cada uma libera ou acelera diagnósticos):
      testador de fonte, multímetro, **placa POST** (código de erro), pendrive de diagnóstico (teste de memória,
      saúde do disco, antivírus), testador de cabo
- ⬜ **Compatibilidade de peças**: memória do tipo errado não encaixa, fonte fraca desliga sob carga, comprar
      errado custa tempo e dinheiro
- ⬜ Problemas: lentidão (memória/disco cheio), vírus/ransomware, Windows não inicia, memória ruim (bipes),
      fonte queimada, HD com setor ruim (backup antes de trocar), **atualização do Windows quebrou o sistema**
- ⬜ Peças compradas na loja e trocadas fisicamente; peça velha volta para a mochila (sucata ou RMA)
- ⬜ Montagem de PC sob encomenda (cliente pede um orçamento, você escolhe as peças, monta e testa)

## Fase 5 — Rede, infraestrutura, CFTV e energia
- ⬜ Roteador realista (luzes reais por estado, reset com clipe, painel web já existente)
- ⬜ Switch de mesa e de rack, rack com patch panel (modelos já baixados)
- ⬜ **Projeto de cabeamento jogável em 3D**: passar cabo por canaleta, crimpar, conectar no patch panel, testar
      ponto a ponto, identificar com etiqueta
- ⬜ **Organizar rack bagunçado** (antes/depois, satisfatório) — serviço vendável por si só
- ⬜ Wi-Fi: posicionar access point, ver sinal fraco nos cantos (mapa de calor simples)
- ⬜ Firewall em comodato instalado no rack do cliente
- ⬜ **Queda do link da operadora**: diagnosticar que não é sua culpa e abrir chamado com a operadora
      (que demora); vender link de backup
- ⬜ **CFTV**: câmeras, DVR/NVR, cabo, fonte; instalar, posicionar a câmera vendo a área certa, configurar acesso
      pelo celular do cliente — nova linha de serviço
- ⬜ **Nobreak**: bateria morta, nobreak apitando, dimensionar e instalar; queda de energia e **raio** queimando
      switch/fonte quem não tem proteção
- ⬜ Ferramentas por código: alicate de crimpar, testador de cabo, punch-down, escada, furadeira, passa-fio

## Fase 6 — Cidade e deslocamento (dirigindo de verdade)
Decisão do dono: **dirigir de verdade** (sem viagem rápida por mapa).
- ⬜ Carro do técnico dirigível (entrar/sair, volante, câmera em 1ª pessoa, física simples com WheelCollider)
- ⬜ Cidade com ruas ligando casa, sede, fornecedores e clientes; GPS no celular até o endereço do chamado
- ⬜ Combustível (posto para abastecer), estacionar na frente do cliente, carregar caixas/equipamentos no porta-malas
- ⬜ Vários endereços (clínica, contabilidade, loja, escola, prefeitura, condomínio) — cada cliente com sala própria
- ⬜ Trânsito simples (carros NPC, semáforos) e horário influenciando o prazo do chamado
- ⬜ Cobrança de deslocamento/km no orçamento
- ⬜ Mais tarde: trocar de veículo (moto → carro → van da empresa adesivada)

## Fase 7 — Clientes e pessoas vivas
- ✅ Clientes Mixamo animados (parado / conversando)
- ⬜ Funcionários do cliente sentados digitando, andando, atendendo telefone (animações já baixadas)
- ⬜ Cliente leva você até o equipamento; reage ao seu trabalho (impaciência, alívio)
- ⬜ **Personalidades de cliente**: o que jura que "não mexeu em nada", o que deixa a senha no post-it, o
      apressado, o que pechincha, o que **não paga**, o fiel que indica outros
- ⬜ **Avaliação por estrelas** depois de cada atendimento (aparece no celular); boas avaliações trazem clientes
- ⬜ **Retrabalho/garantia**: serviço mal feito faz o problema voltar e você volta de graça (e perde estrelas)
- ⬜ Mais variedade de personagens (Mixamo) e escritórios com cara própria por tipo de cliente
- ⬜ Seus funcionários visíveis na sede trabalhando e saindo para atender

## Fase 8 — Empresa, contratos e progressão
- ⬜ Terminar testes: vencer licitação, alertas de firewall/ERP na Central, serviços mensais, equipe e folha,
      virar ME, produto próprio (ERP/bot)
- ⬜ **Orçamento negociável**: você monta (peças + horas + deslocamento), o cliente aprova, pechincha ou some
- ⬜ **Você define seus preços** (visita, hora técnica, formatação, contrato mensal): caro demais = cliente
      reclama/vai embora; barato demais = você quebra
- ⬜ **Contrato mensal de suporte** (pacote de horas por mês) = renda fixa; estourou as horas, cobra à parte
- ⬜ **Documentação da rede do cliente** (IPs, senhas, mapa de pontos, equipamentos): cliente documentado = chamado
      resolvido mais rápido; sem documentação, você perde tempo descobrindo tudo de novo
- ⬜ **Certificações/cursos** que liberam linhas de serviço (CFTV, redes avançadas, firewall, servidores) — como
      licenças que liberam categorias novas
- ⬜ **Funcionários assumem tarefas que eram suas**: auxiliar crimpa sozinho na bancada, técnico atende chamado
      simples, comercial traz clientes — você vê eles fazendo
- ⬜ **Contas do mês**: aluguel, luz, internet, contador, impostos (DAS/Simples), combustível, folha
- ⬜ Problemas brasileiros: **certificado digital A1/A3 vencido** (emissor de NF-e parado), maquininha/Pix sem
      internet, sistema da contabilidade, LGPD (vazamento por senha fraca)
- ⬜ **Plantão e serviço fora do horário** (trocar servidor no domingo) pago mais caro; sobreaviso
- ⬜ **Sazonalidade**: volta às aulas (escolas), época do Imposto de Renda e fim de ano (contadores lotados),
      Black Friday (lojas), eleições/licitações no começo do ano
- ⬜ **Escolhas tentadoras**: instalar programa pirata (rápido e barato, mas risco de fiscalização e de perder
      clientes grandes), "esquecer" de emitir nota
- ⬜ Sede física evoluindo: sala → sala maior → galpão, com móveis comprados aparecendo de verdade
- ⬜ Eventos aleatórios com cena: cliente furioso na porta, queda de energia, rato no forro roendo cabo,
      ransomware num cliente sem backup, backup "que nunca foi testado"
- ⬜ Balanceamento com o dono (valores calibrados com as notas privadas, fora do repositório; M365, ERP e controle de impressão a confirmar ❓)

## Fase 9 — Polimento
- ⬜ Sons gravados (Kenney Audio CC0 / Freesound CC0) no lugar dos gerados; música ambiente
- ⬜ Tutorial guiado no primeiro dia (com os objetivos "Primeiros passos")
- ⬜ Configurações gráficas completas, otimização (texturas 4K do Mixamo → 2K, LOD, oclusão)
- ⬜ Vários saves (slots), autosave
- ⬜ Menu principal 3D (câmera passeando pela casa/escritório)
- ⬜ Revisão de textos, acentuação e glifos

## Fase 10 — Lançamento
- ⬜ Build estável sem console de debug por padrão
- ⬜ Página (itch.io / Steam), capturas e trailer
- ⬜ Revisão de licenças (CREDITOS.md completo, sem marcas reais, Mixamo embutido só no build)

---

### Ideias soltas (avaliar depois)
- Modo "dia livre" sem pressão de prazo
- Conquistas ligadas a situações reais ("Salvou a contabilidade em pleno IR", "Rack impecável")
- Fotos de antes/depois dos serviços num portfólio que atrai clientes

### Como executar
Uma fase por vez, na ordem; o pilar gráfico avança junto em cada fase. Em cada sessão: pegar o próximo ⬜,
implementar, testar na Unity (skill `testar-na-unity`), marcar ✅ aqui, commit local; push quando o dono pedir.
Itens ❓ precisam da resposta do dono antes de começar.
