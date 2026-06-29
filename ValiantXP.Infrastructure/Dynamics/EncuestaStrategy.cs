using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Interfaces;

namespace ValiantXP.Infrastructure.Dynamics;

public class EncuestaStrategy : IDynamicStrategy
{
    public string DynamicType => Domain.Enums.DynamicType.Encuesta.ToString();

    public Task<DynamicResult> ExecuteAsync(DynamicContext context, CancellationToken cancellationToken)
    {
        // Surveys are complete upon submission, so success is always true
        var payload = new Dictionary<string, object>
        {
            { "Score", 100 }
        };

        return Task.FromResult(new DynamicResult
        {
            Success = true,
            Message = "Survey submitted successfully.",
            Payload = payload
        });
    }
}
