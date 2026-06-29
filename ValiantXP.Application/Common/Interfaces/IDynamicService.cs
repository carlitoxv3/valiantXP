using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Domain.Entities;

namespace ValiantXP.Application.Common.Interfaces;

public interface IDynamicService
{
    /// <summary>
    /// Orquesta la ejecución de una dinámica de usuario cargando la estrategia adecuada.
    /// </summary>
    /// <param name="dynamicId">ID de la dinámica a procesar.</param>
    /// <param name="userId">ID del usuario que participa.</param>
    /// <param name="inputs">Diccionario de entradas enviadas por el usuario (respuestas, opciones).</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Detalles del resultado y los premios asignados.</returns>
    Task<DynamicResult> ProcessDynamicAsync(
        Guid dynamicId, 
        Guid userId, 
        Dictionary<string, string> inputs, 
        CancellationToken cancellationToken);
}
