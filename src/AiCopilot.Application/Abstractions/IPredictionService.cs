using AiCopilot.Shared.Models;

namespace AiCopilot.Application.Abstractions;

public interface IPredictionService
{
    Task<PredictionResult> PredictAsync(PredictionRequest request, CancellationToken cancellationToken = default);
}
