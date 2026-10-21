# Praticas aprendidas

| Pratica | Motivo | Limite ou alternativa |
| --- | --- | --- |
| Evento distinto para atualizacao | Criacao e atualizacao sao fatos diferentes | Evoluir contratos sem misturar consumidores |
| Uma fila por finalidade | Consumidores na mesma fila dividem as mensagens | Exchange e filas separadas quando varios servicos precisam do mesmo fato |
| ACK apos processamento | Nao confirmar email que falhou | Ainda pode duplicar; avaliar idempotencia |
| Fila de falhas | Preservar mensagens que nao puderam ser enviadas | Inspecionar e republicar apos corrigir a causa |
| Configuracao externa | Evitar segredos versionados | Secret manager em hospedagem |
| Interface para publicacao | Testar regra sem broker | Integracao real em teste separado |
| Explicitar memoria didatica | Nao prometer persistencia inexistente | Banco transacional e Outbox para requisitos de durabilidade |

Fonte: atividade do professor Rannyer e implementacao local. Validacao estatica, teste unitario, teste SMTP e integracao completa sao evidencias diferentes e devem ser relatadas separadamente.
