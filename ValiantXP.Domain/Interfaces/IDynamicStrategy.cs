using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Domain.Entities;

namespace ValiantXP.Domain.Interfaces;

public interface IDynamicStrategy
{
    /// <summary>
    /// Identificador único del tipo de dinámica (ej. "Trivia", "Encuesta").
    /// </summary>
    string DynamicType { get; }

    /// <summary>
    /// Ejecuta la lógica particular del tipo de dinámica.
    /// </summary>
    /// <param name="context">Contexto de la ejecución (datos del usuario, respuestas).</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Resultado de la ejecución de la dinámica.</returns>
    Task<DynamicResult> ExecuteAsync(DynamicContext context, CancellationToken cancellationToken);
}
