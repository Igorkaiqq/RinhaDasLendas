# Message Catalog

Catálogo inicial de mensagens do RinhaDasLendas. Códigos publicados são imutáveis e devem ser mantidos para compatibilidade.

| Code | Category | pt-BR | en-US | Context | Severity |
|------|----------|-------|-------|---------|----------|
| MI001 | Info | Carregando informações | Loading information | Carregamento inicial de telas e listas | info |
| MI002 | Info | Aguardando resposta do servidor | Waiting for server response | Requisições HTTP em andamento | info |
| MI003 | Info | Sincronizando dados | Syncing data | Atualização de dados locais com backend | info |
| MI004 | Info | Nenhum registro encontrado | No records found | Listas vazias sem erro | info |
| MI005 | Info | Filtros aplicados | Filters applied | Confirmação neutra de filtro | info |
| MI006 | Info | Preferências carregadas | Preferences loaded | Carregamento de preferências do jogador | info |
| MI007 | Info | Draft aguardando jogadores | Draft waiting for players | Estado inicial do draft | info |
| MI008 | Info | Fila aguardando confirmação | Queue waiting for confirmation | Estado de fila/votação | info |
| MI009 | Info | Estatísticas em atualização | Statistics updating | Recalculo ou entrada manual de estatísticas | info |
| MSIS001 | Success | Operação realizada com sucesso | Operation completed successfully | Sucesso genérico | info |
| MSIS002 | Success | Jogador criado com sucesso | Player created successfully | Cadastro de jogador | info |
| MSIS003 | Success | Jogador atualizado com sucesso | Player updated successfully | Atualização de cadastro | info |
| MSIS004 | Success | Jogador inativado com sucesso | Player deactivated successfully | Inativação de jogador | info |
| MSIS005 | Success | Preferências de rota atualizadas | Route preferences updated | Atualização de rotas | info |
| MSIS006 | Success | Fila criada com sucesso | Queue created successfully | Criação de fila | info |
| MSIS007 | Success | Presença confirmada | Attendance confirmed | Confirmação em fila | info |
| MSIS008 | Success | Draft salvo com sucesso | Draft saved successfully | Salvamento de draft | info |
| MSIS009 | Success | Resultado registrado com sucesso | Result registered successfully | Registro manual de partida | info |
| ME001 | Error | Ocorreu um erro inesperado | An unexpected error occurred | Erro genérico não tratado | error |
| ME002 | Error | Falha ao conectar ao servidor | Failed to connect to server | Falha de comunicação HTTP | error |
| ME003 | Error | Jogador não encontrado | Player not found | Busca por jogador inexistente | error |
| ME004 | Error | Não foi possível salvar o jogador | Could not save player | Falha persistindo jogador | error |
| ME005 | Error | Não foi possível atualizar o jogador | Could not update player | Falha atualizando jogador | error |
| ME006 | Error | Não foi possível inativar o jogador | Could not deactivate player | Falha inativando jogador | error |
| ME007 | Error | Preferências de rota não encontradas | Route preferences not found | Preferências ausentes | error |
| ME008 | Error | Fila não encontrada | Queue not found | Busca por fila inexistente | error |
| ME009 | Error | Draft não encontrado | Draft not found | Busca por draft inexistente | error |
| ME010 | Error | Partida não encontrada | Match not found | Busca por partida inexistente | error |
| ME011 | Error | Série não encontrada | Series not found | Busca por série inexistente | error |
| ME012 | Error | Falha ao carregar estatísticas | Failed to load statistics | Erro na leitura de estatísticas | error |
| ME013 | Error | Falha ao registrar resultado | Failed to register result | Erro ao salvar resultado | error |
| ME014 | Error | Integração externa indisponível | External integration unavailable | Discord ou Riot API indisponível | warning |
| ME015 | Error | Acesso não autorizado | Unauthorized access | Ação sem autenticação futura | error |
| ME016 | Error | Permissão insuficiente | Insufficient permission | Ação sem papel adequado futuro | error |
| ME017 | Error | Recurso temporariamente indisponível | Resource temporarily unavailable | Manutenção ou instabilidade | warning |
| ME018 | Error | Dados inconsistentes foram encontrados | Inconsistent data was found | Estado inválido detectado | error |
| ME019 | Error | Não foi possível processar a solicitação | Could not process the request | Falha de aplicação recuperável | error |
| ME020 | Error | Tempo limite excedido | Request timed out | Timeout de operação | error |
| MV001 | Validation | Campo obrigatório | Required field | Validação de campo vazio | warning |
| MV002 | Validation | Formato de email inválido | Invalid email format | Validação de email | warning |
| MV003 | Validation | Nome do jogador deve ser informado | Player name is required | Cadastro de jogador | warning |
| MV004 | Validation | Nick do League of Legends deve ser informado | League of Legends nickname is required | Cadastro ou conta LoL | warning |
| MV005 | Validation | Região do jogador é obrigatória | Player region is required | Cadastro de conta LoL | warning |
| MV006 | Validation | Link OP.GG inválido | Invalid OP.GG link | Validação de URL OP.GG | warning |
| MV007 | Validation | Link Deeplol inválido | Invalid Deeplol link | Validação de URL Deeplol | warning |
| MV008 | Validation | Prioridades de rota devem ser únicas | Route priorities must be unique | Preferências de rota | warning |
| MV009 | Validation | Prioridades de rota devem estar entre 1 e 5 | Route priorities must be between 1 and 5 | Preferências de rota | warning |
| MV010 | Validation | Apenas uma rota pode ser marcada como não jogo | Only one role can be marked as never play | Preferências de rota | warning |
| MV011 | Validation | Jogador já existe | Player already exists | Cadastro duplicado | warning |
| MV012 | Validation | Jogador inativo não pode entrar na fila | Inactive player cannot join queue | Fila/votação | warning |
| MV013 | Validation | A fila já possui o limite de jogadores | Queue already has the player limit | Fila com limite padrão | warning |
| MV014 | Validation | Jogador já está na fila | Player is already in queue | Entrada duplicada na fila | warning |
| MV015 | Validation | Um time não pode ter mais de cinco jogadores | A team cannot have more than five players | Formação de times | warning |
| MV016 | Validation | Jogador duplicado na partida | Duplicate player in match | Registro de partida | warning |
| MV017 | Validation | Campeão deve ser informado | Champion is required | Picks e bans | warning |
| MV018 | Validation | Resultado da partida é obrigatório | Match result is required | Registro de resultado | warning |
| MV019 | Validation | Série deve ser MD3 ou MD5 | Series must be best-of-three or best-of-five | Séries | warning |
| MC001 | Confirmation | Confirmar esta ação? | Confirm this action? | Confirmação genérica | info |
| MC002 | Confirmation | Deseja inativar este jogador? | Do you want to deactivate this player? | Inativação de jogador | warning |
| MC003 | Confirmation | Deseja remover o jogador da fila? | Do you want to remove the player from the queue? | Fila/votação | warning |
| MC004 | Confirmation | Deseja encerrar a fila atual? | Do you want to close the current queue? | Administração de fila | warning |
| MC005 | Confirmation | Deseja salvar este draft? | Do you want to save this draft? | Draft | info |
| MC006 | Confirmation | Deseja registrar este resultado? | Do you want to register this result? | Resultado de partida | info |
| MC007 | Confirmation | Deseja sobrescrever as estatísticas? | Do you want to overwrite the statistics? | Estatísticas manuais | warning |
| MC008 | Confirmation | Deseja trocar os capitães? | Do you want to change the captains? | Draft | warning |
| MC009 | Confirmation | Deseja cancelar as alterações? | Do you want to discard changes? | Formulários | warning |
| MA001 | Alert | Existem alterações não salvas | There are unsaved changes | Formulários com estado sujo | warning |
| MA002 | Alert | Integração externa será ignorada | External integration will be skipped | Modo manual | warning |
| MA003 | Alert | Dados informados manualmente precisam de revisão | Manually entered data needs review | Estatísticas manuais | warning |
| MA004 | Alert | Jogador sem preferência de rota completa | Player has incomplete route preferences | Cadastro incompleto | warning |
| MA005 | Alert | Fila próxima do limite de jogadores | Queue is near the player limit | Fila/votação | warning |
| MA006 | Alert | Draft incompleto | Draft is incomplete | Draft sem todas escolhas | warning |
| MA007 | Alert | Estatísticas podem estar desatualizadas | Statistics may be outdated | Dashboard | warning |
| MA008 | Alert | Ação disponível apenas para administradores | Action available only to administrators | Permissões futuras | warning |
| MA009 | Alert | Revise os dados antes de continuar | Review the data before continuing | Confirmação antes de salvar | warning |
