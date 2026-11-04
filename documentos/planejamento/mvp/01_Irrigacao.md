# Fluxo 01 — Irrigação manual, consulta e consumo

**Requisitos:** RF02 parcial/RF07/RF13/RF14. **Estado:** etapas 01.1–01.4 concluídas tecnicamente; aceite da PO pendente. **Dependência:** contexto cadastrado; criação/manutenção dos cadastros permanece para os fluxos seguintes.

Este é o primeiro fluxo sugerido. Seu [plano completo](../mvp-irrigacao/README.md) contém o [contrato específico](../mvp-irrigacao/Contrato_Fluxo.md) e doze casos de uso em arquivos separados. Não duplicar definições neste índice.

| Etapa | Plano e resultado |
| --- | --- |
| 01.1 | [Contexto](../mvp-irrigacao/Etapa_01_Contexto.md): primeira consulta real, horta/setores/canteiros/responsável demonstrativos |
| 01.2 | [Registro e consumo](../mvp-irrigacao/Etapa_02_Registro_Consumo.md): persistir irrigação, consultar origens e litros conhecidos |
| 01.3 | [Correção e histórico](../mvp-irrigacao/Etapa_03_Correcao_Historico.md): motivo, versão, invalidação e resultado recalculado |
| 01.4 | [Interface e roteiro da PO](../mvp-irrigacao/Etapa_04_Interface_PO.md): percurso React → API → banco → dashboard |

O aceite central é registrar 120 L atendendo quatro canteiros, conferir 120 L no setor, corrigir para 100 L e depois invalidar, preservando histórico. Volume desconhecido não vira zero. As regras de fórmula, datas, vínculos e repetição estão no contrato específico.

Executar uma etapa/caso de uso autorizado e parar. O próximo fluxo completa os cadastros e o indicador de irrigação por setor. **Evidências:** etapas 01.1–01.4 na [tabela do plano detalhado](../mvp-irrigacao/README.md#acompanhamento). A aplicação não insere seed; o [Get Started](../../Get_Started.md) descreve preparação explícita para testar antes da entrega dos cadastros.
