# ADR-013: Padronizar erros de validação 400 por campo do contrato

Data: 2026-10-27

## Status
Proposto (2026-10-27)

## Contexto
- [ADR-008](ADR-008-padronizar-erros-com-problemdetails.md) define o ProblemDetails com `errors`, mas não define as chaves, as mensagens nem quem faz a validação.
- No F01, RF-02, RF-03, RF-07 e RF-11 exigem um erro "no campo" `date`, `weekStart` ou `name`, exibido pelo frontend junto ao campo certo.
- No .NET 9, o model binding das Minimal APIs não faz validação de DataAnnotations. Quando um `DateOnly` não consegue ser convertido, a resposta é um 400 genérico, sem `errors`, e o campo se perde.
- Não queremos dependência nova (FluentValidation) para cinco regras.

## Decisão
Vamos validar manualmente, num validador por feature, e responder 400 com `TypedResults.ValidationProblem`:

- **Chaves** de `errors`: nome do campo no contrato, em camelCase (`date`, `name`, `description`, `weekStart`). Nunca nome de propriedade C# nem caminho JSON (`$.date`).
- **Valores**: lista de mensagens em português, voltadas ao usuário final. Todos os campos inválidos aparecem juntos, e nada é gravado.
- **`title`** fixo `Um ou mais campos são inválidos.`; `type` RFC 9110 15.5.1; `traceId` como no ADR-008.
- **Entrada como texto**: datas chegam como `string?` (query `weekStart` e corpo `date`) e o validador as converte com `DateOnly.TryParseExact("yyyy-MM-dd", CultureInfo.InvariantCulture)`. Assim, formato inválido também cai na chave do campo.
- **Ordem**: primeiro a validação (400), depois a existência (404). PUT com corpo inválido em id inexistente responde 400.
- **Corpo malformado**: JSON inválido ou ausente responde 400 em problem+json, possivelmente sem `errors`. O frontend mostra o `title`.
- **Mensagens do F01**, estáveis porque o E2E pode verificá-las:

| Campo | Situação | Mensagem |
|---|---|---|
| `weekStart`, `date` | ausente ou fora de `yyyy-MM-dd` | Informe uma data válida no formato AAAA-MM-DD. |
| `weekStart` | não é segunda | A semana deve começar numa segunda-feira. |
| `date` | sábado ou domingo | Escolha um dia útil (segunda a sexta). |
| `name` | vazio ou só espaços | Informe o nome do prato. |
| `name` | mais de 100 caracteres após o trim | O nome do prato deve ter no máximo 100 caracteres. |
| `description` | mais de 300 caracteres após o trim | A descrição deve ter no máximo 300 caracteres. |

- No Angular, o tratamento central em `core/` repassa `errors` à tela. A tela associa cada chave ao `FormControl` de mesmo nome e mostra as chaves que não têm campo como erro geral do formulário.

## Consequências
- Positivas: o mapeamento chave → campo é direto no frontend, as mensagens são as mesmas em todas as telas, o validador é testável em unidade sem HTTP e não entra nenhuma dependência nova.
- Negativas: mensagens em texto fixo no backend, sem i18n. O DTO de entrada usa `string` para datas, então o contrato C# difere do tipo `DateOnly` usado internamente.
- A acompanhar: se o frontend precisar distinguir erros por código, e não por texto, acrescentar a extensão `code` (já prevista no ADR-008) em nova revisão. Se o projeto migrar para .NET 10, avaliar a validação nativa das Minimal APIs em uma ADR que substitua esta.
