using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Application.Common.Interfaces;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Interfaces;

namespace ValiantXP.Infrastructure.Dynamics;

public class DynamicService : IDynamicService
{
    private readonly IEnumerable<IDynamicStrategy> _strategies;
    private readonly IUnitOfWork _unitOfWork;

    public DynamicService(IEnumerable<IDynamicStrategy> strategies, IUnitOfWork unitOfWork)
    {
        _strategies = strategies;
        _unitOfWork = unitOfWork;
    }

    public async Task<DynamicResult> ProcessDynamicAsync(
        Guid dynamicId, 
        Guid userId, 
        Dictionary<string, string> inputs, 
        CancellationToken cancellationToken)
    {
        // 1. Fetch challenge details to identify the type of challenge
        var challenge = await _unitOfWork.DynamicChallenges.GetAsync(dynamicId, cancellationToken);
        if (challenge == null)
        {
            return new DynamicResult
            {
                Success = false,
                Message = $"Challenge with ID {dynamicId} not found."
            };
        }

        // 2. Resolve strategy by Type
        var strategyType = challenge.Type.ToString();
        var strategy = _strategies.FirstOrDefault(s => string.Equals(s.DynamicType, strategyType, StringComparison.OrdinalIgnoreCase));

        if (strategy == null)
        {
            return new DynamicResult
            {
                Success = false,
                Message = $"No execution strategy registered for challenge type: {strategyType}."
            };
        }

        // 3. Create context and execute strategy
        var context = new DynamicContext
        {
            DynamicId = dynamicId,
            UserId = userId,
            Inputs = inputs
        };

        return await strategy.ExecuteAsync(context, cancellationToken);
    }
}
