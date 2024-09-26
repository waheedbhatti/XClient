using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XClient.Errors;

namespace XClient.Logging
{
    public static class LoggerExtensions
    {

        public static void LogResult<TLogger, TResult>(this ILogger<TLogger> logger, Result<TResult> result, string actionDescription)
        {
            string? operationId = Activity.Current?.Id;
            if (result.Success)
            {
                logger.LogInformation("{action} succeeded", actionDescription);
            }
            else
            {
                if (result.Error is NotFoundError or UnauthorizedError or ForbiddenError)
                {
                    logger.LogWarning("{action} failed. {errorMessage}", actionDescription, result.Error.Message);
                }
                else if (result.Error is ValidationError validationError)
                {
                    string validationErrorMessages = string.Join(". ", validationError.Errors.Select(x => $"{x.Key}: {string.Join(", ", x.Value)}"));
                    logger.LogWarning("{action} failed. Validation errors: {validationErrors}", actionDescription, validationErrorMessages);
                }
                else
                {
                    if (result.Error.LogLevel != ErrorLogLevel.Default)
                    {
                        if (result.Error.LogLevel == ErrorLogLevel.Information)
                            logger.LogInformation("{action}. {message}", actionDescription, result.Error.Message);
                        else if (result.Error.LogLevel == ErrorLogLevel.Warning)
                            logger.LogWarning("{action}. {message}", actionDescription, result.Error.Message);
                        else if (result.Error.LogLevel == ErrorLogLevel.Error)
                            logger.LogError("{action}. {message}", actionDescription, result.Error.Message);
                        else if (result.Error.LogLevel == ErrorLogLevel.Critical)
                            logger.LogCritical("{action}. {message}", actionDescription, result.Error.Message);
                    }
                    else
                    {
                        logger.LogError("{action} failed. Error: {errorMessage}", actionDescription, result.Error.Message);
                    }
                }
            }
        }

    }
}
